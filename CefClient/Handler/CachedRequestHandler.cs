namespace CefClient.Handler
{
    using CefSharp;
    using CefSharp.Handler;
    using System;

    public sealed class CachedRequestHandler : RequestHandler
    {
        private readonly ResourceCacheManager _cacheManager;
        private static bool IsAllowedNavigation(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return true;

            return string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                || string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
                || string.Equals(uri.Scheme, "ws", StringComparison.OrdinalIgnoreCase)
                || string.Equals(uri.Scheme, "wss", StringComparison.OrdinalIgnoreCase)
                || string.Equals(uri.Scheme, "chrome", StringComparison.OrdinalIgnoreCase)
                || string.Equals(uri.Scheme, "about", StringComparison.OrdinalIgnoreCase);
        }

        public CachedRequestHandler(ResourceCacheOptions options)
        {
            _cacheManager = new ResourceCacheManager(options);
        }

        public bool WaitForPendingWrites(int millisecondsTimeout)
        {
            return _cacheManager.WaitForPendingWrites(millisecondsTimeout);
        }

        protected override bool OnBeforeBrowse(
            IWebBrowser chromiumWebBrowser,
            IBrowser browser,
            IFrame frame,
            IRequest request,
            bool userGesture,
            bool isRedirect)
        {
            var url = request.Url ?? string.Empty;
            if (IsAllowedNavigation(url))
                return false;
            return true;
        }

        protected override IResourceRequestHandler GetResourceRequestHandler(
            IWebBrowser chromiumWebBrowser,
            IBrowser browser,
            IFrame frame,
            IRequest request,
            bool isNavigation,
            bool isDownload,
            string requestInitiator,
            ref bool disableDefaultHandling)
        {
            if (isNavigation || isDownload)
                return null;

            if (request == null || string.IsNullOrWhiteSpace(request.Url))
                return null;

            if (!_cacheManager.ShouldCache(request))
                return null;

            return new CachedResourceRequestHandler(_cacheManager);
        }
    }
}
