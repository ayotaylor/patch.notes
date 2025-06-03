using Backend.Models.DTO;
using Backend.Models.Auth;

namespace Backend.Services
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<AuthResponse> ExternalLoginAsync(ExternalLoginRequest request);
        string GenerateJwtTokenAsync(User user);
        Task<User?> GetUserByIdAsync(string userId);
        bool ValidateTokenAsync(string token);
    }
}