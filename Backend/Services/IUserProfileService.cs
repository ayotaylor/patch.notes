public interface IUserProfileService
{
    Task<UserProfileDto> GetUserProfileAsync(string userId);                    
    Task<UserProfileDto> UpdateUserProfileAsync(
        string userId, UpdateUserProfileDto updateDto);
    Task<UserProfileDto> CreateUserProfileAsync(
        string userId, UpdateUserProfileDto createDto);
}