using Backend.Models.DTO.Social;

namespace Backend.Services
{
    public interface IFollowService
    {
        Task<bool> FollowUserAsync(Guid followerId, Guid followingId);
        Task<bool> UnfollowUserAsync(Guid followerId, Guid followingId);
        Task<bool> IsFollowingAsync(Guid followerId, Guid followingId);
        Task<List<FollowDto>> GetFollowersAsync(Guid userId, int page = 1, int pageSize = 20);
        Task<List<FollowDto>> GetFollowingAsync(Guid userId, int page = 1, int pageSize = 20);
        Task<FollowStatsDto> GetFollowStatsAsync(Guid userId, Guid? currentUserId = null);
        Task<int> GetFollowersCountAsync(Guid userId);
        Task<int> GetFollowingCountAsync(Guid userId);
    }
}