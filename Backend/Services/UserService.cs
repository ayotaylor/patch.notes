using Backend.Data;
using Backend.Models.Auth;
using Backend.Models.DTO.Response;
using Backend.Models.DTO.User;
using Backend.Models.Social;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserService> _logger;

        public UserService(ApplicationDbContext context, ILogger<UserService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<PagedResponse<UserDto>> GetAllUsersAsync(int page, int pageSize)
        {
            var query = _context.Users
                .Include(u => u.UserProfile)
                .AsQueryable();

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var userDtos = new List<UserDto>();
            foreach (var user in users)
            {
                var userDto = await MapToUserDtoAsync(user);
                userDtos.Add(userDto);
            }

            _logger.LogInformation("Retrieved {Count} users (page {Page} of {TotalPages})", userDtos.Count, page, totalPages);

            return new PagedResponse<UserDto>
            {
                Data = userDtos,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                HasNextPage = page < totalPages,
                HasPreviousPage = page > 1
            };
        }

        public async Task<List<UserDto>> GetPopularUsersAsync(int limit)
        {
            // Get users with most followers
            var popularUserIds = await _context.Follows
                .GroupBy(f => f.FollowingId)
                .OrderByDescending(g => g.Count())
                .Take(limit)
                .Select(g => g.Key)
                .ToListAsync();

            var users = await _context.Users
                .Include(u => u.UserProfile)
                .Where(u => popularUserIds.Contains(Guid.Parse(u.Id)))
                .ToListAsync();

            var userDtos = new List<UserDto>();
            foreach (var user in users)
            {
                var userDto = await MapToUserDtoAsync(user);
                userDtos.Add(userDto);
            }

            _logger.LogInformation("Retrieved {Count} popular users", userDtos.Count);

            return userDtos;
        }

        public async Task<List<UserDto>> GetFeaturedUsersAsync(int limit)
        {
            // Get users with most reviews
            var featuredUserProfileIds = await _context.Reviews
                .GroupBy(r => r.UserId)
                .OrderByDescending(g => g.Count())
                .Take(limit)
                .Select(g => g.Key)
                .ToListAsync();

            var users = await _context.Users
                .Include(u => u.UserProfile)
                .Where(u => u.UserProfile != null && featuredUserProfileIds.Contains(u.UserProfile.Id))
                .ToListAsync();

            var userDtos = new List<UserDto>();
            foreach (var user in users)
            {
                var userDto = await MapToUserDtoAsync(user);
                userDtos.Add(userDto);
            }

            _logger.LogInformation("Retrieved {Count} featured users", userDtos.Count);

            return userDtos;
        }

        private async Task<UserDto> MapToUserDtoAsync(User user)
        {
            var userProfileId = user.UserProfile?.Id ?? Guid.Empty;

            var reviewsCount = 0;
            var followersCount = 0;
            var followingCount = 0;

            if (userProfileId != Guid.Empty)
            {
                reviewsCount = await _context.Reviews
                    .CountAsync(r => r.UserId == userProfileId);

                followersCount = await _context.Follows
                    .CountAsync(f => f.FollowingId.ToString() == user.Id);

                followingCount = await _context.Follows
                    .CountAsync(f => f.FollowerId.ToString() == user.Id);
            }

            return new UserDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                DisplayName = user.UserProfile?.DisplayName ?? user.UserName ?? "User",
                Email = user.Email,
                ProfileImageUrl = user.UserProfile?.ProfileUrlImageUrl,
                ReviewsCount = reviewsCount,
                FollowersCount = followersCount,
                FollowingCount = followingCount,
                CreatedAt = user.CreatedAt
            };
        }
    }
}
