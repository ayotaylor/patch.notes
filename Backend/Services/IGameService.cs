using Backend.Models.DTO.Game;
using Backend.Models.DTO.Request;
using Backend.Models.DTO.Response;

namespace Backend.Services
{
    public interface IGameService
    {
        Task<PagedResponse<GameDto>> GetGamesAsync(GameSearchParams searchParams, Guid? userId = null);
        Task<GameDto?> GetGameByIdAsync(int id, Guid? userId = null);
        Task<GameDto?> GetGameBySlugAsync(string slug, Guid? userId = null);
        Task<bool> DeleteGameAsync(int id);
        Task<List<GameDto>> GetSimilarGamesAsync(int gameId, int limit = 10);
        Task<List<GameDto>> GetGamesByFranchiseAsync(int franchiseId);
        Task<List<GameDto>> GetPopularGamesAsync(int limit = 20);
        Task<List<GameDto>> GetNewGamesAsync(int limit = 20);
        Task<List<GameDto>> GetGamesByGenreAsync(int genreId, int limit = 20);
    }
}