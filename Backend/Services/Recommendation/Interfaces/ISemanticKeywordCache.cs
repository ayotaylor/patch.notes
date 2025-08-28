using Backend.Configuration;

namespace Backend.Services.Recommendation.Interfaces
{
    public interface ISemanticKeywordCache
    {
        /// <summary>
        /// Initialize the semantic keyword cache by precomputing all possible combinations
        /// </summary>
        Task<bool> InitializeCacheAsync();

        /// <summary>
        /// Get precomputed semantic keywords for a genre
        /// </summary>
        SemanticCategoryMapping? GetGenreKeywords(string genre);

        /// <summary>
        /// Get precomputed semantic keywords for a platform
        /// </summary>
        SemanticCategoryMapping? GetPlatformKeywords(string platform);

        /// <summary>
        /// Get precomputed semantic keywords for a game mode
        /// </summary>
        SemanticCategoryMapping? GetGameModeKeywords(string gameMode);

        /// <summary>
        /// Get precomputed semantic keywords for a player perspective
        /// </summary>
        SemanticCategoryMapping? GetPerspectiveKeywords(string perspective);

        /// <summary>
        /// Get precomputed semantic keywords for combinations (e.g., "Fantasy RPG")
        /// </summary>
        SemanticCategoryMapping? GetCombinationKeywords(string combination);

        /// <summary>
        /// Get all available genres in the cache
        /// </summary>
        List<string> GetCachedGenres();

        /// <summary>
        /// Get all available platforms in the cache
        /// </summary>
        List<string> GetCachedPlatforms();

        /// <summary>
        /// Get all available game modes in the cache
        /// </summary>
        List<string> GetCachedGameModes();

        /// <summary>
        /// Get all available perspectives in the cache
        /// </summary>
        List<string> GetCachedPerspectives();

        /// <summary>
        /// Manually refresh the entire cache (expensive operation)
        /// </summary>
        Task<bool> RefreshCacheAsync();

        /// <summary>
        /// Check if cache is initialized and ready
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// Get cache statistics
        /// </summary>
        SemanticCacheStats GetCacheStats();
    }

    public class SemanticCacheStats
    {
        public int TotalGenres { get; set; }
        public int TotalPlatforms { get; set; }
        public int TotalGameModes { get; set; }
        public int TotalPerspectives { get; set; }
        public int TotalCombinations { get; set; }
        public DateTime LastInitialized { get; set; }
        public TimeSpan InitializationTime { get; set; }
        public long TotalKeywords { get; set; }
    }
}