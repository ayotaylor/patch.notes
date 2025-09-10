using Backend.Data;
using Backend.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services.Recommendation
{
    /// <summary>
    /// Service for normalizing LLM-provided filter terms to database canonical names
    /// Loads all category values into cache at application startup for optimal performance
    /// </summary>
    public class CategoryNormalizationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ISemanticConfigurationService _semanticConfigurationService;
        private readonly ILogger<CategoryNormalizationService> _logger;
        
        // Static shared cache for all category values (populated at startup)
        private static readonly Dictionary<FilterCategory, HashSet<string>> _categoryCache = new();
        private static readonly object _cacheLock = new object();
        private static bool _isInitialized = false;

        public CategoryNormalizationService(
            ApplicationDbContext context, 
            ISemanticConfigurationService semanticConfigurationService,
            ILogger<CategoryNormalizationService> logger)
        {
            _context = context;
            _semanticConfigurationService = semanticConfigurationService;
            _logger = logger;
        }

        /// <summary>
        /// Initializes the cache with all category values from database
        /// Call this at application startup
        /// </summary>
        public async Task InitializeCacheAsync()
        {
            try
            {
                _logger.LogInformation("Initializing category normalization cache...");
                
                // Initialize categories sequentially to avoid DbContext concurrency issues
                await InitializeCategoryAsync(FilterCategory.Genre);
                await InitializeCategoryAsync(FilterCategory.Platform);
                await InitializeCategoryAsync(FilterCategory.GameMode);
                await InitializeCategoryAsync(FilterCategory.PlayerPerspective);

                lock (_cacheLock)
                {
                    _isInitialized = true;
                }

                var stats = GetCacheStats();
                var platformAliasCount = _semanticConfigurationService.PlatformAliases.Count;
                _logger.LogInformation("Category normalization cache initialized successfully: {Stats}, Platform aliases: {AliasCount}", 
                    string.Join(", ", stats.Select(kvp => $"{kvp.Key}: {kvp.Value}")), platformAliasCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize category normalization cache");
                throw;
            }
        }

        /// <summary>
        /// Normalizes a list of LLM-provided filter values to database canonical names
        /// </summary>
        public async Task<List<string>> NormalizeFilterValuesAsync(FilterCategory category, IEnumerable<string> values)
        {
            if (!values.Any())
                return new List<string>();

            // Ensure cache is available
            await EnsureCacheInitializedAsync();

            var normalizedValues = new List<string>();

            foreach (var value in values.Where(v => !string.IsNullOrWhiteSpace(v)))
            {
                var canonical = FindCanonicalName(category, value.Trim());
                if (!string.IsNullOrEmpty(canonical))
                {
                    normalizedValues.Add(canonical);
                }
                else
                {
                    _logger.LogWarning("Could not normalize {Category} value: '{Value}'. Available values: {Count}", 
                        category, value, _categoryCache.GetValueOrDefault(category)?.Count ?? 0);
                }
            }

            return normalizedValues.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        /// <summary>
        /// Finds the canonical database name for an LLM-provided term using cached values
        /// </summary>
        public async Task<string> FindCanonicalNameAsync(FilterCategory category, string llmTerm)
        {
            await EnsureCacheInitializedAsync();
            return FindCanonicalName(category, llmTerm);
        }

        /// <summary>
        /// Finds canonical name using cached values (synchronous version)
        /// </summary>
        private string FindCanonicalName(FilterCategory category, string llmTerm)
        {
            if (string.IsNullOrWhiteSpace(llmTerm))
                return llmTerm;

            lock (_cacheLock)
            {
                if (!_categoryCache.TryGetValue(category, out var canonicalNames))
                {
                    _logger.LogWarning("No cached values for category: {Category}", category);
                    return string.Empty;
                }

                var trimmed = llmTerm.Trim();

                // 1. Exact match (case-insensitive)
                var exactMatch = canonicalNames.FirstOrDefault(name => 
                    string.Equals(name, trimmed, StringComparison.OrdinalIgnoreCase));
                if (exactMatch != null)
                {
                    _logger.LogDebug("Exact match: '{LlmTerm}' -> '{Canonical}' ({Category})", trimmed, exactMatch, category);
                    return exactMatch;
                }

                // 2. Contains match (for "RPG" -> "Role-playing (RPG)")
                var containsMatch = canonicalNames.FirstOrDefault(name => 
                    name.Contains(trimmed, StringComparison.OrdinalIgnoreCase));
                if (containsMatch != null)
                {
                    _logger.LogDebug("Contains match: '{LlmTerm}' -> '{Canonical}' ({Category})", trimmed, containsMatch, category);
                    return containsMatch;
                }

                // 3. Reverse contains match 
                var reverseContainsMatch = canonicalNames.FirstOrDefault(name => 
                    trimmed.Contains(name, StringComparison.OrdinalIgnoreCase));
                if (reverseContainsMatch != null)
                {
                    _logger.LogDebug("Reverse contains match: '{LlmTerm}' -> '{Canonical}' ({Category})", trimmed, reverseContainsMatch, category);
                    return reverseContainsMatch;
                }

                // 4. Common abbreviation patterns
                var abbreviationMatch = FindAbbreviationMatch(canonicalNames, trimmed);
                if (abbreviationMatch != null)
                {
                    _logger.LogDebug("Abbreviation match: '{LlmTerm}' -> '{Canonical}' ({Category})", trimmed, abbreviationMatch, category);
                    return abbreviationMatch;
                }

                // 5. Word boundary matching (for "First Person" -> "First person")
                var wordBoundaryMatch = canonicalNames.FirstOrDefault(name => 
                    string.Equals(name.Replace("-", " "), trimmed.Replace("-", " "), StringComparison.OrdinalIgnoreCase));
                if (wordBoundaryMatch != null)
                {
                    _logger.LogDebug("Word boundary match: '{LlmTerm}' -> '{Canonical}' ({Category})", trimmed, wordBoundaryMatch, category);
                    return wordBoundaryMatch;
                }

                _logger.LogDebug("No match found for: '{LlmTerm}' ({Category})", trimmed, category);
                return string.Empty;
            }
        }

        /// <summary>
        /// Initializes cache for a specific category
        /// </summary>
        private async Task InitializeCategoryAsync(FilterCategory category)
        {
            var canonicalNames = await FetchCanonicalNamesFromDatabase(category);
            
            lock (_cacheLock)
            {
                _categoryCache[category] = canonicalNames;
            }

            _logger.LogDebug("Loaded {Count} {Category} values into cache", canonicalNames.Count, category);
        }

        /// <summary>
        /// Ensures cache is initialized, falls back to database if needed
        /// </summary>
        private async Task EnsureCacheInitializedAsync()
        {
            lock (_cacheLock)
            {
                if (_isInitialized)
                    return;
            }

            _logger.LogWarning("Cache not initialized, performing emergency initialization");
            await InitializeCacheAsync();
        }

        /// <summary>
        /// Fetches all distinct canonical names from database for the category
        /// </summary>
        private async Task<HashSet<string>> FetchCanonicalNamesFromDatabase(FilterCategory category)
        {
            return category switch
            {
                FilterCategory.Genre => (await _context.Genres
                    .AsNoTracking()
                    .Select(g => g.Name)
                    .Where(n => !string.IsNullOrEmpty(n))
                    .Distinct()
                    .ToListAsync())
                    .ToHashSet(StringComparer.OrdinalIgnoreCase),

                FilterCategory.Platform => (await _context.Platforms
                    .AsNoTracking()
                    .Select(p => p.Name)
                    .Where(n => !string.IsNullOrEmpty(n))
                    .Distinct()
                    .ToListAsync())
                    .Where(n => n != null)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase),

                FilterCategory.GameMode => (await _context.GameModes
                    .AsNoTracking()
                    .Select(gm => gm.Name)
                    .Where(n => !string.IsNullOrEmpty(n))
                    .Distinct()
                    .ToListAsync())
                    .ToHashSet(StringComparer.OrdinalIgnoreCase),

                FilterCategory.PlayerPerspective => (await _context.PlayerPerspectives
                    .AsNoTracking()
                    .Select(pp => pp.Name)
                    .Where(n => !string.IsNullOrEmpty(n))
                    .Distinct()
                    .ToListAsync())
                    .ToHashSet(StringComparer.OrdinalIgnoreCase),

                _ => new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            };
        }

        /// <summary>
        /// Handles common gaming abbreviations and patterns, integrating platform aliases from SemanticConfigurationService
        /// </summary>
        private string? FindAbbreviationMatch(HashSet<string> canonicalNames, string llmTerm)
        {
            // First check platform aliases from SemanticConfigurationService
            var platformAliases = _semanticConfigurationService.PlatformAliases;
            
            // Check if the llmTerm is an alias for any platform
            foreach (var platformAlias in platformAliases)
            {
                var canonicalPlatform = platformAlias.Key;
                var aliases = platformAlias.Value;
                
                // Check if llmTerm matches any alias (case-insensitive)
                if (aliases.Any(alias => string.Equals(alias, llmTerm, StringComparison.OrdinalIgnoreCase)))
                {
                    // Check if the canonical platform name exists in our database cache
                    var dbMatch = canonicalNames.FirstOrDefault(name => 
                        string.Equals(name, canonicalPlatform, StringComparison.OrdinalIgnoreCase));
                    if (dbMatch != null)
                    {
                        _logger.LogDebug("Platform alias match: '{LlmTerm}' -> '{Canonical}' via SemanticConfigurationService", llmTerm, dbMatch);
                        return dbMatch;
                    }
                }
            }
            
            // Fallback to hardcoded genre and perspective abbreviations
            var abbreviationPatterns = new Dictionary<string, string[]>
            {
                // Genres
                ["RPG"] = new[] { "role-playing", "role playing" },
                ["FPS"] = new[] { "first-person shooter", "shooter" },
                ["RTS"] = new[] { "real-time strategy", "strategy" },
                ["MMO"] = new[] { "massively multiplayer", "multiplayer" },
                ["MMORPG"] = new[] { "massively multiplayer online role-playing", "massively multiplayer role-playing" },
                ["MOBA"] = new[] { "multiplayer online battle arena" },
                ["TBS"] = new[] { "turn-based strategy" },
                ["JRPG"] = new[] { "japanese role-playing" },
                
                // Perspectives
                ["FP"] = new[] { "first person", "first-person" },
                ["TP"] = new[] { "third person", "third-person" }
            };

            if (abbreviationPatterns.TryGetValue(llmTerm.ToUpper(), out var searchTerms))
            {
                return canonicalNames.FirstOrDefault(name => 
                    searchTerms.Any(term => name.Contains(term, StringComparison.OrdinalIgnoreCase)));
            }

            return null;
        }

        /// <summary>
        /// Gets cache statistics for monitoring
        /// </summary>
        public Dictionary<FilterCategory, int> GetCacheStats()
        {
            lock (_cacheLock)
            {
                return _categoryCache.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Count
                );
            }
        }

        /// <summary>
        /// Gets all cached values for a category (useful for debugging)
        /// </summary>
        public HashSet<string> GetCachedValues(FilterCategory category)
        {
            lock (_cacheLock)
            {
                return _categoryCache.TryGetValue(category, out var values) 
                    ? new HashSet<string>(values, StringComparer.OrdinalIgnoreCase)
                    : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Refreshes cache from database (useful for admin operations)
        /// </summary>
        public async Task RefreshCacheAsync()
        {
            _logger.LogInformation("Refreshing category normalization cache...");
            
            lock (_cacheLock)
            {
                _isInitialized = false;
                _categoryCache.Clear();
            }

            await InitializeCacheAsync();
        }

        /// <summary>
        /// Checks if the cache is initialized
        /// </summary>
        public bool IsInitialized 
        { 
            get 
            { 
                lock (_cacheLock) 
                { 
                    return _isInitialized; 
                } 
            } 
        }
    }

    /// <summary>
    /// Enum for filter categories
    /// </summary>
    public enum FilterCategory
    {
        Genre,
        Platform,
        GameMode,
        PlayerPerspective
    }
}