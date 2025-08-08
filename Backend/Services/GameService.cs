using System.Threading.Tasks;
using Backend.Data;
using Backend.Mapping;
using Backend.Models.DTO.Game;
using Backend.Models.DTO.Request;
using Backend.Models.DTO.Response;
using Backend.Models.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

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

            // // Apply search filters BEFORE includes to reduce data fetching
            // search by name or summary TODO: maybe remove summary from search
            if (!string.IsNullOrEmpty(searchParams.Search))
            {
                query = query.Where(g => g.Name.Contains(searchParams.Search));
                //    || g.Summary != null
                //    && g.Summary.Contains(searchParams.Search));
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

            // OPTIMIZATION 1: Get total count from filtered query (before includes)
            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)searchParams.PageSize);

            // OPTIMIZATION 2: Apply pagination BEFORE includes to reduce data loading
            var pagedGameIds = await query
                .Skip((searchParams.Page - 1) * searchParams.PageSize)
                .Take(searchParams.PageSize)
                .Select(g => g.Id)
                .ToListAsync();

            if (pagedGameIds.Count <= 0)
            {
                return new PagedResponse<GameDto>
                {
                    Data = new List<GameDto>(),
                    Page = searchParams.Page,
                    PageSize = searchParams.PageSize,
                    TotalCount = totalCount,
                    TotalPages = totalPages,
                    HasNextPage = searchParams.Page < totalPages,
                    HasPreviousPage = searchParams.Page > 1
                };
            }

            // OPTIMIZATION 3: Load games with includes only for the paged results
            // TODO: need to optimize includes further by only including necessary data
            var games = await query
                .Where(g => pagedGameIds.Contains(g.Id))
                .AsSplitQuery()
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

            // OPTIMIZATION 4: Batch load user interactions and counts in parallel
            var (userInteractions, counts) = await LoadUserDataAndCountsAsync(pagedGameIds, userId);

            // OPTIMIZATION 5: Preserve original sort order from the query
            var gameDict = games.ToDictionary(g => g.Id);
            var orderedGames = pagedGameIds.Select(id => gameDict[id]).ToList();

            // Map games to DTOs and apply user interactions and counts
            var gameDtos = orderedGames.Select(game =>
            {
                var gameDto = GameMapper.ToDto(game);
                gameDto.IsLikedByUser = userInteractions.UserLikes.Contains(game.Id);
                gameDto.IsFavoriteByUser = userInteractions.UserFavorites.Contains(game.Id);
                gameDto.LikesCount = counts.LikesCount.TryGetValue(game.Id,
                    out var likeCount) ? likeCount : 0;
                gameDto.FavoritesCount = counts.FavoritesCount.TryGetValue(game.Id,
                    out var favoriteCount) ? favoriteCount : 0;
                return gameDto;
            }).ToList();

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

        // OPTIMIZATION 6: Extract user interactions and counts loading into separate method
        private async Task<(UserInteractions userInteractions, GameCounts counts)> LoadUserDataAndCountsAsync(
            List<Guid> gameIds, Guid? userId)
        {
            // OPTION 1: Single query approach - Load all data in one go
            var allLikes = await _context.Likes
                .Where(l => gameIds.Contains(l.GameId))
                .Select(l => new { l.GameId, l.UserId })
                .ToListAsync();

            var allFavorites = await _context.Favorites
                .Where(f => gameIds.Contains(f.GameId))
                .Select(f => new { f.GameId, f.UserId })
                .ToListAsync();

            var userProfileId = await _context.UserProfiles
                    .Where(u => u.UserId == userId.ToString())
                    .Select(u => u.Id)
                    .FirstOrDefaultAsync();

            // Process in memory (fast since we only have page-sized data)
            var userLikes = userId.HasValue
                ? new HashSet<Guid>(allLikes.Where(l => l.UserId == userProfileId).Select(l => l.GameId))
                : new HashSet<Guid>();

            var userFavorites = userId.HasValue
                ? new HashSet<Guid>(allFavorites.Where(f => f.UserId == userProfileId).Select(f => f.GameId))
                : new HashSet<Guid>();

            var likesCount = allLikes
                .GroupBy(l => l.GameId)
                .ToDictionary(g => g.Key, g => g.Count());

            var favoritesCount = allFavorites
                .GroupBy(f => f.GameId)
                .ToDictionary(g => g.Key, g => g.Count());

            return (
                new UserInteractions { UserLikes = userLikes, UserFavorites = userFavorites },
                new GameCounts { LikesCount = likesCount, FavoritesCount = favoritesCount }
            );
        }

        public async Task<GameDto?> GetGameByIdAsync(int id, Guid? userId = null)
        {
            var game = await _context.Games
                .Where(g => g.IgdbId == id)
                .AsSplitQuery()
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
                .FirstOrDefaultAsync(g => g.IgdbId == id);

            if (game == null) return null;

            var gameDto = GameMapper.ToDto(game);

            if (userId != Guid.Empty && userId.HasValue)
            {
                var userProfileId = await _context.UserProfiles
                    .Where(u => u.UserId == userId.ToString())
                    .Select(u => u.Id)
                    .FirstOrDefaultAsync();
                gameDto.IsLikedByUser = await _context.Likes
                    .AnyAsync(l => l.UserId == userProfileId && l.GameId == game.Id);
                gameDto.IsFavoriteByUser = await _context.Favorites
                    .AnyAsync(f => f.UserId == userProfileId && f.GameId == game.Id);
            }

            gameDto.LikesCount = await _context.Likes.CountAsync(l => l.GameId == game.Id);
            gameDto.FavoritesCount = await _context.Favorites.CountAsync(f => f.GameId == game.Id);

            return gameDto;
        }

        public async Task<GameDto?> GetGameBySlugAsync(string slug, Guid? userId = null)
        {
            var game = await _context.Games
                .Where(g => g.Slug == slug)
                .AsSplitQuery()
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

            if (userId != Guid.Empty && userId.HasValue)
            {
                var userProfileId = await _context.UserProfiles
                    .Where(u => u.UserId == userId.ToString())
                    .Select(u => u.Id)
                    .FirstOrDefaultAsync();
                gameDto.IsLikedByUser = await _context.Likes
                    .AnyAsync(l => l.UserId == userProfileId && l.GameId == game.Id);
                gameDto.IsFavoriteByUser = await _context.Favorites
                    .AnyAsync(f => f.UserId == userProfileId && f.GameId == game.Id);
            }

            gameDto.LikesCount = await _context.Likes.CountAsync(l => l.GameId == game.Id);
            gameDto.FavoritesCount = await _context.Favorites.CountAsync(f => f.GameId == game.Id);

            return gameDto;
        }

        public async Task<bool> DeleteGameAsync(int id)
        {
            var game = await _context.Games
                .Where(g => g.IgdbId == id)
                .FirstOrDefaultAsync();
            if (game == null) return false;

            _context.Games.Remove(game);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<GameDto>> GetSimilarGamesAsync(int gameId, int limit = 10)
        {
            var gameIdFromIgdbId = await _context.Games
                .Where(g => g.IgdbId == gameId)
                .Select(g => g.Id)
                .FirstOrDefaultAsync();

            var similarGameIds = await _context.SimilarGames
                .Where(sg => gameIdFromIgdbId == sg.GameId)
                .Select(sg => sg.SimilarGameId)
                .Take(limit)
                .ToListAsync();

            // TODO: decide what to return in the game object
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

        public async Task<List<GameDto>> GetGamesByFranchiseAsync(int franchiseId)
        {
            var franchiseIdFromIgdbId = await _context.Franchises
                .Where(f => f.IgdbId == franchiseId)
                .Select(f => f.Id)
                .FirstOrDefaultAsync();

            var games = await _context.Games
                .Where(g => g.GameFranchises.Any(gf => gf.FranchiseId == franchiseIdFromIgdbId))
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

        // TODO: maybe take only necessary fields...no full object or igdbid(usually used for filtering//search)
        public async Task<List<GameDto>> GetPopularGamesAsync(int limit = 20)
        {
            var popularGameIds = await _context.Games
                .Where(g => g.Hypes > 0 && g.Rating > 0)
                .OrderByDescending(g => g.Hypes)
                .ThenByDescending(g => g.Rating)
                .Take(limit)
                .Select(g => g.Id)
                .ToListAsync();

            if (popularGameIds == null || popularGameIds.Count <= 0)
            {
                return new List<GameDto>();
            }

            var games = await _context.Games
                .Where(g => popularGameIds.Contains(g.Id)) // Filter out games with no hypes/ratings
                .AsSplitQuery()
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
                .Include(g => g.GameType)
                .Include(g => g.GameCompanies).ThenInclude(gc => gc.Company)
                .Include(g => g.GamePlayerPerspectives).ThenInclude(gpp => gpp.PlayerPerspective)
                .ToListAsync();

            var orderedGames = popularGameIds.Join(games, id => id, game => game.Id, (id, game) => game).ToList();

            var gameDtos = new List<GameDto>();
            if (orderedGames != null && orderedGames.Count > 0)
            {
                var gameIds = orderedGames.Select(g => g.Id).ToList();

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

                foreach (var game in orderedGames)
                {
                    var gameDto = GameMapper.ToDto(game);
                    gameDto.LikesCount = likesCount.TryGetValue(game.Id, out var likeCount) ? likeCount : 0;
                    gameDto.FavoritesCount = favoritesCount.TryGetValue(game.Id, out var favoriteCount) ? favoriteCount : 0;
                    gameDtos.Add(gameDto);
                }
            }

            return gameDtos;
        }

        // TODO: get games from the last three months. need to implement a better method of getting new games
        public async Task<List<GameDto>> GetNewGamesAsync(int limit = 20)
        {
            var today = new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds();
            var threeMonthsAgo = new DateTimeOffset(DateTime.Now.AddMonths(-3)).ToUnixTimeSeconds();
            var newGameIds = await _context.Games
                .Where(g => g.FirstReleaseDate <= today && g.FirstReleaseDate >= threeMonthsAgo)
                .OrderByDescending(g => g.Hypes)
                .ThenByDescending(g => g.Rating)
                .Take(limit)
                .Select(g => g.Id)
                .ToListAsync();

            if (newGameIds == null || newGameIds.Count <= 0)
            {
                return new List<GameDto>();
            }

            var games = await _context.Games
                .Where(g => newGameIds.Contains(g.Id)) // Filter out games with no hypes/ratings
                .AsSplitQuery()
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
                .Include(g => g.GameType)
                .Include(g => g.GameCompanies).ThenInclude(gc => gc.Company)
                .Include(g => g.GamePlayerPerspectives).ThenInclude(gpp => gpp.PlayerPerspective)
                .ToListAsync();

            var orderedGames = newGameIds.Join(games, id => id, game => game.Id, (id, game) => game).ToList();

            var gameDtos = new List<GameDto>();
            if (orderedGames != null && orderedGames.Count > 0)
            {
                var gameIds = orderedGames.Select(g => g.Id).ToList();

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

                foreach (var game in orderedGames)
                {
                    var gameDto = GameMapper.ToDto(game);
                    gameDto.LikesCount = likesCount.TryGetValue(game.Id, out var likeCount) ? likeCount : 0;
                    gameDto.FavoritesCount = favoritesCount.TryGetValue(game.Id, out var favoriteCount) ? favoriteCount : 0;
                    gameDtos.Add(gameDto);
                }
            }

            return gameDtos;
        }

        public async Task<List<GameDto>> GetGamesByGenreAsync(int genreId, int limit = 20)
        {
            var genreIdFromIgdbId = await _context.Genres
                .Where(g => g.IgdbId == genreId)
                .Select(g => g.Id)
                .FirstOrDefaultAsync();

            var games = await _context.Games
                .Where(g => g.GameGenres.Any(gg => genreIdFromIgdbId == gg.GenreId))
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
    }
}