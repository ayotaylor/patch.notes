using Backend.Data;
using Backend.Services.Recommendation.Interfaces;
using Microsoft.EntityFrameworkCore;
using Backend.Configuration;

namespace Backend.Services.Recommendation
{
    public class GameIndexingService
    {
        private readonly ApplicationDbContext _context;
        private readonly IVectorDatabase _vectorDatabase;
        private readonly IEmbeddingService _embeddingService;
        private readonly ILogger<GameIndexingService> _logger;
        private readonly ISemanticKeywordCache? _semanticKeywordCache;
        private readonly ISemanticConfigurationService _configService;
        private readonly CategoryNormalizationService _categoryNormalizationService;
        private const string GAMES_COLLECTION = "games";
        
        // Collection name property for consistency
        private string CollectionName => GAMES_COLLECTION;

        public GameIndexingService(
            ApplicationDbContext context,
            IVectorDatabase vectorDatabase,
            IEmbeddingService embeddingService,
            ILogger<GameIndexingService> logger,
            ISemanticConfigurationService configService,
            CategoryNormalizationService categoryNormalizationService,
            ISemanticKeywordCache? semanticKeywordCache = null)
        {
            _context = context;
            _vectorDatabase = vectorDatabase;
            _embeddingService = embeddingService;
            _logger = logger;
            _configService = configService;
            _categoryNormalizationService = categoryNormalizationService;
            _semanticKeywordCache = semanticKeywordCache;
        }

