namespace Backend.Services.Recommendation.Interfaces
{
    public interface IEmbeddingService
    {
        Task<float[]> GenerateEmbeddingAsync(string text);
        Task<float[]> GenerateGameEmbeddingAsync(GameEmbeddingInput gameInput);
        Task<List<float[]>> ProcessGamesInBatch(List<GameEmbeddingInput> gameInputs);
        Task<float[]> GenerateUserPreferenceEmbeddingAsync(UserPreferenceInput userInput);
        int EmbeddingDimensions { get; }
    }

    public class GameEmbeddingInput
    {
        // Core game fields as specified
        public string Name { get; set; } = string.Empty;
        // public string Summary { get; set; } = string.Empty;
        // public string? Storyline { get; set; }
        public List<string> Genres { get; set; } = new();
        public List<string> Platforms { get; set; } = new();
        public List<string> GameModes { get; set; } = new();
        public List<string> PlayerPerspectives { get; set; } = new();
        public decimal? Rating { get; set; }
        public DateTime? ReleaseDate { get; set; }

        // Additional required fields
        public List<string> AgeRatings { get; set; } = new();
        public List<string> Companies { get; set; } = new();
        public string GameType { get; set; } = string.Empty;

        // Enhanced semantic keyword fields (mapped to SemanticCategoryMapping properties)
        // Core game properties
        public List<string> ExtractedGenreKeywords { get; set; } = new();
        public List<string> ExtractedMechanicKeywords { get; set; } = new();
        public List<string> ExtractedThemeKeywords { get; set; } = new();
        public List<string> ExtractedMoodKeywords { get; set; } = new();
        public List<string> ExtractedArtStyleKeywords { get; set; } = new();
        public List<string> ExtractedAudienceKeywords { get; set; } = new();
        
        // Platform-specific keywords
        public List<string> ExtractedPlatformTypeKeywords { get; set; } = new();
        public List<string> ExtractedEraKeywords { get; set; } = new();
        public List<string> ExtractedCapabilityKeywords { get; set; } = new();
        
        // Game mode-specific keywords
        public List<string> ExtractedPlayerInteractionKeywords { get; set; } = new();
        public List<string> ExtractedScaleKeywords { get; set; } = new();
        public List<string> ExtractedCommunicationKeywords { get; set; } = new();
        
        // Perspective-specific keywords
        public List<string> ExtractedViewpointKeywords { get; set; } = new();
        public List<string> ExtractedImmersionKeywords { get; set; } = new();
        public List<string> ExtractedInterfaceKeywords { get; set; } = new();
    }

    public class UserPreferenceInput
    {
        public List<GameEmbeddingInput> FavoriteGames { get; set; } = new();
        public List<GameEmbeddingInput> LikedGames { get; set; } = new();
        public List<string> LikedReviewTexts { get; set; } = new();
        public List<string> LikedGameListDescriptions { get; set; } = new();
        public List<GameEmbeddingInput> FollowedUsersFavorites { get; set; } = new();
    }
}