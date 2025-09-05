using Backend.Data;
using Backend.Services.Recommendation.Interfaces;
using Microsoft.EntityFrameworkCore;
using Backend.Configuration;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace Backend.Services.Recommendation
{
    public class GameIndexingService
    {
        private readonly ApplicationDbContext _context;
        private readonly IVectorDatabase _vectorDatabase;
        private readonly IEmbeddingService _embeddingService;
        private readonly ILogger<GameIndexingService> _logger;
        private readonly ISemanticKeywordCache? _semanticKeywordCache;
        private const string GAMES_COLLECTION = "games";
        private SemanticKeywordConfig? _semanticConfig;
        private EmbeddingDimensions? _embeddingDimensions;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public GameIndexingService(
            ApplicationDbContext context,
            IVectorDatabase vectorDatabase,
            IEmbeddingService embeddingService,
            ILogger<GameIndexingService> logger,
            ISemanticKeywordCache? semanticKeywordCache = null)
        {
            _context = context;
            _vectorDatabase = vectorDatabase;
            _embeddingService = embeddingService;
            _logger = logger;
            _semanticKeywordCache = semanticKeywordCache;
            LoadSemanticConfiguration();
        }

        public async Task<bool> InitializeCollectionAsync()
        {
            try
            {
                var collectionExists = await _vectorDatabase.CollectionExistsAsync(GAMES_COLLECTION);
                if (!collectionExists)
                {
                    var totalDimensions = _embeddingDimensions?.TotalDimensions ?? _embeddingService.EmbeddingDimensions;

                    // Validate dimensions match expected constants
                    if (!EmbeddingConstants.ValidateDimensions(totalDimensions))
                    {
                        _logger.LogError("Dimension mismatch detected! Using {ActualDimensions} but {ExpectedMessage}",
                            totalDimensions, EmbeddingConstants.GetExpectedDimensionsMessage());
                        _logger.LogWarning("Using fallback dimensions: {FallbackDimensions}", EmbeddingConstants.TOTAL_EMBEDDING_DIMENSIONS);
                        totalDimensions = EmbeddingConstants.TOTAL_EMBEDDING_DIMENSIONS;
                    }

                    var success = await _vectorDatabase.CreateCollectionAsync(GAMES_COLLECTION, totalDimensions);
                    if (!success)
                    {
                        _logger.LogError("Failed to create games collection in vector database with {TotalDimensions} dimensions", totalDimensions);
                        return false;
                    }

                    _logger.LogInformation("Created games collection with {TotalDimensions} dimensions", totalDimensions);
                }
                // else
                // {
                //     // Collection exists - validate that our current embedding service generates compatible dimensions
                //     var testGameInput = new GameEmbeddingInput
                //     {
                //         Name = "Test Game",
                //         //Summary = "Test summary for dimension validation",
                //         Genres = [],
                //         Platforms = [],
                //         GameModes = [],
                //         PlayerPerspectives = []
                //     };

                //     var testEmbedding = await _embeddingService.GenerateGameEmbeddingAsync(testGameInput);
                //     if (!EmbeddingConstants.ValidateDimensions(testEmbedding.Length))
                //     {
                //         _logger.LogError("Existing collection may have incompatible dimensions. Current embedding service generates {ActualDimensions}, but {ExpectedMessage}. Consider recreating the collection.",
                //             testEmbedding.Length, EmbeddingConstants.GetExpectedDimensionsMessage());
                //         _logger.LogWarning("This may cause errors during reindexing or searching. To fix this, delete the existing collection and reinitialize.");
                //         // Don't fail initialization but warn about potential issues
                //     }
                //     else
                //     {
                //         _logger.LogDebug("Dimension validation successful: {Dimensions} dimensions", testEmbedding.Length);
                //     }
                // }

                _logger.LogInformation("Games collection initialized successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing games collection");
                return false;
            }
        }

        public async Task<bool> InitializeSemanticCacheAsync()
        {
            if (_semanticKeywordCache == null)
            {
                _logger.LogInformation("Semantic keyword cache not configured, skipping initialization");
                return true; // Not an error, just not configured
            }

            if (_semanticKeywordCache.IsInitialized)
            {
                _logger.LogInformation("Semantic keyword cache already initialized");
                return true;
            }

            _logger.LogInformation("Initializing semantic keyword cache...");
            var success = await _semanticKeywordCache.InitializeCacheAsync();

            if (success)
            {
                var stats = _semanticKeywordCache.GetCacheStats();
                _logger.LogInformation("Semantic keyword cache initialized successfully with {TotalKeywords} keywords across {TotalGenres} genres, {TotalPlatforms} platforms, {TotalGameModes} game modes, {TotalPerspectives} perspectives, and {TotalCombinations} combinations",
                    stats.TotalKeywords, stats.TotalGenres, stats.TotalPlatforms, stats.TotalGameModes, stats.TotalPerspectives, stats.TotalCombinations);
            }
            else
            {
                _logger.LogError("Failed to initialize semantic keyword cache");
            }

            return success;
        }

        public async Task<bool> RefreshSemanticCacheAsync()
        {
            if (_semanticKeywordCache == null)
            {
                _logger.LogWarning("Semantic keyword cache not configured, cannot refresh");
                return false;
            }

            _logger.LogInformation("Manually refreshing semantic keyword cache...");
            var success = await _semanticKeywordCache.RefreshCacheAsync();

            if (success)
            {
                var stats = _semanticKeywordCache.GetCacheStats();
                _logger.LogInformation("Semantic keyword cache refreshed successfully with {TotalKeywords} keywords", stats.TotalKeywords);
            }
            else
            {
                _logger.LogError("Failed to refresh semantic keyword cache");
            }

            return success;
        }

        public SemanticCacheStats? GetSemanticCacheStats()
        {
            return _semanticKeywordCache?.GetCacheStats();
        }

        public async Task<int> IndexAllGamesAsync()
        {
            try
            {
                _logger.LogInformation("Starting to index all games");

                // Initialize semantic cache if available
                await InitializeSemanticCacheAsync();

                var indexed = 0;
                var batchSize = 50;
                var skip = 0;
                var processingStartTime = DateTime.UtcNow;

                while (true)
                {
                    var games = await GetGamesWithRelationshipsBatchAsync(skip, batchSize);

                    if (games.Count == 0)
                    {
                        // Check if this is the first batch and we got no games due to database issues
                        if (skip == 0)
                        {
                            _logger.LogWarning("No games retrieved on first batch. This may indicate database connection issues.");
                        }
                        break;
                    }

                    // Process games using batch embeddings and bulk database insert
                    var gameInputs = games.Select(MapGameToEmbeddingInput).ToList();
                    var embeddings = await _embeddingService.ProcessGamesInBatch(gameInputs);

                    // Prepare bulk vector data
                    var vectorsToStore = new List<(string id, float[] vector, Dictionary<string, object> payload)>();

                    for (int i = 0; i < games.Count; i++)
                    {
                        var game = games[i];
                        var embedding = embeddings[i];

                        // Validate embedding before adding to bulk operation
                        if (!EmbeddingConstants.ValidateDimensions(embedding.Length))
                        {
                            _logger.LogError("Skipping game {GameName} due to invalid embedding dimensions: {ActualDimensions}",
                                game.Name, embedding.Length);
                            continue;
                        }

                        var payload = new Dictionary<string, object>
                        {
                            {"name", game.Name},
                            {"cover_url", game.Covers?.FirstOrDefault()?.Url ?? ""}
                        };

                        vectorsToStore.Add((game.Id.ToString(), embedding, payload));
                    }

                    // Single bulk upsert for entire batch
                    var success = await _vectorDatabase.UpsertVectorsBulkAsync(GAMES_COLLECTION, vectorsToStore);

                    var successCount = success ? vectorsToStore.Count : 0;
                    var failedCount = games.Count - successCount;
                    indexed += successCount;

                    if (failedCount > 0)
                    {
                        _logger.LogWarning("Failed to index {FailedCount} out of {TotalCount} games in batch", failedCount, games.Count);
                    }

                    skip += batchSize;

                    // Enhanced logging with performance metrics and memory monitoring
                    var elapsed = DateTime.UtcNow - processingStartTime;
                    var gamesPerSecond = indexed > 0 ? Math.Round(indexed / elapsed.TotalSeconds, 2) : 0;
                    var memoryMB = GC.GetTotalMemory(false) / 1024 / 1024;
                    _logger.LogInformation("Indexed batch: {BatchSuccess}/{BatchTotal} games. Total: {Total} games in {Elapsed:mm\\:ss} ({Rate} games/sec, {Memory}MB memory)",
                        successCount, games.Count, indexed, elapsed, gamesPerSecond, memoryMB);


                    // Small delay between batches
                    await Task.Delay(10);
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

        /// <summary>
        /// Optimized method to fetch games with all required relationships using AsSplitQuery for better performance
        /// </summary>
        private async Task<List<Backend.Models.Game.Game>> GetGamesWithRelationshipsBatchAsync(int skip, int take)
        {
            try
            {
                // Use AsSplitQuery() to optimize relationship loading - reduces from 7 queries to 2 queries
                var games = await _context.Games
                    .AsNoTracking() // Better performance for read-only operations
                    .AsSplitQuery() // Handles relationship loading efficiently with split queries
                    .Include(g => g.GameGenres).ThenInclude(gg => gg.Genre)
                    .Include(g => g.GamePlatforms).ThenInclude(gp => gp.Platform)
                    .Include(g => g.GameModes).ThenInclude(gm => gm.GameMode)
                    .Include(g => g.GamePlayerPerspectives).ThenInclude(gpp => gpp.PlayerPerspective)
                    .Include(g => g.Covers)
                    .Include(g => g.GameType)
                    .Include(g => g.GameCompanies).ThenInclude(gc => gc.Company)
                    .OrderBy(g => g.Id) // Consistent ordering for pagination
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync();

                _logger.LogDebug("Retrieved {GameCount} games with relationships from database using optimized AsSplitQuery (skip: {Skip}, take: {Take})", games.Count, skip, take);

                return games;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve games with relationships from database (skip: {Skip}, take: {Take}). Indexing will be skipped for this batch.", skip, take);
                return new List<Backend.Models.Game.Game>();
            }
        }

        public async Task<bool> IndexGameAsync(Backend.Models.Game.Game game)
        {
            try
            {
                var gameInput = MapGameToEmbeddingInput(game);
                var embedding = await _embeddingService.GenerateGameEmbeddingAsync(gameInput);

                // Strict validation: NEVER allow incorrect dimensions
                if (!EmbeddingConstants.ValidateDimensions(embedding.Length))
                {
                    var errorMessage = $"CRITICAL: Generated embedding dimension mismatch for game {game.Name} (ID: {game.Id}). Got {embedding.Length}, {EmbeddingConstants.GetExpectedDimensionsMessage()}. This indicates a configuration or implementation error.";
                    _logger.LogCritical(errorMessage);
                    throw new InvalidOperationException(errorMessage);
                }

                // Simplified: only store minimal metadata needed for retrieval
                var payload = new Dictionary<string, object>
                {
                    {"name", game.Name},
                    {"cover_url", game.Covers?.FirstOrDefault()?.Url ?? ""}
                };

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
                    .AsNoTracking() // Better performance for read-only operations
                    .AsSplitQuery() // Optimize relationship loading
                    .Include(g => g.GameGenres).ThenInclude(gg => gg.Genre)
                    .Include(g => g.GamePlatforms).ThenInclude(gp => gp.Platform)
                    .Include(g => g.GameModes).ThenInclude(gm => gm.GameMode)
                    .Include(g => g.GamePlayerPerspectives).ThenInclude(gpp => gpp.PlayerPerspective)
                    .Include(g => g.GameType)
                    .Include(g => g.Covers)
                    .Include(g => g.GameCompanies).ThenInclude(gc => gc.Company)
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
                _logger.LogError(ex, "Database error while updating game index for ID: {GameId}. This may be due to connection issues.", gameId);
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

        private GameEmbeddingInput MapGameToEmbeddingInput(Backend.Models.Game.Game game)
        {
            var gameInput = new GameEmbeddingInput
            {
                Name = game.Name,
                // Summary = game.Summary ?? "",
                // Storyline = game.Storyline ?? "",
                Genres = game.GameGenres?.Select(gg => gg.Genre.Name).ToList() ?? new List<string>(),
                Platforms = game.GamePlatforms?.Select(gp => gp.Platform.Name).ToList() ?? new List<string>(),
                GameModes = game.GameModes?.Select(gm => gm.GameMode.Name).ToList() ?? new List<string>(),
                PlayerPerspectives = game.GamePlayerPerspectives?.Select(gpp => gpp.PlayerPerspective.Name).ToList() ?? new List<string>(),
                //Rating = game.Rating,
                ReleaseDate = game.FirstReleaseDate.HasValue ? DateTimeOffset.FromUnixTimeSeconds(game.FirstReleaseDate.Value).DateTime : null
            };

            // Add the new required fields
            //gameInput.AgeRatings = game.GameAgeRatings?.Select(ar => ar.AgeRating.AgeRatingCategory.).ToList() ?? new List<string>();
            gameInput.Companies = game.GameCompanies?.Select(gc => gc.Company.Name).ToList() ?? new List<string>();
            gameInput.GameType = game.GameType?.Type ?? "";

            // Apply simplified semantic enrichment using existing mappings
            ApplySemanticEnrichment(gameInput);

            return gameInput;
        }

        private void LoadSemanticConfiguration()
        {
            try
            {
                var configPath = Path.Combine(AppContext.BaseDirectory, "Configuration", "SemanticKeywordMappings.json");
                if (File.Exists(configPath))
                {
                    var jsonContent = File.ReadAllText(configPath);
                    _semanticConfig = JsonSerializer.Deserialize<SemanticKeywordConfig>(jsonContent, JsonOptions);
                    _embeddingDimensions = _semanticConfig?.Dimensions;
                    _logger.LogInformation("Loaded semantic keyword configuration from file");
                }
                else
                {
                    _semanticConfig = CreateDefaultSemanticConfig();
                    _embeddingDimensions = _semanticConfig?.Dimensions;
                    _logger.LogWarning("Configuration file not found, using default semantic configuration");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading semantic configuration, using defaults");
                _semanticConfig = CreateDefaultSemanticConfig();
                _embeddingDimensions = _semanticConfig?.Dimensions;
            }
        }

        private static SemanticKeywordConfig CreateDefaultSemanticConfig()
        {
            return new SemanticKeywordConfig
            {
                DefaultWeights = new SemanticWeights(),
                Dimensions = new EmbeddingDimensions()
            };
        }

        private void ApplySemanticEnrichment(GameEmbeddingInput gameInput)
        {
            if (_semanticConfig == null)
            {
                _logger.LogWarning("Semantic configuration not loaded, skipping enrichment");
                return;
            }

            // Simple direct mapping from existing semantic configurations
            foreach (var genre in gameInput.Genres)
            {
                ApplySemanticMapping(_semanticConfig.GenreMappings, genre, gameInput);
            }

            foreach (var platform in gameInput.Platforms)
            {
                ApplySemanticMapping(_semanticConfig.PlatformMappings, platform, gameInput);
            }

            foreach (var gameMode in gameInput.GameModes)
            {
                ApplySemanticMapping(_semanticConfig.GameModeMappings, gameMode, gameInput);
            }

            foreach (var perspective in gameInput.PlayerPerspectives)
            {
                ApplySemanticMapping(_semanticConfig.PerspectiveMappings, perspective, gameInput);
            }

            // Apply weights from configuration
            if (_semanticConfig.DefaultWeights != null)
            {
                gameInput.SemanticWeights = new Dictionary<string, float>
                {
                    ["genre"] = _semanticConfig.DefaultWeights.GenreWeight,
                    ["mechanics"] = _semanticConfig.DefaultWeights.MechanicsWeight,
                    ["theme"] = _semanticConfig.DefaultWeights.ThemeWeight,
                    ["mood"] = _semanticConfig.DefaultWeights.MoodWeight,
                    ["artstyle"] = _semanticConfig.DefaultWeights.ArtStyleWeight,
                    ["audience"] = _semanticConfig.DefaultWeights.AudienceWeight
                };
            }

            _logger.LogDebug("Applied simplified semantic enrichment for game: {GameName}", gameInput.Name);
        }

        private static void ApplySemanticMapping(Dictionary<string, SemanticCategoryMapping> mappings, string key, GameEmbeddingInput gameInput)
        {
            if (mappings.TryGetValue(key, out var mapping))
            {
                gameInput.ExtractedGenreKeywords.AddRange(mapping.GenreKeywords);
                gameInput.ExtractedMechanicKeywords.AddRange(mapping.MechanicKeywords);
                gameInput.ExtractedThemeKeywords.AddRange(mapping.ThemeKeywords);
                gameInput.ExtractedMoodKeywords.AddRange(mapping.MoodKeywords);
                gameInput.ExtractedArtStyleKeywords.AddRange(mapping.ArtStyleKeywords);
                gameInput.ExtractedAudienceKeywords.AddRange(mapping.AudienceKeywords);
            }
        }

    }
}