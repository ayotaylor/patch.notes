using Backend.Models.DTO.Game;

namespace Backend.Services
{
    public interface ISocialService
    {
        // Favorites
        Task<List<GameDto>> GetUserFavoritesAsync(Guid userId, int page = 1, int pageSize = 20);
        Task<bool> AddToFavoritesAsync(Guid userId, int gameId);
        Task<bool> RemoveFromFavoritesAsync(Guid userId, int gameId);
        Task<bool> IsGameFavoriteAsync(Guid userId, int gameId);

        // Likes
        Task<List<GameDto>> GetUserLikesAsync(Guid userId, int page = 1, int pageSize = 20);
        Task<bool> LikeGameAsync(Guid userId, int gameId);
        Task<bool> UnlikeGameAsync(Guid userId, int gameId);
        Task<bool> IsGameLikedAsync(Guid userId, int gameId);
        Task<int> GetGameLikesCountAsync(int gameId);
        Task<int> GetGameFavoritesCountAsync(int gameId);
    }
}