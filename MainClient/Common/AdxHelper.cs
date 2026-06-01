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
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Xml.Linq;
using static System.Windows.Forms.Design.AxImporter;

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






        /// <summary>
        /// 获取任务统计状态
        /// </summary>
        /// <param name="taskId"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task<JObject?> GetTaskStatusAsync(int taskId, CancellationToken token = default)
        {
            return await Task.FromResult<JObject>(new JObject());
            //try
            //{
            //    var host = await CommonHelper.GetLocalHostAsync();
            //    var baseUrl = new Uri(_appSettings.TaskApiUrl).GetLeftPart(UriPartial.Authority);
            //    using var response = await client.GetAsync($"{baseUrl}/api{_apiVersion}/task-status.php?action=task_status&id={taskId}&host={System.Web.HttpUtility.UrlEncode(host)}&_t={System.DateTime.Now.Ticks}", token);
            //    response.EnsureSuccessStatusCode();
            //    return JsonNode.Parse(await response.Content.ReadAsStringAsync(token))?.AsObject();
            //}
            //catch (OperationCanceledException)
            //{
            //    // 请求被取消，安全退出
            //}
            //catch (Exception ex)
            //{
            //    _logger.LogError($"GetTaskStatusAsync TaskId={taskId} failed: {ex.Message}");
            //}
            //return null;
        }

        /// <summary>
        /// 更新任务统计状态
        /// </summary>
        /// <param name="taskId">任务 ID</param>
        /// <param name="metrics">指标字典，例如 start, dsp, click, success</param>
        /// <param name="token">取消令牌</param>
        /// <returns></returns>
        public async Task<JObject?> UpdateTaskStateAsync(int taskId, Dictionary<string, long> metrics, CancellationToken token = default)
        {
            return await Task.FromResult<JObject>(new JObject());

            //return null;
            //try
            //{
            //    var host = await CommonHelper.GetLocalHostAsync();
            //    var baseUrl = new Uri(_appSettings.TaskApiUrl).GetLeftPart(UriPartial.Authority);
            //    StringBuilder builder = new StringBuilder(baseUrl);
            //    builder.Append($"/api{_apiVersion}/task-status.php?action=update_task&_t={System.DateTime.Now.Ticks}");
            //    var bidRequest = new
            //    {
            //        id = taskId,
            //        host = host,
            //        version = _options.AppVersion,
            //        metrics = metrics
            //    };
            //    var postData = JsonSerializer.Serialize(bidRequest);
            //    using var content = new StringContent(postData, Encoding.UTF8, "application/json");
            //    using var response = await client.PostAsync(builder.ToString(), content, token);
            //    response.EnsureSuccessStatusCode();
            //    return JsonNode.Parse(await response.Content.ReadAsStringAsync(token))?.AsObject();
            //}
            //catch (OperationCanceledException)
            //{
            //    // 请求被取消，安全退出
            //}
            //catch (Exception ex)
            //{
            //    _logger.LogError($"UpdateTaskStateAsync TaskId={taskId} failed: {ex.Message}");
            //}
            //return null;
        }



        #region 代理状态统计&更新
        public async Task<JObject?> UpdateProxyIpStateAsync(int taskId, Dictionary<string, long> metrics, IEnumerable<string> ips, CancellationToken token = default)
        {
            return await Task.FromResult<JObject>(new JObject());
            //try
            //{
            //    var host = await CommonHelper.GetLocalHostAsync();
            //    var baseUrl = new Uri(_appSettings.TaskApiUrl).GetLeftPart(UriPartial.Authority);
            //    StringBuilder builder = new StringBuilder(baseUrl);
            //    builder.Append($"/api{_apiVersion}/ip-status.php?action=request&id={taskId}&_t={System.DateTime.Now.Ticks}");
            //    var body = new Dictionary<string, object>
            //    {
            //        ["metrics"] = metrics,
            //        ["ips"] = ips
            //    };
            //    body["host"] = host;
            //    body["agency"] = _appSettings.ProxyIpUrl;

            //    var postData = JsonSerializer.Serialize(body);
            //    using var content = new StringContent(postData, Encoding.UTF8, "application/json");
            //    using var response = await client.PostAsync(builder.ToString(), content, token);
            //    response.EnsureSuccessStatusCode();
            //    return JsonNode.Parse(await response.Content.ReadAsStringAsync(token))?.AsObject();
            //}
            //catch (OperationCanceledException)
            //{
            //    // 请求被取消，安全退出
            //}
            //catch (Exception ex)
            //{
            //    _logger.LogError($"UpdateProxyIpStatAsync TaskId={taskId} failed: {ex.Message}");
            //}
            //return null;
        }



        #endregion

        /// <summary>
        /// 更新主机状态
        /// </summary>
        /// <param name="metrics"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task<JObject?> UpdateHostStateAsync(Dictionary<string, long> metrics, CancellationToken token = default)
        {
            return await Task.FromResult<JObject>(new JObject());
            //metrics ??= new Dictionary<string, long>();
            //string wordName = "default";
            //var host = await CommonHelper.GetLocalHostAsync();
            //return null;
            //try
            //{
            //    var baseUrl = new Uri(_appSettings.TaskApiUrl).GetLeftPart(UriPartial.Authority);
            //    StringBuilder builder = new StringBuilder(baseUrl);
            //    builder.Append($"/api{_apiVersion}/task-status.php?action=update_host&_t={System.DateTime.Now.Ticks}");
            //    var bidRequest = new
            //    {
            //        host = host,
            //        task = _appSettings.TaskName,
            //        version = _options.AppVersion,
            //        proxy = GetProxyHostSafely(_appSettings.ProxyIpUrl),
            //        fullproxy = _appSettings.ProxyIpUrl,
            //        wordname = wordName,
            //        metrics = metrics,
            //    };
            //    var postData = JsonSerializer.Serialize(bidRequest);
            //    using var content = new StringContent(postData, Encoding.UTF8, "application/json");
            //    using var response = await client.PostAsync(builder.ToString(), content, token);
            //    response.EnsureSuccessStatusCode();
            //    var resp = await response.Content.ReadAsStringAsync(token);
            //    return JsonNode.Parse(resp)?.AsObject();
            //}
            //catch (OperationCanceledException)
            //{
            //    // 请求被取消，安全退出
            //}
            //catch (Exception ex)
            //{
            //    _logger.LogError($"UpdateHostStateAsync Host={host} failed: {ex.Message}");
            //}
            //return null;
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
