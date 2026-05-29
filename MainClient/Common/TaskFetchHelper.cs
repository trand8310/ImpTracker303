using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MainClient.Common
{
    internal static class TaskFetchHelper
    {
        public static async Task<List<JToken>> GetRawTasksAsync(
            HttpClient httpClient,
            string taskApiUrl,
            CancellationToken token)
        {
            if (httpClient == null)
                throw new ArgumentNullException(nameof(httpClient));

            if (string.IsNullOrWhiteSpace(taskApiUrl))
                throw new ArgumentNullException(nameof(taskApiUrl));

            using (var request = new HttpRequestMessage(HttpMethod.Get, taskApiUrl))
            using (var response = await httpClient
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token)
                .ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (string.IsNullOrWhiteSpace(json))
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

        public static List<JToken> ExtractTasks(JToken root)
        {
            var result = new List<JToken>();

            if (root == null || root.Type == JTokenType.Null)
                return result;

            // 接口直接返回数组
            if (root is JArray rootArray)
            {
                foreach (var item in rootArray)
                {
                    if (IsValidTask(item))
                        result.Add(item);
                }

                return result;
            }

            // 接口返回对象
            if (root is JObject obj)
            {
                string[] containerNames =
                {
                    "data",
                    "tasks",
                    "list",
                    "rows",
                    "result",
                    "items"
                };

                foreach (var name in containerNames)
                {
                    var token = obj[name];

                    if (token == null || token.Type == JTokenType.Null)
                        continue;

                    if (token is JArray arr)
                    {
                        foreach (var item in arr)
                        {
                            if (IsValidTask(item))
                                result.Add(item);
                        }

                        return result;
                    }

                    if (token is JObject singleObj)
                    {
                        if (IsValidTask(singleObj))
                            result.Add(singleObj);

                        return result;
                    }
                }

                // 本身就是单个任务
                if (IsValidTask(obj))
                    result.Add(obj);
            }

            return result;
        }

        private static bool IsValidTask(JToken token)
        {
            if (token == null)
                return false;

            if (token.Type != JTokenType.Object)
                return false;

            var obj = (JObject)token;

            if (!obj.HasValues)
                return false;

            // 如果你任务必须有 url/id/action，可以在这里加规则
            // var url = obj["url"]?.ToString();
            // if (string.IsNullOrWhiteSpace(url))
            //     return false;

            return true;
        }
    }
}
