

namespace CefClient.Handler
{
    public sealed class ResourceCacheOptions
    {
        public string CacheRoot { get; set; }

        public long MaxMemoryCaptureBytes { get; set; }

        public bool EnableImageCache { get; set; }

        public bool EnableScriptCache { get; set; }

        public bool EnableCssCache { get; set; }

        public bool EnableFontCache { get; set; }

        public bool EnableVideoCache { get; set; }

        public int CacheExpireDays { get; set; }

        public ResourceCacheOptions()
        {
            CacheRoot = "cef_resource_cache";
            MaxMemoryCaptureBytes = 20 * 1024 * 1024;
            EnableImageCache = true;
            EnableScriptCache = true;
            EnableCssCache = true;
            EnableFontCache = true;
            EnableVideoCache = false;
            CacheExpireDays = 7;
        }
    }
}
