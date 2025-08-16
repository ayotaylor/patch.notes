using Backend.Data;
using Backend.Mapping;
using Backend.Models.DTO.Social;
using Backend.Models.DTO.Response;
using Backend.Models.Social;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services
{
    public class ReviewService : IReviewService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReviewService> _logger;

        public ReviewService(ApplicationDbContext context, ILogger<ReviewService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<PagedResponse<ReviewDto>> GetGameReviewsAsync(int gameId, int page = 1, int pageSize = 20)
        {
            var gameGuid = await _context.Games
                .Where(g => g.IgdbId == gameId)
                .Select(g => g.Id)
                .FirstOrDefaultAsync();

            if (gameGuid == Guid.Empty)
            {
                return new PagedResponse<ReviewDto>
                {
                    Data = new List<ReviewDto>(),
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = 0,
                    TotalPages = 0,
                    HasNextPage = false,
                    HasPreviousPage = false
                };
            }

            var totalCount = await _context.Reviews
                .Where(r => r.GameId == gameGuid)
                .CountAsync();

            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var reviews = await _context.Reviews
                .Include(r => r.User)
                .Include(r => r.Game).ThenInclude(c => c.Covers)
                .Where(r => r.GameId == gameGuid)
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResponse<ReviewDto>
            {
                Data = reviews.Select(r => r.ToDto()).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                HasNextPage = page < totalPages,
                HasPreviousPage = page > 1
            };
        }

        public async Task<PagedResponse<ReviewDto>> GetUserReviewsAsync(Guid userId, int page = 1, int pageSize = 20)
        {
            var userProfileId = await _context.UserProfiles
                .Where(u => u.UserId == userId.ToString())
                .Select(u => u.Id)
                .FirstOrDefaultAsync();

            if (userProfileId == Guid.Empty)
            {
                return new PagedResponse<ReviewDto>
                {
                    Data = new List<ReviewDto>(),
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = 0,
                    TotalPages = 0,
                    HasNextPage = false,
                    HasPreviousPage = false
                };
            }

            var totalCount = await _context.Reviews
                .Where(r => r.UserId == userProfileId)
                .CountAsync();

            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var reviews = await _context.Reviews
                .Include(r => r.User)
                .Include(r => r.Game).ThenInclude(c => c.Covers)
                .Where(r => r.UserId == userProfileId)
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResponse<ReviewDto>
            {
                Data = reviews.Select(r => r.ToDto()).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                HasNextPage = page < totalPages,
                HasPreviousPage = page > 1
            };
        }

        public async Task<PagedResponse<ReviewDto>> GetLatestReviewsAsync(int page = 1, int pageSize = 20)
        {
            var totalCount = await _context.Reviews.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var reviews = await _context.Reviews
                .Include(r => r.User)
                .Include(r => r.Game).ThenInclude(c => c.Covers)
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResponse<ReviewDto>
            {
                Data = reviews.Select(r => r.ToDto()).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                HasNextPage = page < totalPages,
                HasPreviousPage = page > 1
            };
        }

        public async Task<ReviewDto?> GetReviewAsync(Guid reviewId)
        {
            var review = await _context.Reviews
                .Include(r => r.User)
                .Include(r => r.Game)
                .FirstOrDefaultAsync(r => r.Id == reviewId);

            return review?.ToDto();
        }

        public async Task<ReviewDto?> GetUserReviewForGameAsync(Guid userId, int gameId)
        {
            var userProfileId = await _context.UserProfiles
                .Where(u => u.UserId == userId.ToString())
                .Select(u => u.Id)
                .FirstOrDefaultAsync();

            var gameGuid = await _context.Games
                .Where(g => g.IgdbId == gameId)
                .Select(g => g.Id)
                .FirstOrDefaultAsync();

            if (userProfileId == Guid.Empty || gameGuid == Guid.Empty)
            {
                return null;
            }

            var review = await _context.Reviews
                .Include(r => r.User)
                .Include(r => r.Game).ThenInclude(c => c.Covers)
                .FirstOrDefaultAsync(r => r.UserId == userProfileId && r.GameId == gameGuid);

            return review?.ToDto();
        }

        public async Task<ReviewDto?> CreateReviewAsync(Guid userId, int gameId, int rating, string? reviewText = null)
        {
            var userProfileId = await _context.UserProfiles
                .Where(u => u.UserId == userId.ToString())
                .Select(u => u.Id)
                .FirstOrDefaultAsync();

            var gameGuid = await _context.Games
                .Where(g => g.IgdbId == gameId)
                .Select(g => g.Id)
                .FirstOrDefaultAsync();

            if (userProfileId == Guid.Empty || gameGuid == Guid.Empty)
            {
                return null;
            }

            if (await HasUserReviewedGameAsync(userId, gameId))
            {
                _logger.LogWarning("User {UserId} already has a review for game {GameId}", userId, gameId);
                return null;
            }

            var review = new Review
            {
                UserId = userProfileId,
                GameId = gameGuid,
                Rating = rating,
                ReviewText = reviewText,
                ReviewDate = DateTime.UtcNow
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            return await GetReviewAsync(review.Id);
        }

        public async Task<ReviewDto?> UpdateReviewAsync(Guid reviewId, int rating, string? reviewText = null)
        {
            var review = await _context.Reviews.FindAsync(reviewId);
            if (review == null)
            {
                return null;
            }

            review.Rating = rating;
            review.ReviewText = reviewText;
            review.ReviewDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return await GetReviewAsync(reviewId);
        }

        public async Task<bool> DeleteReviewAsync(Guid reviewId)
        {
            var review = await _context.Reviews.FindAsync(reviewId);
            if (review == null)
            {
                return false;
            }

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<double> GetGameAverageRatingAsync(int gameId)
        {
            var gameGuid = await _context.Games
                .Where(g => g.IgdbId == gameId)
                .Select(g => g.Id)
                .FirstOrDefaultAsync();

            if (gameGuid == Guid.Empty)
            {
                return 0.0;
            }

            var averageRating = await _context.Reviews
                .Where(r => r.GameId == gameGuid)
                .AverageAsync(r => (double?)r.Rating);

            return averageRating ?? 0.0;
        }

        public async Task<int> GetGameReviewsCountAsync(int gameId)
        {
            var gameGuid = await _context.Games
                .Where(g => g.IgdbId == gameId)
                .Select(g => g.Id)
                .FirstOrDefaultAsync();

            if (gameGuid == Guid.Empty)
            {
                return 0;
            }

            return await _context.Reviews
                .Where(r => r.GameId == gameGuid)
                .CountAsync();
        }

        public async Task<bool> HasUserReviewedGameAsync(Guid userId, int gameId)
        {
            var userProfileId = await _context.UserProfiles
                .Where(u => u.UserId == userId.ToString())
                .Select(u => u.Id)
                .FirstOrDefaultAsync();

            var gameGuid = await _context.Games
                .Where(g => g.IgdbId == gameId)
                .Select(g => g.Id)
                .FirstOrDefaultAsync();

            if (userProfileId == Guid.Empty || gameGuid == Guid.Empty)
            {
                return false;
            }

            return await _context.Reviews
                .AnyAsync(r => r.UserId == userProfileId && r.GameId == gameGuid);
        }
    }
}