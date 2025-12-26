using System.Text.Json.Serialization;

namespace wave.web.Models
{
    public enum MessageRole
    {
        User,
        Assistant,
        System
    }

    public class Message
    {
        [JsonPropertyName("role")]
        public string? Role { get; set; }

        [JsonPropertyName("content")]
        public string? Content { get; set; }

        [JsonPropertyName("usedWebSearch")]
        public bool UsedWebSearch { get; set; }

        [JsonPropertyName("usedRag")]
        public bool UsedRag { get; set; }
    }
}
