using System.Net.Http;
using System.Threading.Tasks;

namespace CefClient.Common
{
    public class AdHelper
    {
        public static async Task<string> GetIp(string url)
        {
            HttpClient httpClient = new HttpClient();
            HttpResponseMessage response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        public static async Task<string> GetDev()
        {
            HttpClient httpClient = new HttpClient();
            HttpResponseMessage response = await httpClient.GetAsync("http://117.21.200.18:9000/api/getdev.php?count=1&type=android");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        public static async Task<string> GetTask(string name)
        {
            HttpClient httpClient = new HttpClient();
            HttpResponseMessage response = await httpClient.GetAsync($"http://117.21.200.19/client-v2.php?type=1&action=getTask&task={name}&test=0&_t={System.DateTime.Now.Ticks}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
    }
}
