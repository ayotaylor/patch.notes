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

    public class SemanticCategoryMapping
    {
        public List<string> GenreKeywords { get; set; } = new();
        public List<string> MechanicKeywords { get; set; } = new();
        public List<string> ThemeKeywords { get; set; } = new();
        public List<string> MoodKeywords { get; set; } = new();
        public List<string> ArtStyleKeywords { get; set; } = new();
        public List<string> AudienceKeywords { get; set; } = new();
    }


    public class SemanticWeights
    {
        // Default values match SemanticKeywordMappings.json configuration
        // These prioritize structured game metadata over potentially poor text descriptions
        public float GenreWeight { get; set; } = 0.4f;                    // Highest priority - structured metadata
        public float MechanicsWeight { get; set; } = 0.3f;               // High priority - structured metadata
        public float ThemeWeight { get; set; } = 0.2f;                   // Medium priority - structured metadata
        public float MoodWeight { get; set; } = 0.1f;                    // Lower priority - may be text-derived
        public float ArtStyleWeight { get; set; } = 0.05f;               // Low priority - often subjective
        public float AudienceWeight { get; set; } = 0.03f;               // Lowest priority - often inferred
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
        public PositionRange Genre { get; set; } = EmbeddingConstants.CategoryRanges.Genre;
        public PositionRange Mechanics { get; set; } = EmbeddingConstants.CategoryRanges.Mechanics;
        public PositionRange Theme { get; set; } = EmbeddingConstants.CategoryRanges.Theme;
        public PositionRange Mood { get; set; } = EmbeddingConstants.CategoryRanges.Mood;
        public PositionRange ArtStyle { get; set; } = EmbeddingConstants.CategoryRanges.ArtStyle;
        public PositionRange Audience { get; set; } = EmbeddingConstants.CategoryRanges.Audience;
    }

    public record PositionRange(int Start, int End)
    {
        public int Size => End - Start;
    }
}