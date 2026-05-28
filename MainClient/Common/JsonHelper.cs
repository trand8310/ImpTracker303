
namespace MainClient.Common
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    public static class JsonHelper
    {
        public static bool IsJson(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            text = text.Trim();

            // 一般 JSON 对象或数组才算常规 JSON
            if (!(text.StartsWith("{") && text.EndsWith("}")) &&
                !(text.StartsWith("[") && text.EndsWith("]")))
            {
                return false;
            }

            try
            {
                JToken.Parse(text);
                return true;
            }
            catch (JsonReaderException)
            {
                return false;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsJsonObject(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            text = text.Trim();

            if (!text.StartsWith("{") || !text.EndsWith("}"))
                return false;

            try
            {
                return JToken.Parse(text).Type == JTokenType.Object;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsJsonArray(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            text = text.Trim();

            if (!text.StartsWith("[") || !text.EndsWith("]"))
                return false;

            try
            {
                return JToken.Parse(text).Type == JTokenType.Array;
            }
            catch
            {
                return false;
            }
        }
    }
}
