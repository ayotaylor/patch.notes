using Backend.Data;
using Backend.Services.Recommendation.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services.Recommendation
{
    public class GameIndexingService
    {
        private readonly ApplicationDbContext _context;
        private readonly IVectorDatabase _vectorDatabase;
        private readonly IEmbeddingService _embeddingService;
        private readonly ILogger<GameIndexingService> _logger;
        private const string GAMES_COLLECTION = "games";

        public GameIndexingService(
            ApplicationDbContext context,
            IVectorDatabase vectorDatabase,
            IEmbeddingService embeddingService,
            ILogger<GameIndexingService> logger)
        {
            _context = context;
            _vectorDatabase = vectorDatabase;
            _embeddingService = embeddingService;
            _logger = logger;
        }

        public async Task<bool> InitializeCollectionAsync()
        {
            try
            {
                var collectionExists = await _vectorDatabase.CollectionExistsAsync(GAMES_COLLECTION);
                if (!collectionExists)
                {
                    var success = await _vectorDatabase.CreateCollectionAsync(GAMES_COLLECTION, _embeddingService.EmbeddingDimensions + 10); // +10 for structured features
                    if (!success)
                    {
                        _logger.LogError("Failed to create games collection in vector database");
                        return false;
                    }
                }

                _logger.LogInformation("Games collection initialized successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing games collection");
                return false;
            }
        }

        public async Task<int> IndexAllGamesAsync()
        {
            try
            {
                _logger.LogInformation("Starting to index all games");
                var indexed = 0;
                var batchSize = 50;
                var skip = 0;

                while (true)
                {
                    var games = await _context.Games
                        .Include(g => g.GameGenres)
                            .ThenInclude(gg => gg.Genre)
                        .Include(g => g.GamePlatforms)
                            .ThenInclude(gp => gp.Platform)
                        .Include(g => g.GameModes)
                            .ThenInclude(gm => gm.GameMode)
                        .Include(g => g.GamePlayerPerspectives)
                            .ThenInclude(gpp => gpp.PlayerPerspective)
                        .Include(g => g.Covers)
                        .Skip(skip)
                        .Take(batchSize)
                        .ToListAsync();

                    if (!games.Any())
                        break;

                    foreach (var game in games)
                    {
                        var success = await IndexGameAsync(game);
                        if (success) indexed++;
                    }

                    skip += batchSize;
                    _logger.LogInformation("Indexed batch of {Count} games, total: {Total}", games.Count, indexed);
                }

                _logger.LogInformation("Completed indexing {Total} games", indexed);
                return indexed;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error indexing all games");
                return 0;
            }
        }

        public async Task<bool> IndexGameAsync(Backend.Models.Game.Game game)
        {
            try
            {
                var gameInput = MapGameToEmbeddingInput(game);
                var embedding = await _embeddingService.GenerateGameEmbeddingAsync(gameInput);

                var payload = CreateGamePayload(game);

                var success = await _vectorDatabase.UpsertVectorAsync(
                    GAMES_COLLECTION,
                    game.Id.ToString(),
                    embedding,
                    payload);

                if (!success)
                {
                    _logger.LogWarning("Failed to index game: {GameName} (ID: {GameId})", game.Name, game.Id);
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error indexing game: {GameName} (ID: {GameId})", game.Name, game.Id);
                return false;
            }
        }

        public async Task<bool> UpdateGameIndexAsync(Guid gameId)
        {
            try
            {
                var game = await _context.Games
                    .Include(g => g.GameGenres)
                        .ThenInclude(gg => gg.Genre)
                    .Include(g => g.GamePlatforms)
                        .ThenInclude(gp => gp.Platform)
                    .Include(g => g.GameModes)
                        .ThenInclude(gm => gm.GameMode)
                    .Include(g => g.GamePlayerPerspectives)
                        .ThenInclude(gpp => gpp.PlayerPerspective)
                    .Include(g => g.Covers)
                    .FirstOrDefaultAsync(g => g.Id == gameId);

                if (game == null)
                {
                    _logger.LogWarning("Game not found for indexing: {GameId}", gameId);
                    return false;
                }

                return await IndexGameAsync(game);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating game index for ID: {GameId}", gameId);
                return false;
            }
        }

        public async Task<bool> RemoveGameFromIndexAsync(Guid gameId)
        {
            try
            {
                var success = await _vectorDatabase.DeleteVectorAsync(GAMES_COLLECTION, gameId.ToString());
                if (success)
                {
                    _logger.LogInformation("Removed game from index: {GameId}", gameId);
                }
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing game from index: {GameId}", gameId);
                return false;
            }
        }

        public async Task<List<VectorSearchResult>> SearchSimilarGamesAsync(float[] queryEmbedding, int limit = 20)
        {
            try
            {
                return await _vectorDatabase.SearchAsync(GAMES_COLLECTION, queryEmbedding, limit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching for similar games");
                return new List<VectorSearchResult>();
            }
        }

        public async Task<List<VectorSearchResult>> SearchGamesWithFiltersAsync(
            float[] queryEmbedding,
            QueryAnalysis analysis,
            int limit = 20)
        {
            try
            {
                var filters = new Dictionary<string, object>();

                if (analysis.Genres?.Count > 0)
                {
                    filters["genres"] = string.Join(",", genres);
                }

                if (platforms?.Count > 0)
                {
                    filters["platforms"] = string.Join(",", platforms);
                }

                if (releaseDateFrom.HasValue)
                {
                    filters["release_year_from"] = releaseDateFrom.Value.Year;
                }

                if (releaseDateTo.HasValue)
                {
                    filters["release_year_to"] = releaseDateTo.Value.Year;
                }

                return await _vectorDatabase.SearchAsync(GAMES_COLLECTION, queryEmbedding, limit, filters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching games with filters");
                return new List<VectorSearchResult>();
            }
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

        private Dictionary<string, object> CreateGamePayload(Backend.Models.Game.Game game)
        {
            var releaseYear = game.FirstReleaseDate.HasValue
                ? DateTimeOffset.FromUnixTimeSeconds(game.FirstReleaseDate.Value).Year
                : 0;

            return new Dictionary<string, object>
            {
                {"name", game.Name},
                {"summary", game.Summary ?? ""},
                {"storyline", game.Storyline ?? ""},
                {"rating", game.Rating?.ToString() ?? "0"},
                {"release_year", releaseYear.ToString()},
                {"genres", string.Join(",", game.GameGenres?.Select(gg => gg.Genre.Name) ?? new List<string>())},
                {"platforms", string.Join(",", game.GamePlatforms?.Select(gp => gp.Platform.Name) ?? new List<string>())},
                {"game_modes", string.Join(",", game.GameModes?.Select(gm => gm.GameMode.Name) ?? new List<string>())},
                {"perspectives", string.Join(",", game.GamePlayerPerspectives?.Select(gpp => gpp.PlayerPerspective.Name) ?? new List<string>())},
                {"cover_url", game.Covers?.FirstOrDefault()?.Url ?? ""},
                {"igdb_id", game.IgdbId.ToString()}
            };
        }
    }
}