        public async Task<bool> InitializeCollectionAsync()
        {
            try
            {
                var collectionExists = await _vectorDatabase.CollectionExistsAsync(GAMES_COLLECTION);
                if (!collectionExists)
                {
                    var totalDimensions = _configService.SemanticConfig.Dimensions?.TotalDimensions ?? _embeddingService.EmbeddingDimensions;

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

        /// <summary>
        /// Ensures semantic cache is ready - uses efficient lazy initialization if needed
        /// </summary>
        private async Task<bool> EnsureSemanticCacheReadyAsync()
        {
            if (_semanticKeywordCache == null)
            {
                _logger.LogDebug("Semantic keyword cache not configured");
                return false;
            }

            return await _semanticKeywordCache.EnsureInitializedAsync();
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

                // Ensure semantic cache is ready (efficient lazy initialization)
                await EnsureSemanticCacheReadyAsync();

                var indexed = 0;
                var batchSize = 128;
                var skip = 0;
                var processingStartTime = DateTime.UtcNow;
                TimeSpan elapsed = TimeSpan.Zero;

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
                        var (isValid, errorMessage) =
                            EmbeddingDimensionValidator.ValidateEmbeddingDimensions(embedding.Length, "game embedding before upsert");
                        if (!isValid)
                        {
                            _logger.LogCritical("CRITICAL: {ErrorMessage}", errorMessage);
                            _logger.LogError("Skipping game {GameName} due to invalid embedding dimensions: {ActualDimensions}",
                                game.Name, embedding.Length);
                            continue;
                        }

                        var payload = new Dictionary<string, object>
                        {
                            {"name", game.Name},
                            {"cover_url", game.Covers?.FirstOrDefault()?.Url ?? ""},
                            {"genres", game.GameGenres?.Select(gg => gg.Genre.Name).ToList() ?? []},
                            {"platforms", game.GamePlatforms?
                                .SelectMany(gp =>
                                    new List<string> {gp.Platform.Name , gp.Platform.AlternativeName})
                                .Where(gp => !string.IsNullOrEmpty(gp))
                                .ToList() ?? []},
                            {"game_modes", game.GameModes?.Select(gm => gm.GameMode.Name).ToList() ?? []},
                            {"player_perspectives", game.GamePlayerPerspectives?.Select(gpp => gpp.PlayerPerspective.Name).ToList() ?? []},
                            {"release_date", game.FirstReleaseDate}
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
                    elapsed = DateTime.UtcNow - processingStartTime;
                    var gamesPerSecond = indexed > 0 ? Math.Round(indexed / elapsed.TotalSeconds, 2) : 0;
                    var memoryMB = GC.GetTotalMemory(false) / 1024 / 1024;
                    _logger.LogInformation("Indexed batch: {BatchSuccess}/{BatchTotal} games. Total: {Total} games in {Elapsed:mm\\:ss} ({Rate} games/sec, {Memory}MB memory)",
                        successCount, games.Count, indexed, elapsed, gamesPerSecond, memoryMB);


                    // Small delay between batches to prevent connection pool exhaustion
                    //await Task.Delay(5);
                }

                _logger.LogInformation("Completed indexing {Total} games in {time elapsed}", indexed, elapsed.ToString());
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
                // Use AsSplitQuery() to optimize relationship loading and improve connection pool efficiency
                var games = await _context.Games
                    .AsNoTracking() // Better performance for read-only operations, reduces connection time

                    .Include(g => g.GameGenres).ThenInclude(gg => gg.Genre)
                    .Include(g => g.GamePlatforms).ThenInclude(gp => gp.Platform)
                    .Include(g => g.GameModes).ThenInclude(gm => gm.GameMode)
                    .Include(g => g.GamePlayerPerspectives).ThenInclude(gpp => gpp.PlayerPerspective)
                    .Include(g => g.Covers)
                    .Include(g => g.GameType)
                    .Include(g => g.GameCompanies).ThenInclude(gc => gc.Company)
                    .AsSplitQuery() // Handles relationship loading efficiently with split queries
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

                // Store metadata needed for filtering and retrieval
                var payload = new Dictionary<string, object>
                {
                    {"name", game.Name},
                    {"cover_url", game.Covers?.FirstOrDefault()?.Url ?? ""},
                    {"genres", game.GameGenres?.Select(gg => gg.Genre.Name).ToList() ?? []},
                    {"platforms", game.GamePlatforms?
                        .SelectMany(gp =>
                            new List<string> {gp.Platform.Name , gp.Platform.AlternativeName})
                        .Where(gp => !string.IsNullOrEmpty(gp)).ToList() ?? []},
                    {"game_modes", game.GameModes?.Select(gm => gm.GameMode.Name).ToList() ?? []},
                    {"player_perspectives", game.GamePlayerPerspectives?.Select(gpp => gpp.PlayerPerspective.Name).ToList() ?? []},
                    {"release_date", game.FirstReleaseDate}
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
                    .Include(g => g.GameGenres).ThenInclude(gg => gg.Genre)
                    .Include(g => g.GamePlatforms).ThenInclude(gp => gp.Platform)
                    .Include(g => g.GameModes).ThenInclude(gm => gm.GameMode)
                    .Include(g => g.GamePlayerPerspectives).ThenInclude(gpp => gpp.PlayerPerspective)
                    .Include(g => g.GameType)
                    .Include(g => g.Covers)
                    .Include(g => g.GameCompanies).ThenInclude(gc => gc.Company)
                    .AsSplitQuery()
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

        public async Task<List<VectorSearchResult>> SearchSimilarGamesAsync(float[] queryEmbedding, int limit = 20, QueryAnalysis? queryAnalysis = null)
        {
            try
            {
                // Construct filters from normalized query analysis
                var filters = ConstructFiltersFromQuery(queryAnalysis);

                // Primary search with enhanced embedding and filters
                var results = await _vectorDatabase.SearchAsync(GAMES_COLLECTION, queryEmbedding, limit, filters, queryAnalysis);

                // Apply semantic combination boosting for higher quality results
                if (_semanticKeywordCache?.IsInitialized == true && results.Count > 0)
                {
                    results = ApplySemanticBoosting(results);
                }

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching for similar games");
                return new List<VectorSearchResult>();
            }
        }

        /// <summary>
        /// Construct filters for vector database search based on normalized query analysis
        /// </summary>
        private Dictionary<string, object>? ConstructFiltersFromQuery(QueryAnalysis? queryAnalysis)
        {
            if (queryAnalysis == null)
                return null;

            var filters = new Dictionary<string, object>();

            // Filter by genres if specified (already normalized)
            if (queryAnalysis.Genres.Count > 0)
            {
                filters["genres"] = new Dictionary<string, object>
                {
                    ["$in"] = queryAnalysis.Genres
                };
            }

            // Filter by platforms if specified (already normalized)
            if (queryAnalysis.Platforms.Count > 0)
            {
                filters["platforms"] = new Dictionary<string, object>
                {
                    ["$in"] = queryAnalysis.Platforms
                };
            }

            // Filter by game modes if specified (already normalized)
            if (queryAnalysis.GameModes.Count > 0)
            {
                filters["game_modes"] = new Dictionary<string, object>
                {
                    ["$in"] = queryAnalysis.GameModes
                };
            }

            // Filter by player perspectives if specified (already normalized)
            if (queryAnalysis.PlayerPerspectives.Count > 0)
            {
                filters["player_perspectives"] = new Dictionary<string, object>
                {
                    ["$in"] = queryAnalysis.PlayerPerspectives
                };
            }
            
            if (!string.IsNullOrWhiteSpace(queryAnalysis.GameType))
            {
                filters["game_type"] = queryAnalysis.GameType;
            }

            if (queryAnalysis.Companies?.Count > 0)
            {
                filters["companies"] = new Dictionary<string, object>
                {
                    ["$in"] = queryAnalysis.Companies
                };
            }

            // Note: Moods are handled through semantic embedding enhancement, not direct filtering
            // since games don't have explicit mood metadata in the database

            // Filter by release date range if specified
            if (queryAnalysis.ReleaseDateRange != null)
            {
                var dateFilter = new Dictionary<string, object>();

                if (queryAnalysis.ReleaseDateRange.From.HasValue)
                {
                    dateFilter["$gte"] = new DateTimeOffset(queryAnalysis.ReleaseDateRange.From.Value).ToUnixTimeSeconds();
                }

                if (queryAnalysis.ReleaseDateRange.To.HasValue)
                {
                    dateFilter["$lte"] = new DateTimeOffset(queryAnalysis.ReleaseDateRange.To.Value).ToUnixTimeSeconds();
                }

                if (dateFilter.Count > 0)
                {
                    filters["release_date"] = dateFilter;
                }
            }

            _logger.LogDebug("Constructed {FilterCount} filters for vector search: {Filters}", 
                filters.Count, string.Join(", ", filters.Keys));

            return filters.Count > 0 ? filters : null;
        }

        /// <summary>
        /// Apply semantic boosting to search results based on combination enrichment
        /// </summary>
        private List<VectorSearchResult> ApplySemanticBoosting(List<VectorSearchResult> results)
        {
            // For search-time enrichment, we boost results that have rich semantic combinations
            // This is a lightweight post-processing step
            foreach (var result in results)
            {
                // Slightly boost results with higher semantic richness
                // In a real scenario, you might analyze the game's metadata against combinations
                if (result.Score > 0.8f) // Already high-confidence results
                {
                    result.Score = Math.Min(1.0f, result.Score * 1.05f); // Small boost
                }
            }

            return results.OrderByDescending(r => r.Score).ToList();
        }

        private GameEmbeddingInput MapGameToEmbeddingInput(Backend.Models.Game.Game game)
        {
            var gameInput = new GameEmbeddingInput
            {
                Name = game.Name,
                // Summary = game.Summary ?? "",
                // Storyline = game.Storyline ?? "",
                Genres = game.GameGenres?.Select(gg => gg.Genre.Name).ToList() ?? new List<string>(),
                Platforms = game.GamePlatforms?
                    .SelectMany(gp =>
                        new List<string> { gp.Platform.Name, gp.Platform.AlternativeName })
                    .Where(gp => !string.IsNullOrEmpty(gp))
                    .ToList() ?? new List<string>(),
                GameModes = game.GameModes?.Select(gm => gm.GameMode.Name).ToList() ?? new List<string>(),
                PlayerPerspectives = game.GamePlayerPerspectives?.Select(gpp => gpp.PlayerPerspective.Name).ToList() ?? new List<string>(),
                Rating = game.Rating ?? null,
                ReleaseDate = game.FirstReleaseDate
            };

            // Add the new required fields
            //gameInput.AgeRatings = game.GameAgeRatings?.Select(ar => ar.AgeRating.AgeRatingCategory.).ToList() ?? new List<string>();
            gameInput.Companies = game.GameCompanies?.Select(gc => gc.Company.Name).ToList() ?? new List<string>();
            gameInput.GameType = game.GameType?.Type ?? "";

            // Apply simplified semantic enrichment using existing mappings
            ApplySemanticEnrichment(gameInput);

            return gameInput;
        }



        private void ApplySemanticEnrichment(GameEmbeddingInput gameInput)
        {
            var semanticConfig = _configService.SemanticConfig;
            if (semanticConfig == null)
            {
                _logger.LogWarning("Semantic configuration not loaded, skipping enrichment");
                return;
            }

            // Simple direct mapping from existing semantic configurations
            foreach (var genre in gameInput.Genres)
            {
                ApplySemanticMapping(semanticConfig.GenreMappings, genre, gameInput);
            }

            foreach (var platform in gameInput.Platforms)
            {
                ApplySemanticMapping(semanticConfig.PlatformMappings, platform, gameInput);
            }

            foreach (var gameMode in gameInput.GameModes)
            {
                ApplySemanticMapping(semanticConfig.GameModeMappings, gameMode, gameInput);
            }

            foreach (var perspective in gameInput.PlayerPerspectives)
            {
                ApplySemanticMapping(semanticConfig.PerspectiveMappings, perspective, gameInput);
            }

            _logger.LogDebug("Applied simplified semantic enrichment for game: {GameName}", gameInput.Name);
        }

        private static void ApplySemanticMapping(Dictionary<string, SemanticCategoryMapping> mappings, string key, GameEmbeddingInput gameInput)
        {
            if (mappings.TryGetValue(key, out var mapping))
            {
                // Core game properties
                AddKeywordsIfNotEmpty(mapping.GenreKeywords, gameInput.ExtractedGenreKeywords);
                AddKeywordsIfNotEmpty(mapping.MechanicKeywords, gameInput.ExtractedMechanicKeywords);
                AddKeywordsIfNotEmpty(mapping.ThemeKeywords, gameInput.ExtractedThemeKeywords);
                AddKeywordsIfNotEmpty(mapping.MoodKeywords, gameInput.ExtractedMoodKeywords);
                AddKeywordsIfNotEmpty(mapping.ArtStyleKeywords, gameInput.ExtractedArtStyleKeywords);
                AddKeywordsIfNotEmpty(mapping.AudienceKeywords, gameInput.ExtractedAudienceKeywords);
                
                // Platform-specific properties
                AddKeywordsIfNotEmpty(mapping.PlatformType, gameInput.ExtractedPlatformTypeKeywords);
                AddKeywordsIfNotEmpty(mapping.EraKeywords, gameInput.ExtractedEraKeywords);
                AddKeywordsIfNotEmpty(mapping.CapabilityKeywords, gameInput.ExtractedCapabilityKeywords);
                
                // Game mode-specific properties
                AddKeywordsIfNotEmpty(mapping.PlayerInteractionKeywords, gameInput.ExtractedPlayerInteractionKeywords);
                AddKeywordsIfNotEmpty(mapping.ScaleKeywords, gameInput.ExtractedScaleKeywords);
                AddKeywordsIfNotEmpty(mapping.CommunicationKeywords, gameInput.ExtractedCommunicationKeywords);
                
                // Perspective-specific properties
                AddKeywordsIfNotEmpty(mapping.ViewpointKeywords, gameInput.ExtractedViewpointKeywords);
                AddKeywordsIfNotEmpty(mapping.ImmersionKeywords, gameInput.ExtractedImmersionKeywords);
                AddKeywordsIfNotEmpty(mapping.InterfaceKeywords, gameInput.ExtractedInterfaceKeywords);
            }
        }
        
        private static void AddKeywordsIfNotEmpty<T>(List<T> source, List<T> destination)
        {
            if (source != null && source.Count > 0)
            {
                destination.AddRange(source);
            }
        }

        /// <summary>
        /// High-performance parallel bulk indexing optimized for 300k+ games
        /// Processes games in parallel batches using all available CPU cores/GPU
        /// </summary>
        public async Task<bool> IndexGamesInParallelAsync(int batchSize = 100, int maxParallelism = 8, int skipCount = 0)
        {
            try
            {
                _logger.LogInformation("Starting parallel bulk indexing with batchSize={BatchSize}, maxParallelism={MaxParallelism}, skip={Skip}", 
                    batchSize, maxParallelism, skipCount);

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                // Get total count for progress tracking
                var totalGames = await _context.Games.CountAsync();
                var gamesToProcess = totalGames - skipCount;
                
                _logger.LogInformation("Processing {GamesToProcess} games out of {TotalGames} total games", gamesToProcess, totalGames);

                // Process games in parallel batches
                var totalProcessed = 0;
                var totalFailed = 0;
                var semaphore = new SemaphoreSlim(maxParallelism);
                var tasks = new List<Task<(int processed, int failed)>>();

                // Fetch games in chunks to avoid loading everything into memory
                const int fetchChunkSize = 2000; // Larger chunks for better performance
                var processedChunks = 0;
                
                for (int offset = skipCount; offset < totalGames; offset += fetchChunkSize)
                {
                    var games = await GetGamesBatchForIndexing(offset, fetchChunkSize);
                    if (games.Count == 0) break;

                    _logger.LogDebug("Fetched {Count} games at offset {Offset}", games.Count, offset);

                    // Split this chunk into processing batches
                    var batches = games.Chunk(batchSize).ToList();
                    
                    foreach (var batch in batches)
                    {
                        tasks.Add(ProcessGameBatchAsync(batch.ToList(), semaphore));
                    }

                    _logger.LogDebug("Created {BatchCount} processing tasks from chunk, total tasks: {TotalTasks}", batches.Count, tasks.Count);

                    // Process accumulated tasks more frequently for better progress tracking
                    if (tasks.Count >= maxParallelism || offset + fetchChunkSize >= totalGames)
                    {
                        var completedTasks = await Task.WhenAll(tasks);
                        var batchProcessed = completedTasks.Sum(r => r.processed);
                        var batchFailed = completedTasks.Sum(r => r.failed);
                        
                        totalProcessed += batchProcessed;
                        totalFailed += batchFailed;
                        processedChunks++;
                        
                        tasks.Clear();

                        // Progress reporting with better frequency
                        if (processedChunks % 5 == 0 || offset + fetchChunkSize >= totalGames)
                        {
                            var elapsed = stopwatch.Elapsed;
                            var rate = totalProcessed > 0 ? totalProcessed / elapsed.TotalMinutes : 0;
                            var eta = totalProcessed > 0 ? (gamesToProcess - totalProcessed) * elapsed.TotalMinutes / totalProcessed : double.PositiveInfinity;
                            
                            _logger.LogInformation("Progress: {Processed}/{Total} games indexed ({Failed} failed) - " +
                                "Rate: {Rate:F1} games/min - ETA: {ETA:F1} minutes - Chunk {ChunkNum}", 
                                totalProcessed, gamesToProcess, totalFailed, rate, eta, processedChunks);
                        }
                        
                        // Periodic verification every 10 chunks
                        if (processedChunks % 10 == 0)
                        {
                            await VerifyIndexingProgress(totalProcessed);
                        }
                    }
                }

                // Process remaining tasks
                if (tasks.Count != 0)
                {
                    var completedTasks = await Task.WhenAll(tasks);
                    totalProcessed += completedTasks.Sum(r => r.processed);
                    totalFailed += completedTasks.Sum(r => r.failed);
                }

                stopwatch.Stop();
                var finalRate = totalProcessed / stopwatch.Elapsed.TotalMinutes;
                
                _logger.LogInformation("Parallel bulk indexing completed: {Processed} games indexed, {Failed} failed " +
                    "in {Duration:F1} minutes (Rate: {Rate:F1} games/min)", 
                    totalProcessed, totalFailed, stopwatch.Elapsed.TotalMinutes, finalRate);

                return totalFailed == 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed during parallel bulk indexing");
                return false;
            }
        }

        /// <summary>
        /// Processes a single batch of games with semaphore control for parallelism
        /// </summary>
        private async Task<(int processed, int failed)> ProcessGameBatchAsync(List<Backend.Models.Game.Game> games, SemaphoreSlim semaphore)
        {
            await semaphore.WaitAsync();
            try
            {
                _logger.LogInformation("BATCH START: Processing batch of {Count} games", games.Count);
                var processed = 0;
                var failed = 0;

                // Generate embeddings for all games in the batch
                var gameEmbeddings = new List<(string id, float[] vector, Dictionary<string, object> payload)>();

                // Prepare all game inputs for batch processing
                var gameInputs = new List<(Backend.Models.Game.Game game, GameEmbeddingInput input)>();
                
                foreach (var game in games)
                {
                    try
                    {
                        var gameInput = MapGameToEmbeddingInput(game);
                        gameInputs.Add((game, gameInput));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to prepare game for embedding: {GameId} - {GameName}", game.Id, game.Name);
                        failed++;
                    }
                }

                // Generate embeddings in batch using GPU acceleration
                if (gameInputs.Count > 0)
                {
                    try
                    {
                        _logger.LogInformation("EMBEDDING: Starting generation for {Count} games", gameInputs.Count);
                        var inputs = gameInputs.Select(gi => gi.input).ToList();
                        
                        _logger.LogInformation("EMBEDDING: Service type is {ServiceType}", _embeddingService.GetType().Name);
                        
                        // Use the new batch method that supports GPU acceleration
                        List<float[]> embeddings;
                        if (_embeddingService is SentenceTransformerEmbeddingService stService)
                        {
                            _logger.LogInformation("EMBEDDING: Using GPU-accelerated batch processing");
                            // Use GPU-accelerated batch processing
                            embeddings = await stService.GenerateGameEmbeddingsBatchAsync(inputs);
                        }
                        else
                        {
                            _logger.LogInformation("EMBEDDING: Using fallback batch method");
                            // Fallback to existing batch method
                            embeddings = await _embeddingService.ProcessGamesInBatch(inputs);
                        }
                        
                        _logger.LogInformation("EMBEDDING: Generated {EmbeddingCount} embeddings for {InputCount} games", 
                            embeddings?.Count ?? -1, gameInputs.Count);
                            
                        if (embeddings == null)
                        {
                            _logger.LogError("EMBEDDING: Embeddings returned NULL!");
                        }
                        
                        // Create game embeddings with payloads
                        if (embeddings != null && embeddings.Count > 0)
                        {
                            _logger.LogInformation("EMBEDDING: Processing {EmbeddingCount} embeddings into payloads", embeddings.Count);
                            for (int i = 0; i < gameInputs.Count && i < embeddings.Count; i++)
                            {
                                var (game, _) = gameInputs[i];
                                var embedding = embeddings[i];
                                
                                if (embedding == null || embedding.Length == 0)
                                {
                                    _logger.LogWarning("EMBEDDING: Empty embedding at index {Index} for game: {GameId} - {GameName}", i, game.Id, game.Name);
                                    failed++;
                                    continue;
                                }
                                
                                _logger.LogDebug("EMBEDDING: Valid embedding at index {Index} with {Dimensions} dimensions for game {GameId}", 
                                    i, embedding.Length, game.Id);
                                
                                var payload = new Dictionary<string, object>
                                {
                                    ["name"] = game.Name ?? "",
                                    ["summary"] = game.Summary ?? "",
                                    ["genres"] = game.GameGenres?.Select(gg => gg.Genre.Name).ToList() ?? new List<string>(),
                                    ["platforms"] = game.GamePlatforms?
                                        .SelectMany(gp =>
                                            new List<string> { gp.Platform.Name, gp.Platform.AlternativeName })
                                        .Where(gp => !string.IsNullOrEmpty(gp))
                                        .ToList() ?? [],
                                    ["game_modes"] = game.GameModes?.Select(gm => gm.GameMode.Name).ToList() ?? new List<string>(),
                                    ["player_perspectives"] = game.GamePlayerPerspectives?.Select(gpp => gpp.PlayerPerspective.Name).ToList() ?? new List<string>(),
                                    ["release_date"] = game.FirstReleaseDate,
                                    ["companies"] = game.GameCompanies?.Select(gc => gc.Company.Name).ToList() ?? [],
                                    ["game_type"] = game.GameType?.Type ?? "",
                                    ["rating"] = (double)(game.Rating ?? 0.0m)
                                };
                                
                                gameEmbeddings.Add((game.Id.ToString(), embedding, payload));
                                processed++;
                            }
                            _logger.LogInformation("EMBEDDING: Created {GameEmbeddingCount} game embeddings from {ProcessedCount} games", 
                                gameEmbeddings.Count, processed);
                        }
                        else
                        {
                            _logger.LogError("EMBEDDING: No embeddings to process - embeddings is null or empty");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to generate batch embeddings for {Count} games", gameInputs.Count);
                        failed += gameInputs.Count;
                    }
                }

                // Bulk upsert the entire batch if we have any successful embeddings
                if (gameEmbeddings.Count > 0)
                {
                    _logger.LogInformation("UPSERT: Attempting to upsert {Count} game embeddings to vector database", gameEmbeddings.Count);
                    var upsertSuccess = await _vectorDatabase.UpsertVectorsBulkAsync(CollectionName, gameEmbeddings);
                    if (!upsertSuccess)
                    {
                        _logger.LogError("UPSERT FAILED: Bulk upsert failed for batch of {Count} games", gameEmbeddings.Count);
                        failed += gameEmbeddings.Count;
                        processed -= gameEmbeddings.Count;
                    }
                    else
                    {
                        _logger.LogInformation("UPSERT SUCCESS: Successfully upserted {Count} game embeddings", gameEmbeddings.Count);
                    }
                }
                else
                {
                    _logger.LogError("NO EMBEDDINGS: No valid embeddings generated for batch of {Count} games", games.Count);
                }

                _logger.LogInformation("BATCH END: {Processed} processed, {Failed} failed out of {Total} games", processed, failed, games.Count);
                return (processed, failed);
            }
            finally
            {
                semaphore.Release();
            }
        }

        /// <summary>
        /// Verify indexing progress by checking actual point count in vector database
        /// </summary>
        private async Task VerifyIndexingProgress(int expectedCount)
        {
            try
            {
                var actualCount = await _vectorDatabase.CollectionExistsAsync(CollectionName) 
                    ? await GetCollectionPointCount() 
                    : 0;
                    
                _logger.LogInformation("Indexing verification: Expected {Expected}, Actual {Actual} points in collection", 
                    expectedCount, actualCount);
                    
                if (actualCount < expectedCount * 0.9) // Allow 10% tolerance for async operations
                {
                    _logger.LogWarning("Potential indexing lag detected: {Actual} points vs {Expected} expected", 
                        actualCount, expectedCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to verify indexing progress");
            }
        }

        /// <summary>
        /// Get current point count from the vector database collection
        /// </summary>
        private async Task<long> GetCollectionPointCount()
        {
            return await _vectorDatabase.GetPointCountAsync(CollectionName);
        }

        /// <summary>
        /// Optimized database query for batch fetching games with minimal includes
        /// </summary>
        private async Task<List<Backend.Models.Game.Game>> GetGamesBatchForIndexing(int offset, int limit)
        {
            return await _context.Games
                .AsNoTracking() // Critical for performance - no change tracking
                                //.Include(g => g.Covers) // Include all covers, will filter in memory if needed
                .Include(g => g.GameGenres).ThenInclude(gg => gg.Genre)
                .Include(g => g.GamePlatforms).ThenInclude(gp => gp.Platform)
                .Include(g => g.GameModes).ThenInclude(gm => gm.GameMode)
                .Include(g => g.GamePlayerPerspectives).ThenInclude(gpp => gpp.PlayerPerspective)
                .Include(g => g.GameType)
                .Include(g => g.GameCompanies).ThenInclude(gc => gc.Company)
                .AsSplitQuery() // Prevent cartesian explosion
                .OrderBy(g => g.Id) // Consistent ordering for pagination
                .Skip(offset)
                .Take(limit)
                .ToListAsync();
        }
    }
}