namespace Backend.Configuration
{
    public class BasicSemanticConfig
    {
        public List<string> GenreKeywords { get; set; } = new();
        public List<string> MechanicsKeywords { get; set; } = new();
        public List<string> ThemeKeywords { get; set; } = new();
        public List<string> MoodKeywords { get; set; } = new();
        public List<string> ArtStyleKeywords { get; set; } = new();
        public List<string> AudienceKeywords { get; set; } = new();
    }
}