using Backend.Data;
using Backend.Mapping;
using Backend.Models.DTO.Social;
using Backend.Models.DTO.Response;
using Backend.Models.Social;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services
{
    public class GameListService : IGameListService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<GameListService> _logger;

        public GameListService(ApplicationDbContext context, ILogger<GameListService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<PagedResponse<GameListDto>> GetUserGameListsAsync(Guid userId, int page = 1, int pageSize = 20)
        {
            var userProfileId = await _context.UserProfiles
                .Where(u => u.UserId == userId.ToString())
                .Select(u => u.Id)
                .FirstOrDefaultAsync();

            if (userProfileId == Guid.Empty)
            {
                return new PagedResponse<GameListDto>
                {
                    Data = new List<GameListDto>(),
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = 0,
                    TotalPages = 0,
                    HasNextPage = false,
                    HasPreviousPage = false
                };
            }

            var totalCount = await _context.GameLists
                .Where(gl => gl.UserId == userProfileId)
                .CountAsync();

            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var gameLists = await _context.GameLists
                .Include(gl => gl.User)
                .Include(gl => gl.GameListItems)
                    .ThenInclude(gli => gli.Game)
                    .ThenInclude(g => g.Covers)
                .Include(gl => gl.Comments)
                .Include(gl => gl.Likes)
                .Where(gl => gl.UserId == userProfileId)
                .OrderByDescending(gl => gl.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResponse<GameListDto>
            {
                Data = gameLists.Select(gl => gl.ToDto()).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                HasNextPage = page < totalPages,
                HasPreviousPage = page > 1
            };
        }

        public async Task<PagedResponse<GameListDto>> GetPublicGameListsAsync(int page = 1, int pageSize = 20)
        {
            var totalCount = await _context.GameLists
                .Where(gl => gl.IsPublic)
                .CountAsync();

            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var gameLists = await _context.GameLists
                .Include(gl => gl.User)
                .Include(gl => gl.GameListItems)
                    .ThenInclude(gli => gli.Game)
                    .ThenInclude(g => g.Covers)
                .Include(gl => gl.Comments)
                .Include(gl => gl.Likes)
                .Where(gl => gl.IsPublic)
                .OrderByDescending(gl => gl.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResponse<GameListDto>
            {
                Data = gameLists.Select(gl => gl.ToDto()).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                HasNextPage = page < totalPages,
                HasPreviousPage = page > 1
            };
        }

        public async Task<GameListDto?> GetGameListAsync(Guid listId)
        {
            var gameList = await _context.GameLists
                .Include(gl => gl.User)
                .Include(gl => gl.GameListItems)
                    .ThenInclude(gli => gli.Game)
                    .ThenInclude(g => g.Covers)
                .Include(gl => gl.Comments)
                    .ThenInclude(c => c.User)
                .Include(gl => gl.Likes)
                .FirstOrDefaultAsync(gl => gl.Id == listId);

            return gameList?.ToDto();
        }

        public async Task<GameListDto?> CreateGameListAsync(Guid userId, string name, string? description = null, bool isPublic = true, List<int>? gameIds = null)
        {
            var userProfileId = await _context.UserProfiles
                .Where(u => u.UserId == userId.ToString())
                .Select(u => u.Id)
                .FirstOrDefaultAsync();

            if (userProfileId == Guid.Empty)
            {
                return null;
            }

            var gameList = new GameList
            {
                UserId = userProfileId,
                Name = name,
                Description = description,
                IsPublic = isPublic
            };

            _context.GameLists.Add(gameList);
            await _context.SaveChangesAsync();

            // Add games to the list if provided
            if (gameIds != null && gameIds.Count > 0)
            {
                var gameGuids = await _context.Games
                    .Where(g => gameIds.Contains(g.IgdbId))
                    .Select(g => new { g.Id, g.IgdbId })
                    .ToListAsync();

                var gameListItems = new List<GameListItem>();
                for (int i = 0; i < gameIds.Count; i++)
                {
                    var gameGuid = gameGuids.FirstOrDefault(g => g.IgdbId == gameIds[i])?.Id;
                    if (gameGuid.HasValue)
                    {
                        gameListItems.Add(new GameListItem
                        {
                            GameListId = gameList.Id,
                            GameId = gameGuid.Value,
                            Order = i + 1
                        });
                    }
                }

                if (gameListItems.Count > 0)
                {
                    _context.GameListItems.AddRange(gameListItems);
                    await _context.SaveChangesAsync();
                }
            }

            return await GetGameListAsync(gameList.Id);
        }

        public async Task<GameListDto?> UpdateGameListAsync(Guid listId, string name, string? description = null, bool? isPublic = null)
        {
            var gameList = await _context.GameLists.FindAsync(listId);
            if (gameList == null)
            {
                return null;
            }

            gameList.Name = name;
            if (description != null)
                gameList.Description = description;
            if (isPublic.HasValue)
                gameList.IsPublic = isPublic.Value;

            await _context.SaveChangesAsync();

            return await GetGameListAsync(listId);
        }

        public async Task<bool> DeleteGameListAsync(Guid listId)
        {
            var gameList = await _context.GameLists.FindAsync(listId);
            if (gameList == null)
            {
                return false;
            }

            _context.GameLists.Remove(gameList);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> AddGameToListAsync(Guid listId, int gameId, string? note = null)
        {
            var gameGuid = await _context.Games
                .Where(g => g.IgdbId == gameId)
                .Select(g => g.Id)
                .FirstOrDefaultAsync();

            if (gameGuid == Guid.Empty)
            {
                return false;
            }

            var existingItem = await _context.GameListItems
                .FirstOrDefaultAsync(gli => gli.GameListId == listId && gli.GameId == gameGuid);

            if (existingItem != null)
            {
                return false;
            }

            var maxOrder = await _context.GameListItems
                .Where(gli => gli.GameListId == listId)
                .MaxAsync(gli => (int?)gli.Order) ?? 0;

            var gameListItem = new GameListItem
            {
                GameListId = listId,
                GameId = gameGuid,
                Order = maxOrder + 1,
                Note = note
            };

            _context.GameListItems.Add(gameListItem);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> RemoveGameFromListAsync(Guid listId, int gameId)
        {
            var gameGuid = await _context.Games
                .Where(g => g.IgdbId == gameId)
                .Select(g => g.Id)
                .FirstOrDefaultAsync();

            if (gameGuid == Guid.Empty)
            {
                return false;
            }

            var gameListItem = await _context.GameListItems
                .FirstOrDefaultAsync(gli => gli.GameListId == listId && gli.GameId == gameGuid);

            if (gameListItem == null)
            {
                return false;
            }

            _context.GameListItems.Remove(gameListItem);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UpdateGameListItemAsync(Guid listId, int gameId, int? order = null, string? note = null)
        {
            var gameGuid = await _context.Games
                .Where(g => g.IgdbId == gameId)
                .Select(g => g.Id)
                .FirstOrDefaultAsync();

            if (gameGuid == Guid.Empty)
            {
                return false;
            }

            var gameListItem = await _context.GameListItems
                .FirstOrDefaultAsync(gli => gli.GameListId == listId && gli.GameId == gameGuid);

            if (gameListItem == null)
            {
                return false;
            }

            if (order.HasValue)
                gameListItem.Order = order.Value;
            if (note != null)
                gameListItem.Note = note;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> IsGameInListAsync(Guid listId, int gameId)
        {
            var gameGuid = await _context.Games
                .Where(g => g.IgdbId == gameId)
                .Select(g => g.Id)
                .FirstOrDefaultAsync();

            if (gameGuid == Guid.Empty)
            {
                return false;
            }

            return await _context.GameListItems
                .AnyAsync(gli => gli.GameListId == listId && gli.GameId == gameGuid);
        }

        public async Task<bool> IsUserListOwnerAsync(Guid listId, Guid userId)
        {
            var userProfileId = await _context.UserProfiles
                .Where(u => u.UserId == userId.ToString())
                .Select(u => u.Id)
                .FirstOrDefaultAsync();

            if (userProfileId == Guid.Empty)
            {
                return false;
            }

            return await _context.GameLists
                .AnyAsync(gl => gl.Id == listId && gl.UserId == userProfileId);
        }
    }
}