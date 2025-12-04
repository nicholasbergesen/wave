namespace wave.web.Models
{
    public class GoogleSearchOptions
    {
        public const string SectionName = "GoogleSearch";
        
        public string ApiKey { get; set; } = string.Empty;
        public string SearchEngineId { get; set; } = string.Empty;
    }
}
