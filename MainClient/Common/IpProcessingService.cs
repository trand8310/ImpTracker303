using MainClient.ProxyChecker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MainClient.Common
{
    internal sealed class IpContext
    {
        public string ProxyServer { get; set; } = string.Empty;
        public string RealIp { get; set; } = string.Empty;
        public string Ip { get; set; } = string.Empty;
        public string TaskProvince { get; set; } = string.Empty;
        public string TaskCity { get; set; } = string.Empty;
        public string Isp { get; set; } = string.Empty;
    }

    internal sealed class ProxyParseResult
    {
        public bool Success { get; set; }
        public IpContext Context { get; set; } = new IpContext();
        public string ErrorMessage { get; set; } = string.Empty;

        public static ProxyParseResult Ok(IpContext context)
        {
            return new ProxyParseResult { Success = true, Context = context };
        }

        public static ProxyParseResult Fail(string errorMessage)
        {
            return new ProxyParseResult { Success = false, ErrorMessage = errorMessage };
        }
    }

    internal sealed class IpAreaParseResult
    {
        public bool Success { get; set; }
        public string Region { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string RealIp { get; set; } = string.Empty;
        public string Isp { get; set; } = string.Empty;

        public static IpAreaParseResult Fail()
        {
            return new IpAreaParseResult { Success = false };
        }
    }

    internal sealed class IpProcessingService
    {
        private readonly AppSetting _setting;
        private readonly ILogger _logger;
        private readonly ProxyTester _ipTester;
        private readonly Func<JObject, int, Task<string>> _getIps;
        private readonly Func<string, string, string> _getIpAreaByLocal;
        private readonly Func<bool> _isRestarting;
        private readonly Action<string> _log;

        public IpProcessingService(
            AppSetting setting,
            ILogger logger,
            ProxyTester ipTester,
            Func<JObject, int, Task<string>> getIps,
            Func<string, string, string> getIpAreaByLocal,
            Func<bool> isRestarting,
            Action<string> log)
        {
            _setting = setting;
            _logger = logger;
            _ipTester = ipTester;
            _getIps = getIps;
            _getIpAreaByLocal = getIpAreaByLocal;
            _isRestarting = isRestarting;
            _log = log;
        }

        public async Task<IpContext> ResolveIpContextAsync(JObject task, string title, CancellationToken token)
        {
            if (_setting.NoProxy)
            {
                return new IpContext();
            }

            while (!token.IsCancellationRequested && !_isRestarting())
            {
                var proxyJson = await _getIps(task, 1);
                if (IsInvalidProxyResponse(proxyJson))
                {
                    _log?.Invoke($"IP异常:{proxyJson}");
                    _logger?.LogError($"任务[{task["id"]}]:,地区:{task["address"]},IP异常{proxyJson}");
                    await Task.Delay(new Random().Next(50, 100), token);
                    continue;
                }

                var proxyParseResult = ParseProxyResponse(proxyJson);
                if (!proxyParseResult.Success)
                {
                    _log?.Invoke(proxyParseResult.ErrorMessage);
                    _logger?.LogError($"任务[{task["id"]}]:,地区:{task["address"]},IP异常{proxyJson}");
                    await Task.Delay(new Random().Next(50, 100), token);
                    continue;
                }
                var ipContext = proxyParseResult.Context;

                if (!await ValidateProxyAsync(ipContext, proxyJson, token))
                {
                    continue;
                }

                if (_setting.IPAreaCheck && !await ValidateIpAreaAsync(ipContext, proxyJson, token))
                {
                    continue;
                }

                _logger?.LogInformation($"任务[{task["id"]}]:{title},IP:{ipContext.RealIp},地区:{task["address"]}");
                return ipContext;
            }

            return null;
        }

        private static bool IsInvalidProxyResponse(string proxyJson)
        {
            return string.IsNullOrWhiteSpace(proxyJson)
                || !proxyJson.Contains(":")
                || proxyJson.Contains("频繁")
                || proxyJson.Contains("频率")
                || proxyJson.Contains("太快")
                || proxyJson.Contains("失败")
                || proxyJson.Contains("错误")
                || proxyJson.Contains("余额不足");
        }

        private ProxyParseResult ParseProxyResponse(string proxyJson)
        {
            var ipContext = new IpContext();

            try
            {
                if (proxyJson.Contains("serialNo") && proxyJson.Contains("realIp"))
                {
                    var jo = JObject.Parse(proxyJson);
                    var ipInfo = ResolveSerialProxyItem(jo);
                    if (ipInfo == null)
                    {
                        return ProxyParseResult.Fail("IP异常1");
                    }

                    ipContext.ProxyServer = $"{ipInfo.Value<string>("ip")?.Trim()}:{ipInfo.Value<string>("port")?.Trim()}";
                    ipContext.RealIp = ipInfo.Value<string>("realIp") ?? ipInfo.Value<string>("rip") ?? string.Empty;
                }
                else
                {
                    var ipData = JObject.Parse(proxyJson);
                    ipContext.TaskProvince = ipData.Value<string>("province")?.Trim() ?? string.Empty;
                    ipContext.TaskCity = ipData.Value<string>("city")?.Trim() ?? string.Empty;

                    if (proxyJson.Contains("data") && proxyJson.Contains("success") && proxyJson.Contains("province") && proxyJson.Contains("city"))
                    {
                        var dataToken = ipData["data"];
                        if (dataToken?.Type == JTokenType.String)
                        {
                            ipContext.ProxyServer = dataToken.Value<string>()?.Trim() ?? string.Empty;
                        }
                        else
                        {
                            var nestedData = dataToken as JObject ?? JObject.Parse(dataToken?.ToString() ?? "{}");
                            var ipItem = nestedData["data"]?.FirstOrDefault() as JObject;
                            if (ipItem == null)
                            {
                                return ProxyParseResult.Fail("IP异常1");
                            }

                            ipContext.ProxyServer = $"{ipItem.Value<string>("ip")?.Trim()}:{ipItem.Value<string>("port")?.Trim()}";
                            ipContext.RealIp = ipItem.Value<string>("rip") ?? string.Empty;
                        }
                    }
                    else
                    {
                        ipContext.ProxyServer = ipData["data"]?.ToString().Trim() ?? string.Empty;
                    }
                }

                if (!TrySetProxyIp(ipContext))
                {
                    return ProxyParseResult.Fail("IP异常");
                }

                return ProxyParseResult.Ok(ipContext);
            }
            catch (JsonException ex)
            {
                return ProxyParseResult.Fail("IP异常,JSON解析失败:" + ex.Message);
            }
            catch (Exception ex)
            {
                return ProxyParseResult.Fail("IP异常:" + ex.Message);
            }
        }

        private static JObject ResolveSerialProxyItem(JObject proxyData)
        {
            var dataToken = proxyData["data"];
            if (dataToken is JArray dataArray)
            {
                return dataArray.FirstOrDefault() as JObject;
            }

            var nestedData = dataToken as JObject ?? JObject.Parse(dataToken?.ToString() ?? "{}");
            return nestedData["data"]?.FirstOrDefault() as JObject;
        }

        private static bool TrySetProxyIp(IpContext ipContext)
        {
            const string pattern = @"(?:(?:[0,1]?\d?\d|2[0-4]\d|25[0-5])\.){3}(?:[0,1]?\d?\d|2[0-4]\d|25[0-5]):\d{0,5}";
            if (string.IsNullOrWhiteSpace(ipContext.ProxyServer) || !Regex.IsMatch(ipContext.ProxyServer, pattern))
            {
                return false;
            }

            ipContext.ProxyServer = ipContext.ProxyServer.Trim();
            ipContext.Ip = ipContext.ProxyServer.Substring(0, ipContext.ProxyServer.IndexOf(":"));
            return true;
        }

        private async Task<bool> ValidateProxyAsync(IpContext ipContext, string proxyJson, CancellationToken token)
        {
            if (!_setting.CheckIp && (!_setting.RealIp || !string.IsNullOrWhiteSpace(ipContext.RealIp)))
            {
                return true;
            }

            var result = await _ipTester.TestAsync(ipContext.ProxyServer);
            if (result.IsValid)
            {
                var ipJson = JObject.Parse(result.Data);
                if (ipJson.ContainsKey("query"))
                    ipContext.RealIp = ipJson["query"].Value<string>();
                if (ipJson.ContainsKey("ip"))
                    ipContext.RealIp = ipJson["ip"].Value<string>();
                return true;
            }

            _log?.Invoke("IP检测失败:" + proxyJson + $",{result.Data}");
            await Task.Delay(new Random().Next(100, 200), token);
            return false;
        }

        private async Task<bool> ValidateIpAreaAsync(IpContext ipContext, string proxyJson, CancellationToken token)
        {
            var areaJson = _getIpAreaByLocal(ipContext.Ip, ipContext.ProxyServer);
            _logger?.LogInformation($"IP检测:{areaJson}");
            if (string.IsNullOrWhiteSpace(areaJson))
            {
                _log?.Invoke($"IP异常,代理无效:{proxyJson}");
                await Task.Delay(new Random().Next(50, 100), token);
                return false;
            }

            var areaParseResult = ParseIpArea(areaJson);
            if (!areaParseResult.Success)
            {
                _log?.Invoke($"IP异常,地区数据无效:{areaJson}");
                await Task.Delay(new Random().Next(50, 100), token);
                return false;
            }

            if (!string.IsNullOrWhiteSpace(ipContext.TaskProvince) && (string.IsNullOrWhiteSpace(areaParseResult.Region) || !areaParseResult.Region.Contains(ipContext.TaskProvince)))
            {
                _log?.Invoke("IP异常,省份无效");
                await Task.Delay(new Random().Next(50, 100), token);
                return false;
            }

            if (!string.IsNullOrWhiteSpace(ipContext.TaskCity) && (string.IsNullOrWhiteSpace(areaParseResult.City) || !areaParseResult.City.Contains(ipContext.TaskCity)))
            {
                _log?.Invoke("IP异常,城市无效");
                await Task.Delay(new Random().Next(50, 100), token);
                return false;
            }

            if (!string.IsNullOrWhiteSpace(areaParseResult.RealIp))
            {
                ipContext.RealIp = areaParseResult.RealIp;
            }
            ipContext.Isp = areaParseResult.Isp;
            return true;
        }

        private static IpAreaParseResult ParseIpArea(string areaJson)
        {
            if (string.IsNullOrWhiteSpace(areaJson))
            {
                return IpAreaParseResult.Fail();
            }

            try
            {
                var areaData = JObject.Parse(areaJson);
                var data = areaData.SelectToken("data") as JObject;
                var content = areaData.SelectToken("content") as JObject ?? data;
                var result = new IpAreaParseResult
                {
                    Region = data?.Value<string>("region") ?? string.Empty,
                    City = data?.Value<string>("city") ?? string.Empty,
                    RealIp = content?.Value<string>("ip")
                        ?? data?.Value<string>("ip")
                        ?? areaData.Value<string>("ip")
                        ?? string.Empty,
                    Isp = content?.Value<string>("isp")
                        ?? data?.Value<string>("isp")
                        ?? areaData.Value<string>("isp")
                        ?? string.Empty
                };
                result.Success = data != null || content != null || !string.IsNullOrWhiteSpace(result.RealIp);
                return result;
            }
            catch (JsonException)
            {
                return IpAreaParseResult.Fail();
            }
        }
    }
}
