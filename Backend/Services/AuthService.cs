using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Backend.Models;
using Backend.Models.DTO;
using Backend.Config;

namespace Backend.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IConfiguration configuration,
            ILogger<AuthService> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            try
            {
                if (request.Password != request.ConfirmPassword)
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "Passwords do not match"
                    };
                }

                var existingUser = await _userManager.FindByEmailAsync(request.Email);
                if (existingUser != null)
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "User with this email already exists"
                    };
                }

                var user = new User
                {
                    UserName = request.Email,
                    Email = request.Email,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Provider = "Local",
                    CreatedAt = DateTime.Now,
                };

                var result = await _userManager.CreateAsync(user, request.Password);

                if (!result.Succeeded)
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = string.Join(", ", result.Errors.Select(e => e.Description))
                    };
                }

                var token = await GenerateJwtTokenAsync(user);

                _logger.LogInformation("User {Email} registered successfully", user.Email);

                return new AuthResponse
                {
                    Success = true,
                    Message = "Registration successful",
                    Token = token,
                    User = new UserInfo
                    {
                        Id = user.Id,
                        Email = user.Email,
                        FirstName = user.FirstName ?? "",
                        LastName = user.LastName ?? "",
                        Provider = user.Provider ?? "Local",
                        CreatedAt = user.CreatedAt,
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user registration");
                return new AuthResponse
                {
                    Success = false,
                    Message = "An error occured during registstarion"
                };
            }
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null)
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "Invalid email or password"
                    };
                }

                var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
                if (!result.Succeeded)
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "Invalid email or password"
                    };
                }

                var token = await GenerateJwtTokenAsync(user);

                _logger.LogInformation("User {Email} logged in successfully", user.Email);

                return new AuthResponse
                {
                    Success = true,
                    Message = "Login successful",
                    Token = token,
                    User = new UserInfo
                    {
                        Id = user.Id,
                        Email = user.Email,
                        FirstName = user.FirstName ?? "",
                        LastName = user.LastName ?? "",
                        Provider = user.Provider ?? "Local",
                        CreatedAt = user.CreatedAt,
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user login");
                return new AuthResponse
                {
                    Success = false,
                    Message = "An error occured during login"
                };
            }
        }

        public async Task<AuthResponse> ExternalLoginAsync(ExternalLoginRequest request)
        {
            try
            {
                var existingUser = await _userManager.FindByEmailAsync(request.Email ?? "");

                if (existingUser != null)
                {
                    // update provider info if needed
                    if (existingUser.Provider != request.Provider)
                    {
                        existingUser.Provider = request.Provider;
                        existingUser.ProviderId = request.ProviderId;
                        await _userManager.UpdateAsync(existingUser);
                    }

                    var token = await GenerateJwtTokenAsync(existingUser);

                    _logger.LogInformation("User {Email} logged in with {Provider}", existingUser.Email, existingUser.Provider);

                    return new AuthResponse
                    {
                        Success = true,
                        Message = "Login successful",
                        Token = token,
                        User = new UserInfo
                        {
                            Id = existingUser.Id,
                            Email = existingUser.Email,
                            FirstName = existingUser.FirstName ?? "",
                            LastName = existingUser.LastName ?? "",
                            Provider = existingUser.Provider ?? "Local",
                            CreatedAt = existingUser.CreatedAt,
                        }
                    };
                }

                // create new user
                var newUser = new User
                {
                    UserName = request.Email,
                    Email = request.Email,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Provider = request.Provider,
                    ProviderId = request.ProviderId,
                    EmailConfirmed = true, // confirmed by external provider usually
                    CreatedAt = DateTime.Now,
                };

                var result = await _userManager.CreateAsync(newUser);

                if (!result.Succeeded)
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = string.Join(", ", result.Errors.Select(e => e.Description))
                    };
                }

                var newToken = await GenerateJwtTokenAsync(newUser);

                _logger.LogInformation("User {Email} registered successfully", newUser.Email);

                return new AuthResponse
                {
                    Success = true,
                    Message = "Registration successful",
                    Token = newToken,
                    User = new UserInfo
                    {
                        Id = newUser.Id,
                        Email = newUser.Email,
                        FirstName = newUser.FirstName ?? "",
                        LastName = newUser.LastName ?? "",
                        Provider = newUser.Provider ?? "Local",
                        CreatedAt = newUser.CreatedAt,
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during external login with {Provider}", request.Provider);
                return new AuthResponse
                {
                    Success = false,
                    Message = "An error occured during external login"
                };
            }
        }

        public async Task<User?> GetUserByIdAsync(string userId)
        {
            return await _userManager.FindByEmailAsync(userId);
        }

        public async Task<bool> ValidateTokenAsync(string token)
        {
            try
            {
                var jwtSettings = _configuration.GetSection("Jwt").Get<JwtSettings>();
                jwtSettings.SecretKey = _configuration["Jwt:SecretKey"]; // From secrets
                var key = Encoding.UTF8.GetBytes(jwtSettings.SecretKey);

                var tokenHandler = new JwtSecurityTokenHandler();
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                };

                tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedtoken);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<string> GenerateJwtTokenAsync(User user)
        {
            var jwtSettings = _configuration.GetSection("Jwt").Get<JwtSettings>();
            jwtSettings.SecretKey = _configuration["Jwt:SecretKey"]; // From secrets

            var key = Encoding.UTF8.GetBytes(jwtSettings.SecretKey);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}".Trim()),
                new Claim("Provider", user.Provider ?? "Local"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat,
                    new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds().ToString(),
                    ClaimValueTypes.Integer64),
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(Convert.ToDouble(jwtSettings.ExpirationMinutes)),
                Issuer = jwtSettings.Issuer,
                Audience = jwtSettings.Audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
    }
}