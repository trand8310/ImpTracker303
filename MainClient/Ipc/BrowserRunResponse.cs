using Newtonsoft.Json.Linq;

namespace MainClient.Ipc
{
    public sealed class BrowserRunResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public JToken? Data { get; set; }
    }
}
