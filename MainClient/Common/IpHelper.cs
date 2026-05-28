using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MainClient.Common
{
    public class IpHelper
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public IpHelper(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }


        public static SemaphoreSlim _mutex = new SemaphoreSlim(1);
        public static async Task<string> GetIp(string url, bool VerifyIP = false)
        {
            try
            {
                await _mutex.WaitAsync();
                using (HttpClient client = new HttpClient())
                {
                    var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, url));
                    var content = await response.Content.ReadAsStringAsync();
                    content = content.Replace("\r\n", "").Trim();
                    if (Regex.IsMatch(content, "\\d{1,5}\\.\\d{1,5}\\.\\d{1,5}\\.\\d{1,5}\\:\\d{1,6}") && (!VerifyIP || await Ping(content.Split(':')[0])))
                    {
                        return content;
                    }
                }
                return null;
            }
            catch (HttpRequestException ex)
            {
                Debug.WriteLine(ex.Message);
                return null;
            }
            finally
            {
                _mutex.Release();
            }
        }
        private static ConcurrentQueue<string> ipQueues = new ConcurrentQueue<string>();
        private static SemaphoreSlim _getIpMutex = new SemaphoreSlim(1);
        /// <summary>
        /// 获取代理IP
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static async Task<string> GetPoolIp(string url)
        {
            if (!ipQueues.IsEmpty)
            {
                if (ipQueues.TryDequeue(out var result))
                {
                    return result;
                }
            }
            try
            {
                await _getIpMutex.WaitAsync();
                using (HttpClient client = new HttpClient())
                {
                    var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, url));
                    var content = await response.Content.ReadAsStringAsync();
                    var lines = content.Split(new string[] { System.Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                    {
                        ipQueues.Enqueue(line);
                    }
                }

            }
            catch (HttpRequestException ex)
            {
                Debug.WriteLine(ex.Message);
                return null;
            }
            finally
            {
                _getIpMutex.Release();
            }

            if (!ipQueues.IsEmpty)
            {
                if (ipQueues.TryDequeue(out var result))
                {
                    return result;
                }
            }
            return null;
        }
        private static async Task<bool> Ping(string ip)
        {
            try
            {
                Ping ping = new Ping();
                PingOptions options = new PingOptions
                {
                    DontFragment = true
                };
                string s = "Test Data!";
                byte[] bytes = Encoding.ASCII.GetBytes(s);
                int timeout = 500;
                PingReply pingReply = await ping.SendPingAsync(ip, timeout, bytes, options);
                if (pingReply == null || pingReply.Status == IPStatus.Success)
                {
                    return true;
                }
                return false;
            }
            catch (PingException)
            {
                return false;
            }
        }
        public async Task<string> GetIpLocation(string ip)
        {
            var url = $"http://122.114.142.173/ip.php?ip={ip}";
            var client = _httpClientFactory.CreateClient();
            try
            {
                client.Timeout = TimeSpan.FromSeconds(15);
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
            }
            catch (WebException ex1)
            {
                Debug.WriteLine(ex1.Message);
            }
            catch (TaskCanceledException ex2)
            {
                Debug.WriteLine(ex2.Message);
            }
            catch (Exception ex3)
            {
                Debug.WriteLine(ex3.Message);
            }
            return null;


        }

        public async Task<string> GetIpInfo(string proxy)
        {
            HttpClientHandler httpClientHandler = new HttpClientHandler() { Proxy = new WebProxy(proxy, BypassOnLocal: false), UseProxy = true };
            using (var client = new HttpClient(httpClientHandler))
            {
                try
                {
                    client.Timeout = TimeSpan.FromSeconds(15);
                    HttpResponseMessage response = await client.GetAsync("http://ip-api.com/json/?lang=zh-CN");

                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        return await response.Content.ReadAsStringAsync();
                    }
                }
                catch (WebException ex)
                {
                    Debug.WriteLine(ex.Message);

                }
                return null;
            }
        }

    }
}