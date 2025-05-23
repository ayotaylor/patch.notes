using Backend.Models.DTO;
using Backend.Models;

namespace Backend.Services
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<AuthResponse> ExternalLoginAsync(ExternalLoginRequest request);
        Task<string> GenerateJwtTokenAsync(User user);
        Task<User?> GetUserByIdAsync(string userId);
        Task<bool> ValidateTokenAsync(string token);
    }
}