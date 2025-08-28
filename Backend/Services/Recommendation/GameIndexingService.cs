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
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;
        private readonly ISemanticKeywordCache? _semanticKeywordCache;
        private const string GAMES_COLLECTION = "games";
        private const string SEMANTIC_CACHE_KEY_PREFIX = "semantic_keywords_";
        private readonly TimeSpan CACHE_DURATION = TimeSpan.FromHours(24);
        private SemanticKeywordConfig? _semanticConfig;
        private EmbeddingDimensions? _embeddingDimensions;

        public GameIndexingService(
            ApplicationDbContext context,
            IVectorDatabase vectorDatabase,
            IEmbeddingService embeddingService,
            ILogger<GameIndexingService> logger,
            IMemoryCache cache,
            IConfiguration configuration,
            ISemanticKeywordCache? semanticKeywordCache = null)
        {
            _context = context;
            _vectorDatabase = vectorDatabase;
            _embeddingService = embeddingService;
            _logger = logger;
            _cache = cache;
            _configuration = configuration;
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
                    var success = await _vectorDatabase.CreateCollectionAsync(GAMES_COLLECTION, totalDimensions);
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

                    // Process games in parallel for better performance
                    var indexingTasks = games.Select(IndexGameAsync).ToList();
                    var results = await Task.WhenAll(indexingTasks);
                    var successCount = results.Count(success => success);
                    indexed += successCount;

                    var failedCount = games.Count - successCount;
                    if (failedCount > 0)
                    {
                        _logger.LogWarning("Failed to index {FailedCount} out of {TotalCount} games in batch", failedCount, games.Count);
                    }

                    skip += batchSize;
                    
                    // Enhanced logging with performance metrics
                    var elapsed = DateTime.UtcNow - processingStartTime;
                    var gamesPerSecond = indexed > 0 ? Math.Round(indexed / elapsed.TotalSeconds, 2) : 0;
                    _logger.LogInformation("Indexed batch: {BatchSuccess}/{BatchTotal} games. Total: {Total} games in {Elapsed:mm\\:ss} ({Rate} games/sec)", 
                        successCount, games.Count, indexed, elapsed, gamesPerSecond);
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
        /// Optimized method to fetch games with all required relationships using manual split queries
        /// </summary>
        private async Task<List<Backend.Models.Game.Game>> GetGamesWithRelationshipsBatchAsync(int skip, int take)
        {
            try
            {
                // First, get the basic game data with ordering for consistent pagination
                var gameIds = await _context.Games
                    .OrderBy(g => g.Id)
                    .Skip(skip)
                    .Take(take)
                    .Select(g => g.Id)
                    .ToListAsync();

                if (gameIds.Count == 0)
                    return new List<Backend.Models.Game.Game>();

                // Load relationships sequentially to avoid DbContext concurrency issues
                var games = await _context.Games
                    .AsNoTracking()
                    .Where(g => gameIds.Contains(g.Id))
                    .ToListAsync();

                var genres = await _context.GameGenres
                    .Include(gg => gg.Genre)
                    .Where(gg => gameIds.Contains(gg.GameId))
                    .ToListAsync();

                var platforms = await _context.GamePlatforms
                    .Include(gp => gp.Platform)
                    .Where(gp => gameIds.Contains(gp.GameId))
                    .ToListAsync();

                var gameModes = await _context.GameModeGames
                    .Include(gm => gm.GameMode)
                    .Where(gm => gameIds.Contains(gm.GameId))
                    .ToListAsync();

                var perspectives = await _context.GamePlayerPerspectives
                    .Include(gpp => gpp.PlayerPerspective)
                    .Where(gpp => gameIds.Contains(gpp.GameId))
                    .ToListAsync();

                var covers = await _context.Covers
                    .Where(c => gameIds.Contains(c.GameId))
                    .ToListAsync();

                // Manually populate relationships for better control over data structure
                foreach (var game in games)
                {
                    game.GameGenres = genres.Where(g => g.GameId == game.Id).ToList();
                    game.GamePlatforms = platforms.Where(p => p.GameId == game.Id).ToList();
                    game.GameModes = gameModes.Where(gm => gm.GameId == game.Id).ToList();
                    game.GamePlayerPerspectives = perspectives.Where(p => p.GameId == game.Id).ToList();
                    game.Covers = covers.Where(c => c.GameId == game.Id).ToList();
                }

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

        public async Task<List<VectorSearchResult>> SearchGamesWithFiltersAsync(
            float[] queryEmbedding,
            QueryAnalysis analysis,
            int limit = 20)
        {
            try
            {
                var filters = BuildEnhancedFilters(analysis);
                _logger.LogDebug("Built {FilterCount} filters for search", filters.Count);

                return await _vectorDatabase.SearchAsync(GAMES_COLLECTION, queryEmbedding, limit, filters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching games with filters");
                return new List<VectorSearchResult>();
            }
        }

        private Dictionary<string, object> BuildEnhancedFilters(QueryAnalysis analysis)
        {
            var filters = new Dictionary<string, object>();

            // Basic filters for exact matching
            AddBasicFilters(filters, analysis);

            // Enhanced semantic filters for broader matching
            AddSemanticFilters(filters, analysis);

            return filters;
        }

        private void AddBasicFilters(Dictionary<string, object> filters, QueryAnalysis analysis)
        {
            // Exact genre matching
            if (analysis.Genres?.Count > 0)
            {
                filters["genres"] = string.Join(",", analysis.Genres);
                _logger.LogDebug("Added genre filters: {Genres}", string.Join(",", analysis.Genres));
            }

            // Exact platform matching
            if (analysis.Platforms?.Count > 0)
            {
                filters["platforms"] = string.Join(",", analysis.Platforms);
                _logger.LogDebug("Added platform filters: {Platforms}", string.Join(",", analysis.Platforms));
            }

            // Exact game mode matching
            if (analysis.GameModes?.Count > 0)
            {
                filters["game_modes"] = string.Join(",", analysis.GameModes);
                _logger.LogDebug("Added game mode filters: {GameModes}", string.Join(",", analysis.GameModes));
            }

            // Release date range filtering
            if (analysis.ReleaseDateRange?.From.HasValue == true)
            {
                filters["release_year_from"] = analysis.ReleaseDateRange.From.Value.Year;
            }
            if (analysis.ReleaseDateRange?.To.HasValue == true)
            {
                filters["release_year_to"] = analysis.ReleaseDateRange.To.Value.Year;
            }
        }

        private void AddSemanticFilters(Dictionary<string, object> filters, QueryAnalysis analysis)
        {
            // Semantic genre expansion (broaden search using extracted semantic keywords)
            if (analysis.Genres?.Count > 0)
            {
                var expandedGenres = ExpandGenresSemanticaly(analysis.Genres);
                if (expandedGenres.Count > analysis.Genres.Count)
                {
                    filters["semantic_genres_expanded"] = string.Join(",", expandedGenres);
                    _logger.LogDebug("Expanded {OriginalCount} genres to {ExpandedCount} semantic genres", 
                        analysis.Genres.Count, expandedGenres.Count);
                }
            }

            // Platform alias expansion (match ps5, playstation 5, sony ps5, etc.)
            if (analysis.Platforms?.Count > 0)
            {
                var expandedPlatforms = PlatformAliasService.ExpandPlatformNamesForSearch(analysis.Platforms);
                filters["platform_aliases"] = string.Join(",", expandedPlatforms);
                _logger.LogDebug("Expanded {OriginalCount} platforms to {ExpandedCount} platform aliases", 
                    analysis.Platforms.Count, expandedPlatforms.Count);

                // Also add canonical platform names for exact matching
                var canonicalPlatforms = PlatformAliasService.NormalizePlatformNames(analysis.Platforms);
                filters["canonical_platforms"] = string.Join(",", canonicalPlatforms);
            }

            // Semantic mood matching (use extracted mood keywords)
            if (analysis.Moods?.Count > 0)
            {
                var expandedMoods = ExpandMoodsSemanticaly(analysis.Moods);
                filters["semantic_moods"] = string.Join(",", expandedMoods);
                _logger.LogDebug("Added semantic mood filters: {Moods}", string.Join(",", expandedMoods));
            }

            // Quality-based filtering (prefer games with rich semantic metadata)
            filters["prefer_semantic_enhanced"] = "true";
        }

        private List<string> ExpandGenresSemanticaly(List<string> originalGenres)
        {
            var expandedGenres = new HashSet<string>(originalGenres, StringComparer.OrdinalIgnoreCase);

            foreach (var genre in originalGenres)
            {
                var semanticExpansions = GetSemanticGenreExpansions(genre.ToLowerInvariant());
                foreach (var expansion in semanticExpansions)
                {
                    expandedGenres.Add(expansion);
                }
            }

            return expandedGenres.ToList();
        }

        private List<string> ExpandMoodsSemanticaly(List<string> originalMoods)
        {
            var expandedMoods = new HashSet<string>(originalMoods, StringComparer.OrdinalIgnoreCase);

            foreach (var mood in originalMoods)
            {
                var semanticExpansions = GetSemanticMoodExpansions(mood.ToLowerInvariant());
                foreach (var expansion in semanticExpansions)
                {
                    expandedMoods.Add(expansion);
                }
            }

            return expandedMoods.ToList();
        }

        private static List<string> GetSemanticGenreExpansions(string genre)
        {
            var expansions = new List<string>();

            switch (genre)
            {
                case "rpg":
                case "role-playing":
                    expansions.AddRange(new[] { "jrpg", "western rpg", "action rpg", "tactical rpg", "mmorpg", "character progression", "leveling", "quest-driven" });
                    break;
                case "action":
                    expansions.AddRange(new[] { "fast-paced", "combat-focused", "reflex-based", "adrenaline", "intense" });
                    break;
                case "adventure":
                    expansions.AddRange(new[] { "exploration", "story-driven", "narrative", "discovery", "atmospheric" });
                    break;
                case "strategy":
                    expansions.AddRange(new[] { "tactical", "planning", "resource management", "turn-based", "real-time strategy", "cerebral" });
                    break;
                case "horror":
                    expansions.AddRange(new[] { "scary", "frightening", "atmospheric", "dark", "psychological", "survival horror", "tension" });
                    break;
                case "puzzle":
                    expansions.AddRange(new[] { "brain teaser", "logic", "problem-solving", "cerebral", "satisfying" });
                    break;
                case "simulation":
                    expansions.AddRange(new[] { "realistic", "management", "building", "sandbox", "meditative" });
                    break;
                default:
                    // For unknown genres, add the original
                    break;
            }

            return expansions;
        }

        private static List<string> GetSemanticMoodExpansions(string mood)
        {
            var expansions = new List<string>();

            switch (mood)
            {
                case "dark":
                    expansions.AddRange(new[] { "serious", "grim", "atmospheric", "intense", "mature" });
                    break;
                case "light-hearted":
                    expansions.AddRange(new[] { "cheerful", "fun", "comedic", "uplifting", "family-friendly" });
                    break;
                case "intense":
                    expansions.AddRange(new[] { "thrilling", "adrenaline", "exciting", "heart-pounding", "challenging" });
                    break;
                case "relaxing":
                    expansions.AddRange(new[] { "peaceful", "calming", "meditative", "zen", "casual" });
                    break;
                case "atmospheric":
                    expansions.AddRange(new[] { "immersive", "moody", "environmental", "ambient" });
                    break;
                case "challenging":
                    expansions.AddRange(new[] { "difficult", "hardcore", "demanding", "punishing", "skill-based" });
                    break;
                default:
                    // For unknown moods, add the original
                    break;
            }

            return expansions;
        }

        private GameEmbeddingInput MapGameToEmbeddingInput(Backend.Models.Game.Game game)
        {
            var cacheKey = $"{SEMANTIC_CACHE_KEY_PREFIX}{game.Id}_{game.UpdatedAt:yyyyMMddHHmmss}";

            if (_cache.TryGetValue(cacheKey, out GameEmbeddingInput? cachedInput) && cachedInput != null)
            {
                _logger.LogDebug("Using cached semantic keywords for game: {GameName}", game.Name);
                return cachedInput;
            }

            var gameInput = new GameEmbeddingInput
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

            // Extract semantic keywords with comprehensive cross-category analysis
            ExtractSemanticKeywords(gameInput);

            // Cache the result
            _cache.Set(cacheKey, gameInput, CACHE_DURATION);
            _logger.LogDebug("Cached semantic keywords for game: {GameName}", game.Name);

            return gameInput;
        }

        private void LoadSemanticConfiguration()
        {
            try
            {
                var configPath = Path.Combine(AppContext.BaseDirectory, "Configuration", "DefaultSemanticKeywordMappings.json");
                if (File.Exists(configPath))
                {
                    var jsonContent = File.ReadAllText(configPath);
                    _semanticConfig = JsonSerializer.Deserialize<SemanticKeywordConfig>(jsonContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
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

        private SemanticKeywordConfig CreateDefaultSemanticConfig()
        {
            return new SemanticKeywordConfig
            {
                DefaultWeights = new SemanticWeights(),
                Dimensions = new EmbeddingDimensions()
            };
        }

        private void ExtractSemanticKeywords(GameEmbeddingInput gameInput)
        {
            if (_semanticConfig == null)
            {
                _logger.LogWarning("Semantic configuration not loaded, skipping keyword extraction");
                return;
            }

            var allExtractedKeywords = new HashSet<string>();
            var usePrecomputedCache = _semanticKeywordCache?.IsInitialized == true;

            if (usePrecomputedCache)
            {
                _logger.LogDebug("Using precomputed semantic keyword cache for game: {GameName}", gameInput.Name);

                // Extract from genres using precomputed cache
                foreach (var genre in gameInput.Genres)
                {
                    ExtractKeywordsFromPrecomputedCache("genre", genre, gameInput, allExtractedKeywords);
                }

                // Extract from platforms using precomputed cache
                foreach (var platform in gameInput.Platforms)
                {
                    ExtractKeywordsFromPrecomputedCache("platform", platform, gameInput, allExtractedKeywords);
                }

                // Extract from game modes using precomputed cache
                foreach (var gameMode in gameInput.GameModes)
                {
                    ExtractKeywordsFromPrecomputedCache("gamemode", gameMode, gameInput, allExtractedKeywords);
                }

                // Extract from player perspectives using precomputed cache
                foreach (var perspective in gameInput.PlayerPerspectives)
                {
                    ExtractKeywordsFromPrecomputedCache("perspective", perspective, gameInput, allExtractedKeywords);
                }

                // Try to find precomputed combinations
                ExtractKeywordsFromPrecomputedCombinations(gameInput, allExtractedKeywords);
            }
            else
            {
                _logger.LogDebug("Using real-time semantic keyword extraction for game: {GameName}", gameInput.Name);

                // Fallback to real-time extraction
                foreach (var genre in gameInput.Genres)
                {
                    ExtractKeywordsFromCategory(_semanticConfig.GenreMappings, genre, gameInput, allExtractedKeywords);
                }

                foreach (var platform in gameInput.Platforms)
                {
                    ExtractKeywordsFromCategory(_semanticConfig.PlatformMappings, platform, gameInput, allExtractedKeywords);
                }

                foreach (var gameMode in gameInput.GameModes)
                {
                    ExtractKeywordsFromCategory(_semanticConfig.GameModeMappings, gameMode, gameInput, allExtractedKeywords);
                }

                foreach (var perspective in gameInput.PlayerPerspectives)
                {
                    ExtractKeywordsFromCategory(_semanticConfig.PerspectiveMappings, perspective, gameInput, allExtractedKeywords);
                }
            }

            // Always extract from text content (not cached as it's game-specific)
            ExtractKeywordsFromText(gameInput, allExtractedKeywords);

            // Apply comprehensive cross-category analysis
            ApplyComprehensiveCrossCategoryBoosts(gameInput, allExtractedKeywords);

            // Calculate semantic weights
            CalculateSemanticWeights(gameInput);

            _logger.LogDebug("Extracted {Count} unique semantic keywords for game: {GameName} (using {Method})",
                allExtractedKeywords.Count, gameInput.Name, usePrecomputedCache ? "precomputed cache" : "real-time extraction");
        }

        private void ExtractKeywordsFromPrecomputedCache(
            string categoryType,
            string key,
            GameEmbeddingInput gameInput,
            HashSet<string> allExtractedKeywords)
        {
            if (_semanticKeywordCache == null) return;

            SemanticCategoryMapping? mapping = categoryType switch
            {
                "genre" => _semanticKeywordCache.GetGenreKeywords(key),
                "platform" => _semanticKeywordCache.GetPlatformKeywords(key),
                "gamemode" => _semanticKeywordCache.GetGameModeKeywords(key),
                "perspective" => _semanticKeywordCache.GetPerspectiveKeywords(key),
                _ => null
            };

            if (mapping != null)
            {
                SemanticUtilityService.AddMappingKeywords(mapping, gameInput, allExtractedKeywords);
                _logger.LogDebug("Used precomputed cache for {CategoryType}: {Key}", categoryType, key);
            }
            else
            {
                // Fallback to real-time extraction if not in cache
                var categoryMappings = categoryType switch
                {
                    "genre" => _semanticConfig?.GenreMappings,
                    "platform" => _semanticConfig?.PlatformMappings,
                    "gamemode" => _semanticConfig?.GameModeMappings,
                    "perspective" => _semanticConfig?.PerspectiveMappings,
                    _ => null
                };

                if (categoryMappings != null)
                {
                    ExtractKeywordsFromCategory(categoryMappings, key, gameInput, allExtractedKeywords);
                    _logger.LogDebug("Cache miss for {CategoryType}: {Key}, used real-time extraction", categoryType, key);
                }
            }
        }

        private void ExtractKeywordsFromPrecomputedCombinations(GameEmbeddingInput gameInput, HashSet<string> allExtractedKeywords)
        {
            if (_semanticKeywordCache == null) return;

            // Try common combinations based on game's genres and modes
            var possibleCombinations = new List<string>();

            // Generate genre combinations
            for (int i = 0; i < gameInput.Genres.Count; i++)
            {
                for (int j = i + 1; j < gameInput.Genres.Count; j++)
                {
                    possibleCombinations.Add($"{gameInput.Genres[i]} {gameInput.Genres[j]}");
                }
            }

            // Generate platform + genre combinations
            foreach (var platform in gameInput.Platforms.Take(2)) // Limit to avoid too many combinations
            {
                foreach (var genre in gameInput.Genres.Take(2))
                {
                    possibleCombinations.Add($"{platform} {genre}");
                }
            }

            // Check precomputed cache for these combinations
            foreach (var combination in possibleCombinations)
            {
                var mapping = _semanticKeywordCache.GetCombinationKeywords(combination);
                if (mapping != null)
                {
                    SemanticUtilityService.AddMappingKeywords(mapping, gameInput, allExtractedKeywords);
                    _logger.LogDebug("Used precomputed combination: {Combination}", combination);
                }
            }
        }

        private void ExtractKeywordsFromCategory(
            Dictionary<string, SemanticCategoryMapping> categoryMappings,
            string key,
            GameEmbeddingInput gameInput,
            HashSet<string> allExtractedKeywords)
        {
            SemanticCategoryMapping? mapping = null;

            // First try exact match
            if (categoryMappings.TryGetValue(key, out mapping))
            {
                SemanticUtilityService.AddMappingKeywords(mapping, gameInput, allExtractedKeywords);
                return;
            }

            // Enhanced fuzzy matching with safeguards
            var fuzzyMatch = SemanticUtilityService.FindBestFuzzyMatch(key, categoryMappings.Keys);
            if (fuzzyMatch != null && categoryMappings.TryGetValue(fuzzyMatch, out mapping))
            {
                SemanticUtilityService.AddMappingKeywords(mapping, gameInput, allExtractedKeywords);
                _logger.LogDebug("Used fuzzy match '{FuzzyMatch}' for key '{Key}' (similarity score: {Score})",
                    fuzzyMatch, key, SemanticUtilityService.CalculateSimilarityScore(key, fuzzyMatch));
            }
        }


        private void ExtractKeywordsFromText(GameEmbeddingInput gameInput, HashSet<string> allExtractedKeywords)
        {
            var combinedText = $"{gameInput.Summary} {gameInput.Storyline}".ToLowerInvariant();

            // Define text-based keyword patterns
            var textKeywordMappings = new Dictionary<string[], string[]>
            {
                [new[] { "magic", "wizard", "spell", "dragon", "medieval" }] = new[] { "fantasy" },
                [new[] { "space", "robot", "future", "technology", "cyber" }] = new[] { "sci-fi", "futuristic" },
                [new[] { "scary", "frightening", "terror", "nightmare", "haunted" }] = new[] { "horror", "scary" },
                [new[] { "funny", "humor", "comedy", "joke", "laugh" }] = new[] { "comedic", "light-hearted" },
                [new[] { "dark", "grim", "serious", "mature", "violence" }] = new[] { "dark", "mature" },
                [new[] { "beautiful", "stunning", "gorgeous", "artistic" }] = new[] { "visually stunning" },
                [new[] { "challenging", "difficult", "hard", "punishing" }] = new[] { "challenging", "hardcore" },
                [new[] { "relaxing", "calm", "peaceful", "zen", "meditative" }] = new[] { "relaxing", "peaceful" },
                [new[] { "multiplayer", "online", "friends", "team", "co-op" }] = new[] { "social", "multiplayer" },
                [new[] { "story", "narrative", "plot", "character", "dialogue" }] = new[] { "story-driven", "narrative" }
            };

            foreach (var (triggers, keywords) in textKeywordMappings)
            {
                if (triggers.Any(trigger => combinedText.Contains(trigger)))
                {
                    foreach (var keyword in keywords)
                    {
                        allExtractedKeywords.Add(keyword);

                        // Add to appropriate category
                        if (keyword.Contains("horror") || keyword.Contains("scary") || keyword.Contains("dark"))
                            gameInput.ExtractedMoodKeywords.Add(keyword);
                        else if (keyword.Contains("fantasy") || keyword.Contains("sci-fi"))
                            gameInput.ExtractedThemeKeywords.Add(keyword);
                        else if (keyword.Contains("challenging") || keyword.Contains("relaxing"))
                            gameInput.ExtractedAudienceKeywords.Add(keyword);
                        else if (keyword.Contains("multiplayer") || keyword.Contains("social"))
                            gameInput.ExtractedMechanicKeywords.Add(keyword);
                        else if (keyword.Contains("story") || keyword.Contains("narrative"))
                            gameInput.ExtractedMoodKeywords.Add(keyword);
                        else
                            gameInput.ExtractedMoodKeywords.Add(keyword); // Default to mood
                    }
                }
            }
        }

        private void ApplyComprehensiveCrossCategoryBoosts(GameEmbeddingInput gameInput, HashSet<string> allExtractedKeywords)
        {
            if (_semanticConfig?.CrossCategoryBoosts == null) return;

            var boostsApplied = new List<string>();

            // Apply configured cross-category boosts
            foreach (var (boostKey, boosts) in _semanticConfig.CrossCategoryBoosts)
            {
                foreach (var boost in boosts)
                {
                    bool shouldApplyBoost = boost.Condition.ToLower() switch
                    {
                        "all" => boost.BoostKeywords.All(k => allExtractedKeywords.Contains(k.ToLowerInvariant())),
                        "exact" => allExtractedKeywords.Contains(boost.TriggerKeyword.ToLowerInvariant()),
                        _ => allExtractedKeywords.Any(k => k.Contains(boost.TriggerKeyword.ToLowerInvariant()))
                    };

                    if (shouldApplyBoost)
                    {
                        ApplyBoostToCategory(gameInput, boost);
                        boostsApplied.Add($"{boost.TriggerKeyword}→{boost.Category}");
                    }
                }
            }

            // Apply intelligent cross-category analysis
            ApplyIntelligentCrossCategoryBoosts(gameInput, allExtractedKeywords, boostsApplied);

            gameInput.HierarchicalBoosts = boostsApplied;
        }

        private void ApplyIntelligentCrossCategoryBoosts(GameEmbeddingInput gameInput, HashSet<string> allExtractedKeywords, List<string> boostsApplied)
        {
            // RPG + Fantasy combination
            if (allExtractedKeywords.Any(k => k.Contains("rpg")) && allExtractedKeywords.Any(k => k.Contains("fantasy")))
            {
                gameInput.ExtractedThemeKeywords.AddRange(new[] { "epic", "quest", "adventure", "magic system" });
                gameInput.ExtractedMechanicKeywords.AddRange(new[] { "character customization", "skill progression" });
                boostsApplied.Add("RPG+Fantasy→Enhanced");
            }

            // Horror + Action combination
            if (allExtractedKeywords.Any(k => k.Contains("horror")) && allExtractedKeywords.Any(k => k.Contains("action")))
            {
                gameInput.ExtractedMechanicKeywords.AddRange(new[] { "survival horror", "resource scarcity" });
                gameInput.ExtractedMoodKeywords.AddRange(new[] { "intense", "thrilling" });
                boostsApplied.Add("Horror+Action→Survival");
            }

            // Strategy + Multiplayer combination
            if (allExtractedKeywords.Any(k => k.Contains("strategy")) && allExtractedKeywords.Any(k => k.Contains("multiplayer")))
            {
                gameInput.ExtractedMechanicKeywords.AddRange(new[] { "competitive strategy", "tactical multiplayer" });
                gameInput.ExtractedAudienceKeywords.AddRange(new[] { "strategy enthusiasts", "competitive" });
                boostsApplied.Add("Strategy+Multiplayer→Competitive");
            }

            // Sci-fi + Shooter combination
            if (allExtractedKeywords.Any(k => k.Contains("sci-fi")) && allExtractedKeywords.Any(k => k.Contains("shooter")))
            {
                gameInput.ExtractedThemeKeywords.AddRange(new[] { "futuristic warfare", "space combat" });
                gameInput.ExtractedArtStyleKeywords.AddRange(new[] { "futuristic", "high-tech" });
                boostsApplied.Add("SciFi+Shooter→FuturisticCombat");
            }

            // Indie + Artistic indicators
            if (gameInput.Platforms.Any(p => p.Contains("PC")) &&
                (allExtractedKeywords.Any(k => k.Contains("artistic") || k.Contains("unique") || k.Contains("creative"))))
            {
                gameInput.ExtractedAudienceKeywords.AddRange(new[] { "indie game lovers", "art enthusiasts" });
                gameInput.ExtractedArtStyleKeywords.AddRange(new[] { "unique", "artistic", "creative" });
                boostsApplied.Add("Indie+Artistic→CreativeAudience");
            }
        }

        private void ApplyBoostToCategory(GameEmbeddingInput gameInput, CrossCategoryBoost boost)
        {
            var keywordsToAdd = boost.BoostKeywords.ToList();

            switch (boost.Category.ToLower())
            {
                case "genre":
                    gameInput.ExtractedGenreKeywords.AddRange(keywordsToAdd);
                    break;
                case "mechanics":
                    gameInput.ExtractedMechanicKeywords.AddRange(keywordsToAdd);
                    break;
                case "themes":
                    gameInput.ExtractedThemeKeywords.AddRange(keywordsToAdd);
                    break;
                case "mood":
                    gameInput.ExtractedMoodKeywords.AddRange(keywordsToAdd);
                    break;
                case "artstyle":
                    gameInput.ExtractedArtStyleKeywords.AddRange(keywordsToAdd);
                    break;
                case "audience":
                    gameInput.ExtractedAudienceKeywords.AddRange(keywordsToAdd);
                    break;
                default:
                    gameInput.ExtractedMoodKeywords.AddRange(keywordsToAdd); // Default fallback
                    break;
            }
        }

        private void CalculateSemanticWeights(GameEmbeddingInput gameInput)
        {
            if (_semanticConfig?.DefaultWeights == null) return;

            var weights = _semanticConfig.DefaultWeights;

            gameInput.SemanticWeights = new Dictionary<string, float>
            {
                ["genre"] = weights.GenreWeight,
                ["mechanics"] = weights.MechanicsWeight,
                ["theme"] = weights.ThemeWeight,
                ["mood"] = weights.MoodWeight,
                ["artstyle"] = weights.ArtStyleWeight,
                ["audience"] = weights.AudienceWeight,
                ["hierarchical_boost"] = weights.HierarchicalBoostMultiplier,
                ["cross_category_boost"] = weights.CrossCategoryBoostMultiplier
            };

            // Apply dynamic weight adjustments based on content richness
            var totalKeywordCount = gameInput.ExtractedGenreKeywords.Count +
                                  gameInput.ExtractedMechanicKeywords.Count +
                                  gameInput.ExtractedThemeKeywords.Count +
                                  gameInput.ExtractedMoodKeywords.Count +
                                  gameInput.ExtractedArtStyleKeywords.Count +
                                  gameInput.ExtractedAudienceKeywords.Count;

            // Boost weights for games with rich semantic content
            if (totalKeywordCount > 15)
            {
                foreach (var key in gameInput.SemanticWeights.Keys.ToList())
                {
                    if (key.Contains("boost"))
                        gameInput.SemanticWeights[key] *= 1.2f; // Increase boost effectiveness
                }
            }

            // Reduce noise for games with minimal semantic content
            if (totalKeywordCount < 5)
            {
                foreach (var key in gameInput.SemanticWeights.Keys.ToList())
                {
                    if (!key.Contains("genre")) // Keep genre weight stable
                        gameInput.SemanticWeights[key] *= 0.8f;
                }
            }
        }

        private Dictionary<string, object> CreateGamePayload(Backend.Models.Game.Game game)
        {
            var releaseYear = game.FirstReleaseDate.HasValue
                ? DateTimeOffset.FromUnixTimeSeconds(game.FirstReleaseDate.Value).Year
                : 0;

            // Get the enhanced game input with semantic keywords
            var gameInput = MapGameToEmbeddingInput(game);

            var payload = new Dictionary<string, object>
            {
                // Basic game information
                {"name", game.Name},
                {"summary", game.Summary ?? ""},
                {"storyline", game.Storyline ?? ""},
                {"rating", game.Rating?.ToString() ?? "0"},
                {"release_year", releaseYear.ToString()},
                {"cover_url", game.Covers?.FirstOrDefault()?.Url ?? ""},
                {"igdb_id", game.IgdbId.ToString()},

                // Original game attributes (for exact matching)
                {"genres", string.Join(",", game.GameGenres?.Select(gg => gg.Genre.Name) ?? new List<string>())},
                {"platforms", string.Join(",", game.GamePlatforms?.Select(gp => gp.Platform.Name) ?? new List<string>())},
                {"game_modes", string.Join(",", game.GameModes?.Select(gm => gm.GameMode.Name) ?? new List<string>())},
                {"perspectives", string.Join(",", game.GamePlayerPerspectives?.Select(gpp => gpp.PlayerPerspective.Name) ?? new List<string>())},

                // Enhanced platform information with aliases
                {"platform_aliases", CreatePlatformAliasesString(game.GamePlatforms?.Select(gp => gp.Platform.Name) ?? new List<string>())},
                {"canonical_platforms", string.Join(",", PlatformAliasService.NormalizePlatformNames(game.GamePlatforms?.Select(gp => gp.Platform.Name) ?? new List<string>()))}
            };

            // Enhanced semantic metadata for better filtering and search
            AddSemanticMetadataToPayload(payload, gameInput);

            return payload;
        }

        private static void AddSemanticMetadataToPayload(Dictionary<string, object> payload, GameEmbeddingInput gameInput)
        {
            // Add extracted semantic keywords for advanced filtering
            if (HasEnhancedSemanticKeywords(gameInput))
            {
                payload["semantic_genres"] = string.Join(",", gameInput.ExtractedGenreKeywords);
                payload["semantic_mechanics"] = string.Join(",", gameInput.ExtractedMechanicKeywords);
                payload["semantic_themes"] = string.Join(",", gameInput.ExtractedThemeKeywords);
                payload["semantic_moods"] = string.Join(",", gameInput.ExtractedMoodKeywords);
                payload["semantic_artstyles"] = string.Join(",", gameInput.ExtractedArtStyleKeywords);
                payload["semantic_audiences"] = string.Join(",", gameInput.ExtractedAudienceKeywords);

                // Add hierarchical boost information for ranking insights
                payload["hierarchical_boosts"] = string.Join(",", gameInput.HierarchicalBoosts);

                // Add semantic weight distribution for quality scoring
                if (gameInput.SemanticWeights.Count > 0)
                {
                    var semanticWeightSum = gameInput.SemanticWeights.Values.Sum();
                    payload["semantic_weight_total"] = semanticWeightSum.ToString("F2");

                    // Category richness indicators
                    var categoryCount = gameInput.ExtractedGenreKeywords.Count +
                                      gameInput.ExtractedMechanicKeywords.Count +
                                      gameInput.ExtractedThemeKeywords.Count +
                                      gameInput.ExtractedMoodKeywords.Count +
                                      gameInput.ExtractedArtStyleKeywords.Count +
                                      gameInput.ExtractedAudienceKeywords.Count;
                    payload["semantic_keyword_count"] = categoryCount.ToString();
                }
            }

            // Add embedding confidence metrics (useful for result ranking)
            payload["embedding_method"] = HasEnhancedSemanticKeywords(gameInput) ? "semantic_enhanced" : "text_based";
        }

        private static bool HasEnhancedSemanticKeywords(GameEmbeddingInput gameInput)
        {
            return gameInput.ExtractedGenreKeywords.Count > 0 ||
                   gameInput.ExtractedMechanicKeywords.Count > 0 ||
                   gameInput.ExtractedThemeKeywords.Count > 0 ||
                   gameInput.ExtractedMoodKeywords.Count > 0 ||
                   gameInput.ExtractedArtStyleKeywords.Count > 0 ||
                   gameInput.ExtractedAudienceKeywords.Count > 0;
        }

        private static string CreatePlatformAliasesString(IEnumerable<string> platformNames)
        {
            var allAliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            foreach (var platform in platformNames)
            {
                var aliases = PlatformAliasService.GetAllPlatformAliases(platform);
                foreach (var alias in aliases)
                {
                    allAliases.Add(alias);
                }
            }
            
            return string.Join(",", allAliases);
        }
    }
}