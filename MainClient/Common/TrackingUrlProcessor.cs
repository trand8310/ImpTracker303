using MainClient.Common;
using Newtonsoft.Json.Linq;
using System;

namespace MainClient.Common
{
    internal sealed class TrackingUrlProcessor
    {
        public delegate string TrackingFormatter(string url, string ip, string userAgent, JToken param, OSType os, JObject ipInfo, JToken dev = null);

        private readonly TrackingFormatter _defaultFormatter;
        private readonly TrackingFormatter _gridSumFormatter;
        private readonly TrackingFormatter _ipinyouFormatter;
        private readonly TrackingFormatter _mafengwoFormatter;

        public TrackingUrlProcessor(
            TrackingFormatter defaultFormatter,
            TrackingFormatter gridSumFormatter,
            TrackingFormatter ipinyouFormatter,
            TrackingFormatter mafengwoFormatter)
        {
            _defaultFormatter = defaultFormatter;
            _gridSumFormatter = gridSumFormatter;
            _ipinyouFormatter = ipinyouFormatter;
            _mafengwoFormatter = mafengwoFormatter;
        }

        public string Format(string sourceUrl, string ip, string userAgent, JObject task, OSType os, JToken dev)
        {
            if (string.IsNullOrWhiteSpace(sourceUrl))
            {
                return string.Empty;
            }

            var domain = new Uri(sourceUrl).Host;
            if (domain.Contains("gridsumdissector.com"))
            {
                return _gridSumFormatter(sourceUrl, ip, userAgent, task, os, null, dev);
            }
            if (domain.Contains("ipinyou.com"))
            {
                return _ipinyouFormatter(sourceUrl, ip, userAgent, task, os, null, dev);
            }
            if (domain.Contains("mafengwo.cn"))
            {
                return _mafengwoFormatter(sourceUrl, ip, userAgent, task, os, null, dev);
            }

            return _defaultFormatter(sourceUrl, ip, userAgent, task, os, null, dev);
        }
    }
}
