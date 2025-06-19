using Backend.Models.DTO.Social;

namespace Backend.Services
{
    public interface ISocialService
    {
        // Favorites
        Task<List<FavoriteDto>> GetUserFavoritesAsync(Guid userId, int page = 1, int pageSize = 20);
        Task<bool> AddToFavoritesAsync(Guid userId, Guid gameId);
        Task<bool> RemoveFromFavoritesAsync(Guid userId, Guid gameId);
        Task<bool> IsGameFavoriteAsync(Guid userId, Guid gameId);

        // Likes
        Task<List<LikeDto>> GetUserLikesAsync(Guid userId, int page = 1, int pageSize = 20);
        Task<bool> LikeGameAsync(Guid userId, Guid gameId);
        Task<bool> UnlikeGameAsync(Guid userId, Guid gameId);
        Task<bool> IsGameLikedAsync(Guid userId, Guid gameId);
        Task<int> GetGameLikesCountAsync(Guid gameId);
        Task<int> GetGameFavoritesCountAsync(Guid gameId);
    }
}