using System.Security.Claims;
using Backend.Models.DTO.Response;
using Backend.Models.DTO.Social;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/follow")]
    [Authorize]
    public class FollowController : ControllerBase
    {
        private readonly IFollowService _followService;
        private readonly ILogger<FollowController> _logger;

        public FollowController(IFollowService followService, ILogger<FollowController> logger)
        {
            _followService = followService;
            _logger = logger;
        }

        [HttpPost()]
        public async Task<ActionResult<ApiResponse<bool>>> FollowUser([FromBody] CreateFollowDto request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId) || !Guid.TryParse(currentUserId, out var currentUserGuid))
                {
                    return Unauthorized(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                if (request == null || request.FollowingId == Guid.Empty)
                {
                    return BadRequest(new ApiResponse<ReviewDto>
                    {
                        Success = false,
                        Message = "Invalid user"
                    });
                }

                var result = await _followService.FollowUserAsync(currentUserGuid, request.FollowingId);
                if (result)
                {
                    return Ok(new ApiResponse<bool>
                    {
                        Success = true,
                        Data = true,
                        Message = "User followed successfully"
                    });
                }

                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = "Failed to follow user"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error following user {UserId}", request.FollowingId);
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An error occurred while following the user"
                });
            }
        }

        [HttpDelete()]
        public async Task<ActionResult<ApiResponse<bool>>> UnfollowUser([FromBody] CreateFollowDto request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId) || !Guid.TryParse(currentUserId, out var currentUserGuid))
                {
                    return Unauthorized(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }
                if (request == null || request.FollowingId == Guid.Empty)
                {
                    return BadRequest(new ApiResponse<ReviewDto>
                    {
                        Success = false,
                        Message = "Invalid user"
                    });
                }

                var result = await _followService.UnfollowUserAsync(currentUserGuid, request.FollowingId);
                if (result)
                {
                    return Ok(new ApiResponse<bool>
                    {
                        Success = true,
                        Data = true,
                        Message = "User unfollowed successfully"
                    });
                }

                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = "Failed to unfollow user"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unfollowing user {UserId}", request.FollowingId);
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An error occurred while unfollowing the user"
                });
            }
        }

        [HttpGet("{userId:guid}/followers")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<List<FollowDto>>>> GetFollowers(Guid userId, int page = 1, int pageSize = 20)
        {
            try
            {
                var followers = await _followService.GetFollowersAsync(userId, page, pageSize);
                return Ok(new ApiResponse<List<FollowDto>>
                {
                    Success = true,
                    Data = followers
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching followers for user {UserId}", userId);
                return StatusCode(500, new ApiResponse<List<UserSummaryDto>>
                {
                    Success = false,
                    Message = "An error occurred while fetching followers"
                });
            }
        }

        [HttpGet("{userId:guid}/following")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<List<FollowDto>>>> GetFollowing(Guid userId, int page = 1, int pageSize = 20)
        {
            try
            {
                var following = await _followService.GetFollowingAsync(userId, page, pageSize);
                return Ok(new ApiResponse<List<FollowDto>>
                {
                    Success = true,
                    Data = following
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching following for user {UserId}", userId);
                return StatusCode(500, new ApiResponse<List<FollowDto>>
                {
                    Success = false,
                    Message = "An error occurred while fetching following users"
                });
            }
        }

        [HttpGet("{userId:guid}/stats")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<FollowStatsDto>>> GetFollowStats(Guid userId)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                Guid? currentUserGuid = null;
                if (!string.IsNullOrEmpty(currentUserId) && Guid.TryParse(currentUserId, out var parsed))
                {
                    currentUserGuid = parsed;
                }

                var stats = await _followService.GetFollowStatsAsync(userId, currentUserGuid);
                return Ok(new ApiResponse<FollowStatsDto>
                {
                    Success = true,
                    Data = stats
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching follow stats for user {UserId}", userId);
                return StatusCode(500, new ApiResponse<FollowStatsDto>
                {
                    Success = false,
                    Message = "An error occurred while fetching follow statistics"
                });
            }
        }

        [HttpGet("{userId:guid}/is-following")]
        public async Task<ActionResult<ApiResponse<bool>>> IsFollowing(Guid userId)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId) || !Guid.TryParse(currentUserId, out var currentUserGuid))
                {
                    return Unauthorized(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                var isFollowing = await _followService.IsFollowingAsync(currentUserGuid, userId);
                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = isFollowing
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if following user {UserId}", userId);
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An error occurred while checking follow status"
                });
            }
        }
    }
}