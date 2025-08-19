using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Backend.Services;
using Backend.Models.DTO.Response;
using Backend.Models.DTO.Social;
using System.Security.Claims;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/comments")]
    public class CommentController : ControllerBase
    {
        private readonly ICommentService _commentService;
        private readonly ILogger<CommentController> _logger;

        public CommentController(ICommentService commentService, ILogger<CommentController> logger)
        {
            _commentService = commentService;
            _logger = logger;
        }

        [HttpGet("review/{reviewId:guid}")]
        public async Task<ActionResult<ApiResponse<PagedResponse<CommentDto>>>> GetReviewComments(Guid reviewId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var comments = await _commentService.GetReviewCommentsAsync(reviewId, page, pageSize);
                return Ok(new ApiResponse<PagedResponse<CommentDto>>
                {
                    Success = true,
                    Data = comments,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching comments for review {ReviewId}", reviewId);
                return StatusCode(500, new ApiResponse<PagedResponse<CommentDto>>
                {
                    Success = false,
                    Message = "An error occurred while fetching review comments",
                });
            }
        }

        [HttpGet("review/{reviewId:guid}/count")]
        public async Task<ActionResult<ApiResponse<CommentCountDto>>> GetReviewCommentCount(Guid reviewId)
        {
            try
            {
                var count = await _commentService.GetReviewCommentCountAsync(reviewId);
                return Ok(new ApiResponse<CommentCountDto>
                {
                    Success = true,
                    Data = new CommentCountDto { TotalCount = count },
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching comment count for review {ReviewId}", reviewId);
                return StatusCode(500, new ApiResponse<CommentCountDto>
                {
                    Success = false,
                    Message = "An error occurred while fetching review comment count",
                });
            }
        }

        [HttpGet("list/{gameListId:guid}")]
        public async Task<ActionResult<ApiResponse<PagedResponse<CommentDto>>>> GetGameListComments(Guid gameListId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var comments = await _commentService.GetGameListCommentsAsync(gameListId, page, pageSize);
                return Ok(new ApiResponse<PagedResponse<CommentDto>>
                {
                    Success = true,
                    Data = comments,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching comments for game list {GameListId}", gameListId);
                return StatusCode(500, new ApiResponse<PagedResponse<CommentDto>>
                {
                    Success = false,
                    Message = "An error occurred while fetching game list comments",
                });
            }
        }

        [HttpGet("list/{gameListId:guid}/count")]
        public async Task<ActionResult<ApiResponse<CommentCountDto>>> GetGameListCommentCount(Guid gameListId)
        {
            try
            {
                var count = await _commentService.GetGameListCommentCountAsync(gameListId);
                return Ok(new ApiResponse<CommentCountDto>
                {
                    Success = true,
                    Data = new CommentCountDto { TotalCount = count },
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching comment count for game list {GameListId}", gameListId);
                return StatusCode(500, new ApiResponse<CommentCountDto>
                {
                    Success = false,
                    Message = "An error occurred while fetching game list comment count",
                });
            }
        }

        [HttpGet("{commentId:guid}/replies")]
        public async Task<ActionResult<ApiResponse<PagedResponse<CommentDto>>>> GetCommentReplies(Guid commentId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var replies = await _commentService.GetCommentRepliesAsync(commentId, page, pageSize);
                return Ok(new ApiResponse<PagedResponse<CommentDto>>
                {
                    Success = true,
                    Data = replies,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching replies for comment {CommentId}", commentId);
                return StatusCode(500, new ApiResponse<PagedResponse<CommentDto>>
                {
                    Success = false,
                    Message = "An error occurred while fetching comment replies",
                });
            }
        }

        [HttpGet("{commentId:guid}")]
        public async Task<ActionResult<ApiResponse<CommentDto>>> GetComment(Guid commentId)
        {
            try
            {
                var comment = await _commentService.GetCommentAsync(commentId);
                if (comment == null)
                {
                    return NotFound(new ApiResponse<CommentDto>
                    {
                        Success = false,
                        Message = "Comment not found",
                    });
                }

                return Ok(new ApiResponse<CommentDto>
                {
                    Success = true,
                    Data = comment,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching comment {CommentId}", commentId);
                return StatusCode(500, new ApiResponse<CommentDto>
                {
                    Success = false,
                    Message = "An error occurred while fetching the comment",
                });
            }
        }

        [HttpPost("review/{reviewId:guid}")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<CommentDto>>> CreateReviewComment(Guid reviewId, [FromBody] CreateCommentRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
                {
                    return Unauthorized(new ApiResponse<CommentDto>
                    {
                        Success = false,
                        Message = "User not authenticated",
                    });
                }

                var comment = await _commentService.CreateReviewCommentAsync(userGuid, reviewId, request.Content, request.ParentCommentId);
                if (comment == null)
                {
                    return BadRequest(new ApiResponse<CommentDto>
                    {
                        Success = false,
                        Message = "Failed to create comment",
                    });
                }

                return CreatedAtAction(nameof(GetComment), new { commentId = comment.Id }, new ApiResponse<CommentDto>
                {
                    Success = true,
                    Data = comment,
                    Message = "Comment created successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating comment for review {ReviewId}", reviewId);
                return StatusCode(500, new ApiResponse<CommentDto>
                {
                    Success = false,
                    Message = "An error occurred while creating the comment",
                });
            }
        }

        [HttpPost("list/{gameListId:guid}")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<CommentDto>>> CreateGameListComment(Guid gameListId, [FromBody] CreateCommentRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
                {
                    return Unauthorized(new ApiResponse<CommentDto>
                    {
                        Success = false,
                        Message = "User not authenticated",
                    });
                }

                var comment = await _commentService.CreateGameListCommentAsync(userGuid, gameListId, request.Content, request.ParentCommentId);
                if (comment == null)
                {
                    return BadRequest(new ApiResponse<CommentDto>
                    {
                        Success = false,
                        Message = "Failed to create comment",
                    });
                }

                return CreatedAtAction(nameof(GetComment), new { commentId = comment.Id }, new ApiResponse<CommentDto>
                {
                    Success = true,
                    Data = comment,
                    Message = "Comment created successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating comment for game list {GameListId}", gameListId);
                return StatusCode(500, new ApiResponse<CommentDto>
                {
                    Success = false,
                    Message = "An error occurred while creating the comment",
                });
            }
        }

        [HttpPut("{commentId:guid}")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<CommentDto>>> UpdateComment(Guid commentId, [FromBody] UpdateCommentRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
                {
                    return Unauthorized(new ApiResponse<CommentDto>
                    {
                        Success = false,
                        Message = "User not authenticated",
                    });
                }

                if (!await _commentService.IsUserCommentOwnerAsync(commentId, userGuid))
                {
                    return Forbid();
                }

                var comment = await _commentService.UpdateCommentAsync(commentId, request.Content);
                if (comment == null)
                {
                    return NotFound(new ApiResponse<CommentDto>
                    {
                        Success = false,
                        Message = "Comment not found",
                    });
                }

                return Ok(new ApiResponse<CommentDto>
                {
                    Success = true,
                    Data = comment,
                    Message = "Comment updated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating comment {CommentId}", commentId);
                return StatusCode(500, new ApiResponse<CommentDto>
                {
                    Success = false,
                    Message = "An error occurred while updating the comment",
                });
            }
        }

        [HttpDelete("{commentId:guid}")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<object>>> DeleteComment(Guid commentId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "User not authenticated",
                    });
                }

                if (!await _commentService.IsUserCommentOwnerAsync(commentId, userGuid))
                {
                    return Forbid();
                }

                var success = await _commentService.DeleteCommentAsync(commentId);
                if (!success)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Comment not found",
                    });
                }

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Comment deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting comment {CommentId}", commentId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while deleting the comment",
                });
            }
        }
    }

    public class CreateCommentRequest
    {
        public string Content { get; set; } = string.Empty;
        public Guid? ParentCommentId { get; set; }
    }

    public class UpdateCommentRequest
    {
        public string Content { get; set; } = string.Empty;
    }
}