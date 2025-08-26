using Backend.Data;
using Backend.Models.DTO.Recommendation;
using Backend.Models.Social;
using Backend.Services.Recommendation.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services.Recommendation
{
    public class UserPreferenceService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmbeddingService _embeddingService;
        private readonly ILogger<UserPreferenceService> _logger;

        public UserPreferenceService(
            ApplicationDbContext context,
            IEmbeddingService embeddingService,
            ILogger<UserPreferenceService> logger)
        {
            _context = context;
            _embeddingService = embeddingService;
            _logger = logger;
        }

        public async Task<UserPreferenceInput> GetUserPreferenceDataAsync(Guid userId)
        {
            try
            {
                _logger.LogInformation("Gathering preference data for user: {UserId}", userId);

                // Get user's favorite games
                var userFavorites = await GetUserFavoriteGamesAsync(userId);

                // Get user's liked games
                var userLikedGames = await GetUserLikedGamesAsync(userId);

                // Get texts from reviews the user has liked
                var likedReviewTexts = await GetUserLikedReviewTextsAsync(userId);

                // Get descriptions from game lists the user has liked
                var likedGameListDescriptions = await GetUserLikedGameListDescriptionsAsync(userId);

                // Get favorite games from users this user follows
                var followedUsersFavorites = await GetFollowedUsersFavoriteGamesAsync(userId);

                return new UserPreferenceInput
                {
                    FavoriteGames = userFavorites,
                    LikedGames = userLikedGames,
                    LikedReviewTexts = likedReviewTexts,
                    LikedGameListDescriptions = likedGameListDescriptions,
                    FollowedUsersFavorites = followedUsersFavorites
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error gathering user preference data for user: {UserId}", userId);
                return new UserPreferenceInput();
            }
        }

        private async Task<List<GameEmbeddingInput>> GetUserFavoriteGamesAsync(Guid userId)
        {
            var favorites = await _context.Favorites
                .Where(f => f.UserId == userId)
                .Include(f => f.Game)
                    .ThenInclude(g => g.GameGenres)
                    .ThenInclude(gg => gg.Genre)
                .Include(f => f.Game)
                    .ThenInclude(g => g.GamePlatforms)
                    .ThenInclude(gp => gp.Platform)
                .Include(f => f.Game)
                    .ThenInclude(g => g.GameModes)
                    .ThenInclude(gm => gm.GameMode)
                .Include(f => f.Game)
                    .ThenInclude(g => g.GamePlayerPerspectives)
                    .ThenInclude(gpp => gpp.PlayerPerspective)
                .OrderByDescending(f => f.AddedAt)
                .Take(20) // Limit to recent favorites
                .ToListAsync();

            return favorites.Select(f => MapGameToEmbeddingInput(f.Game)).ToList();
        }

        private async Task<List<GameEmbeddingInput>> GetUserLikedGamesAsync(Guid userId)
        {
            var likedGames = await _context.Likes
                .Where(l => l.UserId == userId)
                .Include(l => l.Game)
                    .ThenInclude(g => g.GameGenres)
                    .ThenInclude(gg => gg.Genre)
                .Include(l => l.Game)
                    .ThenInclude(g => g.GamePlatforms)
                    .ThenInclude(gp => gp.Platform)
                .Include(l => l.Game)
                    .ThenInclude(g => g.GameModes)
                    .ThenInclude(gm => gm.GameMode)
                .Include(l => l.Game)
                    .ThenInclude(g => g.GamePlayerPerspectives)
                    .ThenInclude(gpp => gpp.PlayerPerspective)
                .OrderByDescending(l => l.CreatedAt)
                .Take(20)
                .ToListAsync();

            return likedGames.Select(l => MapGameToEmbeddingInput(l.Game)).ToList();
        }

        private async Task<List<string>> GetUserLikedReviewTextsAsync(Guid userId)
        {
            // Get reviews that the user has liked (ReviewLike table)
            var likedReviews = await _context.ReviewLikes
                .Where(rl => rl.UserId == userId)
                .Include(rl => rl.Review)
                .Where(rl => !string.IsNullOrEmpty(rl.Review.ReviewText))
                .OrderByDescending(rl => rl.CreatedAt)
                .Take(50) // Get more review texts as they're shorter
                .Select(rl => rl.Review.ReviewText!)
                .ToListAsync();

            return likedReviews;
        }

        private async Task<List<string>> GetUserLikedGameListDescriptionsAsync(Guid userId)
        {
            // Get game lists that the user has liked
            var likedGameLists = await _context.GameListLikes
                .Where(gll => gll.UserId == userId)
                .Include(gll => gll.GameList)
                .Where(gll => !string.IsNullOrEmpty(gll.GameList.Description))
                .OrderByDescending(gll => gll.CreatedAt)
                .Take(30)
                .Select(gll => gll.GameList.Description!)
                .ToListAsync();

            return likedGameLists;
        }

        private async Task<List<GameEmbeddingInput>> GetFollowedUsersFavoriteGamesAsync(Guid userId)
        {
            // Get games favorited by users this user follows
            var followedUsersFavorites = await _context.Follows
                .Where(f => f.FollowerId == userId)
                .SelectMany(f => _context.Favorites.Where(fav => fav.UserId == f.FollowingId))
                .Include(fav => fav.Game)
                    .ThenInclude(g => g.GameGenres)
                    .ThenInclude(gg => gg.Genre)
                .Include(fav => fav.Game)
                    .ThenInclude(g => g.GamePlatforms)
                    .ThenInclude(gp => gp.Platform)
                .Include(fav => fav.Game)
                    .ThenInclude(g => g.GameModes)
                    .ThenInclude(gm => gm.GameMode)
                .Include(fav => fav.Game)
                    .ThenInclude(g => g.GamePlayerPerspectives)
                    .ThenInclude(gpp => gpp.PlayerPerspective)
                .OrderByDescending(fav => fav.AddedAt)
                .Take(50) // Get more from followed users
                .ToListAsync();

            return followedUsersFavorites.Select(fav => MapGameToEmbeddingInput(fav.Game)).ToList();
        }

        public async Task<Dictionary<Guid, UserActivityMatch>> GetUserActivityMatchesAsync(Guid userId, List<Guid> gameIds)
        {
            var matches = new Dictionary<Guid, UserActivityMatch>();

            // Get user's favorites, likes, and followed users' activities for these specific games
            var userFavorites = await _context.Favorites
                .Where(f => f.UserId == userId && gameIds.Contains(f.GameId))
                .Select(f => f.GameId)
                .ToHashSetAsync();

            var userLikes = await _context.Likes
                .Where(l => l.UserId == userId && gameIds.Contains(l.GameId))
                .Select(l => l.GameId)
                .ToHashSetAsync();

            // Get followed users who have favorited these games
            var followedUsersFavorites = await _context.Follows
                .Where(f => f.FollowerId == userId)
                .SelectMany(f => _context.Favorites.Where(fav => fav.UserId == f.FollowingId && gameIds.Contains(fav.GameId)))
                .Include(fav => fav.User)
                .GroupBy(fav => fav.GameId)
                .ToDictionaryAsync(
                    g => g.Key,
                    g => g.Select(fav => fav.User.DisplayName ?? "Unknown").ToList()  // TODO: Handle null display names appropriately, maybe use userid or something else
                );

            foreach (var gameId in gameIds)
            {
                matches[gameId] = new UserActivityMatch
                {
                    IsUserFavorite = userFavorites.Contains(gameId),
                    IsUserLiked = userLikes.Contains(gameId),
                    IsFromFollowedUsers = followedUsersFavorites.ContainsKey(gameId),
                    FollowedUsersWhoLiked = followedUsersFavorites.GetValueOrDefault(gameId, new List<string>())
                };
            }

            return matches;
        }

        public async Task<List<GameEmbeddingInput>> GetSimilarGamesFromUserActivityAsync(Guid userId, string activityType)
        {
            return activityType.ToLower() switch
            {
                "favorites" => await GetUserFavoriteGamesAsync(userId),
                "likes" => await GetUserLikedGamesAsync(userId),
                "followed_favorites" => await GetFollowedUsersFavoriteGamesAsync(userId),
                _ => new List<GameEmbeddingInput>()
            };
        }

        private GameEmbeddingInput MapGameToEmbeddingInput(Backend.Models.Game.Game game)
        {
            return new GameEmbeddingInput
            {
                Name = game.Name,
                Summary = game.Summary ?? "",
                Storyline = game.Storyline,
                Genres = game.GameGenres?.Select(gg => gg.Genre.Name).ToList() ?? new List<string>(),
                Platforms = game.GamePlatforms?.Select(gp => gp.Platform.Name).ToList() ?? new List<string>(),
                GameModes = game.GameModes?.Select(gm => gm.GameMode.Name).ToList() ?? new List<string>(),
                PlayerPerspectives = game.GamePlayerPerspectives?.Select(gpp => gpp.PlayerPerspective.Name).ToList() ?? new List<string>(),
                Rating = game.Rating,
                ReleaseDate = game.FirstReleaseDate.HasValue ? DateTimeOffset.FromUnixTimeSeconds(game.FirstReleaseDate.Value).DateTime : null
            };
        }
    }
}