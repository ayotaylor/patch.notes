namespace Backend.Configuration
{
    public class SemanticKeywordConfig
    {
        public Dictionary<string, SemanticCategoryMapping> GenreMappings { get; set; } = new();
        public Dictionary<string, SemanticCategoryMapping> PlatformMappings { get; set; } = new();
        public Dictionary<string, SemanticCategoryMapping> GameModeMappings { get; set; } = new();
        public Dictionary<string, SemanticCategoryMapping> PerspectiveMappings { get; set; } = new();
        public Dictionary<string, List<CrossCategoryBoost>> CrossCategoryBoosts { get; set; } = new();
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

    public class CrossCategoryBoost
    {
        public string TriggerKeyword { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public List<string> BoostKeywords { get; set; } = new();
        public float BoostWeight { get; set; } = 0.3f;
        public string Condition { get; set; } = "any"; // "any", "all", "exact"
    }

    public class SemanticWeights
    {
        public float GenreWeight { get; set; } = 0.25f;
        public float MechanicsWeight { get; set; } = 0.20f;
        public float ThemeWeight { get; set; } = 0.15f;
        public float MoodWeight { get; set; } = 0.15f;
        public float ArtStyleWeight { get; set; } = 0.10f;
        public float AudienceWeight { get; set; } = 0.05f;
        public float HierarchicalBoostMultiplier { get; set; } = 1.3f;
        public float CrossCategoryBoostMultiplier { get; set; } = 1.5f;
    }

    public class EmbeddingDimensions
    {
        /// <summary>
        /// Base text embedding dimensions (default: 384 for sentence transformers)
        /// </summary>
        public int BaseTextEmbedding { get; set; } = 384;
        
        /// <summary>
        /// Additional structured feature dimensions (default: 20)
        /// </summary>
        public int StructuredFeatures { get; set; } = 20;
        
        /// <summary>
        /// Total combined embedding dimensions
        /// </summary>
        public int TotalDimensions => BaseTextEmbedding + StructuredFeatures;
        
        /// <summary>
        /// Semantic category position ranges for keyword placement
        /// </summary>
        public CategoryPositionRanges CategoryRanges { get; set; } = new();
    }

    public class CategoryPositionRanges
    {
        public PositionRange Genre { get; set; } = new(0, 30);
        public PositionRange Mechanics { get; set; } = new(30, 60);
        public PositionRange Theme { get; set; } = new(60, 90);
        public PositionRange Mood { get; set; } = new(90, 120);
        public PositionRange ArtStyle { get; set; } = new(120, 140);
        public PositionRange Audience { get; set; } = new(150, 170);
        public PositionRange HierarchicalBoosts { get; set; } = new(170, 220);
    }

    public record PositionRange(int Start, int End)
    {
        public int Size => End - Start;
    }
}