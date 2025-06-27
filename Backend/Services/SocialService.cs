using Backend.Data;
using Backend.Mapping;
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

        public async Task<List<FavoriteDto>> GetUserFavoritesAsync(Guid userId, int page = 1, int pageSize = 20)
        {
            var favorites = await _context.Favorites
                .Where(f => f.UserId == userId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(f => new FavoriteDto
                {
                    GameId = f.GameId,
                    UserId = f.UserId,
                    AddedAt = f.AddedAt
                })
                .ToListAsync();

            var games = await _context.Games
                .Where(g => favorites.Select(f => f.GameId).Contains(g.Id))
                .ToListAsync();
            
            var favoritesDto = new List<FavoriteDto>();
            if (favorites != null && favorites.Count > 0)
            {
                foreach (var favorite in favorites)
                {
                    var game = games.FirstOrDefault(g => g.Id == favorite.GameId);
                    if (game != null)
                    {
                        favorite.Game = game.ToDto();
                    }
                    favoritesDto.Add(favorite);
                }

                _logger.LogInformation("Retrieved {Count} favorites for user {UserId}", favoritesDto.Count, userId);
            }
            else
            {
                _logger.LogInformation("No favorites found for user {UserId}", userId);
            }

            return favoritesDto;
        }

        public async Task<bool> AddToFavoritesAsync(Guid userId, Guid gameId)
        {
            var existingFavorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserId == userId && f.GameId == gameId);

            if (existingFavorite != null)
            {
                _logger.LogInformation("Game {GameId} is already in favorites for user {UserId}", gameId, userId);
                return false; // Game is already a favorite
            }

            var favorite = new Favorite
            {
                UserId = userId,
                GameId = gameId,
                AddedAt = DateTime.UtcNow
            };

            _context.Favorites.Add(favorite);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Game {GameId} added to favorites for user {UserId}", gameId, userId);
            return true;
        }

        public async Task<bool> RemoveFromFavoritesAsync(Guid userId, Guid gameId)
        {
            var favorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserId == userId && f.GameId == gameId);

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

        public async Task<bool> IsGameFavoriteAsync(Guid userId, Guid gameId)
        {
            var isFavorite = await _context.Favorites
                .AnyAsync(f => f.UserId == userId && f.GameId == gameId);

            _logger.LogInformation("Game {GameId} is {Status} favorite for user {UserId}", 
                gameId, isFavorite ? "a" : "not a", userId);
            return isFavorite;
        }

        public async Task<List<LikeDto>> GetUserLikesAsync(Guid userId, int page = 1, int pageSize = 20)
        {
            // Implementation for fetching user likes
            var likes = await _context.Likes
                .Where(l => l.UserId == userId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(l => new LikeDto
                {
                    GameId = l.GameId,
                    UserId = l.UserId,
                })
                .ToListAsync();

            var games = await _context.Games
                .Where(g => likes.Select(l => l.GameId).Contains(g.Id))
                .ToListAsync();

            var likesDto = new List<LikeDto>();
            if (likes != null && likes.Count > 0)
            {
                foreach (var like in likes)
                {
                    var game = games.FirstOrDefault(g => g.Id == like.GameId);
                    if (game != null)
                    {
                        like.Game = game.ToDto();
                    }
                    likesDto.Add(like);
                }
            }
            return likesDto;
        }

        public async Task<bool> LikeGameAsync(Guid userId, Guid gameId)
        {
            var existingLike = await _context.Likes
                .FirstOrDefaultAsync(l => l.UserId == userId && l.GameId == gameId);

            if (existingLike != null)
            {
                _logger.LogInformation("Game {GameId} is already liked by user {UserId}", gameId, userId);
                return false; // Game is already liked
            }

            var like = new Like
            {
                UserId = userId,
                GameId = gameId,
            };

            _context.Likes.Add(like);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Game {GameId} liked by user {UserId}", gameId, userId);
            return true;
        }

        public async Task<bool> UnlikeGameAsync(Guid userId, Guid gameId)
        {
            var like = await _context.Likes
                .FirstOrDefaultAsync(l => l.UserId == userId && l.GameId == gameId);

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

        public async Task<bool> IsGameLikedAsync(Guid userId, Guid gameId)
        {
            var isLiked = await _context.Likes
                .AnyAsync(l => l.UserId == userId && l.GameId == gameId);

            _logger.LogInformation("Game {GameId} is {Status} liked by user {UserId}", 
                gameId, isLiked ? "a" : "not a", userId);
            return isLiked;
        }

        public async Task<int> GetGameLikesCountAsync(Guid gameId)
        {
            var likesCount = await _context.Likes
                .CountAsync(l => l.GameId == gameId);

            _logger.LogInformation("Game {GameId} has {Count} likes", gameId, likesCount);
            return likesCount;
        }

        public async Task<int> GetGameFavoritesCountAsync(Guid gameId)
        {
            var favoritesCount = await _context.Favorites
                .CountAsync(f => f.GameId == gameId);

            _logger.LogInformation("Game {GameId} has {Count} favorites", gameId, favoritesCount);
            return favoritesCount;
        }
     }
}