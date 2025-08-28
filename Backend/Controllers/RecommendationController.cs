using Backend.Models.DTO.Recommendation;
using Backend.Models.DTO.Response;
using Backend.Services.Recommendation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RecommendationController : ControllerBase
    {
        private readonly GameRecommendationService _recommendationService;
        private readonly GameIndexingService _indexingService;
        private readonly ILogger<RecommendationController> _logger;

        public RecommendationController(
            GameRecommendationService recommendationService,
            GameIndexingService indexingService,
            ILogger<RecommendationController> logger)
        {
            _recommendationService = recommendationService;
            _indexingService = indexingService;
            _logger = logger;
        }

        /// <summary>
        /// Get semantic keyword cache statistics
        /// </summary>
        /// <returns>Cache statistics including performance metrics</returns>
        [HttpGet("cache/stats")]
        [Authorize(Roles = "Admin")] // Restrict to admin users
        public ActionResult<ApiResponse<object>> GetCacheStats()
        {
            try
            {
                var stats = _indexingService.GetSemanticCacheStats();
                if (stats == null)
                {
                    return Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Data = new { message = "Semantic cache not configured or not initialized" }
                    });
                }

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = new
                    {
                        TotalKeywords = stats.TotalKeywords,
                        TotalGenres = stats.TotalGenres,
                        TotalPlatforms = stats.TotalPlatforms,
                        TotalGameModes = stats.TotalGameModes,
                        TotalPerspectives = stats.TotalPerspectives,
                        TotalCombinations = stats.TotalCombinations,
                        LastInitialized = stats.LastInitialized,
                        InitializationTimeMs = stats.InitializationTime.TotalMilliseconds,
                        IsInitialized = true
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving cache statistics");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Error retrieving cache statistics"
                });
            }
        }

        /// <summary>
        /// Manually refresh the semantic keyword cache
        /// </summary>
        /// <returns>Success status of cache refresh operation</returns>
        [HttpPost("cache/refresh")]
        [Authorize(Roles = "Admin")] // Restrict to admin users
        public async Task<ActionResult<ApiResponse<object>>> RefreshCache()
        {
            try
            {
                _logger.LogInformation("Manual cache refresh requested");
                var success = await _indexingService.RefreshSemanticCacheAsync();
                
                if (success)
                {
                    var stats = _indexingService.GetSemanticCacheStats();
                    return Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Message = "Cache refreshed successfully",
                        Data = stats != null ? new
                        {
                            TotalKeywords = stats.TotalKeywords,
                            InitializationTimeMs = stats.InitializationTime.TotalMilliseconds,
                            RefreshedAt = stats.LastInitialized
                        } : null
                    });
                }
                else
                {
                    return StatusCode(500, new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Failed to refresh cache"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing semantic cache");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Error refreshing semantic cache"
                });
            }
        }

        /// <summary>
        /// Get game recommendations based on natural language query
        /// </summary>
        /// <param name="request">The recommendation request containing the user's query</param>
        /// <returns>List of recommended games with explanations</returns>
        [HttpPost("search")]
        public async Task<ActionResult<ApiResponse<RecommendationResponse>>> GetRecommendations([FromBody] RecommendationRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<RecommendationResponse>
                    {
                        Success = false,
                        Message = "Invalid request",
                        Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                    });
                }

                _logger.LogInformation("Received recommendation request: {Query}", request.Query);

                var result = await _recommendationService.GetRecommendationsAsync(request, User);

                return Ok(new ApiResponse<RecommendationResponse>
                {
                    Success = true,
                    Data = result,
                    Message = result.Games.Any()
                        ? $"Found {result.Games.Count} recommendations"
                        : "No games found matching your criteria"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing recommendation request: {Query}", request.Query);
                return StatusCode(500, new ApiResponse<RecommendationResponse>
                {
                    Success = false,
                    Message = "An error occurred while processing your request"
                });
            }
        }

        /// <summary>
        /// Get personalized recommendations for authenticated users
        /// </summary>
        /// <param name="request">The recommendation request</param>
        /// <returns>Personalized game recommendations</returns>
        [HttpPost("personalized")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<RecommendationResponse>>> GetPersonalizedRecommendations([FromBody] RecommendationRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<RecommendationResponse>
                    {
                        Success = false,
                        Message = "Invalid request",
                        Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                    });
                }

                // Ensure personalization is enabled for this endpoint
                request.IncludeFollowedUsersPreferences = true;

                var result = await _recommendationService.GetRecommendationsAsync(request, User);

                return Ok(new ApiResponse<RecommendationResponse>
                {
                    Success = true,
                    Data = result,
                    Message = result.Games.Any()
                        ? $"Found {result.Games.Count} personalized recommendations"
                        : "No games found matching your preferences"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing personalized recommendation request: {Query}", request.Query);
                return StatusCode(500, new ApiResponse<RecommendationResponse>
                {
                    Success = false,
                    Message = "An error occurred while processing your personalized request"
                });
            }
        }

        /// <summary>
        /// Continue a conversation with follow-up queries
        /// </summary>
        /// <param name="conversationId">The conversation ID from previous interaction</param>
        /// <param name="request">The follow-up request</param>
        /// <returns>Updated recommendations based on follow-up</returns>
        [HttpPost("continue/{conversationId}")]
        public async Task<ActionResult<ApiResponse<RecommendationResponse>>> ContinueConversation(
            string conversationId,
            [FromBody] RecommendationRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<RecommendationResponse>
                    {
                        Success = false,
                        Message = "Invalid request",
                        Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                    });
                }

                // Set the conversation ID from the route
                request.ConversationId = conversationId;

                _logger.LogInformation("Continuing conversation {ConversationId} with query: {Query}", conversationId, request.Query);

                var result = await _recommendationService.GetRecommendationsAsync(request, User);

                return Ok(new ApiResponse<RecommendationResponse>
                {
                    Success = true,
                    Data = result,
                    Message = $"Updated recommendations based on your follow-up"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error continuing conversation {ConversationId}: {Query}", conversationId, request.Query);
                return StatusCode(500, new ApiResponse<RecommendationResponse>
                {
                    Success = false,
                    Message = "An error occurred while processing your follow-up"
                });
            }
        }

        /// <summary>
        /// Initialize or re-index all games in the vector database (Admin only)
        /// </summary>
        /// <returns>Number of games indexed</returns>
        [HttpPost("admin/reindex")]
        //[Authorize(Roles = "Admin")] // Assuming you have admin roles
        public async Task<ActionResult<ApiResponse<int>>> ReindexGames()
        {
            try
            {
                _logger.LogInformation("Starting game reindexing process");

                // Initialize collection if it doesn't exist
                var initialized = await _indexingService.InitializeCollectionAsync();
                if (!initialized)
                {
                    return StatusCode(500, new ApiResponse<int>
                    {
                        Success = false,
                        Message = "Failed to initialize vector database collection"
                    });
                }

                // Index all games
                var indexedCount = await _indexingService.IndexAllGamesAsync();

                _logger.LogInformation("Completed reindexing {Count} games", indexedCount);

                return Ok(new ApiResponse<int>
                {
                    Success = true,
                    Data = indexedCount,
                    Message = $"Successfully indexed {indexedCount} games"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during game reindexing");
                return StatusCode(500, new ApiResponse<int>
                {
                    Success = false,
                    Message = "An error occurred during reindexing"
                });
            }
        }

        /// <summary>
        /// Get examples of supported query types
        /// </summary>
        /// <returns>List of example queries users can try</returns>
        [HttpGet("examples")]
        public ActionResult<ApiResponse<List<string>>> GetQueryExamples()
        {
            var examples = new List<string>
            {
                "I want to play an RPG that would put me in a happy mood, released in the last few years",
                "Show me horror games similar to Silent Hill",
                "I'm looking for indie games with great storytelling",
                "What are some good multiplayer games for playing with friends?",
                "I want something relaxing and peaceful to play after work",
                "Recommend me strategy games like Civilization",
                "I'm in the mood for a dark, atmospheric game",
                "Show me games with strong female protagonists",
                "I want to play something similar to what my friends have been enjoying",
                "What are some critically acclaimed games from 2023?"
            };

            return Ok(new ApiResponse<List<string>>
            {
                Success = true,
                Data = examples,
                Message = "Example queries for game recommendations"
            });
        }

        /// <summary>
        /// Health check for the recommendation system
        /// </summary>
        /// <returns>System status</returns>
        [HttpGet("health")]
        public async Task<ActionResult<ApiResponse<object>>> HealthCheck()
        {
            try
            {
                // Check if vector database collection exists
                var collectionExists = await _indexingService.InitializeCollectionAsync();

                var status = new
                {
                    VectorDatabase = collectionExists ? "Healthy" : "Collection not initialized",
                    Timestamp = DateTime.UtcNow,
                    Status = collectionExists ? "Ready" : "Initializing"
                };

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = status,
                    Message = collectionExists ? "Recommendation system is ready" : "System initializing"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Health check failed"
                });
            }
        }
    }
}