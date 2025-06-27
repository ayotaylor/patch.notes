using Backend.Data;
using Backend.Mapping;
using Backend.Models.DTO.Game;
using Backend.Models.DTO.Response;
using Microsoft.EntityFrameworkCore;

// TODO: refactor class to remove repeated code and improve readability
// TODO: refactor this service to use a repository pattern or 
// similar for better separation of concerns
namespace Backend.Services
{
    public class GameService : IGameService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<GameService> _logger;

        public GameService(ApplicationDbContext context, ILogger<GameService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<PagedResponse<GameDto>> GetGamesAsync(
            GameSearchParams searchParams, Guid? userId = null)
        {
            var query = _context.Games.AsQueryable();

            // Apply search filters
            // search by name or summary TODO: maybe remove summary from search
            if (!string.IsNullOrEmpty(searchParams.Search))
            {
                query = query.Where(g => g.Name.Contains(searchParams.Search)
                                       || g.Summary != null
                                       && g.Summary.Contains(searchParams.Search));
            }

            if (searchParams.GenreIds?.Count > 0)
            {
                query = query.Where(g => g.GameGenres.Any(gg => searchParams.GenreIds.Contains(gg.GenreId)));
            }

            if (searchParams.PlatformIds?.Count > 0)
            {
                query = query.Where(g => g.ReleaseDates.Any(gp => searchParams.PlatformIds.Contains(gp.PlatformId)));
            }

            if (searchParams.MinRating.HasValue)
            {
                query = query.Where(g => g.Rating >= searchParams.MinRating);
            }

            if (searchParams.MaxRating.HasValue)
            {
                query = query.Where(g => g.Rating <= searchParams.MaxRating);
            }

            // Apply sorting
            query = searchParams.SortBy?.ToLower() switch
            {
                "rating" => searchParams.SortOrder?.ToLower() == "desc" ?
                    query.OrderByDescending(g => g.Rating) : query.OrderBy(g => g.Rating),
                "hypes" => searchParams.SortOrder?.ToLower() == "desc" ?
                    query.OrderByDescending(g => g.Hypes) : query.OrderBy(g => g.Hypes),
                "release_date" => searchParams.SortOrder?.ToLower() == "desc" ?
                    query.OrderByDescending(g => g.FirstReleaseDate) : query.OrderBy(g => g.FirstReleaseDate),
                _ => searchParams.SortOrder?.ToLower() == "desc" ?
                    query.OrderByDescending(g => g.Name) : query.OrderBy(g => g.Name)
            };

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)searchParams.PageSize);

            var games = await query
                .Include(g => g.GameGenres).ThenInclude(gg => gg.Genre)
                .Include(g => g.GameAgeRatings).ThenInclude(gar => gar.AgeRating)
                    .ThenInclude(ar => ar.AgeRatingCategory)
                    .ThenInclude(arc => arc.RatingOrganization)
                .Include(g => g.AltNames)
                .Include(g => g.Covers)
                .Include(g => g.Screenshots)
                .Include(g => g.ReleaseDates).ThenInclude(rd => rd.Platform)
                .Include(g => g.ReleaseDates).ThenInclude(rd => rd.ReleaseDateRegion)
                .Include(g => g.GameFranchises).ThenInclude(gf => gf.Franchise)
                .Include(g => g.GameModes).ThenInclude(gmg => gmg.GameMode)
                .Include(g => g.GameType)//.ThenInclude(gtg => gtg.GameType)
                .Include(g => g.GameCompanies).ThenInclude(gc => gc.Company)
                .Include(g => g.GamePlayerPerspectives).ThenInclude(gpp => gpp.PlayerPerspective)
                .Skip((searchParams.Page - 1) * searchParams.PageSize)
                .Take(searchParams.PageSize)
                .ToListAsync();

