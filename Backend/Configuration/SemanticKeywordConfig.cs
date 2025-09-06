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
        public float GenreWeight { get; set; }
        public float MechanicsWeight { get; set; }
        public float ThemeWeight { get; set; }
        public float MoodWeight { get; set; }
        
        // Platform-specific properties (medium-high priority)
        public float PlatformTypeWeight { get; set; }
        public float EraWeight { get; set; }
        public float CapabilityWeight { get; set; }
        
        // Game mode-specific properties (medium priority)
        public float PlayerInteractionWeight { get; set; }
        public float ScaleWeight { get; set; }
        public float CommunicationWeight { get; set; }
        
        // Visual and interface properties (lower priority)
        public float ArtStyleWeight { get; set; }
        public float ViewpointWeight { get; set; }
        public float ImmersionWeight { get; set; }
        public float InterfaceWeight { get; set; }
        public float AudienceWeight { get; set; }
    }

    public class EmbeddingDimensions
    {
        /// <summary>
        /// Base text embedding dimensions (loaded from JSON)
        /// </summary>
        public int BaseTextEmbedding { get; set; }

        /// <summary>
        /// Total embedding dimensions (should match BaseTextEmbedding for ONNX-only approach)
        /// </summary>
        public int TotalDimensions { get; set; }

        /// <summary>
        /// Category position ranges for positional keyword placement (loaded from JSON)
        /// </summary>
        public Dictionary<string, JsonPositionRange> CategoryRanges { get; set; } = new();
    }

    /// <summary>
    /// JSON-serializable position range structure
    /// </summary>
    public class JsonPositionRange
    {
        public int Start { get; set; }
        public int End { get; set; }
    }

    public record PositionRange(int Start, int End)
    {
        public int Size => End - Start;
    }
}