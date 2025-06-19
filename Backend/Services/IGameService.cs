using Backend.Models.DTO.Game;
using Backend.Models.DTO.Response;

namespace Backend.Services
{
    public interface IGameService
    {
        Task<PagedResponse<GameDto>> GetGamesAsync(GameSearchParams searchParams, Guid? userId = null);
        Task<GameDto?> GetGameByIdAsync(Guid id, Guid? userId = null);
        Task<GameDto?> GetGameBySlugAsync(string slug, Guid? userId = null);
        Task<bool> DeleteGameAsync(Guid id);
        Task<List<GameDto>> GetSimilarGamesAsync(Guid gameId, int limit = 10);
        Task<List<GameDto>> GetGamesByFranchiseAsync(Guid franchiseId);
        Task<List<GameDto>> GetPopularGamesAsync(int limit = 20);
        Task<List<GameDto>> GetGamesByGenreAsync(Guid genreId, int limit = 20);
    }
}