using MainClient.Infrastructure;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MainClient.Common
{
    public class AdxHelper
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AppSettings _appSettings;
        private readonly ILogger _logger;
        public static HttpClient client = new HttpClient();

        public static void SendSms(string name, string phone)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(phone))
            {
                return;
            }

            try
            {
                HttpHelper http = new HttpHelper();
                var item = new HttpItem()
                {
                    URL = AppSettings.SmsApiUrl,
                    Method = "POST",
                    ContentType = "application/x-www-form-urlencoded",
                    Postdata = $"name={System.Web.HttpUtility.UrlEncode(name)}&phone={phone}",
                    Timeout = 10000,
                    Allowautoredirect = true,
                };
                var hr = http.GetHtml(item);
                if (hr.StatusCode == System.Net.HttpStatusCode.OK)
                {

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

            }

        }
        public AdxHelper(IHttpClientFactory httpClientFactory, AppSettings appSettings, ILogger<AdxHelper> logger)
        {
            _httpClientFactory = httpClientFactory;
            _appSettings = appSettings;
            _logger = logger;
        }


        public async Task<List<JToken>> GetTasksAsync(CancellationToken token = default)
        {
            var host = await IpHelper.GetLocalHostAsync();
            var url = $"{_appSettings.TaskApiUrl}?type=1&test=0&action=getTask&task={_appSettings.TaskName}&host={System.Web.HttpUtility.UrlEncode(host)}&ver={AppConsts.AppVersion}&_t={DateTime.Now.Ticks}";
            using (var request = new HttpRequestMessage(HttpMethod.Get, url))
            using (var response = await client .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (string.IsNullOrWhiteSpace(json) || json.Equals("empty"))
                    return new List<JToken>();

                JToken root;

                try
                {
                    root = JToken.Parse(json);
                }
                catch (JsonReaderException)
                {
                    return new List<JToken>();
                }

                return ExtractTasks(root);
            }
        }

        public  List<JToken> ExtractTasks(JToken root)
        {
            var result = new List<JToken>();
            if (root == null || root.Type == JTokenType.Null)
                return result;
            var task = root.SelectToken("task");
            if (task == null)
                return result;

            foreach (var item in root.SelectToken("task")!)
            {
                result.Add(item);
            }
            return result;
        }


        #region 系统设备
        private static ConcurrentQueue<JToken> ANDROID_QUEUE = new();
        private static ConcurrentQueue<JToken> iOS_QUEUE = new();
        private readonly SemaphoreSlim iOS_SIGNAL = new(1, 1);
        private readonly SemaphoreSlim ANDROID_SIGNAL = new(1, 1);
        private async Task<string?> GetDevByOSInternal(OSType os, int count)
        {
            try
            {
                var devApiUrl = _appSettings.DevApiUrl;
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json; charset=utf-8");
                var type = os == OSType.IOS ? "ios" : os == OSType.PC ? "win" : "android";

                var url = $"{devApiUrl}?type={type}&count={count}&t={System.DateTime.Now.Ticks}";
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            return null;
        }
        public async Task<JToken?> GetDevByOS(OSType os, int count = 5)
        {
            (ConcurrentQueue<JToken> devs, SemaphoreSlim sem) =
                os == OSType.IOS ?
                (iOS_QUEUE, iOS_SIGNAL) :
                (ANDROID_QUEUE, ANDROID_SIGNAL);

            if (devs.TryDequeue(out var cached))
            {
                return cached;
            }
            await sem.WaitAsync();
            try
            {
                if (devs.TryDequeue(out cached))
                {
                    return cached;
                }
                var text = await GetDevByOSInternal(os, count);
                if (string.IsNullOrWhiteSpace(text))
                {
                    return null;
                }
                var json = JToken.Parse(text);
                var data = json["data"] as JArray;
                if (data == null || data.Count == 0)
                {
                    return null;
                }
                JToken first = data[0];
                for (int i = 1; i < data.Count; i++)
                {
                    devs.Enqueue(data[i]);
                }

                return first;
            }
            catch (Exception ex)
            {
                _logger.LogError($"GetDevByOS error: {ex.Message}");
                return null;
            }
            finally
            {
                sem.Release();
            }
        }

        public OSType GetOS(string devClientId)
        {
            return devClientId switch
            {
                "7" => OSType.PC,
                "4" => OSType.IOS,
                _ => OSType.ANDROID
            };
        }
        public async Task<JToken?> GetDeviceAsync(OSType os, int count)
        {
            int retry = 0;
            JToken? dev = null;
            while (retry++ < 5)
            {
                dev = await GetDevByOS(os, count);
                if (dev != null) break;
            }
            return dev;
        }

        #endregion


    }
}
