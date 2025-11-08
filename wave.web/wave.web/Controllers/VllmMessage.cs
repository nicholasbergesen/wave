using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace wave.web.Controllers
{
    public class VllmMessage
    {
        [JsonPropertyName("role")]
        public string? Role { get; set; }

        [JsonPropertyName("content")]
        public string? Content { get; set; }

        [JsonPropertyName("refusal")]
        public object? Refusal { get; set; }

        [JsonPropertyName("annotations")]
        public object? Annotations { get; set; }

        [JsonPropertyName("audio")]
        public object? Audio { get; set; }

        [JsonPropertyName("function_call")]
        public object? FunctionCall { get; set; }

        [JsonPropertyName("tool_calls")]
        public List<object>? ToolCalls { get; set; }

        [JsonPropertyName("reasoning_content")]
        public object? ReasoningContent { get; set; }
    }
}
