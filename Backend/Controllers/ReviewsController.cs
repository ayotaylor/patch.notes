using Microsoft.AspNetCore.Mvc;
using Backend.Services;
using Backend.Models.DTO.Response;
using Backend.Models.DTO.Social;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/reviews")]
    public class ReviewsController : ControllerBase
    {
        private readonly IReviewService _reviewService;
        private readonly ILogger<ReviewsController> _logger;

        public ReviewsController(IReviewService reviewService, ILogger<ReviewsController> logger)
        {
            _reviewService = reviewService;
            _logger = logger;
        }

        [HttpGet("game/{gameId:int}")]
        public async Task<ActionResult<ApiResponse<PagedResponse<ReviewDto>>>> GetGameReviews(int gameId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var reviews = await _reviewService.GetGameReviewsAsync(gameId, page, pageSize);
               if (reviews == null || reviews.Data == null || reviews.Data.Count <= 0)
                {
                    return Ok(new ApiResponse<PagedResponse<ReviewDto>>
                    {
                        Success = false,
                        Message = "No reviews found",
                        Data = new PagedResponse<ReviewDto>()
                    });
                }
                return Ok(new ApiResponse<PagedResponse<ReviewDto>>
                {
                    Success = true,
                    Data = reviews,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching reviews for game {GameId}", gameId);
                return StatusCode(500, new ApiResponse<List<ReviewDto>>
                {
                    Success = false,
                    Message = "An error occurred while fetching game reviews",
                    Errors = [ex.Message]
                });
            }
        }

        [HttpGet("user/{userId:guid}")]
        public async Task<ActionResult<ApiResponse<PagedResponse<ReviewDto>>>> GetUserReviews(Guid userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var reviews = await _reviewService.GetUserReviewsAsync(userId, page, pageSize);
                if (reviews == null || reviews.Data == null || reviews.Data.Count <= 0)
                {
                    return Ok(new ApiResponse<PagedResponse<ReviewDto>>
                    {
                        Success = false,
                        Message = "No reviews found",
                        Data = new PagedResponse<ReviewDto>()
                    });
                }
                return Ok(new ApiResponse<PagedResponse<ReviewDto>>
                {
                    Success = true,
                    Data = reviews,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching reviews for user {UserId}", userId);
                return StatusCode(500, new ApiResponse<PagedResponse<ReviewDto>>
                {
                    Success = false,
                    Message = "An error occurred while fetching user reviews",
                    Errors = [ex.Message]
                });
            }
        }

        [HttpGet("latest")]
        public async Task<ActionResult<ApiResponse<PagedResponse<ReviewDto>>>> GetLatestReviews([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var reviews = await _reviewService.GetLatestReviewsAsync(page, pageSize);
                if (reviews == null || reviews.Data == null || reviews.Data.Count <= 0)
                {
                    return Ok(new ApiResponse<PagedResponse<ReviewDto>>
                    {
                        Success = false,
                        Message = "No reviews found",
                        Data = new PagedResponse<ReviewDto>()
                    });
                }

                return Ok(new ApiResponse<PagedResponse<ReviewDto>>
                {
                    Success = true,
                    Data = reviews,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching latest reviews");
                return StatusCode(500, new ApiResponse<PagedResponse<ReviewDto>>
                {
                    Success = false,
                    Message = "An error occurred while fetching latest reviews",
                    Errors = [ex.Message]
                });
            }
        }

        [HttpGet("{reviewId:guid}")]
        public async Task<ActionResult<ApiResponse<ReviewDto>>> GetReview(Guid reviewId)
        {
            try
            {
                var review = await _reviewService.GetReviewAsync(reviewId);
                if (review == null)
                {
                    return NotFound(new ApiResponse<ReviewDto>
                    {
                        Success = false,
                        Message = "Review not found"
                    });
                }

                return Ok(new ApiResponse<ReviewDto>
                {
                    Success = true,
                    Data = review
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching review {ReviewId}", reviewId);
                return StatusCode(500, new ApiResponse<ReviewDto>
                {
                    Success = false,
                    Message = "An error occurred while fetching the review",
                    Errors = [ex.Message]
                });
            }
        }

        [HttpGet("user/{userId:guid}/game/{gameId:int}")]
        public async Task<ActionResult<ApiResponse<ReviewDto>>> GetUserReviewForGame(Guid userId, int gameId)
        {
            try
            {
                var review = await _reviewService.GetUserReviewForGameAsync(userId, gameId);
                if (review == null)
                {
                    return Ok(new ApiResponse<ReviewDto>
                    {
                        Success = false,
                        Message = "Review not found"
                    });
                }

                return Ok(new ApiResponse<ReviewDto>
                {
                    Success = true,
                    Data = review
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching review for user {UserId} and game {GameId}", userId, gameId);
                return StatusCode(500, new ApiResponse<ReviewDto>
                {
                    Success = false,
                    Message = "An error occurred while fetching the review",
                    Errors = [ex.Message]
                });
            }
        }

        [HttpGet("user/{displayName}/game/{slug}")]
        public async Task<ActionResult<ApiResponse<ReviewDto>>> GetUserReviewForGame(string displayName, string slug)
        {
            try
            {
                var review = await _reviewService.GetUserReviewForGameWithSlugAsync(displayName, slug);
                if (review == null)
                {
                    return Ok(new ApiResponse<ReviewDto>
                    {
                        Success = false,
                        Message = "Review not found"
                    });
                }

                return Ok(new ApiResponse<ReviewDto>
                {
                    Success = true,
                    Data = review
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching review for user {displayName} and game {GameId}", displayName, slug);
                return StatusCode(500, new ApiResponse<ReviewDto>
                {
                    Success = false,
                    Message = "An error occurred while fetching the review",
                    Errors = [ex.Message]
                });
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<ApiResponse<ReviewDto>>> CreateReview([FromBody] CreateReviewRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new ApiResponse<ReviewDto>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                if (request == null || request.GameId <= 0 || request.Rating < 1 || request.Rating > 5)
                {
                    return BadRequest(new ApiResponse<ReviewDto>
                    {
                        Success = false,
                        Message = "Invalid review data. Rating must be between 1 and 5."
                    });
                }

                var review = await _reviewService.CreateReviewAsync(userId, request.GameId, request.Rating, request.ReviewText);
                if (review == null)
                {
                    return BadRequest(new ApiResponse<ReviewDto>
                    {
                        Success = false,
                        Message = "Failed to create review. You may already have a review for this game."
                    });
                }

                return CreatedAtAction(nameof(GetReview), new { reviewId = review.Id }, new ApiResponse<ReviewDto>
                {
                    Success = true,
                    Data = review,
                    Message = "Review created successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating review");
                return StatusCode(500, new ApiResponse<ReviewDto>
                {
                    Success = false,
                    Message = "An error occurred while creating the review",
                    Errors = [ex.Message]
                });
            }
        }

        [HttpPut("{reviewId:guid}")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<ReviewDto>>> UpdateReview(Guid reviewId, [FromBody] UpdateReviewRequest request)
        {
            try
            {
                if (request == null || request.Rating < 1 || request.Rating > 5)
                {
                    return BadRequest(new ApiResponse<ReviewDto>
                    {
                        Success = false,
                        Message = "Invalid review data. Rating must be between 1 and 5."
                    });
                }

                var review = await _reviewService.UpdateReviewAsync(reviewId, request.Rating, request.ReviewText);
                if (review == null)
                {
                    return NotFound(new ApiResponse<ReviewDto>
                    {
                        Success = false,
                        Message = "Review not found"
                    });
                }

                return Ok(new ApiResponse<ReviewDto>
                {
                    Success = true,
                    Data = review,
                    Message = "Review updated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating review {ReviewId}", reviewId);
                return StatusCode(500, new ApiResponse<ReviewDto>
                {
                    Success = false,
                    Message = "An error occurred while updating the review",
                    Errors = [ex.Message]
                });
            }
        }

        [HttpDelete("{reviewId:guid}")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteReview(Guid reviewId)
        {
            try
            {
                var result = await _reviewService.DeleteReviewAsync(reviewId);
                if (!result)
                {
                    return NotFound(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Review not found",
                        Data = false
                    });
                }

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "Review deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting review {ReviewId}", reviewId);
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An error occurred while deleting the review",
                    Errors = [ex.Message],
                    Data = false
                });
            }
        }

        [HttpGet("game/{gameId:int}/stats")]
        public async Task<ActionResult<ApiResponse<GameReviewStats>>> GetGameReviewStats(int gameId)
        {
            try
            {
                var averageRating = await _reviewService.GetGameAverageRatingAsync(gameId);
                var reviewsCount = await _reviewService.GetGameReviewsCountAsync(gameId);

                var stats = new GameReviewStats
                {
                    GameId = gameId,
                    AverageRating = Math.Round(averageRating, 2),
                    ReviewsCount = reviewsCount
                };

                return Ok(new ApiResponse<GameReviewStats>
                {
                    Success = true,
                    Data = stats
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching review stats for game {GameId}", gameId);
                return StatusCode(500, new ApiResponse<GameReviewStats>
                {
                    Success = false,
                    Message = "An error occurred while fetching review stats",
                    Errors = [ex.Message]
                });
            }
        }
    }

    public class CreateReviewRequest
    {
        public int GameId { get; set; }
        public int Rating { get; set; }
        public string? ReviewText { get; set; }
    }

    public class UpdateReviewRequest
    {
        public int Rating { get; set; }
        public string? ReviewText { get; set; }
    }

    public class GameReviewStats
    {
        public int GameId { get; set; }
        public double AverageRating { get; set; }
        public int ReviewsCount { get; set; }
    }
}