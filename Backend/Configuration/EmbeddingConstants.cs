namespace Backend.Configuration
{
    public static class EmbeddingConstants
    {
        /// <summary>
        /// Base text embedding dimensions - must match the model's actual output size
        /// Updated to 384 to match all-MiniLM-L6-v2 ONNX model output
        /// </summary>
        public const int BASE_TEXT_EMBEDDING_DIMENSIONS = 384;
        
        /// <summary>
        /// Total embedding dimensions (ONNX-only, no structured features)
        /// </summary>
        public const int TOTAL_EMBEDDING_DIMENSIONS = BASE_TEXT_EMBEDDING_DIMENSIONS;

        /// <summary>
        /// Semantic category position ranges for keyword placement in embeddings
        /// Redistributed across full 384-dimension space to support all SemanticCategoryMapping properties
        /// </summary>
        public static class CategoryRanges
        {
            // Core game properties (higher weight - larger ranges)
            public static readonly PositionRange Genre = new(0, 48);              // GenreKeywords - 48 dims
            public static readonly PositionRange Mechanics = new(48, 96);         // MechanicKeywords - 48 dims  
            public static readonly PositionRange Theme = new(96, 144);            // ThemeKeywords - 48 dims
            public static readonly PositionRange Mood = new(144, 180);            // MoodKeywords - 36 dims
            
            // Platform and era properties (medium weight)
            public static readonly PositionRange PlatformType = new(180, 210);    // PlatformType - 30 dims
            public static readonly PositionRange Era = new(210, 240);             // EraKeywords - 30 dims
            public static readonly PositionRange Capability = new(240, 264);      // CapabilityKeywords - 24 dims
            
            // Player interaction and social aspects (medium weight)
            public static readonly PositionRange PlayerInteraction = new(264, 288); // PlayerInteractionKeywords - 24 dims
            public static readonly PositionRange Scale = new(288, 306);           // ScaleKeywords - 18 dims
            public static readonly PositionRange Communication = new(306, 324);   // CommunicationKeywords - 18 dims
            
            // Visual and interface properties (lower weight - smaller ranges)
            public static readonly PositionRange ArtStyle = new(324, 342);        // ArtStyleKeywords - 18 dims
            public static readonly PositionRange Viewpoint = new(342, 360);       // ViewpointKeywords - 18 dims
            public static readonly PositionRange Immersion = new(360, 372);       // ImmersionKeywords - 12 dims
            public static readonly PositionRange Interface = new(372, 378);       // InterfaceKeywords - 6 dims
            public static readonly PositionRange Audience = new(378, 384);        // AudienceKeywords - 6 dims
        }

        /// <summary>
        /// Validates that embedding dimensions are consistent with constants
        /// </summary>
        /// <param name="actualDimensions">The actual embedding dimensions being used</param>
        /// <returns>True if dimensions are consistent, false otherwise</returns>
        public static bool ValidateDimensions(int actualDimensions)
        {
            return actualDimensions == TOTAL_EMBEDDING_DIMENSIONS;
        }

        /// <summary>
        /// Gets the expected dimensions for error messages
        /// </summary>
        public static string GetExpectedDimensionsMessage()
        {
            return $"Expected {TOTAL_EMBEDDING_DIMENSIONS} dimensions (ONNX text embeddings only)";
        }

        /// <summary>
        /// Constants for text description weighting
        /// </summary>
        public static class TextWeights
        {
            public const float TEXT_DESCRIPTION_WEIGHT = 0.05f;       // Very low weight for storylines/summaries due to poor quality
            public const float STORYLINE_WEIGHT = 0.03f;              // Even lower for storyline specifically
            public const float SUMMARY_WEIGHT = 0.05f;                // Low weight for summary
        }
    }
}