            var gameDtos = new List<GameDto>();
            if (games != null && games.Count > 0)
            {
                var gameIds = games.Select(g => g.Id).ToList();
                var userLikes = userId.HasValue
                    ? await _context.Likes
                        .Where(l => l.UserId == userId && gameIds.Contains(l.GameId))
                        .Select(l => l.GameId)
                        .ToListAsync()
                    : new List<Guid>();

                var userFavorites = userId.HasValue
                    ? await _context.Favorites
                        .Where(f => f.UserId == userId && gameIds.Contains(f.GameId))
                        .Select(f => f.GameId)
                        .ToListAsync()
                    : new List<Guid>();

                var likesCount = await _context.Likes
                    .Where(l => gameIds.Contains(l.GameId))
                    .GroupBy(l => l.GameId)
                    .Select(g => new { GameId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(g => g.GameId, g => g.Count);

                var favoritesCount = await _context.Favorites
                    .Where(f => gameIds.Contains(f.GameId))
                    .GroupBy(f => f.GameId)
                    .Select(g => new { GameId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(g => g.GameId, g => g.Count);

                foreach (var game in games)
                {
                    var gameDto = GameMapper.ToDto(game);
                    gameDto.IsLikedByUser = userLikes.Contains(game.Id);
                    gameDto.IsFavoriteByUser = userFavorites.Contains(game.Id);
                    gameDto.LikesCount = likesCount.TryGetValue(game.Id, out var likeCount) ? likeCount : 0;
                    gameDto.FavoritesCount = favoritesCount.TryGetValue(game.Id, out var favoriteCount) ? favoriteCount : 0;
                    gameDtos.Add(gameDto);
                }
            }

            return new PagedResponse<GameDto>
            {
                Data = gameDtos,
                Page = searchParams.Page,
                PageSize = searchParams.PageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                HasNextPage = searchParams.Page < totalPages,
                HasPreviousPage = searchParams.Page > 1
            };
        }

        public async Task<GameDto?> GetGameByIdAsync(Guid id, Guid? userId = null)
        {
            var game = await _context.Games
                .Include(g => g.GameGenres).ThenInclude(gg => gg.Genre)
                .Include(g => g.GameAgeRatings).ThenInclude(ar => ar.AgeRating)
                    .ThenInclude(ar => ar.AgeRatingCategory)
                    .ThenInclude(arc => arc.RatingOrganization)
                .Include(g => g.AltNames)
                .Include(g => g.Covers)
                .Include(g => g.Screenshots)
                .Include(g => g.ReleaseDates).ThenInclude(rd => rd.Platform)
                .Include(g => g.ReleaseDates).ThenInclude(rd => rd.ReleaseDateRegion)
                .Include(g => g.GameFranchises).ThenInclude(gf => gf.Franchise)
                .Include(g => g.GameModes).ThenInclude(gmg => gmg.GameMode)
                .Include(g => g.GameType)//.ThenInclude(gtg => gtg.GameType)
                .Include(g => g.GameCompanies).ThenInclude(gc => gc.Company)
                .Include(g => g.GamePlayerPerspectives)
                    .ThenInclude(gpp => gpp.PlayerPerspective)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (game == null) return null;

            var gameDto = GameMapper.ToDto(game);

            if (userId.HasValue)
            {
                gameDto.IsLikedByUser = await _context.Likes
                    .AnyAsync(l => l.UserId == userId && l.GameId == game.Id);
                gameDto.IsFavoriteByUser = await _context.Favorites
                    .AnyAsync(f => f.UserId == userId && f.GameId == game.Id);
            }

            gameDto.LikesCount = await _context.Likes.CountAsync(l => l.GameId == game.Id);
            gameDto.FavoritesCount = await _context.Favorites.CountAsync(f => f.GameId == game.Id);

            return gameDto;
        }

        public async Task<GameDto?> GetGameBySlugAsync(string slug, Guid? userId = null)
        {
            var game = await _context.Games
                .Include(g => g.GameGenres).ThenInclude(gg => gg.Genre)
                .Include(g => g.GameAgeRatings).ThenInclude(ar => ar.AgeRating)
                    .ThenInclude(ar => ar.AgeRatingCategory)
                    .ThenInclude(arc => arc.RatingOrganization)
                .Include(g => g.AltNames)
                .Include(g => g.Covers)
                .Include(g => g.Screenshots)
                .Include(g => g.ReleaseDates).ThenInclude(rd => rd.Platform)
                .Include(g => g.ReleaseDates).ThenInclude(rd => rd.ReleaseDateRegion)
                .Include(g => g.GameFranchises).ThenInclude(gf => gf.Franchise)
                .Include(g => g.GameModes).ThenInclude(gmg => gmg.GameMode)
                .Include(g => g.GameType)//.ThenInclude(gtg => gtg.GameType)
                .Include(g => g.GameCompanies).ThenInclude(gc => gc.Company)
                .Include(g => g.GamePlayerPerspectives).ThenInclude(gpp => gpp.PlayerPerspective)
                .FirstOrDefaultAsync(g => g.Slug == slug);

            if (game == null) return null;

            var gameDto = GameMapper.ToDto(game);

            if (userId.HasValue)
            {
                gameDto.IsLikedByUser = await _context.Likes
                    .AnyAsync(l => l.UserId == userId && l.GameId == game.Id);
                gameDto.IsFavoriteByUser = await _context.Favorites
                    .AnyAsync(f => f.UserId == userId && f.GameId == game.Id);
            }

            gameDto.LikesCount = await _context.Likes.CountAsync(l => l.GameId == game.Id);
            gameDto.FavoritesCount = await _context.Favorites.CountAsync(f => f.GameId == game.Id);

            return gameDto;
        }

        // public async Task<GameDto> CreateGameAsync(CreateGameDto createGameDto)
        // {
        //     var game = _mapper.Map<Game>(createGameDto);

        //     _context.Games.Add(game);

        //     // Add genre relationships
        //     foreach (var genreId in createGameDto.GenreIds)
        //     {
        //         _context.GameGenres.Add(new GameGenre { GameId = game.Id, GenreId = genreId });
        //     }

        //     // Add franchise relationships
        //     foreach (var franchiseId in createGameDto.FranchiseIds)
        //     {
        //         _context.GameFranchises.Add(new GameFranchise { GameId = game.Id, FranchiseId = franchiseId });
        //     }

        //     // Add game mode relationships
        //     foreach (var gameModeId in createGameDto.GameModeIds)
        //     {
        //         _context.GameModeGames.Add(new GameModeGame { GameId = game.Id, GameModeId = gameModeId });
        //     }

        //     // Add game type relationships
        //     foreach (var gameTypeId in createGameDto.GameTypeIds)
        //     {
        //         _context.GameTypeGames.Add(new GameTypeGame { GameId = game.Id, GameTypeId = gameTypeId });
        //     }

        //     // Add platform relationships
        //     foreach (var platformId in createGameDto.PlatformIds)
        //     {
        //         _context.GamePlatforms.Add(new GamePlatform { GameId = game.Id, PlatformId = platformId });
        //     }

        //     // Add player perspective relationships
        //     foreach (var perspectiveId in createGameDto.PlayerPerspectiveIds)
        //     {
        //         _context.GamePlayerPerspectives.Add(new GamePlayerPerspective
        //         {
        //             GameId = game.Id,
        //             PlayerPerspectiveId = perspectiveId
        //         });
        //     }

        //     // Add company relationships
        //     foreach (var companyDto in createGameDto.Companies)
        //     {
        //         if (Enum.TryParse<CompanyRole>(companyDto.Role, true, out var role))
        //         {
        //             _context.GameCompanies.Add(new GameCompany
        //             {
        //                 GameId = game.Id,
        //                 CompanyId = companyDto.CompanyId,
        //                 Role = role
        //             });
        //         }
        //     }

        //     await _context.SaveChangesAsync();

        //     return await GetGameByIdAsync(game.Id) ?? throw new InvalidOperationException("Failed to retrieve created game");
        // }

        // public async Task<GameDto?> UpdateGameAsync(Guid id, UpdateGameDto updateGameDto)
        // {
        //     var game = await _context.Games.FindAsync(id);
        //     if (game == null) return null;

        //     _mapper.Map(updateGameDto, game);
        //     await _context.SaveChangesAsync();

        //     return await GetGameByIdAsync(id);
        // }

        public async Task<bool> DeleteGameAsync(Guid id)
        {
            var game = await _context.Games.FindAsync(id);
            if (game == null) return false;

            _context.Games.Remove(game);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<GameDto>> GetSimilarGamesAsync(Guid gameId, int limit = 10)
        {
            var similarGameIds = await _context.SimilarGames
                .Where(sg => sg.GameId == gameId)
                .Select(sg => sg.SimilarGameId)
                .Take(limit)
                .ToListAsync();

            var games = await _context.Games
                .Where(g => similarGameIds.Contains(g.Id))
                .Include(g => g.GameGenres).ThenInclude(gg => gg.Genre)
                .Include(g => g.Covers)
                .ToListAsync();

            var gameDtos = new List<GameDto>();
            if (games != null && games.Count > 0)
            {
                var gameIds = games.Select(g => g.Id).ToList();

                var likesCount = await _context.Likes
                    .Where(l => gameIds.Contains(l.GameId))
                    .GroupBy(l => l.GameId)
                    .Select(g => new { GameId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(g => g.GameId, g => g.Count);

                var favoritesCount = await _context.Favorites
                    .Where(f => gameIds.Contains(f.GameId))
                    .GroupBy(f => f.GameId)
                    .Select(g => new { GameId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(g => g.GameId, g => g.Count);


                foreach (var game in games)
                {
                    var gameDto = GameMapper.ToDto(game);
                    gameDto.LikesCount = likesCount.TryGetValue(game.Id, out var likeCount) ? likeCount : 0;
                    gameDto.FavoritesCount = favoritesCount.TryGetValue(game.Id, out var favoriteCount) ? favoriteCount : 0;
                    gameDtos.Add(gameDto);
                }
            }

            return gameDtos;
        }

        public async Task<List<GameDto>> GetGamesByFranchiseAsync(Guid franchiseId)
        {
            var games = await _context.Games
                .Where(g => g.GameFranchises.Any(gf => gf.FranchiseId == franchiseId))
                .Include(g => g.GameGenres).ThenInclude(gg => gg.Genre)
                .Include(g => g.Covers)
                .OrderBy(g => g.FirstReleaseDate)
                .ToListAsync();

            var gameDtos = new List<GameDto>();
            if (games != null && games.Count > 0)
            {
                var gameIds = games.Select(g => g.Id).ToList();

                var likesCount = await _context.Likes
                    .Where(l => gameIds.Contains(l.GameId))
                    .GroupBy(l => l.GameId)
                    .Select(g => new { GameId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(g => g.GameId, g => g.Count);

                var favoritesCount = await _context.Favorites
                    .Where(f => gameIds.Contains(f.GameId))
                    .GroupBy(f => f.GameId)
                    .Select(g => new { GameId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(g => g.GameId, g => g.Count);


                foreach (var game in games)
                {
                    var gameDto = GameMapper.ToDto(game);
                    gameDto.LikesCount = likesCount.TryGetValue(game.Id, out var likeCount) ? likeCount : 0;
                    gameDto.FavoritesCount = favoritesCount.TryGetValue(game.Id, out var favoriteCount) ? favoriteCount : 0;
                    gameDtos.Add(gameDto);
                }
            }

            return gameDtos;
        }

        public async Task<List<GameDto>> GetPopularGamesAsync(int limit = 20)
        {
            var games = await _context.Games
                .Include(g => g.GameGenres).ThenInclude(gg => gg.Genre)
                .Include(g => g.Covers)
                .OrderByDescending(g => g.Hypes)
                .ThenByDescending(g => g.Rating)
                .Take(limit)
                .ToListAsync();

            var gameDtos = new List<GameDto>();
            if (games != null && games.Count > 0)
            {
                var gameIds = games.Select(g => g.Id).ToList();

                var likesCount = await _context.Likes
                    .Where(l => gameIds.Contains(l.GameId))
                    .GroupBy(l => l.GameId)
                    .Select(g => new { GameId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(g => g.GameId, g => g.Count);

                var favoritesCount = await _context.Favorites
                    .Where(f => gameIds.Contains(f.GameId))
                    .GroupBy(f => f.GameId)
                    .Select(g => new { GameId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(g => g.GameId, g => g.Count);

                foreach (var game in games)
                {
                    var gameDto = GameMapper.ToDto(game);
                    gameDto.LikesCount = likesCount.TryGetValue(game.Id, out var likeCount) ? likeCount : 0;
                    gameDto.FavoritesCount = favoritesCount.TryGetValue(game.Id, out var favoriteCount) ? favoriteCount : 0;
                    gameDtos.Add(gameDto);
                }
            }

            return gameDtos;
        }

        public async Task<List<GameDto>> GetGamesByGenreAsync(Guid genreId, int limit = 20)
        {
            var games = await _context.Games
                .Where(g => g.GameGenres.Any(gg => gg.GenreId == genreId))
                .Include(g => g.GameGenres).ThenInclude(gg => gg.Genre)
                .Include(g => g.Covers)
                .OrderByDescending(g => g.Hypes)
                .ThenByDescending(g => g.Rating)
                .Take(limit)
                .ToListAsync();

            var gameDtos = new List<GameDto>();
            if (games != null && games.Count > 0)
            {
                var gameIds = games.Select(g => g.Id).ToList();

                var likesCount = await _context.Likes
                    .Where(l => gameIds.Contains(l.GameId))
                    .GroupBy(l => l.GameId)
                    .Select(g => new { GameId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(g => g.GameId, g => g.Count);

                var favoritesCount = await _context.Favorites
                    .Where(f => gameIds.Contains(f.GameId))
                    .GroupBy(f => f.GameId)
                    .Select(g => new { GameId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(g => g.GameId, g => g.Count);

                foreach (var game in games)
                {
                    var gameDto = GameMapper.ToDto(game);
                    gameDto.LikesCount = likesCount.TryGetValue(game.Id, out var likeCount) ? likeCount : 0;
                    gameDto.FavoritesCount = favoritesCount.TryGetValue(game.Id, out var favoriteCount) ? favoriteCount : 0;
                    gameDtos.Add(gameDto);
                }
            }

            return gameDtos;
        }
    }
}