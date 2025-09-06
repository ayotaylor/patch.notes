using System.Text.Json;

namespace Backend.Configuration
{
    /// <summary>
    /// Singleton service that loads and caches all semantic keyword configurations
    /// Ensures configurations are loaded once at application startup
    /// </summary>
    public class SemanticConfigurationService : ISemanticConfigurationService
    {
        private readonly ILogger<SemanticConfigurationService> _logger;
        private readonly object _lock = new object();
        private SemanticKeywordConfig? _semanticConfig;
        private Dictionary<string, List<string>>? _platformAliases;
        private bool _isConfigurationLoaded = false;
        private DateTime? _lastLoadTime;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        public SemanticConfigurationService(ILogger<SemanticConfigurationService> logger)
        {
            _logger = logger;
        }

        public SemanticKeywordConfig SemanticConfig 
        {
            get
            {
                EnsureConfigurationLoaded();
                return _semanticConfig ?? CreateDefaultSemanticConfig();
            }
        }

        public Dictionary<string, List<string>> PlatformAliases
        {
            get
            {
                EnsureConfigurationLoaded();
                return _platformAliases ?? new Dictionary<string, List<string>>();
            }
        }

        public bool IsConfigurationLoaded => _isConfigurationLoaded;

        public DateTime? LastLoadTime => _lastLoadTime;

        public async Task RefreshConfigurationAsync()
        {
            await Task.Run(LoadConfiguration);
        }

        private void EnsureConfigurationLoaded()
        {
            if (!_isConfigurationLoaded)
            {
                lock (_lock)
                {
                    if (!_isConfigurationLoaded)
                    {
                        LoadConfiguration();
                    }
                }
            }
        }

        private void LoadConfiguration()
        {
            try
            {
                _logger.LogInformation("Loading semantic keyword configurations...");
                var startTime = DateTime.UtcNow;

                var configBasePath = Path.Combine(AppContext.BaseDirectory, "Configuration");
                
                // Load all configuration files
                var semanticKeywordConfig = LoadSemanticKeywordMappings(configBasePath);
                var genreKeywordConfig = LoadGenreKeywordMappings(configBasePath);
                var platformKeywordConfig = LoadPlatformKeywordMappings(configBasePath);
                var playerPerspectiveConfig = LoadPlayerPerspectiveMappings(configBasePath);
                var platformAliasConfig = LoadPlatformAliases(configBasePath);
                
                // Check if we have configuration files for game modes
                var gameModeKeywordConfig = LoadGameModeKeywordMappings(configBasePath);

                // Merge all configurations
                _semanticConfig = MergeConfigurations(
                    semanticKeywordConfig, 
                    genreKeywordConfig, 
                    platformKeywordConfig,
                    gameModeKeywordConfig,
                    playerPerspectiveConfig);

                _platformAliases = platformAliasConfig;

                _isConfigurationLoaded = true;
                _lastLoadTime = DateTime.UtcNow;

                var loadTime = DateTime.UtcNow - startTime;
                var totalMappings = (_semanticConfig?.GenreMappings.Count ?? 0) +
                                  (_semanticConfig?.PlatformMappings.Count ?? 0) +
                                  (_semanticConfig?.GameModeMappings.Count ?? 0) +
                                  (_semanticConfig?.PerspectiveMappings.Count ?? 0);

                _logger.LogInformation("Successfully loaded semantic configurations in {LoadTimeMs}ms. " +
                    "Total mappings: {TotalMappings}, Platform aliases: {PlatformAliases}",
                    loadTime.TotalMilliseconds, totalMappings, _platformAliases?.Count ?? 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load semantic keyword configurations");
                
                // Create fallback configuration
                _semanticConfig = CreateDefaultSemanticConfig();
                _platformAliases = new Dictionary<string, List<string>>();
                _isConfigurationLoaded = true;
                _lastLoadTime = DateTime.UtcNow;
            }
        }

        private SemanticKeywordConfig? LoadSemanticKeywordMappings(string basePath)
        {
            var filePath = Path.Combine(basePath, "SemanticKeywordMappings.json");
            return LoadConfigurationFile<SemanticKeywordConfig>(filePath, "SemanticKeywordMappings");
        }

        private Dictionary<string, SemanticCategoryMapping>? LoadGenreKeywordMappings(string basePath)
        {
            var filePath = Path.Combine(basePath, "GenreKeywordMappings.json");
            return LoadConfigurationFile<Dictionary<string, SemanticCategoryMapping>>(filePath, "GenreKeywordMappings");
        }

        private Dictionary<string, SemanticCategoryMapping>? LoadPlatformKeywordMappings(string basePath)
        {
            var filePath = Path.Combine(basePath, "PlatformKeywordMappings.json");
            return LoadConfigurationFile<Dictionary<string, SemanticCategoryMapping>>(filePath, "PlatformKeywordMappings");
        }

        private Dictionary<string, SemanticCategoryMapping>? LoadPlayerPerspectiveMappings(string basePath)
        {
            var filePath = Path.Combine(basePath, "PlayerPerspectiveKeywordMappings.json");
            return LoadConfigurationFile<Dictionary<string, SemanticCategoryMapping>>(filePath, "PlayerPerspectiveMappings");
        }

        private Dictionary<string, SemanticCategoryMapping>? LoadGameModeKeywordMappings(string basePath)
        {
            var filePath = Path.Combine(basePath, "GameModeKeywordMappings.json");
            return LoadConfigurationFile<Dictionary<string, SemanticCategoryMapping>>(filePath, "GameModeKeywordMappings");
        }

        private Dictionary<string, List<string>>? LoadPlatformAliases(string basePath)
        {
            var filePath = Path.Combine(basePath, "PlatformAlias.json");
            return LoadConfigurationFile<Dictionary<string, List<string>>>(filePath, "PlatformAlias");
        }

        private T? LoadConfigurationFile<T>(string filePath, string configName) where T : class
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning("Configuration file not found: {FilePath}", filePath);
                    return null;
                }

