namespace Backend.Services.Recommendation.Interfaces
{
    public interface IEmbeddingService
    {
        Task<float[]> GenerateEmbeddingAsync(string text);
        Task<float[]> GenerateGameEmbeddingAsync(GameEmbeddingInput gameInput);
        Task<float[]> GenerateUserPreferenceEmbeddingAsync(UserPreferenceInput userInput);
        int EmbeddingDimensions { get; }
    }

    public class GameEmbeddingInput
    {
        public string Name { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string? Storyline { get; set; }
        public List<string> Genres { get; set; } = new();
        public List<string> Platforms { get; set; } = new();
        public List<string> GameModes { get; set; } = new();
        public List<string> PlayerPerspectives { get; set; } = new();
        public decimal? Rating { get; set; }
        public DateTime? ReleaseDate { get; set; }
        
        // Enhanced semantic keyword fields
        public List<string> ExtractedGenreKeywords { get; set; } = new();
        public List<string> ExtractedMechanicKeywords { get; set; } = new();
        public List<string> ExtractedThemeKeywords { get; set; } = new();
        public List<string> ExtractedMoodKeywords { get; set; } = new();
        public List<string> ExtractedArtStyleKeywords { get; set; } = new();
        public List<string> ExtractedAudienceKeywords { get; set; } = new();
        public Dictionary<string, float> SemanticWeights { get; set; } = new();
        public List<string> HierarchicalBoosts { get; set; } = new();
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