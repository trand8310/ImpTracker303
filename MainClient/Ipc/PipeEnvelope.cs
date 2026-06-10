using Newtonsoft.Json.Linq;


namespace MainClient.Ipc
{
    public sealed class PipeEnvelope
    {
        public string Type { get; set; } = "";
        public string? TaskId { get; set; }
        public string? BrowserId { get; set; }
        public bool? Success { get; set; }
        public string? Message { get; set; }
        public JToken? Payload { get; set; }
        public JToken? Data { get; set; }
    }
}
