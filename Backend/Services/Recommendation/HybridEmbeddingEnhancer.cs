using Backend.Configuration;
using Backend.Services.Recommendation.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace Backend.Services.Recommendation
{
    /// <summary>
    /// Provides hybrid embedding enhancement using both text-based semantic enhancement 
    /// and positional keyword placement using JSON configuration
    /// </summary>
    public class HybridEmbeddingEnhancer
    {
        private readonly ISemanticConfigurationService _configService;
        private readonly ILogger<HybridEmbeddingEnhancer> _logger;
        private readonly Dictionary<string, PositionRange> _cachedPositionRanges = new();

        public HybridEmbeddingEnhancer(ISemanticConfigurationService configService, ILogger<HybridEmbeddingEnhancer> logger)
        {
            _configService = configService;
            _logger = logger;
            InitializePositionRanges();
        }

        /// <summary>
        /// Applies positional keyword enhancement to an embedding using semantic weights and JSON position ranges
        /// </summary>
        public void ApplyPositionalEnhancement(float[] embedding, GameEmbeddingInput gameInput)
        {
            var (isValid, errorMessage) = EmbeddingDimensionValidator.ValidateEmbeddingDimensions(embedding.Length, "positional enhancement");
            if (!isValid)
            {
                _logger.LogWarning("Skipping positional enhancement: {ErrorMessage}", errorMessage);
                return;
            }

            var semanticConfig = _configService.SemanticConfig;
            if (semanticConfig?.DefaultWeights == null)
            {
                _logger.LogDebug("No semantic configuration available for positional enhancement");
                return;
            }

            // Apply positional enhancements for each category
            ApplyKeywordsToPosition(embedding, gameInput.ExtractedGenreKeywords, "Genre", semanticConfig.DefaultWeights.GenreWeight);
            ApplyKeywordsToPosition(embedding, gameInput.ExtractedMechanicKeywords, "Mechanics", semanticConfig.DefaultWeights.MechanicsWeight);
            ApplyKeywordsToPosition(embedding, gameInput.ExtractedThemeKeywords, "Theme", semanticConfig.DefaultWeights.ThemeWeight);
            ApplyKeywordsToPosition(embedding, gameInput.ExtractedMoodKeywords, "Mood", semanticConfig.DefaultWeights.MoodWeight);
            
            ApplyKeywordsToPosition(embedding, gameInput.ExtractedPlatformTypeKeywords, "PlatformType", semanticConfig.DefaultWeights.PlatformTypeWeight);
            ApplyKeywordsToPosition(embedding, gameInput.ExtractedEraKeywords, "Era", semanticConfig.DefaultWeights.EraWeight);
            ApplyKeywordsToPosition(embedding, gameInput.ExtractedCapabilityKeywords, "Capability", semanticConfig.DefaultWeights.CapabilityWeight);
            
            ApplyKeywordsToPosition(embedding, gameInput.ExtractedPlayerInteractionKeywords, "PlayerInteraction", semanticConfig.DefaultWeights.PlayerInteractionWeight);
            ApplyKeywordsToPosition(embedding, gameInput.ExtractedScaleKeywords, "Scale", semanticConfig.DefaultWeights.ScaleWeight);
            ApplyKeywordsToPosition(embedding, gameInput.ExtractedCommunicationKeywords, "Communication", semanticConfig.DefaultWeights.CommunicationWeight);
            
            ApplyKeywordsToPosition(embedding, gameInput.ExtractedArtStyleKeywords, "ArtStyle", semanticConfig.DefaultWeights.ArtStyleWeight);
            ApplyKeywordsToPosition(embedding, gameInput.ExtractedViewpointKeywords, "Viewpoint", semanticConfig.DefaultWeights.ViewpointWeight);
            ApplyKeywordsToPosition(embedding, gameInput.ExtractedImmersionKeywords, "Immersion", semanticConfig.DefaultWeights.ImmersionWeight);
            ApplyKeywordsToPosition(embedding, gameInput.ExtractedInterfaceKeywords, "Interface", semanticConfig.DefaultWeights.InterfaceWeight);
            ApplyKeywordsToPosition(embedding, gameInput.ExtractedAudienceKeywords, "Audience", semanticConfig.DefaultWeights.AudienceWeight);
        }

        /// <summary>
        /// Applies keywords to specific positions in the embedding using weighted additive approach
        /// </summary>
        private void ApplyKeywordsToPosition(float[] embedding, List<string> keywords, string categoryName, float weight)
        {
            if (keywords.Count == 0 || weight <= 0) return;

            if (!_cachedPositionRanges.TryGetValue(categoryName, out var positionRange))
            {
                _logger.LogTrace("No position range found for category: {Category}", categoryName);
                return;
            }

            // Convert keywords to normalized values and average them
            var keywordValues = keywords.Select(ConvertKeywordToNormalizedValue).ToArray();
            var avgKeywordValue = keywordValues.Length > 0 ? keywordValues.Average() : 0f;

            // Apply to each position in the range with decaying weight
            var rangeSize = positionRange.Size;
            for (int i = 0; i < rangeSize && positionRange.Start + i < embedding.Length; i++)
            {
                var position = positionRange.Start + i;
                var positionWeight = weight * (1.0f - (float)i / rangeSize * 0.3f); // Gentle decay across range
                
                // Weighted additive approach: blend original with keyword value
                embedding[position] = embedding[position] * (1.0f - positionWeight) + avgKeywordValue * positionWeight;
            }

            _logger.LogTrace("Applied {KeywordCount} keywords to {Category} positions {Start}-{End} with weight {Weight}", 
                keywords.Count, categoryName, positionRange.Start, positionRange.End, weight);
        }

        /// <summary>
        /// Converts a keyword to a consistent normalized float value using deterministic hashing
        /// </summary>
        private static float ConvertKeywordToNormalizedValue(string keyword)
        {
            if (string.IsNullOrEmpty(keyword)) return 0f;

            // Use deterministic hash for consistency
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(keyword.ToLowerInvariant()));

            // Convert first 4 bytes to int, then normalize to [-1, 1] range
            var intValue = BitConverter.ToInt32(hash, 0);
            return intValue / (float)int.MaxValue;
        }

        /// <summary>
        /// Initializes position ranges from JSON configuration
        /// </summary>
        private void InitializePositionRanges()
        {
            try
            {
                var config = _configService.SemanticConfig;
                if (config?.Dimensions?.CategoryRanges != null)
                {
                    // Load from JSON configuration
                    foreach (var kvp in config.Dimensions.CategoryRanges)
                    {
                        _cachedPositionRanges[kvp.Key] = new PositionRange(kvp.Value.Start, kvp.Value.End);
                    }
                    _logger.LogInformation("Loaded {Count} position ranges from JSON configuration", _cachedPositionRanges.Count);
                }
                else
                {
                    _logger.LogWarning("No CategoryRanges found in JSON configuration, positional enhancement disabled");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize position ranges from configuration");
            }
        }
    }
}