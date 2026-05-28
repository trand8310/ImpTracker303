namespace CefClient.Handler
{
    using CefSharp;
    using CefSharp.Handler;
    using System.Collections.Concurrent;
    using System.IO;


    public sealed class CachedResourceRequestHandler : ResourceRequestHandler
    {
        private readonly ResourceCacheManager _cacheManager;

        private static readonly ConcurrentDictionary<ulong, CaptureResponseFilter> ActiveFilters =
            new ConcurrentDictionary<ulong, CaptureResponseFilter>();

        public CachedResourceRequestHandler(ResourceCacheManager cacheManager)
        {
            _cacheManager = cacheManager;
        }

        protected override IResourceHandler GetResourceHandler(
            IWebBrowser chromiumWebBrowser,
            IBrowser browser,
            IFrame frame,
            IRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Url))
                return null;

            if (_cacheManager.IsRangeRequest(request))
                return null;

            byte[] hotBytes;
            string hotMimeType;
            if (_cacheManager.TryGetHotCache(request, out hotBytes, out hotMimeType))
            {
                var hotHandler = ResourceHandler.FromByteArray(hotBytes, string.IsNullOrWhiteSpace(hotMimeType) ? "application/octet-stream" : hotMimeType);
                hotHandler.StatusCode = 200;
                hotHandler.StatusText = "OK";
                if (hotHandler.Headers != null)
                {
                    hotHandler.Headers["X-CefSharp-Resource-Cache"] = "HOT-HIT";
                    hotHandler.Headers["Cache-Control"] = "public, max-age=31536000";
                }
                return hotHandler;
            }

            var cacheItem = _cacheManager.TryGetCache(request);

            if (cacheItem == null)
                return null;

            try
            {
                var stream = new FileStream(
                    cacheItem.FilePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete,
                    1024 * 128,
                    false);

                var handler = ResourceHandler.FromStream(
                    stream,
                    cacheItem.MimeType,
                    true);

                handler.StatusCode = 200;
                handler.StatusText = "OK";

                if (handler.Headers != null)
                {
                    handler.Headers["X-CefSharp-Resource-Cache"] = "HIT";
                    handler.Headers["Cache-Control"] = "public, max-age=31536000";
                }

                return handler;
            }
            catch
            {
                return null;
            }
        }

        protected override IResponseFilter GetResourceResponseFilter(
            IWebBrowser chromiumWebBrowser,
            IBrowser browser,
            IFrame frame,
            IRequest request,
            IResponse response)
        {
            if (request == null || response == null)
                return null;

            if (!_cacheManager.ShouldCaptureResponse(request, response))
                return null;

            var filter = new CaptureResponseFilter(
                request.Identifier,
                request.Url,
                _cacheManager.Options.MaxMemoryCaptureBytes);

            ActiveFilters[request.Identifier] = filter;

            return filter;
        }

        protected override void OnResourceLoadComplete(
            IWebBrowser chromiumWebBrowser,
            IBrowser browser,
            IFrame frame,
            IRequest request,
            IResponse response,
            UrlRequestStatus status,
            long receivedContentLength)
        {
            if (request == null || response == null)
                return;

            CaptureResponseFilter filter;

            if (!ActiveFilters.TryRemove(request.Identifier, out filter))
                return;

            try
            {
                if (status != UrlRequestStatus.Success)
                    return;

                if (!_cacheManager.ShouldSaveResponse(request, response, filter.Length))
                    return;

                var bytes = filter.ToArray();

                if (bytes == null || bytes.Length == 0)
                    return;

                _cacheManager.SaveAsync(request, response, bytes);
            }
            finally
            {
                filter.Dispose();
            }
        }
    }
}
