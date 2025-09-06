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
                var semanticKeywordConfig = LoadConfigurationFile<SemanticKeywordConfig>(
                    Path.Combine(configBasePath, "SemanticKeywordMappings.json"), "SemanticKeywordMappings");
                var genreKeywordConfig = LoadConfigurationFile<Dictionary<string, SemanticCategoryMapping>>(
                    Path.Combine(configBasePath, "GenreKeywordMappings.json"), "GenreKeywordMappings");
                var platformKeywordConfig = LoadConfigurationFile<Dictionary<string, SemanticCategoryMapping>>(
                    Path.Combine(configBasePath, "PlatformKeywordMappings.json"), "PlatformKeywordMappings");
                var playerPerspectiveConfig = LoadConfigurationFile<Dictionary<string, SemanticCategoryMapping>>(
                    Path.Combine(configBasePath, "PlayerPerspectiveKeywordMappings.json"), "PlayerPerspectiveMappings");
                var gameModeKeywordConfig = LoadConfigurationFile<Dictionary<string, SemanticCategoryMapping>>(
                    Path.Combine(configBasePath, "GameModeKeywordMappings.json"), "GameModeKeywordMappings");
                var platformAliasConfig = LoadConfigurationFile<Dictionary<string, List<string>>>(
                    Path.Combine(configBasePath, "PlatformAlias.json"), "PlatformAlias");

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
                _logger.LogDebug("Merged {Count} perspective mappings", perspectiveConfig.Count);
            }

            // Ensure fallback values with validation
            EnsureConfigurationDefaults(mergedConfig);

            return mergedConfig;
        }

        private SemanticKeywordConfig CreateDefaultSemanticConfig()
        {
            _logger.LogWarning("Creating fallback semantic configuration with EmbeddingConstants defaults");
            
            var config = new SemanticKeywordConfig
            {
                GenreMappings = new Dictionary<string, SemanticCategoryMapping>(),
                PlatformMappings = new Dictionary<string, SemanticCategoryMapping>(),
                GameModeMappings = new Dictionary<string, SemanticCategoryMapping>(),
                PerspectiveMappings = new Dictionary<string, SemanticCategoryMapping>()
            };
            
            EnsureConfigurationDefaults(config);
            return config;
        }

        /// <summary>
        /// Ensures configuration has proper defaults with validation
        /// </summary>
        private void EnsureConfigurationDefaults(SemanticKeywordConfig config)
        {
            // Ensure semantic weights with fallback values
            config.DefaultWeights ??= CreateDefaultSemanticWeights();
            
            // Ensure dimensions with validation and fallback
            config.Dimensions ??= CreateDefaultEmbeddingDimensions();
            
            // Validate JSON dimensions against EmbeddingConstants
            if (config.Dimensions.TotalDimensions > 0 && 
                !EmbeddingConstants.ValidateDimensions(config.Dimensions.TotalDimensions))
            {
                _logger.LogWarning(
                    "Configuration dimensions {JsonDimensions} don't match EmbeddingConstants {ExpectedDimensions}. Using EmbeddingConstants values.",
                    config.Dimensions.TotalDimensions, EmbeddingConstants.TOTAL_EMBEDDING_DIMENSIONS);
                
                config.Dimensions = CreateDefaultEmbeddingDimensions();
            }
        }

        private static SemanticWeights CreateDefaultSemanticWeights()
        {
            return new SemanticWeights
            {
                // Core game properties (highest priority - structured metadata)
                GenreWeight = 0.4f,
                MechanicsWeight = 0.3f,
                ThemeWeight = 0.2f,
                MoodWeight = 0.1f,
                
                // Platform-specific properties (medium-high priority)
                PlatformTypeWeight = 0.15f,
                EraWeight = 0.12f,
                CapabilityWeight = 0.08f,
                
                // Game mode-specific properties (medium priority)
                PlayerInteractionWeight = 0.1f,
                ScaleWeight = 0.08f,
                CommunicationWeight = 0.07f,
                
                // Visual and interface properties (lower priority)
                ArtStyleWeight = 0.05f,
                ViewpointWeight = 0.04f,
                ImmersionWeight = 0.03f,
                InterfaceWeight = 0.02f,
                AudienceWeight = 0.03f
            };
        }

        private static EmbeddingDimensions CreateDefaultEmbeddingDimensions()
        {
            return new EmbeddingDimensions
            {
                BaseTextEmbedding = EmbeddingConstants.BASE_TEXT_EMBEDDING_DIMENSIONS,
                TotalDimensions = EmbeddingConstants.TOTAL_EMBEDDING_DIMENSIONS,
                CategoryRanges = new Dictionary<string, JsonPositionRange>()
            };
        }
    }
}