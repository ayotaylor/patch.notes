using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/profile")]
    public class UserProfileController : ControllerBase
    {
        private readonly IUserProfileService _userProfileService;
        private readonly ILogger<UserProfileController> _logger;

        public UserProfileController(IUserProfileService userProfileService, ILogger<UserProfileController> logger)
        {
            _userProfileService = userProfileService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetUserProfile()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var profile = await _userProfileService.GetUserProfileAsync(userId);
                if (profile == null)
                {
                    return NotFound();
                }

                return Ok(profile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetUserProfile endpoint");
                return StatusCode(500, new { Message = "An internal server error occurred" });
            }
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetUserProfileById(string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest("User ID cannot be null or empty.");
                }

                var profile = await _userProfileService.GetUserProfileAsync(userId);
                if (profile == null)
                {
                    return NotFound();
                }

                return Ok(profile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetUserProfileById endpoint");
                return StatusCode(500, new { Message = "An internal server error occurred" });
            }
        }

        [HttpPut("update")]
        public async Task<IActionResult> UpdateUserProfile([FromBody] UpdateUserProfileDto updateDto)
        {
            try
            {
                if (updateDto == null)
                {
                    return BadRequest("Update data cannot be null.");
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var updatedProfile = await _userProfileService.UpdateUserProfileAsync(userId, updateDto);
                if (updatedProfile == null)
                {
                    return NotFound();
                }

                return Ok(updatedProfile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateUserProfile endpoint");
                return StatusCode(500, new { Message = "An internal server error occurred" });
            }
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateUserProfile([FromBody] UpdateUserProfileDto createDto)
        {
            try
            {
                if (createDto == null)
                {
                    return BadRequest("Create data cannot be null.");
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var createdProfile = await _userProfileService.CreateUserProfileAsync(userId, createDto);
                if (createdProfile == null)
                {
                    return BadRequest("Failed to create user profile.");
                }

                return CreatedAtAction(nameof(GetUserProfileById), new { userId = userId }, createdProfile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateUserProfile endpoint");
                return StatusCode(500, new { Message = "An internal server error occurred" });
            }
        }
    }
}