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
                _logger.LogInformation("Processing recommendation request: {Query}", request.Query);

                // Parse user ID if available
                Guid? userId = null;
                if (user?.Identity?.IsAuthenticated == true)
                {
                    var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (Guid.TryParse(userIdClaim, out var parsedUserId))
                    {
                        userId = parsedUserId;
                    }
                }

                // Analyze the query using LLM
                var queryAnalysis = await _languageModel.AnalyzeQueryAsync(request.Query);

                // Generate embedding for the query
                var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(queryAnalysis.ProcessedQuery);

                // If user is authenticated and wants personalized recommendations
                if (userId.HasValue && request.IncludeFollowedUsersPreferences)
                {
                    // Get user preferences and incorporate them into the query
                    var userPreferences = await _userPreferenceService.GetUserPreferenceDataAsync(userId.Value);
                    var userPreferenceEmbedding = await _embeddingService.GenerateUserPreferenceEmbeddingAsync(userPreferences);

                    // Blend query embedding with user preference embedding (70% query, 30% user preferences)
                    queryEmbedding = BlendEmbeddings(queryEmbedding, userPreferenceEmbedding, 0.7f, 0.3f);
                }

                // Search for similar games using vector search
                var searchResults = await _gameIndexingService.SearchGamesWithFiltersAsync(
                    queryEmbedding,
                    queryAnalysis,
                    request.MaxResults * 2 // Get more results for filtering
                );

                _logger.LogInformation("Vector search returned {Count} results for query: {Query}", searchResults.Count, request.Query);

                // Convert search results to game recommendations
                var gameIds = searchResults.Select(r => Guid.Parse(r.Id)).ToList();
                var games = await GetGamesWithDetailsAsync(gameIds);

                var recommendations = new List<GameRecommendation>();
                foreach (var result in searchResults)
                {
                    var game = games.FirstOrDefault(g => g.Id.ToString() == result.Id);
                    if (game == null) continue;

                    var recommendation = MapToGameRecommendation(game, result.Score);
                    recommendations.Add(recommendation);
                }

                // Add user activity matches if user is authenticated
                if (userId.HasValue)
                {
                    var userActivityMatches = await _userPreferenceService.GetUserActivityMatchesAsync(userId.Value, gameIds);
                    foreach (var recommendation in recommendations)
                    {
                        if (userActivityMatches.TryGetValue(recommendation.GameId, out var activityMatch))
                        {
                            recommendation.UserActivityMatch = activityMatch;

                            // Boost confidence score based on user activity
                            if (activityMatch.IsUserFavorite) recommendation.ConfidenceScore += 0.2f;
                            if (activityMatch.IsUserLiked) recommendation.ConfidenceScore += 0.1f;
                            if (activityMatch.IsFromFollowedUsers) recommendation.ConfidenceScore += 0.15f;

                            recommendation.ConfidenceScore = Math.Min(1.0f, recommendation.ConfidenceScore);
                        }
                    }
                }

                // Sort by confidence score and take requested amount
                recommendations = recommendations
                    .OrderByDescending(r => r.ConfidenceScore)
                    .Take(request.MaxResults)
                    .ToList();

                // Generate explanations and follow-up questions using LLM
                var responseMessage = await _languageModel.ExplainRecommendationsAsync(recommendations, request.Query);

                var followUpQuestions = new List<string>();
                if (queryAnalysis.IsAmbiguous || queryAnalysis.ConfidenceScore < 0.7f)
                {
                    followUpQuestions = await _languageModel.GenerateFollowUpQuestionsAsync(request.Query, recommendations);
                }

                // Generate reasoning for each recommendation
                foreach (var recommendation in recommendations)
                {
                    recommendation.Reasoning = GenerateRecommendationReasoning(recommendation, queryAnalysis, userId.HasValue);
                }

                return new RecommendationResponse
                {
                    Games = recommendations,
                    ResponseMessage = responseMessage,
                    FollowUpQuestions = followUpQuestions,
                    ConversationId = request.ConversationId ?? Guid.NewGuid().ToString(),
                    RequiresFollowUp = followUpQuestions.Any()
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

        private async Task<List<Backend.Models.Game.Game>> GetGamesWithDetailsAsync(List<Guid> gameIds)
        {
            var games = await _context.Games
                .Where(g => gameIds.Contains(g.Id))
                .AsSplitQuery()
                //.Include(g => g.Slug)
                //.Include(g => g.Rating)
                .Include(g => g.GameGenres)
                    .ThenInclude(gg => gg.Genre)
                .Include(g => g.GamePlatforms)
                    .ThenInclude(gp => gp.Platform)
                .Include(g => g.Covers)
                .ToListAsync(); ;
            return games;
        }

        private GameRecommendation MapToGameRecommendation(Backend.Models.Game.Game game, float score)
        {
            return new GameRecommendation
            {
                GameId = game.Id,
                Slug = game.Slug,
                Name = game.Name,
                Summary = game.Summary ?? "",
                CoverUrl = game.Covers?.FirstOrDefault()?.Url,
                Rating = game.Rating,
                Genres = game.GameGenres?.Select(gg => gg.Genre.Name).ToList() ?? new List<string>(),
                Platforms = game.GamePlatforms?.Select(gp => gp.Platform.Name).ToList() ?? new List<string>(),
                ConfidenceScore = score,
                UserActivityMatch = new UserActivityMatch() // Will be populated later if user is authenticated
            };
        }

        private string GenerateRecommendationReasoning(GameRecommendation recommendation, QueryAnalysis queryAnalysis, bool isAuthenticated)
        {
            var reasons = new List<string>();

            // Genre matching
            var matchedGenres = recommendation.Genres.Intersect(queryAnalysis.Genres, StringComparer.OrdinalIgnoreCase).ToList();
            if (matchedGenres.Count > 0)
            {
                reasons.Add($"Matches your preferred genres: {string.Join(", ", matchedGenres)}");
            }

            // Platform matching
            var matchedPlatforms = recommendation.Platforms.Intersect(queryAnalysis.Platforms, StringComparer.OrdinalIgnoreCase).ToList();
            if (matchedPlatforms.Count > 0)
            {
                reasons.Add($"Available on {string.Join(", ", matchedPlatforms)}");
            }

            // User activity reasoning
            if (isAuthenticated && recommendation.UserActivityMatch != null)
            {
                if (recommendation.UserActivityMatch.IsUserFavorite)
                {
                    reasons.Add("You've favorited this game");
                }
                else if (recommendation.UserActivityMatch.IsUserLiked)
                {
                    reasons.Add("You've liked this game");
                }

                if (recommendation.UserActivityMatch.FollowedUsersWhoLiked.Count > 0)
                {
                    var users = recommendation.UserActivityMatch.FollowedUsersWhoLiked.Take(2);
                    reasons.Add($"Liked by {string.Join(" and ", users)} (who you follow)");
                }
            }

            // Rating-based reasoning
            if (recommendation.Rating.HasValue && recommendation.Rating >= 8.0m)
            {
                reasons.Add($"Highly rated ({recommendation.Rating:F1}/10)");
            }

            // Confidence-based reasoning
            if (recommendation.ConfidenceScore > 0.8f)
            {
                reasons.Add("Strong match for your query");
            }

            return reasons.Count > 0 ? string.Join(". ", reasons) + "." : "Recommended based on content similarity.";
        }

        private float[] BlendEmbeddings(float[] embedding1, float[] embedding2, float weight1, float weight2)
        {
            var blended = new float[Math.Min(embedding1.Length, embedding2.Length)];

            for (int i = 0; i < blended.Length; i++)
            {
                blended[i] = (embedding1[i] * weight1) + (embedding2[i] * weight2);
            }

            // Normalize the blended embedding
            var magnitude = Math.Sqrt(blended.Sum(x => x * x));
            if (magnitude > 0)
            {
                for (int i = 0; i < blended.Length; i++)
                {
                    blended[i] = (float)(blended[i] / magnitude);
                }
            }

            return blended;
        }
    }
}