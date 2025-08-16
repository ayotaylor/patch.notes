using Backend.Data;
using Backend.Mapping;
using Backend.Models.DTO.Game;
using Backend.Models.DTO.Social;
using Backend.Models.Social;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services
{
    public class SocialService : ISocialService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SocialService> _logger;

        public SocialService(ApplicationDbContext context, ILogger<SocialService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<GameDto>> GetUserFavoritesAsync(Guid userId, int page = 1, int pageSize = 5)
        {
            var userProfileId = await _context.UserProfiles
                .Where(u => u.UserId == userId.ToString())
                .Select(u => u.Id)
                .FirstOrDefaultAsync();

            var favoriteGameIds = await _context.Favorites
                .Where(f => f.UserId == userProfileId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(f => f.GameId)
                .ToListAsync();

            var games = await _context.Games
                .Where(g => favoriteGameIds.Contains(g.Id))
                .Include(c => c.Covers)
                .ToListAsync();

            var favoritesDto = new List<GameDto>();
            if (games != null && games.Count > 0)
            {
                foreach (var game in games)
                {
                    favoritesDto.Add(game.ToDto());
                }

                _logger.LogInformation("Retrieved {Count} favorites for user {UserId}", favoritesDto.Count, userId);
            }
            else
            {
                _logger.LogInformation("No favorites found for user {UserId}", userId);
            }

            return favoritesDto;
        }

        public async Task<bool> AddToFavoritesAsync(Guid userId, int gameId)
        {
            var favoriteGameId = await _context.Games
                .Where(g => g.IgdbId == gameId)
                .Select(g => g.Id)
                .FirstOrDefaultAsync();

            var userProfileId = await _context.UserProfiles
                .Where(u => u.UserId == userId.ToString())
                .Select(u => u.Id)
                .FirstOrDefaultAsync();

            var existingFavorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserId == userProfileId && f.GameId == favoriteGameId);

            if (existingFavorite != null)
            {
                _logger.LogInformation("Game {GameId} is already in favorites for user {UserId}", gameId, userId);
                return false; // Game is already a favorite
            }

            var favorite = new Favorite
            {
                UserId = userProfileId,
                GameId = favoriteGameId,
                AddedAt = DateTime.UtcNow
            };

            _context.Favorites.Add(favorite);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Game {GameId} added to favorites for user {UserId}", gameId, userId);
            return true;
        }

        public async Task<bool> RemoveFromFavoritesAsync(Guid userId, int gameId)
        {
            var favoriteGameId = await _context.Games
                .Where(g => g.IgdbId == gameId)
                .Select(g => g.Id)
                .FirstOrDefaultAsync();

            var userProfileId = await _context.UserProfiles
                .Where(u => u.UserId == userId.ToString())
                .Select(u => u.Id)
                .FirstOrDefaultAsync();

            var favorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserId == userProfileId && f.GameId == favoriteGameId);

            if (favorite == null)
            {
                _logger.LogInformation("Game {GameId} is not in favorites for user {UserId}", gameId, userId);
                return false; // Game is not a favorite
            }

            _context.Favorites.Remove(favorite);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Game {GameId} removed from favorites for user {UserId}", gameId, userId);
            return true;
        }

        public async Task<bool> IsGameFavoriteAsync(Guid userId, int gameId)
        {
            var favoriteGameId = await _context.Games
                .Where(g => g.IgdbId == gameId)
                .Select(g => g.Id)
                .FirstOrDefaultAsync();

            var userProfileId = await _context.UserProfiles
                .Where(u => u.UserId == userId.ToString())
                .Select(u => u.Id)
                .FirstOrDefaultAsync();

            var isFavorite = await _context.Favorites
                .AnyAsync(f => f.UserId == userProfileId && f.GameId == favoriteGameId);

            _logger.LogInformation("Game {GameId} is {Status} favorite for user {UserId}",
                gameId, isFavorite ? "a" : "not a", userId);
            return isFavorite;
        }

        public async Task<List<GameDto>> GetUserLikesAsync(Guid userId, int page = 1, int pageSize = 20)
        {
            var userProfileId = await _context.UserProfiles
                .Where(u => u.UserId == userId.ToString())
                .Select(u => u.Id)
                .FirstOrDefaultAsync();

            var likedGameIds = await _context.Likes
                .Where(f => f.UserId == userProfileId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(f => f.GameId)
                .ToListAsync();

            var games = await _context.Games
                .Where(g => likedGameIds.Contains(g.Id))
                .ToListAsync();

            var gameLikes = new List<GameDto>();
            if (games != null && games.Count > 0)
            {
                foreach (var game in games)
                {
                    gameLikes.Add(game.ToDto());
                }
                _logger.LogInformation("Retrieved {Count} favorites for user {UserId}", gameLikes.Count, userId);
            }
            else
            {
                _logger.LogInformation("No favorites found for user {UserId}", userId);
            }

            return gameLikes;
        }

        public async Task<bool> LikeGameAsync(Guid userId, int gameId)
        {
            var likedGameId = await _context.Games
                .Where(g => g.IgdbId == gameId)
                .Select(g => g.Id)
                .FirstOrDefaultAsync();

            var userProfileId = await _context.UserProfiles
                .Where(u => u.UserId == userId.ToString())
                .Select(u => u.Id)
                .FirstOrDefaultAsync();

            var existingLike = await _context.Likes
                .FirstOrDefaultAsync(l => l.UserId == userProfileId && l.GameId == likedGameId);

            if (existingLike != null)
            {
                _logger.LogInformation("Game {GameId} is already liked by user {UserId}", gameId, userId);
                return false; // Game is already liked
            }

            var like = new Like
            {
                UserId = userProfileId,
                GameId = likedGameId,
            };

            _context.Likes.Add(like);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Game {GameId} liked by user {UserId}", gameId, userId);
            return true;
        }

        public async Task<bool> UnlikeGameAsync(Guid userId, int gameId)
        {
            var likedGameId = await _context.Games
                .Where(g => g.IgdbId == gameId)
                .Select(g => g.Id)
                .FirstOrDefaultAsync();

            var userProfileId = await _context.UserProfiles
                .Where(u => u.UserId == userId.ToString())
                .Select(u => u.Id)
                .FirstOrDefaultAsync();

            var like = await _context.Likes
                .FirstOrDefaultAsync(l => l.UserId == userProfileId && l.GameId == likedGameId);

            if (like == null)
            {
                _logger.LogInformation("Game {GameId} is not liked by user {UserId}", gameId, userId);
                return false; // Game is not liked
            }

            _context.Likes.Remove(like);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Game {GameId} unliked by user {UserId}", gameId, userId);
            return true;
        }

        public async Task<bool> IsGameLikedAsync(Guid userId, int gameId)
        {
            var likedGameId = await _context.Games
                .Where(g => g.IgdbId == gameId)
                .Select(g => g.Id)
                .FirstOrDefaultAsync();

            var userProfileId = await _context.UserProfiles
                .Where(u => u.UserId == userId.ToString())
                .Select(u => u.Id)
                .FirstOrDefaultAsync();

            var isLiked = await _context.Likes
                .AnyAsync(l => l.UserId == userProfileId && l.GameId == likedGameId);

            _logger.LogInformation("Game {GameId} is {Status} liked by user {UserId}",
                gameId, isLiked ? "a" : "not a", userId);
            return isLiked;
        }

        public async Task<int> GetGameLikesCountAsync(int gameId)
        {
            var likedGameId = await _context.Games
                .Where(g => g.IgdbId == gameId)
                .Select(g => g.Id)
                .FirstOrDefaultAsync();

            var likesCount = await _context.Likes
                .CountAsync(l => l.GameId == likedGameId);

            _logger.LogInformation("Game {GameId} has {Count} likes", gameId, likesCount);
            return likesCount;
        }

        public async Task<int> GetGameFavoritesCountAsync(int gameId)
        {
            var favoriteGameId = await _context.Games
                .Where(g => g.IgdbId == gameId)
                .Select(g => g.Id)
                .FirstOrDefaultAsync();

            var favoritesCount = await _context.Favorites
                .CountAsync(f => f.GameId == favoriteGameId);

            _logger.LogInformation("Game {GameId} has {Count} favorites", gameId, favoritesCount);
            return favoritesCount;
        }

        // Review Likes
        public async Task<bool> LikeReviewAsync(Guid userId, Guid reviewId)
        {
            var userProfileId = await _context.UserProfiles
                .Where(u => u.UserId == userId.ToString())
                .Select(u => u.Id)
                .FirstOrDefaultAsync();

            if (userProfileId == Guid.Empty)
                return false;

            var existingLike = await _context.ReviewLikes
                .FirstOrDefaultAsync(rl => rl.UserId == userProfileId && rl.ReviewId == reviewId);

            if (existingLike != null)
                return false;

            var reviewLike = new ReviewLike
            {
                UserId = userProfileId,
                ReviewId = reviewId
            };

            _context.ReviewLikes.Add(reviewLike);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UnlikeReviewAsync(Guid userId, Guid reviewId)
        {
            var userProfileId = await _context.UserProfiles
                .Where(u => u.UserId == userId.ToString())
                .Select(u => u.Id)
                .FirstOrDefaultAsync();

            if (userProfileId == Guid.Empty)
                return false;

            var reviewLike = await _context.ReviewLikes
                .FirstOrDefaultAsync(rl => rl.UserId == userProfileId && rl.ReviewId == reviewId);

            if (reviewLike == null)
                return false;

            _context.ReviewLikes.Remove(reviewLike);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsReviewLikedAsync(Guid userId, Guid reviewId)
        {
            var userProfileId = await _context.UserProfiles
                .Where(u => u.UserId == userId.ToString())
                .Select(u => u.Id)
                .FirstOrDefaultAsync();

            if (userProfileId == Guid.Empty)
                return false;

            return await _context.ReviewLikes
                .AnyAsync(rl => rl.UserId == userProfileId && rl.ReviewId == reviewId);
        }

        public async Task<int> GetReviewLikesCountAsync(Guid reviewId)
        {
            return await _context.ReviewLikes
                .CountAsync(rl => rl.ReviewId == reviewId);
        }

        // GameList Likes
        public async Task<bool> LikeGameListAsync(Guid userId, Guid gameListId)
        {
            var userProfileId = await _context.UserProfiles
                .Where(u => u.UserId == userId.ToString())
                .Select(u => u.Id)
                .FirstOrDefaultAsync();

            if (userProfileId == Guid.Empty)
                return false;

            var existingLike = await _context.GameListLikes
                .FirstOrDefaultAsync(gll => gll.UserId == userProfileId && gll.GameListId == gameListId);

            if (existingLike != null)
                return false;

            var gameListLike = new GameListLike
            {
                UserId = userProfileId,
                GameListId = gameListId
            };

            _context.GameListLikes.Add(gameListLike);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UnlikeGameListAsync(Guid userId, Guid gameListId)
        {
            var userProfileId = await _context.UserProfiles
                .Where(u => u.UserId == userId.ToString())
                .Select(u => u.Id)
                .FirstOrDefaultAsync();

            if (userProfileId == Guid.Empty)
                return false;

            var gameListLike = await _context.GameListLikes
                .FirstOrDefaultAsync(gll => gll.UserId == userProfileId && gll.GameListId == gameListId);

            if (gameListLike == null)
                return false;

            _context.GameListLikes.Remove(gameListLike);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsGameListLikedAsync(Guid userId, Guid gameListId)
        {
            var userProfileId = await _context.UserProfiles
                .Where(u => u.UserId == userId.ToString())
                .Select(u => u.Id)
                .FirstOrDefaultAsync();

            if (userProfileId == Guid.Empty)
                return false;

            return await _context.GameListLikes
                .AnyAsync(gll => gll.UserId == userProfileId && gll.GameListId == gameListId);
        }

        public async Task<int> GetGameListLikesCountAsync(Guid gameListId)
        {
            return await _context.GameListLikes
                .CountAsync(gll => gll.GameListId == gameListId);
        }

        // Comment Likes
        public async Task<bool> LikeCommentAsync(Guid userId, Guid commentId)
        {
            var userProfileId = await _context.UserProfiles
                .Where(u => u.UserId == userId.ToString())
                .Select(u => u.Id)
                .FirstOrDefaultAsync();

            if (userProfileId == Guid.Empty)
                return false;

            var existingLike = await _context.CommentLikes
                .FirstOrDefaultAsync(cl => cl.UserId == userProfileId && cl.CommentId == commentId);

            if (existingLike != null)
                return false;

            var commentLike = new CommentLike
            {
                UserId = userProfileId,
                CommentId = commentId
            };

            _context.CommentLikes.Add(commentLike);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UnlikeCommentAsync(Guid userId, Guid commentId)
        {
            var userProfileId = await _context.UserProfiles
                .Where(u => u.UserId == userId.ToString())
                .Select(u => u.Id)
                .FirstOrDefaultAsync();

            if (userProfileId == Guid.Empty)
                return false;

            var commentLike = await _context.CommentLikes
                .FirstOrDefaultAsync(cl => cl.UserId == userProfileId && cl.CommentId == commentId);

            if (commentLike == null)
                return false;

            _context.CommentLikes.Remove(commentLike);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsCommentLikedAsync(Guid userId, Guid commentId)
        {
            var userProfileId = await _context.UserProfiles
                .Where(u => u.UserId == userId.ToString())
                .Select(u => u.Id)
                .FirstOrDefaultAsync();

            if (userProfileId == Guid.Empty)
                return false;

            return await _context.CommentLikes
                .AnyAsync(cl => cl.UserId == userProfileId && cl.CommentId == commentId);
        }

        public async Task<int> GetCommentLikesCountAsync(Guid commentId)
        {
            return await _context.CommentLikes
                .CountAsync(cl => cl.CommentId == commentId);
        }
     }
}