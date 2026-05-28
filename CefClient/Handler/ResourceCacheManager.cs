

namespace CefClient.Handler
{
    using CefSharp;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public sealed class ResourceCacheManager
    {
        private sealed class RequestSnapshot
        {
            public string Url { get; set; }
            public string Method { get; set; }
            public string ResourceType { get; set; }
            public bool IsRangeRequest { get; set; }
        }

        private sealed class ResponseSnapshot
        {
            public int StatusCode { get; set; }
            public string MimeType { get; set; }
            public NameValueCollection Headers { get; set; }
        }

        private readonly ConcurrentDictionary<string, ResourceCacheItem> _memoryIndex;
        private readonly ConcurrentDictionary<string, HotCacheEntry> _hotCache;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _keyLocks;
        private int _pendingWriteCount;
        private readonly ManualResetEventSlim _pendingWriteCompleted = new ManualResetEventSlim(true);
        private const int HotCacheMaxEntries = 256;
        private const long HotCacheMaxBytes = 64L * 1024 * 1024;
        private long _hotCacheBytes;

        private sealed class HotCacheEntry
        {
            public byte[] Bytes { get; set; }
            public string MimeType { get; set; }
            public DateTime LastHitAt { get; set; }
            public int HitCount { get; set; }
        }

        private static readonly HashSet<string> ImageExts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".svg", ".ico"
        };

