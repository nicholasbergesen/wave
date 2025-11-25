using System.Text.Json.Serialization;

namespace wave.web.Models
{
    public class VllmChoice
    {
        [JsonPropertyName("index")]
        public int? Index { get; set; }

        [JsonPropertyName("message")]
        public VllmMessage? Message { get; set; }

        [JsonPropertyName("logprobs")]
        public object? Logprobs { get; set; }

        [JsonPropertyName("finish_reason")]
        public string? FinishReason { get; set; }

        [JsonPropertyName("stop_reason")]
        public string? StopReason { get; set; }

        [JsonPropertyName("token_ids")]
        public object? TokenIds { get; set; }
    }
}
