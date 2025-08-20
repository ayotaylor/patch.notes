using Backend.Models.DTO.Social;
using Backend.Models.DTO.Response;

namespace Backend.Services
{
    public interface IGameListService
    {
        Task<PagedResponse<GameListDto>> GetUserGameListsAsync(Guid userId, int page = 1, int pageSize = 20);
        Task<PagedResponse<GameListDto>> GetPublicGameListsAsync(int page = 1, int pageSize = 20);
        Task<GameListDto?> GetGameListAsync(Guid listId);
        Task<GameListDto?> CreateGameListAsync(Guid userId, string name, string? description = null, bool isPublic = true, List<int>? gameIds = null);
        Task<GameListDto?> UpdateGameListAsync(Guid listId, string name, string? description = null, bool? isPublic = null);
        Task<bool> DeleteGameListAsync(Guid listId);
        Task<bool> AddGameToListAsync(Guid listId, int gameId, string? note = null);
        Task<bool> RemoveGameFromListAsync(Guid listId, int gameId);
        Task<bool> UpdateGameListItemAsync(Guid listId, int gameId, int? order = null, string? note = null);
        Task<bool> IsGameInListAsync(Guid listId, int gameId);
        Task<bool> IsUserListOwnerAsync(Guid listId, Guid userId);
    }
}