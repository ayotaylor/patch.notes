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
        /// Redistributed across full 384-dimension space without hierarchical boosts
        /// </summary>
        public static class CategoryRanges
        {
            public static readonly PositionRange Genre = new(0, 64);
            public static readonly PositionRange Mechanics = new(64, 192);
            public static readonly PositionRange Theme = new(192, 256);
            public static readonly PositionRange Mood = new(256, 320);
            public static readonly PositionRange ArtStyle = new(320, 352);
            public static readonly PositionRange Audience = new(352, 384);
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
        /// Constants for text description weighting - configuration-driven weights are preferred
        /// </summary>
        public static class SemanticWeights
        {
            public const float TEXT_DESCRIPTION_WEIGHT = 0.05f;       // Very low weight for storylines/summaries due to poor quality
            public const float STORYLINE_WEIGHT = 0.03f;              // Even lower for storyline specifically
            public const float SUMMARY_WEIGHT = 0.05f;                // Low weight for summary
        }
    }
}