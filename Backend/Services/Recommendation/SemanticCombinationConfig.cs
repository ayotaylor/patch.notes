namespace Backend.Services.Recommendation
{
    /// <summary>
    /// Configuration constants for semantic combination generation
    /// </summary>
    public static class SemanticCombinationConfig
    {
        // Limits to prevent cache bloat
        public const int MaxCombinations = 200;
        public const int MaxGenresPerTheme = 3;
        public const int MaxPlatformsPerEra = 2;
        public const int MaxGenresPerPlatform = 3;
        public const int MaxGenresPerGameMode = 3;
        public const int MaxGenresPerPerspective = 3;
        public const int MinKeywordsForCaching = 3;

        // Era classification keywords
        public static readonly string[] ModernEraKeywords = { "modern", "contemporary", "current" };
        public static readonly string[] RetroEraKeywords = { "retro", "vintage", "classic", "old-school" };

        // Popular genre combination patterns
        public static readonly (string Primary, string Secondary)[] PopularGenreCombinations = 
        {
            ("Action", "RPG"),
            ("Horror", "Survival"), 
            ("Racing", "Simulation"),
            ("Strategy", "Real-time"),
            ("Puzzle", "Adventure"),
            ("Fighting", "Action"),
            ("Sports", "Simulation"),
            ("Shooter", "First-person")
        };

        // High-value game mode keywords for compatibility matching
        public static readonly string[] SocialGameModeKeywords = 
        {
            "multiplayer", "cooperative", "team-based", "social", "competitive"
        };

        public static readonly string[] SoloGameModeKeywords = 
        {
            "single-player", "solo", "narrative", "story-driven"
        };

        // Perspective compatibility indicators
        public static readonly string[] ImmersivePerspectiveKeywords = 
        {
            "first-person", "immersive", "realistic", "simulation"
        };

        public static readonly string[] AccessiblePerspectiveKeywords = 
        {
            "third-person", "accessible", "casual", "family-friendly"
        };
    }
}