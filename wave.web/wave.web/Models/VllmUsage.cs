using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace wave.web.Models
{
    public class VllmUsage
    {
        [JsonPropertyName("prompt_tokens")]
        public int? PromptTokens { get; set; }

        [JsonPropertyName("total_tokens")]
        public int? TotalTokens { get; set; }

        [JsonPropertyName("completion_tokens")]
        public int? CompletionTokens { get; set; }

        [JsonPropertyName("prompt_tokens_details")]
        public object? PromptTokensDetails { get; set; }
    }
}
