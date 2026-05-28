using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;


namespace MainClient.ProxyChecker
{
    public class ProxyTester
    {
        private readonly List<string> _testUrls;
        private readonly TimeSpan _timeout;

        public ProxyTester(IEnumerable<string> testUrls = null, int timeoutSeconds = 15)
        {
            if (testUrls == null || !testUrls.Any())
            {
                _testUrls = new List<string>
                {
                    "http://211.154.24.179:9000/api/dash/ipinfo.php",
                    "http://117.21.200.18:9000/api/dash/ipinfo.php",
                    "http://117.21.200.221/api/dash/ipinfo.php",
                    "http://ip-api.com/json/?lang=zh-CN",
                    "https://ipinfo.io/json"
                };
            }
            else
            {
                _testUrls = testUrls.ToList();
            }

            _timeout = TimeSpan.FromSeconds(timeoutSeconds);
        }

        public async Task<ProxyTestResult> TestAsync(string proxyAddress = null)
        {
            using (var cts = new CancellationTokenSource(_timeout))
            {
                var tasks = _testUrls
                    .Select(url => TryRequestAsync(url, proxyAddress, cts.Token))
                    .ToList();

                while (tasks.Count > 0)
                {
                    var finished = await Task.WhenAny(tasks).ConfigureAwait(false);
                    tasks.Remove(finished);

                    var result = await finished.ConfigureAwait(false);
                    if (result.IsValid)
                    {
                        try
                        {
                            cts.Cancel();
                        }
                        catch
                        {
                        }

                        return result;
                    }
                }

                return new ProxyTestResult
                {
                    Proxy = proxyAddress ?? "",
                    IsValid = false,
                    ErrorMessage = "全部测试站点请求失败"
                };
            }
        }

        public async Task<List<ProxyTestResult>> TestManyAsync(IEnumerable<string> proxies, int maxDegreeOfParallelism = 10)
        {
            var results = new List<ProxyTestResult>();

            if (proxies == null)
            {
                return results;
            }

            using (var throttler = new SemaphoreSlim(maxDegreeOfParallelism))
            {
                var tasks = proxies.Select(async proxy =>
                {
                    await throttler.WaitAsync().ConfigureAwait(false);

                    try
                    {
                        var result = await TestAsync(proxy).ConfigureAwait(false);

                        lock (results)
                        {
                            results.Add(result);
                        }
                    }
                    finally
                    {
                        throttler.Release();
                    }
                }).ToList();

                await Task.WhenAll(tasks).ConfigureAwait(false);
            }

            return results;
        }

        private async Task<ProxyTestResult> TryRequestAsync(string url, string proxyAddress, CancellationToken cancellationToken)
        {
            var result = new ProxyTestResult
            {
                Proxy = proxyAddress ?? "",
                SuccessUrl = url
            };

            var sw = Stopwatch.StartNew();

            try
            {
                var handler = new HttpClientHandler
                {
                    UseCookies = false
                };

                if (!string.IsNullOrWhiteSpace(proxyAddress))
                {
                    handler.Proxy = new WebProxy(proxyAddress);
                    handler.UseProxy = true;
                }
                else
                {
                    handler.Proxy = null;
                    handler.UseProxy = false;
                }

                using (var client = new HttpClient(handler))
                {
                    client.Timeout = Timeout.InfiniteTimeSpan;

                    var response = await client.GetAsync(url, cancellationToken).ConfigureAwait(false);

                    // .NET Framework 4.6.2 的 ReadAsStringAsync 没有 CancellationToken 参数
                    result.Data = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    sw.Stop();

                    result.ElapsedMilliseconds = sw.ElapsedMilliseconds;
                    result.StatusCode = response.StatusCode;
                    result.IsValid = response.IsSuccessStatusCode;

                    if (!result.IsValid)
                    {
                        result.ErrorMessage = string.Format(
                            "HTTP {0} {1}",
                            (int)response.StatusCode,
                            response.ReasonPhrase
                        );
                    }
                }
            }
            catch (OperationCanceledException)
            {
                sw.Stop();

                result.IsValid = false;
                result.ElapsedMilliseconds = sw.ElapsedMilliseconds;
                result.ErrorMessage = "请求已取消或超时";
            }
            catch (Exception ex)
            {
                sw.Stop();

                result.IsValid = false;
                result.ElapsedMilliseconds = sw.ElapsedMilliseconds;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }
    }

    public class ProxyTestResult
    {
        public string Proxy { get; set; }
        public bool IsValid { get; set; }
        public string SuccessUrl { get; set; }
        public long ElapsedMilliseconds { get; set; }
        public HttpStatusCode? StatusCode { get; set; }
        public string ErrorMessage { get; set; }
        public string Data { get; set; }

        public ProxyTestResult()
        {
            Proxy = "";
            SuccessUrl = "";
            ErrorMessage = "";
            Data = "";
        }
    }
}