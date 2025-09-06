namespace Backend.Configuration
{
    public class SemanticKeywordConfig
    {
        public Dictionary<string, SemanticCategoryMapping> GenreMappings { get; set; } = new();
        public Dictionary<string, SemanticCategoryMapping> PlatformMappings { get; set; } = new();
        public Dictionary<string, SemanticCategoryMapping> GameModeMappings { get; set; } = new();
        public Dictionary<string, SemanticCategoryMapping> PerspectiveMappings { get; set; } = new();
        public SemanticWeights DefaultWeights { get; set; } = new();
        public EmbeddingDimensions Dimensions { get; set; } = new();
    }

    /// <summary>
    /// Unified semantic category mapping that contains all possible keyword properties.
    /// Each mapping type (Genre, Platform, GameMode, Perspective) uses only the relevant properties.
    /// </summary>
    public class SemanticCategoryMapping
    {
        // Common properties (used by all mapping types)
        public List<string> MoodKeywords { get; set; } = new();
        public List<string> AudienceKeywords { get; set; } = new();

        // Genre-specific properties
        public List<string> GenreKeywords { get; set; } = new();
        public List<string> MechanicKeywords { get; set; } = new();
        public List<string> ThemeKeywords { get; set; } = new();
        public List<string> ArtStyleKeywords { get; set; } = new();

        // Platform-specific properties
        public List<string> PlatformType { get; set; } = new();
        public List<string> EraKeywords { get; set; } = new();
        public List<string> CapabilityKeywords { get; set; } = new();

        // Game mode-specific properties
        public List<string> PlayerInteractionKeywords { get; set; } = new();
        public List<string> ScaleKeywords { get; set; } = new();
        public List<string> CommunicationKeywords { get; set; } = new();

        // Perspective-specific properties
        public List<string> ViewpointKeywords { get; set; } = new();
        public List<string> ImmersionKeywords { get; set; } = new();
        public List<string> InterfaceKeywords { get; set; } = new();
    }

    public class SemanticWeights
    {
        // Core game properties (highest priority - structured metadata)
        public float GenreWeight { get; set; } = 0.4f;
        public float MechanicsWeight { get; set; } = 0.3f;
        public float ThemeWeight { get; set; } = 0.2f;
        public float MoodWeight { get; set; } = 0.1f;
        
        // Platform-specific properties (medium-high priority)
        public float PlatformTypeWeight { get; set; } = 0.15f;
        public float EraWeight { get; set; } = 0.12f;
        public float CapabilityWeight { get; set; } = 0.08f;
        
        // Game mode-specific properties (medium priority)
        public float PlayerInteractionWeight { get; set; } = 0.1f;
        public float ScaleWeight { get; set; } = 0.08f;
        public float CommunicationWeight { get; set; } = 0.07f;
        
        // Visual and interface properties (lower priority)
        public float ArtStyleWeight { get; set; } = 0.05f;
        public float ViewpointWeight { get; set; } = 0.04f;
        public float ImmersionWeight { get; set; } = 0.03f;
        public float InterfaceWeight { get; set; } = 0.02f;
        public float AudienceWeight { get; set; } = 0.03f;
    }

    public class EmbeddingDimensions
    {
        /// <summary>
        /// Base text embedding dimensions (default: from EmbeddingConstants)
        /// </summary>
        public int BaseTextEmbedding { get; set; } = EmbeddingConstants.BASE_TEXT_EMBEDDING_DIMENSIONS;

        /// <summary>
        /// Total embedding dimensions (ONNX-only, no structured features)
        /// </summary>
        public int TotalDimensions => BaseTextEmbedding;

        /// <summary>
        /// Semantic category position ranges for keyword placement
        /// </summary>
        public CategoryPositionRanges CategoryRanges { get; set; } = new();
    }

    public class CategoryPositionRanges
    {
        // Core game properties (mapped to new SemanticCategoryMapping)
        public PositionRange Genre { get; set; } = EmbeddingConstants.CategoryRanges.Genre;
        public PositionRange Mechanics { get; set; } = EmbeddingConstants.CategoryRanges.Mechanics;
        public PositionRange Theme { get; set; } = EmbeddingConstants.CategoryRanges.Theme;
        public PositionRange Mood { get; set; } = EmbeddingConstants.CategoryRanges.Mood;
        
        // Platform-specific properties
        public PositionRange PlatformType { get; set; } = EmbeddingConstants.CategoryRanges.PlatformType;
        public PositionRange Era { get; set; } = EmbeddingConstants.CategoryRanges.Era;
        public PositionRange Capability { get; set; } = EmbeddingConstants.CategoryRanges.Capability;
        
        // Game mode-specific properties
        public PositionRange PlayerInteraction { get; set; } = EmbeddingConstants.CategoryRanges.PlayerInteraction;
        public PositionRange Scale { get; set; } = EmbeddingConstants.CategoryRanges.Scale;
        public PositionRange Communication { get; set; } = EmbeddingConstants.CategoryRanges.Communication;
        
        // Visual and interface properties
        public PositionRange ArtStyle { get; set; } = EmbeddingConstants.CategoryRanges.ArtStyle;
        public PositionRange Viewpoint { get; set; } = EmbeddingConstants.CategoryRanges.Viewpoint;
        public PositionRange Immersion { get; set; } = EmbeddingConstants.CategoryRanges.Immersion;
        public PositionRange Interface { get; set; } = EmbeddingConstants.CategoryRanges.Interface;
        public PositionRange Audience { get; set; } = EmbeddingConstants.CategoryRanges.Audience;
    }

    public record PositionRange(int Start, int End)
    {
        public int Size => End - Start;
    }
}