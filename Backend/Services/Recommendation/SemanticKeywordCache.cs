using Backend.Configuration;
using Backend.Data;
using Backend.Services.Recommendation.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Backend.Services.Recommendation
{
    public class SemanticKeywordCache : ISemanticKeywordCache
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IMemoryCache _cache;
        private readonly ILogger<SemanticKeywordCache> _logger;
        private SemanticKeywordConfig? _semanticConfig;
        
        private const string CACHE_KEY_PREFIX = "precomputed_semantic_";
        private const string INIT_STATUS_KEY = "semantic_cache_initialized";
        private const string STATS_KEY = "semantic_cache_stats";
        private readonly TimeSpan CACHE_DURATION = TimeSpan.FromDays(1);
        
        public SemanticKeywordCache(
            IServiceScopeFactory scopeFactory,
            IMemoryCache cache,
            ILogger<SemanticKeywordCache> logger)
        {
            _scopeFactory = scopeFactory;
            _cache = cache;
            _logger = logger;
            LoadSemanticConfiguration();
        }

        public bool IsInitialized => _cache.TryGetValue(INIT_STATUS_KEY, out var initialized) && (bool)initialized!;

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
                    if (_semanticConfig != null)
                    {
                        genres = _semanticConfig.GenreMappings.Keys.ToList();
                        platforms = _semanticConfig.PlatformMappings.Keys.ToList();
                        gameModes = _semanticConfig.GameModeMappings.Keys.ToList();
                        perspectives = _semanticConfig.PerspectiveMappings.Keys.ToList();
                        
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
                totalKeywords += await PrecomputeCategoryKeywords("genre", genres, _semanticConfig?.GenreMappings);
                totalKeywords += await PrecomputeCategoryKeywords("platform", platforms, _semanticConfig?.PlatformMappings);
                totalKeywords += await PrecomputeCategoryKeywords("gamemode", gameModes, _semanticConfig?.GameModeMappings);
                totalKeywords += await PrecomputeCategoryKeywords("perspective", perspectives, _semanticConfig?.PerspectiveMappings);

                // Precompute common combinations
                var combinations = GenerateCommonCombinations(genres, platforms, gameModes);
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

            foreach (var combination in combinations)
            {
                var mapping = ExtractSemanticMappingForCombination(combination);
                if (mapping != null)
                {
                    var cacheKey = $"{CACHE_KEY_PREFIX}combination_{combination.ToLowerInvariant()}";
                    _cache.Set(cacheKey, mapping, CACHE_DURATION);

                    totalKeywords += mapping.GenreKeywords.Count + mapping.MechanicKeywords.Count +
                                   mapping.ThemeKeywords.Count + mapping.MoodKeywords.Count +
                                   mapping.ArtStyleKeywords.Count + mapping.AudienceKeywords.Count;
                }
            }

            // Cache the list of combinations
            _cache.Set($"{CACHE_KEY_PREFIX}combination_list", combinations, CACHE_DURATION);

            _logger.LogDebug("Precomputed keywords for {Count} combinations", combinations.Count);
            return totalKeywords;
        }

        private SemanticCategoryMapping? ExtractSemanticMappingForItem(
            string item,
            Dictionary<string, SemanticCategoryMapping> categoryMappings)
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

            return null;
        }

        private SemanticCategoryMapping? ExtractSemanticMappingForCombination(string combination)
        {
            // For combinations like "Fantasy RPG", "Horror Action", etc.
            var parts = combination.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var combinedMapping = new SemanticCategoryMapping();

            foreach (var part in parts)
            {
                // Try each category mapping
                var genreMapping = ExtractSemanticMappingForItem(part, _semanticConfig?.GenreMappings);
                var platformMapping = ExtractSemanticMappingForItem(part, _semanticConfig?.PlatformMappings);
                var gameModeMapping = ExtractSemanticMappingForItem(part, _semanticConfig?.GameModeMappings);
                var perspectiveMapping = ExtractSemanticMappingForItem(part, _semanticConfig?.PerspectiveMappings);

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


        private List<string> GenerateCommonCombinations(List<string> genres, List<string> platforms, List<string> gameModes)
        {
            var combinations = new List<string>();

            // Genre + Genre combinations (e.g., "Action RPG", "Horror Adventure")
            var commonGenreCombinations = new[]
            {
                "Action RPG", "Horror Action", "Fantasy RPG", "Sci-fi Shooter",
                "Strategy Simulation", "Puzzle Adventure", "Horror Survival"
            };

            // Filter to only include combinations where both parts exist in database
            foreach (var combo in commonGenreCombinations)
            {
                var parts = combo.Split(' ');
                if (parts.All(part => genres.Any(g => g.Contains(part, StringComparison.OrdinalIgnoreCase))))
                {
                    combinations.Add(combo);
                }
            }

            // Platform + Genre combinations for major genres
            var majorGenres = genres.Where(g => new[] { "Action", "RPG", "Strategy", "Shooter", "Horror" }
                .Any(major => g.Contains(major, StringComparison.OrdinalIgnoreCase))).Take(5);
            
            foreach (var genre in majorGenres)
            {
                foreach (var platform in platforms.Take(3)) // Limit to top 3 platforms
                {
                    combinations.Add($"{platform} {genre}");
                }
            }

            return combinations.Distinct().ToList();
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
                    _logger.LogInformation("Loaded semantic keyword configuration from file");
                }
                else
                {
                    _logger.LogWarning("Configuration file not found, semantic cache will have limited functionality");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading semantic configuration for cache");
            }
        }
    }
}