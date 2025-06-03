using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Backend.Services;

using System.Security.Claims;
using Backend.Models.DTO;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _authService.RegisterAsync(request);

                if (result.Success)
                {
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Register endpoint");
                return StatusCode(500, new AuthResponse
                {
                    Success = false,
                    Message = "An internal server error occurred"
                });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _authService.LoginAsync(request);

                if (result.Success)
                {
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Login endpoint");
                return StatusCode(500, new AuthResponse
                {
                    Success = false,
                    Message = "An internal server error occurred on Login"
                });
            }
        }

        [HttpPost("external-login")]
        public async Task<IActionResult> ExternalLogin([FromBody] ExternalLoginRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _authService.ExternalLoginAsync(request);

                if (result.Success)
                {
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in External Login endpoint");
                return StatusCode(500, new AuthResponse
                {
                    Success = false,
                    Message = "An internal server error occurred on External Login"
                });
            }
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var user = await _authService.GetUserByIdAsync(userId);

                if (user == null)
                {
                    return NotFound(new { Messsage = "User not found" });
                }

                // var email = User.FindFirst(ClaimTypes.Email)?.Value;
                // var name = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
                // var provider = User.FindFirst("provider")?.Value;

                return Ok(new UserInfo
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName ?? "",
                    LastName = user.LastName ?? "",
                    Provider = user.Provider ?? "Local",
                    CreatedAt = user.CreatedAt,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetCurrentUser endpoint");
                return StatusCode(500, new
                {
                    Message = "An internal server error occurred on Get Current User"
                });
            }
        }

        [HttpPost("validateToken")]
        public async Task<IActionResult> ValidateToken()
        {
            try
            {
                var header = Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrEmpty(header) || !header.StartsWith("Bearer "))
                {
                    return BadRequest(new
                    {
                        Message = "Authorization header is missing or invalid"
                    });
                }

                var token = header.Substring("Bearer ".Length).Trim();
                if (string.IsNullOrEmpty(token))
                {
                    return BadRequest(new
                    {
                        Message = "Token is required"
                    });
                }

                var isValid = _authService.ValidateTokenAsync(token);

                // maybe update response
                return Ok(new { IsValid = isValid });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in validateToken endpoint");
                return StatusCode(500, new
                {
                    Messsage = "An internal server error occurred"
                });
            }
        }

        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow
            });
        }
    }
}