using Backend.Data;
using Backend.Models.DTO.Recommendation;
using Backend.Services.Recommendation.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Backend.Services.Recommendation
{
    public class GameRecommendationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IVectorDatabase _vectorDatabase;
        private readonly IEmbeddingService _embeddingService;
        private readonly ILanguageModel _languageModel;
        private readonly UserPreferenceService _userPreferenceService;
        private readonly GameIndexingService _gameIndexingService;
        private readonly ISemanticKeywordCache? _semanticKeywordCache;
        private readonly PlatformAliasService _platformAliasService;
        private readonly ILogger<GameRecommendationService> _logger;

        public GameRecommendationService(
            ApplicationDbContext context,
            IVectorDatabase vectorDatabase,
            IEmbeddingService embeddingService,
            ILanguageModel languageModel,
            UserPreferenceService userPreferenceService,
            GameIndexingService gameIndexingService,
            PlatformAliasService platformAliasService,
            ILogger<GameRecommendationService> logger,
            ISemanticKeywordCache? semanticKeywordCache = null)
        {
            _context = context;
            _vectorDatabase = vectorDatabase;
            _embeddingService = embeddingService;
            _languageModel = languageModel;
            _userPreferenceService = userPreferenceService;
            _gameIndexingService = gameIndexingService;
            _semanticKeywordCache = semanticKeywordCache;
            _platformAliasService = platformAliasService;
            _logger = logger;
        }

        public async Task<RecommendationResponse> GetRecommendationsAsync(RecommendationRequest request, ClaimsPrincipal? user)
        {
            try
            {
                _logger.LogInformation("Processing recommendation request: {Query}", request.Query);

                // Step 1: Analyze query with Groq
                var queryAnalysis = await _languageModel.AnalyzeQueryAsync(request.Query);
                
                // Step 2: Enhance query with semantic combinations  TODO: review this
                var enhancedQuery = await EnhanceQueryWithSemanticCombinations(queryAnalysis);
                
                // Step 3: Generate embedding for enhanced query
                var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(enhancedQuery);

                // Step 4: Search vector database with filters
                var searchResults = await _gameIndexingService.SearchSimilarGamesAsync(queryEmbedding, request.MaxResults, queryAnalysis);

                _logger.LogInformation("Vector search returned {Count} results for enhanced query", searchResults.Count);

                if (searchResults.Count == 0)
                {
                    var followUpQuestions = queryAnalysis.IsAmbiguous 
                        ? await _languageModel.GenerateFollowUpQuestionsAsync(request.Query, new List<GameRecommendation>())
                        : new List<string>();

                    return new RecommendationResponse
                    {
                        ResponseMessage = "No games found matching your query. Could you be more specific?",
                        FollowUpQuestions = followUpQuestions,
                        ConversationId = request.ConversationId ?? Guid.NewGuid().ToString(),
                        RequiresFollowUp = true
                    };
                }

                // Step 5: Convert to recommendations with explanations
                var recommendations = await ConvertToRecommendationsWithExplanationsAsync(searchResults, request.Query);

                // Step 6: Generate follow-up questions if needed
                var shouldGenerateFollowUp = queryAnalysis.IsAmbiguous || queryAnalysis.ConfidenceScore < 0.7f || searchResults.Count < request.MaxResults / 2;
                var followUps = shouldGenerateFollowUp 
                    ? await _languageModel.GenerateFollowUpQuestionsAsync(request.Query, recommendations)
                    : new List<string>();

                // Step 7: Generate overall explanation
                var overallExplanation = await _languageModel.ExplainRecommendationsAsync(recommendations, request.Query);

                return new RecommendationResponse
                {
                    Games = recommendations,
                    ResponseMessage = overallExplanation,
                    FollowUpQuestions = followUps,
                    ConversationId = request.ConversationId ?? Guid.NewGuid().ToString(),
                    RequiresFollowUp = shouldGenerateFollowUp
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing recommendation request: {Query}", request.Query);
                return new RecommendationResponse
                {
                    ResponseMessage = "I'm having trouble processing your request right now. Please try again.",
                    RequiresFollowUp = false
                };
            }
        }

        /// <summary>
        /// Convert search results to simplified recommendations with name, slug, and cover
        /// </summary>
        private async Task<List<GameRecommendation>> ConvertToRecommendationAsync(List<VectorSearchResult> searchResults)
        {
            var gameIds = searchResults.Select(r => Guid.Parse(r.Id)).ToList();

            var games = await _context.Games
                .Where(g => gameIds.Contains(g.Id))
                .Include(g => g.Covers)
                .ToListAsync();

            var recommendations = new List<GameRecommendation>();

            foreach (var result in searchResults)
            {
                var game = games.FirstOrDefault(g => g.Id.ToString() == result.Id);
                if (game == null) continue;

                recommendations.Add(new GameRecommendation
                {
                    GameId = game.Id,
                    Name = game.Name,
                    Slug = game.Slug,
                    CoverUrl = game.Covers?.FirstOrDefault()?.Url,
                    ConfidenceScore = result.Score,
                    Reasoning = "Recommended based on semantic similarity to your query."
                });
            }

            return recommendations.OrderByDescending(r => r.ConfidenceScore).ToList();
        }

        /// <summary>
        /// Enhance query using semantic combinations based on Groq analysis
        /// </summary>
        private async Task<string> EnhanceQueryWithSemanticCombinations(QueryAnalysis analysis)
        {
            if (_semanticKeywordCache == null)
                return analysis.ProcessedQuery;

            await _semanticKeywordCache.EnsureInitializedAsync();
            
            var enhancementKeywords = new List<string> { analysis.ProcessedQuery };
            
            // Add genre combinations
            for (int i = 0; i < analysis.Genres.Count - 1; i++)
            {
                for (int j = i + 1; j < Math.Min(analysis.Genres.Count, i + 3); j++)
                {
                    var combination = $"{analysis.Genres[i]} {analysis.Genres[j]}";
                    var comboMapping = _semanticKeywordCache.GetCombinationKeywords(combination);
                    if (comboMapping != null)
                    {
                        enhancementKeywords.AddRange(comboMapping.GenreKeywords.Take(3));
                        enhancementKeywords.AddRange(comboMapping.MechanicKeywords.Take(2));
                        enhancementKeywords.AddRange(comboMapping.MoodKeywords.Take(2));
                    }
                }
            }
            
            // Add platform aliases and platform-genre combinations
            foreach (var platform in analysis.Platforms.Take(2))
            {
                // Add platform aliases for broader matching
                var aliases = _platformAliasService.GetAllPlatformAliases(platform);
                enhancementKeywords.AddRange(aliases.Take(3));
                
                foreach (var genre in analysis.Genres.Take(2))
                {
                    var combination = $"{platform} {genre}";
                    var comboMapping = _semanticKeywordCache.GetCombinationKeywords(combination);
                    if (comboMapping != null)
                    {
                        enhancementKeywords.AddRange(comboMapping.PlatformType.Take(2));
                        enhancementKeywords.AddRange(comboMapping.EraKeywords.Take(2));
                    }
                }
            }
            
            // Add mood keywords directly
            enhancementKeywords.AddRange(analysis.Moods);
            
            var enhancedQuery = string.Join(" ", enhancementKeywords.Where(k => !string.IsNullOrWhiteSpace(k)).Distinct());
            
            _logger.LogDebug("Enhanced query: '{Original}' → '{Enhanced}'", analysis.ProcessedQuery, enhancedQuery);
            
            return enhancedQuery;
        }

        /// <summary>
        /// Convert search results to recommendations with batch explanations
        /// </summary>
        private async Task<List<GameRecommendation>> ConvertToRecommendationsWithExplanationsAsync(
            List<VectorSearchResult> searchResults, string originalQuery)
        {
            var gameIds = searchResults.Select(r => Guid.Parse(r.Id)).ToList();

            // TODO: fetch only necessary fields using select
            var games = await _context.Games
                .Where(g => gameIds.Contains(g.Id))
                .Include(g => g.Covers)
                .Include(g => g.GameGenres).ThenInclude(gg => gg.Genre)
                .Include(g => g.GamePlatforms).ThenInclude(gp => gp.Platform)
                .ToListAsync();

            var recommendations = new List<GameRecommendation>();

            foreach (var result in searchResults)
            {
                var game = games.FirstOrDefault(g => g.Id.ToString() == result.Id);
                if (game == null) continue;

                var recommendation = new GameRecommendation
                {
                    GameId = game.Id,
                    Name = game.Name,
                    Slug = game.Slug,
                    Summary = game.Summary ?? string.Empty,
                    Rating = game.Rating,
                    ConfidenceScore = result.Score,
                    CoverUrl = game.Covers?.FirstOrDefault()?.Url,
                    Genres = game.GameGenres?.Select(gg => gg.Genre.Name).ToList() ?? [],
                    Platforms = game.GamePlatforms?.Select(gp => gp.Platform.Name).ToList() ?? []
                };

                recommendations.Add(recommendation);
            }

            // Generate explanations for all games in a single batch call
            try
            {
                var explanations = await _languageModel.ExplainGameRecommendationsBatchAsync(recommendations, originalQuery);
                
                for (int i = 0; i < recommendations.Count && i < explanations.Count; i++)
                {
                    recommendations[i].Reasoning = explanations[i];
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate batch explanations, using fallback individual explanations");
                
                // Fallback to individual explanations if batch fails
                foreach (var recommendation in recommendations)
                {
                    try
                    {
                        recommendation.Reasoning = await _languageModel.ExplainGameRecommendationAsync(recommendation, originalQuery);
                    }
                    catch
                    {
                        recommendation.Reasoning = $"This game matches your search for {originalQuery} based on its genre and features.";
                    }
                }
            }

            return recommendations.OrderByDescending(r => r.ConfidenceScore).ToList();
        }
    }
}