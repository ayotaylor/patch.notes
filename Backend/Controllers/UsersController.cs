using Backend.Models.DTO.Response;
using Backend.Models.DTO.User;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IUserService userService, ILogger<UsersController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        // GET: api/users
        [HttpGet]
        public async Task<ActionResult<ApiResponse<PagedResponse<UserDto>>>> GetAllUsers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                if (page < 1 || pageSize < 1 || pageSize > 100)
                {
                    return BadRequest(new ApiResponse<PagedResponse<UserDto>>
                    {
                        Success = false,
                        Message = "Invalid pagination parameters. Page must be >= 1 and pageSize must be between 1 and 100."
                    });
                }

                var result = await _userService.GetAllUsersAsync(page, pageSize);

                return Ok(new ApiResponse<PagedResponse<UserDto>>
                {
                    Success = true,
                    Data = result,
                    Message = "Users retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users");
                return StatusCode(500, new ApiResponse<PagedResponse<UserDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving users",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // GET: api/users/popular
        [HttpGet("popular")]
        public async Task<ActionResult<ApiResponse<List<UserDto>>>> GetPopularUsers([FromQuery] int limit = 5)
        {
            try
            {
                if (limit < 1 || limit > 50)
                {
                    return BadRequest(new ApiResponse<List<UserDto>>
                    {
                        Success = false,
                        Message = "Limit must be between 1 and 50."
                    });
                }

                var users = await _userService.GetPopularUsersAsync(limit);

                return Ok(new ApiResponse<List<UserDto>>
                {
                    Success = true,
                    Data = users,
                    Message = "Popular users retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving popular users");
                return StatusCode(500, new ApiResponse<List<UserDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving popular users",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // GET: api/users/featured
        [HttpGet("featured")]
        public async Task<ActionResult<ApiResponse<List<UserDto>>>> GetFeaturedUsers([FromQuery] int limit = 5)
        {
            try
            {
                if (limit < 1 || limit > 50)
                {
                    return BadRequest(new ApiResponse<List<UserDto>>
                    {
                        Success = false,
                        Message = "Limit must be between 1 and 50."
                    });
                }

                var users = await _userService.GetFeaturedUsersAsync(limit);

                return Ok(new ApiResponse<List<UserDto>>
                {
                    Success = true,
                    Data = users,
                    Message = "Featured users retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving featured users");
                return StatusCode(500, new ApiResponse<List<UserDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving featured users",
                    Errors = new List<string> { ex.Message }
                });
            }
        }
    }
}
