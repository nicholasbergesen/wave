using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace wave.web.Models
{
    public class VllmChatCompletionResponse
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("object")]
        public string? Object { get; set; }

        [JsonPropertyName("created")]
        public long? Created { get; set; }

        [JsonPropertyName("model")]
        public string? Model { get; set; }

        [JsonPropertyName("choices")]
        public List<VllmChoice>? Choices { get; set; }

        [JsonPropertyName("service_tier")]
        public string? ServiceTier { get; set; }

        [JsonPropertyName("system_fingerprint")]
        public string? SystemFingerprint { get; set; }

        [JsonPropertyName("usage")]
        public VllmUsage? Usage { get; set; }

        [JsonPropertyName("prompt_logprobs")]
        public object? PromptLogprobs { get; set; }

        [JsonPropertyName("prompt_token_ids")]
        public object? PromptTokenIds { get; set; }

        [JsonPropertyName("kv_transfer_params")]
        public object? KvTransferParams { get; set; }
    }
}
