using Backend.Configuration;
using Backend.Data;
using Backend.Services.Recommendation.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Backend.Services.Recommendation
{
    public class SemanticKeywordCache : ISemanticKeywordCache
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IMemoryCache _cache;
        private readonly ILogger<SemanticKeywordCache> _logger;
        private readonly ISemanticConfigurationService _configService;

        private const string CACHE_KEY_PREFIX = "precomputed_semantic_";
        private const string INIT_STATUS_KEY = "semantic_cache_initialized";
        private const string STATS_KEY = "semantic_cache_stats";
        private readonly TimeSpan CACHE_DURATION = TimeSpan.FromDays(1);

        public SemanticKeywordCache(
            IServiceScopeFactory scopeFactory,
            IMemoryCache cache,
            ILogger<SemanticKeywordCache> logger,
            ISemanticConfigurationService configService)
        {
            _scopeFactory = scopeFactory;
            _cache = cache;
            _logger = logger;
            _configService = configService;
        }

        public bool IsInitialized => _cache.TryGetValue(INIT_STATUS_KEY, out var initialized) && (bool)initialized!;

        /// <summary>
        /// Ensures cache is initialized - uses lazy initialization if not already done
        /// </summary>
        public async Task<bool> EnsureInitializedAsync()
        {
            if (IsInitialized)
                return true;

            _logger.LogInformation("Cache not initialized, performing lazy initialization...");
            return await InitializeCacheAsync();
        }

        public async Task<bool> InitializeCacheAsync()
        {
            var startTime = DateTime.UtcNow;
            _logger.LogInformation("Starting semantic keyword cache initialization");

            try
            {
                // Get all unique values from database with fallback handling
                var genres = await GetUniqueGenresAsync();
                var platforms = await GetUniquePlatformsAsync();
                var gameModes = await GetUniqueGameModesAsync();
                var perspectives = await GetUniquePerspectivesAsync();

                // Check if database queries returned any data
                if (genres.Count == 0 && platforms.Count == 0 && gameModes.Count == 0 && perspectives.Count == 0)
                {
                    _logger.LogWarning("All database queries returned empty results. This may indicate a database connection issue. Cache will be initialized with fallback data from configuration.");

                    // Use fallback data from semantic configuration
                    var semanticConfig = _configService.SemanticConfig;
                    if (semanticConfig != null)
                    {
                        genres = semanticConfig.GenreMappings.Keys.ToList();
                        platforms = semanticConfig.PlatformMappings.Keys.ToList();
                        gameModes = semanticConfig.GameModeMappings.Keys.ToList();
                        perspectives = semanticConfig.PerspectiveMappings.Keys.ToList();

                        _logger.LogInformation("Using fallback data from configuration: {GenreCount} genres, {PlatformCount} platforms, {GameModeCount} game modes, {PerspectiveCount} perspectives",
                            genres.Count, platforms.Count, gameModes.Count, perspectives.Count);
                    }
                }

                var stats = new SemanticCacheStats
                {
                    TotalGenres = genres.Count,
                    TotalPlatforms = platforms.Count,
                    TotalGameModes = gameModes.Count,
                    TotalPerspectives = perspectives.Count,
                    LastInitialized = DateTime.UtcNow
                };

                var totalKeywords = 0L;

                // Precompute semantic keywords for each category
                var config = _configService.SemanticConfig;
                totalKeywords += await PrecomputeCategoryKeywords("genre", genres, config?.GenreMappings);
                totalKeywords += await PrecomputeCategoryKeywords("platform", platforms, config?.PlatformMappings);
                totalKeywords += await PrecomputeCategoryKeywords("gamemode", gameModes, config?.GameModeMappings);
                totalKeywords += await PrecomputeCategoryKeywords("perspective", perspectives, config?.PerspectiveMappings);

                // Precompute semantic combinations
                var combinations = GenerateSemanticCombinations(genres, platforms, gameModes, perspectives);
                totalKeywords += await PrecomputeCombinationKeywords(combinations);

                stats.TotalCombinations = combinations.Count;
                stats.TotalKeywords = totalKeywords;
                stats.InitializationTime = DateTime.UtcNow - startTime;

                // Mark as initialized and cache stats
                _cache.Set(INIT_STATUS_KEY, true, CACHE_DURATION);
                _cache.Set(STATS_KEY, stats, CACHE_DURATION);

                _logger.LogInformation("Semantic keyword cache initialized successfully in {Duration}ms. Total keywords: {Keywords}",
                    stats.InitializationTime.TotalMilliseconds, totalKeywords);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize semantic keyword cache");
                return false;
            }
        }

        public async Task<bool> RefreshCacheAsync()
        {
            _logger.LogInformation("Manually refreshing semantic keyword cache");

            // Clear existing cache entries
            ClearCacheEntries();

            return await InitializeCacheAsync();
        }

        public SemanticCategoryMapping? GetGenreKeywords(string genre)
        {
            return GetCachedKeywords("genre", genre);
        }

        public SemanticCategoryMapping? GetPlatformKeywords(string platform)
        {
            return GetCachedKeywords("platform", platform);
        }

        public SemanticCategoryMapping? GetGameModeKeywords(string gameMode)
        {
            return GetCachedKeywords("gamemode", gameMode);
        }

        public SemanticCategoryMapping? GetPerspectiveKeywords(string perspective)
        {
            return GetCachedKeywords("perspective", perspective);
        }

        public SemanticCategoryMapping? GetCombinationKeywords(string combination)
        {
            return GetCachedKeywords("combination", combination);
        }

        public List<string> GetCachedGenres()
        {
            return GetCachedList("genre");
        }

        public List<string> GetCachedPlatforms()
        {
            return GetCachedList("platform");
        }

        public List<string> GetCachedGameModes()
        {
            return GetCachedList("gamemode");
        }

        public List<string> GetCachedPerspectives()
        {
            return GetCachedList("perspective");
        }

        public SemanticCacheStats GetCacheStats()
        {
            return _cache.TryGetValue(STATS_KEY, out var stats) && stats is SemanticCacheStats cacheStats
                ? cacheStats
                : new SemanticCacheStats();
        }

        private async Task<List<string>> GetUniqueGenresAsync()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                return await context.Genres
                    .Select(g => g.Name)
                    .Distinct()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve unique genres from database. Using fallback empty list.");
                return new List<string>();
            }
        }

        private async Task<List<string>> GetUniquePlatformsAsync()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                return await context.Platforms
                    .Select(p => p.Name)
                    .Distinct()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve unique platforms from database. Using fallback empty list.");
                return new List<string>();
            }
        }

        private async Task<List<string>> GetUniqueGameModesAsync()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                return await context.GameModes
                    .Select(gm => gm.Name)
                    .Distinct()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve unique game modes from database. Using fallback empty list.");
                return new List<string>();
            }
        }

        private async Task<List<string>> GetUniquePerspectivesAsync()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                return await context.PlayerPerspectives
                    .Select(pp => pp.Name)
                    .Distinct()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve unique perspectives from database. Using fallback empty list.");
                return new List<string>();
            }
        }

        private async Task<long> PrecomputeCategoryKeywords(
            string categoryType,
            List<string> items,
            Dictionary<string, SemanticCategoryMapping>? categoryMappings)
        {
            if (categoryMappings == null) return 0;

            var totalKeywords = 0L;
            var itemList = new List<string>();

            foreach (var item in items)
            {
                var mapping = ExtractSemanticMappingForItem(item, categoryMappings);
                if (mapping != null)
                {
                    var cacheKey = $"{CACHE_KEY_PREFIX}{categoryType}_{item.ToLowerInvariant()}";
                    _cache.Set(cacheKey, mapping, CACHE_DURATION);
                    itemList.Add(item);

                    totalKeywords += mapping.GenreKeywords.Count + mapping.MechanicKeywords.Count +
                                   mapping.ThemeKeywords.Count + mapping.MoodKeywords.Count +
                                   mapping.ArtStyleKeywords.Count + mapping.AudienceKeywords.Count;
                }
            }

            // Cache the list of items for this category
            _cache.Set($"{CACHE_KEY_PREFIX}{categoryType}_list", itemList, CACHE_DURATION);

            _logger.LogDebug("Precomputed keywords for {Count} {Category} items", itemList.Count, categoryType);
            return totalKeywords;
        }

        private async Task<long> PrecomputeCombinationKeywords(List<string> combinations)
        {
            var totalKeywords = 0L;
            var cachedCombinations = new List<string>();

            foreach (var combination in combinations)
            {
                var mapping = ExtractSemanticMappingForCombination(combination);
                if (mapping != null && HasMeaningfulKeywords(mapping))
                {
                    var cacheKey = $"{CACHE_KEY_PREFIX}combination_{combination.ToLowerInvariant()}";
                    _cache.Set(cacheKey, mapping, CACHE_DURATION);
                    cachedCombinations.Add(combination);

                    totalKeywords += CountTotalKeywords(mapping);
                }
            }

            _cache.Set($"{CACHE_KEY_PREFIX}combination_list", cachedCombinations, CACHE_DURATION);
            _logger.LogDebug("Precomputed keywords for {CachedCount}/{TotalCount} quality combinations", 
                cachedCombinations.Count, combinations.Count);
            
            return totalKeywords;
        }

        private static bool HasMeaningfulKeywords(SemanticCategoryMapping mapping)
        {
            return CountTotalKeywords(mapping) >= SemanticCombinationConfig.MinKeywordsForCaching;
        }

        private static long CountTotalKeywords(SemanticCategoryMapping mapping)
        {
            return mapping.GenreKeywords.Count + mapping.MechanicKeywords.Count +
                   mapping.ThemeKeywords.Count + mapping.MoodKeywords.Count +
                   mapping.ArtStyleKeywords.Count + mapping.AudienceKeywords.Count +
                   mapping.PlatformType.Count + mapping.EraKeywords.Count +
                   mapping.CapabilityKeywords.Count + mapping.PlayerInteractionKeywords.Count +
                   mapping.ScaleKeywords.Count + mapping.CommunicationKeywords.Count +
                   mapping.ViewpointKeywords.Count + mapping.ImmersionKeywords.Count +
                   mapping.InterfaceKeywords.Count;
        }

        private T? ExtractSemanticMappingForItem<T>(
            string item,
            Dictionary<string, T> categoryMappings)
        {
            // First try exact match
            if (categoryMappings.TryGetValue(item, out var exactMapping))
            {
                return exactMapping;
            }

            // Use fuzzy matching logic from utility service
            var fuzzyMatch = SemanticUtilityService.FindBestFuzzyMatch(item, categoryMappings.Keys);
            if (fuzzyMatch != null && categoryMappings.TryGetValue(fuzzyMatch, out var fuzzyMapping))
            {
                _logger.LogDebug("Used fuzzy match '{FuzzyMatch}' for item '{Item}'", fuzzyMatch, item);
                return fuzzyMapping;
            }

            return default;
        }

        private SemanticCategoryMapping? ExtractSemanticMappingForCombination(string combination)
        {
            // For combinations like "Fantasy RPG", "Horror Action", etc.
            var parts = combination.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var combinedMapping = new SemanticCategoryMapping();

            foreach (var part in parts)
            {
                // Try each category mapping
                var config = _configService.SemanticConfig;
                var genreMapping = ExtractSemanticMappingForItem(part, config?.GenreMappings);
                var platformMapping = ExtractSemanticMappingForItem(part, config?.PlatformMappings);
                var gameModeMapping = ExtractSemanticMappingForItem(part, config?.GameModeMappings);
                var perspectiveMapping = ExtractSemanticMappingForItem(part, config?.PerspectiveMappings);

                // Merge all found mappings
                MergeMappings(combinedMapping, genreMapping);
                MergeMappings(combinedMapping, platformMapping);
                MergeMappings(combinedMapping, gameModeMapping);
                MergeMappings(combinedMapping, perspectiveMapping);
            }

            return combinedMapping.GenreKeywords.Any() || combinedMapping.MechanicKeywords.Any() ||
                   combinedMapping.ThemeKeywords.Any() || combinedMapping.MoodKeywords.Any() ||
                   combinedMapping.ArtStyleKeywords.Any() || combinedMapping.AudienceKeywords.Any()
                ? combinedMapping
                : null;
        }

        private void MergeMappings(SemanticCategoryMapping target, SemanticCategoryMapping? source)
        {
            if (source == null) return;

            target.GenreKeywords.AddRange(source.GenreKeywords.Where(k => !target.GenreKeywords.Contains(k)));
            target.MechanicKeywords.AddRange(source.MechanicKeywords.Where(k => !target.MechanicKeywords.Contains(k)));
            target.ThemeKeywords.AddRange(source.ThemeKeywords.Where(k => !target.ThemeKeywords.Contains(k)));
            target.MoodKeywords.AddRange(source.MoodKeywords.Where(k => !target.MoodKeywords.Contains(k)));
            target.ArtStyleKeywords.AddRange(source.ArtStyleKeywords.Where(k => !target.ArtStyleKeywords.Contains(k)));
            target.AudienceKeywords.AddRange(source.AudienceKeywords.Where(k => !target.AudienceKeywords.Contains(k)));
        }


        private List<string> GenerateSemanticCombinations(List<string> genres, List<string> platforms, List<string> gameModes, List<string> perspectives)
        {
            var combinations = new List<string>();
            var config = _configService.SemanticConfig;
            
            if (config == null) return combinations;

            combinations.AddRange(GenerateGenreCombinations(genres, config.GenreMappings));
            combinations.AddRange(GeneratePlatformGenreCombinations(platforms, genres, config));
            combinations.AddRange(GenerateGameModeGenreCombinations(gameModes, genres, config));
            combinations.AddRange(GeneratePerspectiveGenreCombinations(perspectives, genres, config));

            return combinations.Distinct().Take(SemanticCombinationConfig.MaxCombinations).ToList();
        }

        private List<string> GenerateGenreCombinations(List<string> genres, Dictionary<string, SemanticCategoryMapping> genreMappings)
        {
            var combinations = new List<string>();

            // Theme-based combinations
            var genresByTheme = genres
                .Where(g => genreMappings.ContainsKey(g))
                .GroupBy(g => genreMappings[g].ThemeKeywords.FirstOrDefault() ?? string.Empty)
                .Where(group => !string.IsNullOrEmpty(group.Key) && group.Count() > 1);

            foreach (var themeGroup in genresByTheme)
            {
                var genreList = themeGroup.Take(SemanticCombinationConfig.MaxGenresPerTheme).ToList();
                for (int i = 0; i < genreList.Count - 1; i++)
                {
                    for (int j = i + 1; j < genreList.Count; j++)
                    {
                        combinations.Add($"{genreList[i]} {genreList[j]}");
                    }
                }
            }

            // Popular proven combinations
            foreach (var (primary, secondary) in SemanticCombinationConfig.PopularGenreCombinations)
            {
                var primaryGenre = genres.FirstOrDefault(g => g.Contains(primary, StringComparison.OrdinalIgnoreCase));
                var secondaryGenre = genres.FirstOrDefault(g => g.Contains(secondary, StringComparison.OrdinalIgnoreCase));
                
                if (primaryGenre != null && secondaryGenre != null && primaryGenre != secondaryGenre)
                {
                    combinations.Add($"{primaryGenre} {secondaryGenre}");
                }
            }

            return combinations;
        }

        private List<string> GeneratePlatformGenreCombinations(List<string> platforms, List<string> genres, SemanticKeywordConfig config)
        {
            var combinations = new List<string>();

            var platformsByEra = platforms
                .Where(p => config.PlatformMappings.ContainsKey(p))
                .GroupBy(p => ClassifyPlatformEra(config.PlatformMappings[p].EraKeywords))
                .Where(group => !string.IsNullOrEmpty(group.Key));

            foreach (var eraGroup in platformsByEra)
            {
                var eraPlatforms = eraGroup.Take(SemanticCombinationConfig.MaxPlatformsPerEra).ToList();
                var compatibleGenres = GetEraCompatibleGenres(eraGroup.Key, genres, config.GenreMappings);

                foreach (var platform in eraPlatforms)
                {
                    foreach (var genre in compatibleGenres.Take(SemanticCombinationConfig.MaxGenresPerPlatform))
                    {
                        combinations.Add($"{platform} {genre}");
                    }
                }
            }

            return combinations;
        }

        private List<string> GenerateGameModeGenreCombinations(List<string> gameModes, List<string> genres, SemanticKeywordConfig config)
        {
            var combinations = new List<string>();

            foreach (var gameMode in gameModes.Take(SemanticCombinationConfig.MaxPlatformsPerEra))
            {
                if (!config.GameModeMappings.ContainsKey(gameMode)) continue;

                var gameModeMapping = config.GameModeMappings[gameMode];
                var compatibleGenres = genres
                    .Where(g => config.GenreMappings.ContainsKey(g))
                    .Where(g => IsGameModeGenreCompatible(gameModeMapping, config.GenreMappings[g]))
                    .Take(SemanticCombinationConfig.MaxGenresPerGameMode);

                foreach (var genre in compatibleGenres)
                {
                    combinations.Add($"{gameMode} {genre}");
                }
            }

            return combinations;
        }

        private List<string> GeneratePerspectiveGenreCombinations(List<string> perspectives, List<string> genres, SemanticKeywordConfig config)
        {
            var combinations = new List<string>();

            foreach (var perspective in perspectives.Take(SemanticCombinationConfig.MaxPlatformsPerEra))
            {
                if (!config.PerspectiveMappings.ContainsKey(perspective)) continue;

                var perspectiveMapping = config.PerspectiveMappings[perspective];
                var compatibleGenres = genres
                    .Where(g => config.GenreMappings.ContainsKey(g))
                    .Where(g => IsPerspectiveGenreCompatible(perspectiveMapping, config.GenreMappings[g]))
                    .Take(SemanticCombinationConfig.MaxGenresPerPerspective);

                foreach (var genre in compatibleGenres)
                {
                    combinations.Add($"{perspective} {genre}");
                }
            }

            return combinations;
        }

        private static string ClassifyPlatformEra(List<string> eraKeywords)
        {
            if (eraKeywords.Any(k => SemanticCombinationConfig.ModernEraKeywords.Contains(k, StringComparer.OrdinalIgnoreCase)))
                return "modern";
            if (eraKeywords.Any(k => SemanticCombinationConfig.RetroEraKeywords.Contains(k, StringComparer.OrdinalIgnoreCase)))
                return "retro";
            return string.Empty;
        }

        private List<string> GetEraCompatibleGenres(string era, List<string> genres, Dictionary<string, SemanticCategoryMapping> genreMappings)
        {
            return genres
                .Where(g => genreMappings.ContainsKey(g))
                .Where(g => era == "modern" || HasClassicMechanics(genreMappings[g]))
                .ToList();
        }

        private static bool HasClassicMechanics(SemanticCategoryMapping genreMapping)
        {
            return genreMapping.MechanicKeywords.Any(m => 
                SemanticCombinationConfig.RetroEraKeywords.Any(retro => 
                    m.Contains(retro, StringComparison.OrdinalIgnoreCase)));
        }

        private static bool IsGameModeGenreCompatible(SemanticCategoryMapping gameModeMapping, SemanticCategoryMapping genreMapping)
        {
            return gameModeMapping.AudienceKeywords.Intersect(genreMapping.AudienceKeywords, StringComparer.OrdinalIgnoreCase).Any() ||
                   gameModeMapping.MoodKeywords.Intersect(genreMapping.MoodKeywords, StringComparer.OrdinalIgnoreCase).Any();
        }

        private static bool IsPerspectiveGenreCompatible(SemanticCategoryMapping perspectiveMapping, SemanticCategoryMapping genreMapping)
        {
            return perspectiveMapping.MoodKeywords.Intersect(genreMapping.MoodKeywords, StringComparer.OrdinalIgnoreCase).Any() ||
                   HasCompatibleComplexity(perspectiveMapping, genreMapping);
        }

        private static bool HasCompatibleComplexity(SemanticCategoryMapping perspectiveMapping, SemanticCategoryMapping genreMapping)
        {
            var isImmersivePerspective = perspectiveMapping.ImmersionKeywords.Any(k => 
                SemanticCombinationConfig.ImmersivePerspectiveKeywords.Contains(k, StringComparer.OrdinalIgnoreCase));
            var isAccessibleGenre = genreMapping.AudienceKeywords.Any(k => 
                SemanticCombinationConfig.AccessiblePerspectiveKeywords.Contains(k, StringComparer.OrdinalIgnoreCase));

            return isImmersivePerspective == !isAccessibleGenre; // Opposite complexity levels are compatible
        }

        private SemanticCategoryMapping? GetCachedKeywords(string categoryType, string item)
        {
            var cacheKey = $"{CACHE_KEY_PREFIX}{categoryType}_{item.ToLowerInvariant()}";
            return _cache.TryGetValue(cacheKey, out var mapping) && mapping is SemanticCategoryMapping semanticMapping
                ? semanticMapping
                : null;
        }

        private List<string> GetCachedList(string categoryType)
        {
            var cacheKey = $"{CACHE_KEY_PREFIX}{categoryType}_list";
            return _cache.TryGetValue(cacheKey, out var list) && list is List<string> items
                ? items
                : new List<string>();
        }

        private void ClearCacheEntries()
        {
            var keysToRemove = new List<string>
            {
                INIT_STATUS_KEY,
                STATS_KEY
            };

            // Add category-specific keys
            var categories = new[] { "genre", "platform", "gamemode", "perspective", "combination" };
            foreach (var category in categories)
            {
                keysToRemove.Add($"{CACHE_KEY_PREFIX}{category}_list");
            }

            foreach (var key in keysToRemove)
            {
                _cache.Remove(key);
            }

            // Note: We can't easily enumerate all cache keys to remove individual item entries
            // This is a limitation of IMemoryCache. In production, consider using a more sophisticated cache
            // or implementing a custom cache wrapper that tracks keys.
        }

    }
}