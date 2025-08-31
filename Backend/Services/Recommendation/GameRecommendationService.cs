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
        private readonly ILogger<GameRecommendationService> _logger;

        public GameRecommendationService(
            ApplicationDbContext context,
            IVectorDatabase vectorDatabase,
            IEmbeddingService embeddingService,
            ILanguageModel languageModel,
            UserPreferenceService userPreferenceService,
            GameIndexingService gameIndexingService,
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
            _logger = logger;
        }

        public async Task<RecommendationResponse> GetRecommendationsAsync(RecommendationRequest request, ClaimsPrincipal? user)
        {
            try
            {
                _logger.LogInformation("Processing simplified recommendation request: {Query}", request.Query);

                // Step 5: Generate normalized vector for user query using ONNX model
                var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(request.Query);

                // Step 6: Use vector database to find most similar games
                var searchResults = await _gameIndexingService.SearchSimilarGamesAsync(queryEmbedding, request.MaxResults);

                _logger.LogInformation("Vector search returned {Count} results for query: {Query}", searchResults.Count, request.Query);

                if (searchResults.Count == 0)
                {
                    return new RecommendationResponse
                    {
                        ResponseMessage = "No games found matching your query. Please try a different search.",
                        RequiresFollowUp = false
                    };
                }

                // Step 7: Convert to recommendations with name, slug, and cover
                var recommendations = await ConvertToRecommendationAsync(searchResults);

                return new RecommendationResponse
                {
                    Games = recommendations,
                    ResponseMessage = $"Found {recommendations.Count} games matching your query.",
                    ConversationId = request.ConversationId ?? Guid.NewGuid().ToString(),
                    RequiresFollowUp = false
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing simplified recommendation request: {Query}", request.Query);
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

    }
}