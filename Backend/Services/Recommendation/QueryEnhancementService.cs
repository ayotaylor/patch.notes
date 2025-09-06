using Backend.Configuration;
using Backend.Services.Recommendation.Interfaces;

namespace Backend.Services.Recommendation
{
    /// <summary>
    /// Service for enhancing user queries with semantic combinations and context
    /// </summary>
    public class QueryEnhancementService
    {
        private readonly ISemanticKeywordCache _semanticCache;
        private readonly ILogger<QueryEnhancementService> _logger;

        public QueryEnhancementService(
            ISemanticKeywordCache semanticCache,
            ILogger<QueryEnhancementService> logger)
        {
            _semanticCache = semanticCache;
            _logger = logger;
        }

        /// <summary>
        /// Enhances a query using semantic combinations from the analysis
        /// </summary>
        public async Task<string> EnhanceQueryAsync(QueryAnalysis analysis)
        {
            await _semanticCache.EnsureInitializedAsync();
            
            var enhancementKeywords = new List<string>();
            
            // Add the processed query
            enhancementKeywords.Add(analysis.ProcessedQuery);
            
            // Add semantic combinations based on detected elements
            enhancementKeywords.AddRange(GetGenreCombinationKeywords(analysis.Genres));
            enhancementKeywords.AddRange(GetPlatformGenreCombinationKeywords(analysis.Platforms, analysis.Genres));
            enhancementKeywords.AddRange(GetGameModeGenreCombinationKeywords(analysis.GameModes, analysis.Genres));
            enhancementKeywords.AddRange(GetMoodBasedKeywords(analysis.Moods));
            
            var enhancedQuery = string.Join(" ", enhancementKeywords.Where(k => !string.IsNullOrWhiteSpace(k)).Distinct());
            
            _logger.LogDebug("Enhanced query from '{OriginalQuery}' to '{EnhancedQuery}' with {KeywordCount} semantic keywords",
                analysis.ProcessedQuery, enhancedQuery, enhancementKeywords.Count);
            
            return enhancedQuery;
        }

        private List<string> GetGenreCombinationKeywords(List<string> genres)
        {
            var keywords = new List<string>();
            
            // Generate genre combinations
            for (int i = 0; i < genres.Count - 1; i++)
            {
                for (int j = i + 1; j < genres.Count; j++)
                {
                    var combination = $"{genres[i]} {genres[j]}";
                    var comboMapping = _semanticCache.GetCombinationKeywords(combination);
                    if (comboMapping != null)
                    {
                        keywords.AddRange(comboMapping.GenreKeywords);
                        keywords.AddRange(comboMapping.MechanicKeywords);
                        keywords.AddRange(comboMapping.ThemeKeywords);
                    }
                }
            }
            
            return keywords;
        }

        private List<string> GetPlatformGenreCombinationKeywords(List<string> platforms, List<string> genres)
        {
            var keywords = new List<string>();
            
            foreach (var platform in platforms.Take(2)) // Limit platforms
            {
                foreach (var genre in genres.Take(3)) // Limit genres per platform
                {
                    var combination = $"{platform} {genre}";
                    var comboMapping = _semanticCache.GetCombinationKeywords(combination);
                    if (comboMapping != null)
                    {
                        keywords.AddRange(comboMapping.PlatformType);
                        keywords.AddRange(comboMapping.EraKeywords);
                        keywords.AddRange(comboMapping.CapabilityKeywords);
                        keywords.AddRange(comboMapping.GenreKeywords);
                    }
                }
            }
            
            return keywords;
        }

        private List<string> GetGameModeGenreCombinationKeywords(List<string> gameModes, List<string> genres)
        {
            var keywords = new List<string>();
            
            foreach (var gameMode in gameModes.Take(2))
            {
                foreach (var genre in genres.Take(2))
                {
                    var combination = $"{gameMode} {genre}";
                    var comboMapping = _semanticCache.GetCombinationKeywords(combination);
                    if (comboMapping != null)
                    {
                        keywords.AddRange(comboMapping.PlayerInteractionKeywords);
                        keywords.AddRange(comboMapping.ScaleKeywords);
                        keywords.AddRange(comboMapping.CommunicationKeywords);
                    }
                }
            }
            
            return keywords;
        }

        private List<string> GetMoodBasedKeywords(List<string> moods)
        {
            var keywords = new List<string>();
            
            // Add mood keywords directly as they enhance semantic understanding
            keywords.AddRange(moods);
            
            return keywords;
        }
    }
}