                var jsonContent = File.ReadAllText(filePath);
                if (string.IsNullOrWhiteSpace(jsonContent))
                {
                    _logger.LogWarning("Configuration file is empty: {FilePath}", filePath);
                    return null;
                }

                var config = JsonSerializer.Deserialize<T>(jsonContent, JsonOptions);
                if (config != null)
                {
                    _logger.LogDebug("Loaded configuration: {ConfigName}", configName);
                    return config;
                }
                else
                {
                    _logger.LogWarning("Configuration deserialized to null: {ConfigName}", configName);
                    return null;
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON parsing error in configuration file: {FilePath}", filePath);
                return null;
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "IO error reading configuration file: {FilePath}", filePath);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error loading configuration: {ConfigName}", configName);
                return null;
            }
        }

        private SemanticKeywordConfig MergeConfigurations(
            SemanticKeywordConfig? mainConfig,
            Dictionary<string, SemanticCategoryMapping>? genreConfig,
            Dictionary<string, SemanticCategoryMapping>? platformConfig,
            Dictionary<string, SemanticCategoryMapping>? gameModeConfig,
            Dictionary<string, SemanticCategoryMapping>? perspectiveConfig)
        {
            var mergedConfig = mainConfig ?? new SemanticKeywordConfig();

            // Merge genre mappings
            if (genreConfig != null)
            {
                foreach (var kvp in genreConfig)
                {
                    mergedConfig.GenreMappings[kvp.Key] = kvp.Value;
                }
                _logger.LogDebug("Merged {Count} genre mappings", genreConfig.Count);
            }

            // Merge platform mappings
            if (platformConfig != null)
            {
                foreach (var kvp in platformConfig)
                {
                    mergedConfig.PlatformMappings[kvp.Key] = kvp.Value;
                }
                _logger.LogDebug("Merged {Count} platform mappings", platformConfig.Count);
            }

            // Merge game mode mappings
            if (gameModeConfig != null)
            {
                foreach (var kvp in gameModeConfig)
                {
                    mergedConfig.GameModeMappings[kvp.Key] = kvp.Value;
                }
                _logger.LogDebug("Merged {Count} game mode mappings", gameModeConfig.Count);
            }
            
            // Merge player perspective mappings
            if (perspectiveConfig != null)
            {
                foreach (var kvp in perspectiveConfig)
                {
                    mergedConfig.PerspectiveMappings[kvp.Key] = kvp.Value;
                }
                _logger.LogDebug("Merged {Count} game mode mappings", perspectiveConfig.Count);
            }

            // If we had a main config with existing genre mappings, preserve them
            // (the individual files take precedence over the main file)

            // Ensure we have default weights and dimensions
            mergedConfig.DefaultWeights ??= new SemanticWeights();
            mergedConfig.Dimensions ??= new EmbeddingDimensions();

            return mergedConfig;
        }

        private static SemanticKeywordConfig CreateDefaultSemanticConfig()
        {
            return new SemanticKeywordConfig
            {
                DefaultWeights = new SemanticWeights(),
                Dimensions = new EmbeddingDimensions(),
                GenreMappings = new Dictionary<string, SemanticCategoryMapping>(),
                PlatformMappings = new Dictionary<string, SemanticCategoryMapping>(),
                GameModeMappings = new Dictionary<string, SemanticCategoryMapping>(),
                PerspectiveMappings = new Dictionary<string, SemanticCategoryMapping>()
            };
        }
    }
}