        private static readonly HashSet<string> ScriptExts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".js", ".mjs"
        };

        private static readonly HashSet<string> CssExts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".css"
        };

        private static readonly HashSet<string> FontExts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".woff", ".woff2", ".ttf", ".otf", ".eot"
        };

        private static readonly HashSet<string> VideoExts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".mp4", ".webm", ".m4v", ".mov", ".m3u8", ".ts"
        };

        private static readonly HashSet<string> TrackingQueryKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "m", "sid", "ot", "ac", "_aid", "_refer", "_source", "ver"
        };

        public ResourceCacheOptions Options { get; private set; }

        public ResourceCacheManager(ResourceCacheOptions options)
        {
            Options = options ?? new ResourceCacheOptions();

            if (string.IsNullOrWhiteSpace(Options.CacheRoot))
            {
                Options.CacheRoot = "cef_resource_cache";
            }

            _memoryIndex = new ConcurrentDictionary<string, ResourceCacheItem>();
            _hotCache = new ConcurrentDictionary<string, HotCacheEntry>();
            _keyLocks = new ConcurrentDictionary<string, SemaphoreSlim>();

            Directory.CreateDirectory(Options.CacheRoot);
        }

        public bool ShouldCache(IRequest request)
        {
            return ShouldCache(CreateRequestSnapshot(request));
        }

        private bool ShouldCache(RequestSnapshot request)
        {
            if (request == null)
                return false;

            if (!string.Equals(request.Method, "GET", StringComparison.OrdinalIgnoreCase))
                return false;

            if (string.IsNullOrWhiteSpace(request.Url))
                return false;

            Uri uri;

            if (!Uri.TryCreate(request.Url, UriKind.Absolute, out uri))
                return false;

            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                return false;

            if (IsLikelyTrackingRequest(request.Url, request.ResourceType))
                return false;

            if (request.IsRangeRequest)
                return false;

            var ext = GetExtensionFromUrl(request.Url);

            if (Options.EnableImageCache && ImageExts.Contains(ext))
                return true;

            if (Options.EnableScriptCache && ScriptExts.Contains(ext))
                return true;

            if (Options.EnableCssCache && CssExts.Contains(ext))
                return true;

            if (Options.EnableFontCache && FontExts.Contains(ext))
                return true;

            if (Options.EnableVideoCache && VideoExts.Contains(ext))
                return true;

            // 不同 CefSharp 版本 ResourceType 枚举名称可能略有差别。
            // 这里尽量用 ToString 兼容。
            var resourceType = request.ResourceType;

            if (Options.EnableImageCache &&
                string.Equals(resourceType, "Image", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (Options.EnableScriptCache &&
                string.Equals(resourceType, "Script", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (Options.EnableCssCache &&
                string.Equals(resourceType, "Stylesheet", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (Options.EnableFontCache &&
                (string.Equals(resourceType, "FontResource", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(resourceType, "Font", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            if (Options.EnableVideoCache &&
                (string.Equals(resourceType, "Media", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(resourceType, "MediaResource", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            return false;
        }

        private static bool IsLikelyTrackingRequest(string url, string resourceType)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
                return false;

            var path = uri.AbsolutePath ?? string.Empty;
            var ext = Path.GetExtension(path);
            if (!string.Equals(ext, ".gif", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(resourceType, "Image", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var query = uri.Query;
            if (string.IsNullOrWhiteSpace(query) || query.Length <= 1)
                return false;

            var queryText = query[0] == '?' ? query.Substring(1) : query;
            var pairs = queryText.Split('&');
            for (int i = 0; i < pairs.Length; i++)
            {
                var part = pairs[i];
                if (string.IsNullOrWhiteSpace(part))
                    continue;

                var idx = part.IndexOf('=');
                var rawKey = idx >= 0 ? part.Substring(0, idx) : part;
                var key = Uri.UnescapeDataString(rawKey ?? string.Empty);

                if (TrackingQueryKeys.Contains(key))
                    return true;
            }

            return false;
        }

        public bool ShouldCaptureResponse(IRequest request, IResponse response)
        {
            return ShouldCaptureResponse(CreateRequestSnapshot(request), CreateResponseSnapshot(response));
        }

        private bool ShouldCaptureResponse(RequestSnapshot request, ResponseSnapshot response)
        {
            if (!ShouldCache(request))
                return false;

            if (response == null)
                return false;

            if (response.StatusCode < 200 || response.StatusCode >= 300)
                return false;

            // 206 是 Range 分片，不要当完整文件缓存
            if (response.StatusCode == 206)
                return false;

            var headers = response.Headers;

            if (headers != null)
            {
                var cacheControl = headers["Cache-Control"];

                if (!string.IsNullOrWhiteSpace(cacheControl) &&
                    cacheControl.IndexOf("no-store", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return false;
                }

                var contentLengthText = headers["Content-Length"];
                long contentLength;

                if (long.TryParse(contentLengthText, out contentLength))
                {
                    if (contentLength <= 0)
                        return false;

                    if (contentLength > Options.MaxMemoryCaptureBytes)
                        return false;
                }
            }

            return true;
        }

        public bool ShouldSaveResponse(IRequest request, IResponse response, long bodyLength)
        {
            var requestSnapshot = CreateRequestSnapshot(request);
            var responseSnapshot = CreateResponseSnapshot(response);

            if (!ShouldCaptureResponse(requestSnapshot, responseSnapshot))
                return false;

            if (bodyLength <= 0)
                return false;

            if (bodyLength > Options.MaxMemoryCaptureBytes)
                return false;

            return true;
        }

        public ResourceCacheItem TryGetCache(IRequest request)
        {
            if (!ShouldCache(request))
                return null;

            return TryGetCacheByUrl(request.Url);
        }

        public bool TryGetHotCache(IRequest request, out byte[] bytes, out string mimeType)
        {
            bytes = null;
            mimeType = null;

            if (request == null || string.IsNullOrWhiteSpace(request.Url))
                return false;

            var key = BuildCacheKey(request.Url);
            HotCacheEntry entry;
            if (!_hotCache.TryGetValue(key, out entry) || entry == null || entry.Bytes == null || entry.Bytes.Length == 0)
                return false;

            entry.LastHitAt = DateTime.Now;
            entry.HitCount++;
            bytes = entry.Bytes;
            mimeType = entry.MimeType;
            return true;
        }

        public void SaveAsync(IRequest request, IResponse response, byte[] bytes)
        {
            if (request == null || response == null || bytes == null || bytes.Length == 0)
                return;

            var requestSnapshot = CreateRequestSnapshot(request);
            var responseSnapshot = CreateResponseSnapshot(response);

            if (!ShouldSaveResponse(requestSnapshot, responseSnapshot, bytes.Length))
                return;

            var url = requestSnapshot.Url;
            var mimeTypeFromResponse = responseSnapshot.MimeType;

            if (string.IsNullOrWhiteSpace(url))
                return;

            var key = BuildCacheKey(url);
            System.Threading.Interlocked.Increment(ref _pendingWriteCount);
            _pendingWriteCompleted.Reset();

            Task.Run(delegate
            {
                var sem = _keyLocks.GetOrAdd(key, delegate { return new SemaphoreSlim(1, 1); });

                sem.Wait();

                try
                {
                    var old = TryGetCacheByUrl(url);

                    if (old != null)
                        return;

                    var ext = GetExtensionFromUrl(url);
                    var mimeType = mimeTypeFromResponse;

                    if (string.IsNullOrWhiteSpace(ext))
                    {
                        ext = GetExtensionFromMimeType(mimeType);
                    }

                    if (string.IsNullOrWhiteSpace(ext))
                    {
                        ext = ".bin";
                    }

                    var dir = GetCacheDir(key);
                    Directory.CreateDirectory(dir);

                    var filePath = Path.Combine(dir, key + ext);
                    var tempPath = filePath + ".tmp";
                    var metaPath = GetMetaPath(key);

                    File.WriteAllBytes(tempPath, bytes);

                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }

                    File.Move(tempPath, filePath);

                    var item = new ResourceCacheItem
                    {
                        Url = url,
                        FilePath = filePath,
                        MimeType = string.IsNullOrWhiteSpace(mimeType) ? GetMimeTypeByExt(ext) : mimeType,
                        Extension = ext,
                        Length = bytes.Length,
                        CreatedAt = DateTime.Now,
                        LastHitAt = DateTime.Now
                    };

                    var json = JsonConvert.SerializeObject(item);
                    File.WriteAllText(metaPath, json, Encoding.UTF8);

                    _memoryIndex[key] = item;
                    AddOrUpdateHotCache(key, bytes, item.MimeType);
                }
                catch
                {
                    // 这里接你的日志
                }
                finally
                {
                    sem.Release();

                    SemaphoreSlim removed;
                    _keyLocks.TryRemove(key, out removed);

                    if (System.Threading.Interlocked.Decrement(ref _pendingWriteCount) == 0)
                    {
                        _pendingWriteCompleted.Set();
                    }
                }
            });
        }

        private void AddOrUpdateHotCache(string key, byte[] bytes, string mimeType)
        {
            if (string.IsNullOrWhiteSpace(key) || bytes == null || bytes.Length == 0)
                return;

            if (bytes.Length > 2 * 1024 * 1024)
                return;

            HotCacheEntry old;
            if (_hotCache.TryGetValue(key, out old) && old?.Bytes != null)
            {
                Interlocked.Add(ref _hotCacheBytes, -old.Bytes.Length);
            }

            var entry = new HotCacheEntry
            {
                Bytes = bytes,
                MimeType = mimeType,
                LastHitAt = DateTime.Now,
                HitCount = old == null ? 1 : old.HitCount + 1
            };

            _hotCache[key] = entry;
            Interlocked.Add(ref _hotCacheBytes, bytes.Length);
            TrimHotCacheIfNeeded();
        }

        private void TrimHotCacheIfNeeded()
        {
            while (_hotCache.Count > HotCacheMaxEntries || Interlocked.Read(ref _hotCacheBytes) > HotCacheMaxBytes)
            {
                string removeKey = null;
                HotCacheEntry removeEntry = null;

                foreach (var pair in _hotCache)
                {
                    var e = pair.Value;
                    if (e == null)
                        continue;

                    if (removeEntry == null || e.HitCount < removeEntry.HitCount ||
                        (e.HitCount == removeEntry.HitCount && e.LastHitAt < removeEntry.LastHitAt))
                    {
                        removeKey = pair.Key;
                        removeEntry = e;
                    }
                }

                if (removeKey == null)
                    break;

                HotCacheEntry removed;
                if (_hotCache.TryRemove(removeKey, out removed) && removed?.Bytes != null)
                {
                    Interlocked.Add(ref _hotCacheBytes, -removed.Bytes.Length);
                }
                else
                {
                    break;
                }
            }
        }

        public bool WaitForPendingWrites(int millisecondsTimeout)
        {
            if (millisecondsTimeout < 0)
                millisecondsTimeout = 0;

            if (System.Threading.Volatile.Read(ref _pendingWriteCount) == 0)
                return true;

            return _pendingWriteCompleted.Wait(millisecondsTimeout);
        }

        private bool ShouldSaveResponse(RequestSnapshot request, ResponseSnapshot response, long bodyLength)
        {
            if (!ShouldCaptureResponse(request, response))
                return false;

            if (bodyLength <= 0)
                return false;

            if (bodyLength > Options.MaxMemoryCaptureBytes)
                return false;

            return true;
        }

        private RequestSnapshot CreateRequestSnapshot(IRequest request)
        {
            if (request == null)
                return null;

            try
            {
                return new RequestSnapshot
                {
                    Url = request.Url,
                    Method = request.Method,
                    ResourceType = request.ResourceType.ToString(),
                    IsRangeRequest = IsRangeRequest(request)
                };
            }
            catch
            {
                return null;
            }
        }

        private ResponseSnapshot CreateResponseSnapshot(IResponse response)
        {
            if (response == null)
                return null;

            try
            {
                return new ResponseSnapshot
                {
                    StatusCode = response.StatusCode,
                    MimeType = response.MimeType,
                    Headers = CloneHeaders(response.Headers)
                };
            }
            catch
            {
                return null;
            }
        }

        private static NameValueCollection CloneHeaders(NameValueCollection headers)
        {
            if (headers == null)
                return null;

            var copy = new NameValueCollection(headers.Count, StringComparer.OrdinalIgnoreCase);

            foreach (string key in headers.AllKeys)
            {
                copy[key] = headers[key];
            }

            return copy;
        }

        private ResourceCacheItem TryGetCacheByUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return null;

            var key = BuildCacheKey(url);

            ResourceCacheItem item;

            if (_memoryIndex.TryGetValue(key, out item))
            {
                if (IsCacheItemValid(item))
                {
                    item.LastHitAt = DateTime.Now;
                    return item;
                }

                ResourceCacheItem removed;
                _memoryIndex.TryRemove(key, out removed);
            }

            var metaPath = GetMetaPath(key);

            if (!File.Exists(metaPath))
                return null;

            try
            {
                var json = File.ReadAllText(metaPath, Encoding.UTF8);
                var diskItem = JsonConvert.DeserializeObject<ResourceCacheItem>(json);

                if (diskItem == null || !IsCacheItemValid(diskItem))
                    return null;

                diskItem.LastHitAt = DateTime.Now;
                _memoryIndex[key] = diskItem;

                return diskItem;
            }
            catch
            {
                return null;
            }
        }

        public bool IsRangeRequest(IRequest request)
        {
            try
            {
                if (request == null)
                    return false;

                if (request.IsDisposed)
                    return false;

                var headers = request.Headers;

                if (headers == null)
                    return false;

                var range = headers["Range"];

                return !string.IsNullOrWhiteSpace(range);
            }
            catch
            {
                return false;
            }
        }

        private bool IsCacheItemValid(ResourceCacheItem item)
        {
            if (item == null)
                return false;

            if (string.IsNullOrWhiteSpace(item.Url))
                return false;

            if (string.IsNullOrWhiteSpace(item.FilePath))
                return false;

            if (!File.Exists(item.FilePath))
                return false;

            if (Options.CacheExpireDays > 0)
            {
                if (item.CreatedAt.AddDays(Options.CacheExpireDays) < DateTime.Now)
                {
                    TryDelete(item.FilePath);
                    TryDelete(GetMetaPath(BuildCacheKey(item.Url)));
                    return false;
                }
            }

            return true;
        }

        private string GetCacheDir(string key)
        {
            var d1 = key.Substring(0, 2);
            var d2 = key.Substring(2, 2);

            return Path.Combine(Options.CacheRoot, d1, d2);
        }

        private string GetMetaPath(string key)
        {
            return Path.Combine(GetCacheDir(key), key + ".json");
        }

        private static string BuildCacheKey(string url)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(url));
                return BytesToHex(bytes);
            }
        }

        private static string BytesToHex(byte[] bytes)
        {
            var sb = new StringBuilder(bytes.Length * 2);

            for (int i = 0; i < bytes.Length; i++)
            {
                sb.Append(bytes[i].ToString("x2"));
            }

            return sb.ToString();
        }

        private static string GetExtensionFromUrl(string url)
        {
            try
            {
                var uri = new Uri(url);
                var path = uri.AbsolutePath;
                var ext = Path.GetExtension(path);

                if (string.IsNullOrWhiteSpace(ext))
                    return "";

                return ext.ToLowerInvariant();
            }
            catch
            {
                return "";
            }
        }

        private static string GetExtensionFromMimeType(string mimeType)
        {
            if (string.IsNullOrWhiteSpace(mimeType))
                return ".bin";

            mimeType = mimeType.ToLowerInvariant();

            if (mimeType == "image/jpeg") return ".jpg";
            if (mimeType == "image/png") return ".png";
            if (mimeType == "image/gif") return ".gif";
            if (mimeType == "image/webp") return ".webp";
            if (mimeType == "image/svg+xml") return ".svg";

            if (mimeType == "text/javascript") return ".js";
            if (mimeType == "application/javascript") return ".js";
            if (mimeType == "application/x-javascript") return ".js";

            if (mimeType == "text/css") return ".css";

            if (mimeType == "font/woff") return ".woff";
            if (mimeType == "font/woff2") return ".woff2";
            if (mimeType == "application/font-woff") return ".woff";
            if (mimeType == "application/font-woff2") return ".woff2";
            if (mimeType == "application/vnd.ms-fontobject") return ".eot";
            if (mimeType == "font/ttf") return ".ttf";
            if (mimeType == "font/otf") return ".otf";

            if (mimeType == "video/mp4") return ".mp4";
            if (mimeType == "video/webm") return ".webm";
            if (mimeType == "application/vnd.apple.mpegurl") return ".m3u8";
            if (mimeType == "application/x-mpegurl") return ".m3u8";
            if (mimeType == "video/mp2t") return ".ts";

            return ".bin";
        }

        private static string GetMimeTypeByExt(string ext)
        {
            if (string.IsNullOrWhiteSpace(ext))
                return "application/octet-stream";

            ext = ext.ToLowerInvariant();

            if (ext == ".jpg" || ext == ".jpeg") return "image/jpeg";
            if (ext == ".png") return "image/png";
            if (ext == ".gif") return "image/gif";
            if (ext == ".webp") return "image/webp";
            if (ext == ".bmp") return "image/bmp";
            if (ext == ".svg") return "image/svg+xml";
            if (ext == ".ico") return "image/x-icon";

            if (ext == ".js" || ext == ".mjs") return "application/javascript";
            if (ext == ".css") return "text/css";

            if (ext == ".woff") return "font/woff";
            if (ext == ".woff2") return "font/woff2";
            if (ext == ".ttf") return "font/ttf";
            if (ext == ".otf") return "font/otf";
            if (ext == ".eot") return "application/vnd.ms-fontobject";

            if (ext == ".mp4") return "video/mp4";
            if (ext == ".webm") return "video/webm";
            if (ext == ".m4v") return "video/x-m4v";
            if (ext == ".mov") return "video/quicktime";
            if (ext == ".m3u8") return "application/vnd.apple.mpegurl";
            if (ext == ".ts") return "video/mp2t";

            return "application/octet-stream";
        }

        private static void TryDelete(string file)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(file) && File.Exists(file))
                {
                    File.Delete(file);
                }
            }
            catch
            {
            }
        }
    }
}
