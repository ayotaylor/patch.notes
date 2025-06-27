namespace Backend.Config
{
    public class IgdbSettings
    {
        public const string SectionName = "Igdb";
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://api.igdb.com/v4";
        public int RateLimitDelayMs { get; set; } = 250; // 4 requests per second
        public int BatchSize { get; set; } = 1;//500;
    }
}
