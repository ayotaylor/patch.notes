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
        private readonly PlatformAliasService _platformAliasService;
        private const string GAMES_COLLECTION = "games";

        public GameIndexingService(
            ApplicationDbContext context,
            IVectorDatabase vectorDatabase,
            IEmbeddingService embeddingService,
            ILogger<GameIndexingService> logger,
            ISemanticConfigurationService configService,
            PlatformAliasService platformAliasService,
            ISemanticKeywordCache? semanticKeywordCache = null)
        {
            _context = context;
            _vectorDatabase = vectorDatabase;
            _embeddingService = embeddingService;
            _logger = logger;
            _configService = configService;
            _platformAliasService = platformAliasService;
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
                            {"platforms", ExpandPlatformsWithAliases(game.GamePlatforms?.Select(gp => gp.Platform.Name).ToList() ?? [])},
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

                // Store metadata needed for filtering and retrieval
                var payload = new Dictionary<string, object>
                {
                    {"name", game.Name},
                    {"cover_url", game.Covers?.FirstOrDefault()?.Url ?? ""},
                    {"genres", game.GameGenres?.Select(gg => gg.Genre.Name).ToList() ?? []},
                    {"platforms", ExpandPlatformsWithAliases(game.GamePlatforms?.Select(gp => gp.Platform.Name).ToList() ?? [])},
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

        public async Task<List<VectorSearchResult>> SearchSimilarGamesAsync(float[] queryEmbedding, int limit = 20, QueryAnalysis? queryAnalysis = null)
        {
            try
            {
                // Construct filters from query analysis
                var filters = ConstructFiltersFromQuery(queryAnalysis);

                // Primary search with enhanced embedding and filters
                var results = await _vectorDatabase.SearchAsync(GAMES_COLLECTION, queryEmbedding, limit, filters);

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
        /// Construct filters for vector database search based on query analysis
        /// </summary>
        private Dictionary<string, object>? ConstructFiltersFromQuery(QueryAnalysis? queryAnalysis)
        {
            if (queryAnalysis == null)
                return null;

            var filters = new Dictionary<string, object>();

            // Filter by genres if specified
            if (queryAnalysis.Genres.Count > 0)
            {
                filters["genres"] = new Dictionary<string, object>
                {
                    ["$in"] = queryAnalysis.Genres
                };
            }

            // Filter by platforms if specified
            if (queryAnalysis.Platforms.Count > 0)
            {
                // Expand platforms with aliases for better matching
                var expandedPlatforms = queryAnalysis.Platforms
                    .SelectMany(platform => new[] { platform }.Concat(_platformAliasService.GetAllPlatformAliases(platform)))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                filters["platforms"] = new Dictionary<string, object>
                {
                    ["$in"] = expandedPlatforms
                };
            }

            // Filter by game modes if specified
            if (queryAnalysis.GameModes.Count > 0)
            {
                filters["game_modes"] = new Dictionary<string, object>
                {
                    ["$in"] = queryAnalysis.GameModes
                };
            }

            // Filter by player perspectives if specified
            if (queryAnalysis.PlayerPerspectives.Count > 0)
            {
                filters["player_perspectives"] = new Dictionary<string, object>
                {
                    ["$in"] = queryAnalysis.PlayerPerspectives
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
                Platforms = ExpandPlatformsWithAliases(game.GamePlatforms?.Select(gp => gp.Platform.Name).ToList() ?? new List<string>()),
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

        /// <summary>
        /// Expands platform names to include aliases for better matching
        /// </summary>
        private List<string> ExpandPlatformsWithAliases(List<string> platforms)
        {
            var expandedPlatforms = new List<string>();

            foreach (var platform in platforms.Where(p => !string.IsNullOrWhiteSpace(p)))
            {
                // Add original platform
                expandedPlatforms.Add(platform);

                // Add up to 2 aliases to enrich embedding without text bloat
                var aliases = _platformAliasService.GetAllPlatformAliases(platform);
                expandedPlatforms.AddRange(aliases.Where(a => !string.Equals(a, platform, StringComparison.OrdinalIgnoreCase)).Take(2));
            }

            return expandedPlatforms.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
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
    }
}