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