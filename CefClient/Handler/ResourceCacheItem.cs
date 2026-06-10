

namespace CefClient.Handler
{
    using System;

    public sealed class ResourceCacheItem
    {
        public string Url { get; set; }

        public string FilePath { get; set; }

        public string MimeType { get; set; }

        public string Extension { get; set; }

        public long Length { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime LastHitAt { get; set; }
    }
}
