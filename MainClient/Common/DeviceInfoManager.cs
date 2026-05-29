using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MainClient.Common
{
    internal sealed class DeviceContext
    {
        public OSType Os { get; set; } = OSType.UNUNKNOWN;
        public JToken Dev { get; set; }
        public string UserAgent { get; set; } = string.Empty;
    }

    internal sealed class DeviceInfoManager
    {
        private static readonly HttpClient Client = new HttpClient();
        private readonly ConcurrentQueue<JToken> _androidDevices = new ConcurrentQueue<JToken>();
        private readonly ConcurrentQueue<JToken> _iosDevices = new ConcurrentQueue<JToken>();
        private readonly SemaphoreSlim _mutex = new SemaphoreSlim(1);
        private readonly AppSetting _setting;
        private readonly ILogger _logger;
        private readonly Action<string> _log;

        public DeviceInfoManager(AppSetting setting, ILogger logger, Action<string> log)
        {
            _setting = setting;
            _logger = logger;
            _log = log;
        }

        public async Task<DeviceContext> ResolveDeviceContextAsync(JObject task, Dictionary<int, int> uaRates, int abl)
        {
            var uaClients = task["client"].ToString().Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries);
            var os = SelectOsForTask(uaRates, uaClients, abl);
            var dev = await GetDevByOS(os);
            if (dev == null)
            {
                _log?.Invoke($"设备异常:os={os}");
                return null;
            }

            var userAgent = dev["ua"]?.Value<string>();
            if (string.IsNullOrWhiteSpace(userAgent))
            {
                _log?.Invoke($"设备异常:缺少UA,os={os}");
                return null;
            }

            return new DeviceContext
            {
                Os = os,
                Dev = dev,
                UserAgent = userAgent
            };
        }

        private static OSType SelectOsForTask(Dictionary<int, int> uaRates, string[] uaClients, int abl)
        {
            if (abl != 0)
            {
                var uaIndex = uaRates.OrderBy(o => o.Value).FirstOrDefault().Key;
                var uaValue = uaRates[uaIndex];

                if (abl != 100)
                {
                    uaRates[uaIndex] = uaIndex == 1 ? uaValue + (100 - abl) : uaValue + abl;
                }
                else
                {
                    uaRates[uaIndex] = uaValue + 1;
                }

                return DevMan.GetOSByClient(uaIndex);
            }

            if (uaClients.Length > 1)
            {
                var randomIndex = new Random(Guid.NewGuid().GetHashCode()).Next(0, uaClients.Length);
                return DevMan.GetOSByClient(Convert.ToInt32(uaClients[randomIndex]));
            }

            return DevMan.GetOSByClient(uaRates.FirstOrDefault().Key);
        }

        private async Task<JToken> GetDevByOS(OSType os)
        {
            var queue = os == OSType.IOS ? _iosDevices : os == OSType.ANDROID ? _androidDevices : null;
            if (queue == null)
            {
                return null;
            }

            if (queue.TryDequeue(out var result))
            {
                return result;
            }

            var json = await GetDevs(os, 100);
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                var jo = (JObject)JsonConvert.DeserializeObject(json);
                if (jo != null)
                {
                    foreach (var item in jo["data"])
                    {
                        queue.Enqueue(item);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            return queue.TryDequeue(out result) ? result : null;
        }

        private async Task<string> GetDevs(OSType os, int count = 100)
        {
            try
            {
                await _mutex.WaitAsync();
                HttpResponseMessage response = await Client.GetAsync($"{_setting.DevApiUrl}?type={(os == OSType.IOS ? "ios" : "android")}&count={count}");
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    return responseBody;
                }

                _logger?.LogInformation($"GetDevs,{response.StatusCode}");
                return null;
            }
            catch (HttpRequestException ex)
            {
                _logger?.LogInformation($"GetDevs,{ex.Message},{ex.StackTrace},{ex.InnerException?.Message}");
                return null;
            }
            finally
            {
                _mutex.Release();
            }
        }
    }
}
