using Backend.Models.DTO.Game;
using Backend.Models.DTO.Response;
using Backend.Models.DTO.Social;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/social")]
    public class SocialController : ControllerBase
    {
        private readonly ISocialService _socialService;
        private readonly ILogger<SocialController> _logger;

        public SocialController(ISocialService socialService, ILogger<SocialController> logger)
        {
            _socialService = socialService;
            _logger = logger;
        }

        [HttpGet("favorites/{userId}")]
        public async Task<ActionResult<ApiResponse<List<GameDto>>>> GetUserFavorites(Guid userId, int page = 1, int pageSize = 5)
        {
            try
            {
                var favorites = await _socialService.GetUserFavoritesAsync(userId, page, pageSize);
                if (favorites == null || favorites.Count <= 0)
                {
                    return Ok(new ApiResponse<List<FavoriteDto>>
                    {
                        Success = false,
                        Message = "No favorites found for this user",
                        Data = []
                    });
                }
                return Ok(new ApiResponse<List<GameDto>>
                {
                    Success = true,
                    Data = favorites
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user favorites");
                return StatusCode(500, new ApiResponse<List<GameDto>>
                {
                    Success = false,
                    Message = "An error occurred while fetching user favorites",
                    Errors = [ex.Message]
                });
            }
        }

        [HttpPost("favorites")]
        public async Task<ActionResult<ApiResponse<bool>>> AddToFavorites([FromBody] FavoriteDto favoriteDto)
        {
            try
            {
                if (favoriteDto == null
                || favoriteDto.UserId == Guid.Empty
                    || favoriteDto.GameId <= 0)
                {
                    return BadRequest(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Invalid favorite data",
                        Data = false
                    });
                }
                var result = await _socialService.AddToFavoritesAsync(favoriteDto.UserId, favoriteDto.GameId);
                if (result)
                {
                    return Ok(new ApiResponse<bool>
                    {
                        Success = true,
                        Data = true,
                        Message = "Game added to favorites successfully"
                    });
                }
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = "Failed to add game to favorites"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding game to favorites");
                return StatusCode(500, new { Success = false, Message = "An error occurred while adding to favorites" });
            }
        }

        [HttpDelete("favorites")]
        public async Task<ActionResult<ApiResponse<bool>>> RemoveFromFavorites([FromBody] FavoriteDto favoriteDto)
        {
            try
            {
                if (favoriteDto == null || favoriteDto.UserId == Guid.Empty
                    || favoriteDto.GameId <= 0)
                {
                    return BadRequest(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Invalid favorite data",
                        Data = false
                    });
                }
                var result = await _socialService.RemoveFromFavoritesAsync(favoriteDto.UserId, favoriteDto.GameId);
                if (result)
                {
                    return Ok(new ApiResponse<bool>
                    {
                        Success = true,
                        Data = true,
                        Message = "Game removed from favorites successfully"
                    });
                }
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = "Failed to remove game from favorites"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing game from favorites");
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An error occurred while removing from favorites",
                    Errors = [ex.Message]
                });
            }
        }

        [HttpGet("likes/{userId}")]
        public async Task<ActionResult<ApiResponse<List<LikeDto>>>> GetUserLikes(Guid userId, int page = 1, int pageSize = 20)
        {
            try
            {
                var likes = await _socialService.GetUserLikesAsync(userId, page, pageSize);
                if (likes == null || likes.Count <= 0)
                {
                    return Ok(new ApiResponse<List<LikeDto>>
                    {
                        Success = false,
                        Message = "No likes found for this user",
                        Data = []
                    });
                }
                return Ok(new ApiResponse<List<GameDto>>
                {
                    Success = true,
                    Data = likes
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user likes");
                return StatusCode(500, new ApiResponse<List<LikeDto>>
                {
                    Success = false,
                    Message = "An error occurred while fetching user likes",
                    Errors = [ex.Message]
                });
            }
        }

        [HttpPost("likes")]
        public async Task<ActionResult<ApiResponse<bool>>> LikeGame([FromBody] LikeDto likeDto)
        {
            try
            {
                if (likeDto == null || likeDto.UserId == Guid.Empty || likeDto.GameId <= 0)
                {
                    return BadRequest(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Invalid like data",
                        Data = false
                    });
                }
                var result = await _socialService.LikeGameAsync(likeDto.UserId, likeDto.GameId);
                if (result)
                {
                    return Ok(new ApiResponse<bool>
                    {
                        Success = true,
                        Data = true,
                        Message = "Game liked successfully"
                    });
                }
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = "Failed to like game"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error liking game");
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An error occurred while liking the game",
                    Errors = [ex.Message]
                });
            }
        }

        [HttpDelete("likes")]
        public async Task<ActionResult<ApiResponse<bool>>> UnlikeGame([FromBody] LikeDto likeDto)
        {
            try
            {
                if (likeDto == null || likeDto.UserId == Guid.Empty || likeDto.GameId <= 0)
                {
                    return BadRequest(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Invalid like data",
                        Data = false
                    });
                }
                var result = await _socialService.UnlikeGameAsync(likeDto.UserId, likeDto.GameId);
                if (result)
                {
                    return Ok(new ApiResponse<bool>
                    {
                        Success = true,
                        Data = true,
                        Message = "Game unliked successfully"
                    });
                }
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = "Failed to unlike game"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unliking game");
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An error occurred while unliking the game",
                    Errors = [ex.Message]
                });
            }
        }

        // Review Likes
        [HttpPost("reviews/{reviewId:guid}/like")]
        public async Task<ActionResult<ApiResponse<bool>>> LikeReview(Guid reviewId, [FromBody] LikeRequest request)
        {
            try
            {
                var result = await _socialService.LikeReviewAsync(request.UserId, reviewId);
                if (result)
                {
                    return Ok(new ApiResponse<bool>
                    {
                        Success = true,
                        Data = true,
                        Message = "Review liked successfully"
                    });
                }
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = "Failed to like review"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error liking review {ReviewId}", reviewId);
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An error occurred while liking the review"
                });
            }
        }

        [HttpDelete("reviews/{reviewId:guid}/like")]
        public async Task<ActionResult<ApiResponse<bool>>> UnlikeReview(Guid reviewId, [FromBody] LikeRequest request)
        {
            try
            {
                var result = await _socialService.UnlikeReviewAsync(request.UserId, reviewId);
                if (result)
                {
                    return Ok(new ApiResponse<bool>
                    {
                        Success = true,
                        Data = true,
                        Message = "Review unliked successfully"
                    });
                }
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = "Failed to unlike review"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unliking review {ReviewId}", reviewId);
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An error occurred while unliking the review"
                });
            }
        }

        // GameList Likes
        [HttpPost("lists/{gameListId:guid}/like")]
        public async Task<ActionResult<ApiResponse<bool>>> LikeGameList(Guid gameListId, [FromBody] LikeRequest request)
        {
            try
            {
                var result = await _socialService.LikeGameListAsync(request.UserId, gameListId);
                if (result)
                {
                    return Ok(new ApiResponse<bool>
                    {
                        Success = true,
                        Data = true,
                        Message = "Game list liked successfully"
                    });
                }
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = "Failed to like game list"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error liking game list {GameListId}", gameListId);
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An error occurred while liking the game list"
                });
            }
        }

        [HttpDelete("lists/{gameListId:guid}/like")]
        public async Task<ActionResult<ApiResponse<bool>>> UnlikeGameList(Guid gameListId, [FromBody] LikeRequest request)
        {
            try
            {
                var result = await _socialService.UnlikeGameListAsync(request.UserId, gameListId);
                if (result)
                {
                    return Ok(new ApiResponse<bool>
                    {
                        Success = true,
                        Data = true,
                        Message = "Game list unliked successfully"
                    });
                }
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = "Failed to unlike game list"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unliking game list {GameListId}", gameListId);
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An error occurred while unliking the game list"
                });
            }
        }

        // Comment Likes
        [HttpPost("comments/{commentId:guid}/like")]
        public async Task<ActionResult<ApiResponse<bool>>> LikeComment(Guid commentId, [FromBody] LikeRequest request)
        {
            try
            {
                var result = await _socialService.LikeCommentAsync(request.UserId, commentId);
                if (result)
                {
                    return Ok(new ApiResponse<bool>
                    {
                        Success = true,
                        Data = true,
                        Message = "Comment liked successfully"
                    });
                }
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = "Failed to like comment"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error liking comment {CommentId}", commentId);
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An error occurred while liking the comment"
                });
            }
        }

        [HttpDelete("comments/{commentId:guid}/like")]
        public async Task<ActionResult<ApiResponse<bool>>> UnlikeComment(Guid commentId, [FromBody] LikeRequest request)
        {
            try
            {
                var result = await _socialService.UnlikeCommentAsync(request.UserId, commentId);
                if (result)
                {
                    return Ok(new ApiResponse<bool>
                    {
                        Success = true,
                        Data = true,
                        Message = "Comment unliked successfully"
                    });
                }
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = "Failed to unlike comment"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unliking comment {CommentId}", commentId);
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An error occurred while unliking the comment"
                });
            }
        }
    }

    public class LikeRequest
    {
        public Guid UserId { get; set; }
    }
}