using Backend.Data;
using Backend.Mapping;
using Backend.Models.DTO.Social;
using Backend.Models.Social;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services
{
    public class FollowService : IFollowService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FollowService> _logger;

        public FollowService(ApplicationDbContext context, ILogger<FollowService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> FollowUserAsync(Guid followerId, Guid followingId)
        {
            if (followerId == followingId)
            {
                _logger.LogWarning("User {UserId} tried to follow themselves", followerId);
                return false;
            }

            var followerProfileId = await GetUserProfileIdAsync(followerId);
            var followingProfileId = await GetUserProfileIdAsync(followingId);

            if (followerProfileId == Guid.Empty || followingProfileId == Guid.Empty)
            {
                _logger.LogWarning("Invalid user profiles for follow operation. Follower: {FollowerId}, Following: {FollowingId}",
                    followerId, followingId);
                return false;
            }

            var existingFollow = await _context.Follows
                .FirstOrDefaultAsync(f => f.FollowerId == followerProfileId && f.FollowingId == followingProfileId);

            if (existingFollow != null)
            {
                _logger.LogInformation("User {FollowerId} is already following {FollowingId}", followerId, followingId);
                return false;
            }

            var follow = new Follow
            {
                FollowerId = followerProfileId,
                FollowingId = followingProfileId,
                FollowedAt = DateTime.UtcNow
            };

            _context.Follows.Add(follow);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User {FollowerId} started following {FollowingId}", followerId, followingId);
            return true;
        }

        public async Task<bool> UnfollowUserAsync(Guid followerId, Guid followingId)
        {
            var followerProfileId = await GetUserProfileIdAsync(followerId);
            var followingProfileId = await GetUserProfileIdAsync(followingId);

            if (followerProfileId == Guid.Empty || followingProfileId == Guid.Empty)
            {
                _logger.LogWarning("Invalid user profiles for unfollow operation. Follower: {FollowerId}, Following: {FollowingId}",
                    followerId, followingId);
                return false;
            }

            var follow = await _context.Follows
                .FirstOrDefaultAsync(f => f.FollowerId == followerProfileId && f.FollowingId == followingProfileId);

            if (follow == null)
            {
                _logger.LogInformation("User {FollowerId} is not following {FollowingId}", followerId, followingId);
                return false;
            }

            _context.Follows.Remove(follow);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User {FollowerId} unfollowed {FollowingId}", followerId, followingId);
            return true;
        }

        public async Task<bool> IsFollowingAsync(Guid followerId, Guid followingId)
        {
            var followerProfileId = await GetUserProfileIdAsync(followerId);
            var followingProfileId = await GetUserProfileIdAsync(followingId);

            if (followerProfileId == Guid.Empty || followingProfileId == Guid.Empty)
                return false;

            var isFollowing = await _context.Follows
                .AnyAsync(f => f.FollowerId == followerProfileId && f.FollowingId == followingProfileId);

            _logger.LogInformation("User {FollowerId} is {Status} following {FollowingId}",
                followerId, isFollowing ? "" : "not", followingId);
            return isFollowing;
        }

        public async Task<List<FollowDto>> GetFollowersAsync(Guid userId, int page = 1, int pageSize = 20)
        {
            var userProfileId = await GetUserProfileIdAsync(userId);
            if (userProfileId == Guid.Empty)
            {
                _logger.LogWarning("Invalid user profile for getting followers: {UserId}", userId);
                return new List<FollowDto>();
            }

            var followEntities = await _context.Follows
                .Where(f => f.FollowingId == userProfileId)
                .Include(f => f.Follower)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var followers = followEntities.Select(f => f.FollowerToDto()).ToList();

            _logger.LogInformation("Retrieved {Count} followers for user {UserId}", followers.Count, userId);
            return followers;
        }

        public async Task<List<FollowDto>> GetFollowingAsync(Guid userId, int page = 1, int pageSize = 20)
        {
            var userProfileId = await GetUserProfileIdAsync(userId);
            if (userProfileId == Guid.Empty)
            {
                _logger.LogWarning("Invalid user profile for getting following: {UserId}", userId);
                return new List<FollowDto>();
            }

            var followingEntities = await _context.Follows
                .Where(f => f.FollowerId == userProfileId)
                .Include(f => f.Following)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var following = followingEntities.Select(f => f.FollowingToDto()).ToList();

            _logger.LogInformation("Retrieved {Count} following for user {UserId}", following.Count, userId);
            return following;
        }

        public async Task<FollowStatsDto> GetFollowStatsAsync(Guid userId, Guid? currentUserId = null)
        {
            var userProfileId = await GetUserProfileIdAsync(userId);
            if (userProfileId == Guid.Empty)
            {
                return new FollowStatsDto();
            }

            var followersCount = await GetFollowersCountAsync(userId);
            var followingCount = await GetFollowingCountAsync(userId);

            var isFollowing = false;
            if (currentUserId.HasValue && currentUserId.Value != userId)
            {
                isFollowing = await IsFollowingAsync(currentUserId.Value, userId);
            }

            return new FollowStatsDto
            {
                FollowersCount = followersCount,
                FollowingCount = followingCount,
                IsFollowing = isFollowing
            };
        }

        public async Task<int> GetFollowersCountAsync(Guid userId)
        {
            var userProfileId = await GetUserProfileIdAsync(userId);
            if (userProfileId == Guid.Empty)
                return 0;

            var count = await _context.Follows
                .CountAsync(f => f.FollowingId == userProfileId);

            _logger.LogInformation("User {UserId} has {Count} followers", userId, count);
            return count;
        }

        public async Task<int> GetFollowingCountAsync(Guid userId)
        {
            var userProfileId = await GetUserProfileIdAsync(userId);
            if (userProfileId == Guid.Empty)
                return 0;

            var count = await _context.Follows
                .CountAsync(f => f.FollowerId == userProfileId);

            _logger.LogInformation("User {UserId} is following {Count} users", userId, count);
            return count;
        }

        private async Task<Guid> GetUserProfileIdAsync(Guid userId)
        {
            var userProfileId = await _context.UserProfiles
                .Where(u => u.UserId == userId.ToString())
                .Select(u => u.Id)
                .FirstOrDefaultAsync();

            return userProfileId;
        }
    }
}