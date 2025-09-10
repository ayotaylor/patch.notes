using Backend.Configuration;
using Backend.Services.Recommendation.Interfaces;

namespace Backend.Services.Recommendation
{
    /// <summary>
    /// Validates embedding dimensions across all services to ensure consistency
    /// Follows vector database best practices for dimension management
    /// </summary>
    public class EmbeddingDimensionValidator
    {
        private readonly IEmbeddingService _embeddingService;
        private readonly ILogger<EmbeddingDimensionValidator> _logger;

        public EmbeddingDimensionValidator(
            IEmbeddingService embeddingService,
            ILogger<EmbeddingDimensionValidator> logger)
        {
            _embeddingService = embeddingService;
            _logger = logger;
        }

        /// <summary>
        /// Simple helper to validate embedding dimensions and return detailed error if invalid
        /// Can be used by any service for consistent validation
        /// </summary>
        public static (bool IsValid, string? ErrorMessage) ValidateEmbeddingDimensions(int actualDimensions, string context = "")
        {
            if (EmbeddingConstants.ValidateDimensions(actualDimensions))
            {
                return (true, null);
            }

            var errorMessage = string.IsNullOrEmpty(context) 
                ? $"Embedding dimension mismatch: got {actualDimensions}, {EmbeddingConstants.GetExpectedDimensionsMessage()}"
                : $"Embedding dimension mismatch in {context}: got {actualDimensions}, {EmbeddingConstants.GetExpectedDimensionsMessage()}";
            
            return (false, errorMessage);
        }

        /// <summary>
        /// Performs comprehensive dimension validation across the entire embedding system
        /// This should be called at startup to prevent runtime dimension errors
        /// </summary>
        /// <returns>True if all dimensions are consistent, false otherwise</returns>
        public async Task<bool> ValidateSystemDimensionsAsync()
        {
            try
            {
                _logger.LogInformation("Starting comprehensive embedding dimension validation...");

                // Test 1: Validate embedding service reports correct dimensions
                var reportedDimensions = _embeddingService.EmbeddingDimensions;
                if (!EmbeddingConstants.ValidateDimensions(reportedDimensions))
                {
                    _logger.LogError("CRITICAL: EmbeddingService reports incorrect dimensions: {Reported}, {Expected}",
                        reportedDimensions, EmbeddingConstants.GetExpectedDimensionsMessage());
                    return false;
                }

                // Test 2: Generate actual test embeddings and validate their dimensions
                var testResults = await ValidateActualEmbeddingGeneration();
                if (!testResults.IsValid)
                {
                    _logger.LogError("CRITICAL: Embedding generation produces incorrect dimensions: {Details}",
                        testResults.ErrorMessage);
                    return false;
                }

                // Test 3: Validate constants consistency
                if (!ValidateConstantsConsistency())
                {
                    _logger.LogError("CRITICAL: EmbeddingConstants internal consistency failed");
                    return false;
                }

                _logger.LogInformation("✅ All embedding dimension validations passed successfully!");
                _logger.LogInformation("System validated dimensions: Text={TextDims}, Total={TotalDims}",
                    EmbeddingConstants.BASE_TEXT_EMBEDDING_DIMENSIONS,
                    EmbeddingConstants.TOTAL_EMBEDDING_DIMENSIONS);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "CRITICAL: Exception during dimension validation");
                return false;
            }
        }

        private async Task<ValidationResult> ValidateActualEmbeddingGeneration()
        {
            try
            {
                // Test simple text embedding
                var textEmbedding = await _embeddingService.GenerateEmbeddingAsync("Test embedding validation");
                if (!EmbeddingConstants.ValidateDimensions(textEmbedding.Length))
                {
                    return new ValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = $"Text embedding: got {textEmbedding.Length}, expected {EmbeddingConstants.TOTAL_EMBEDDING_DIMENSIONS}"
                    };
                }

                // Test game embedding
                var testGameInput = CreateTestGameInput();
                var gameEmbedding = await _embeddingService.GenerateGameEmbeddingAsync(testGameInput);
                if (!EmbeddingConstants.ValidateDimensions(gameEmbedding.Length))
                {
                    return new ValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = $"Game embedding: got {gameEmbedding.Length}, expected {EmbeddingConstants.TOTAL_EMBEDDING_DIMENSIONS}"
                    };
                }

                // Test user preference embedding
                var testUserInput = CreateTestUserInput();
                var userEmbedding = await _embeddingService.GenerateUserPreferenceEmbeddingAsync(testUserInput);
                if (!EmbeddingConstants.ValidateDimensions(userEmbedding.Length))
                {
                    return new ValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = $"User preference embedding: got {userEmbedding.Length}, expected {EmbeddingConstants.TOTAL_EMBEDDING_DIMENSIONS}"
                    };
                }

                return new ValidationResult { IsValid = true };
            }
            catch (Exception ex)
            {
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"Exception during embedding generation test: {ex.Message}"
                };
            }
        }

        private bool ValidateConstantsConsistency()
        {
            try
            {
                // Verify total dimensions calculation
                var expectedTotal = EmbeddingConstants.BASE_TEXT_EMBEDDING_DIMENSIONS;
                if (EmbeddingConstants.TOTAL_EMBEDDING_DIMENSIONS != expectedTotal)
                {
                    _logger.LogError("Constants inconsistency: TOTAL_EMBEDDING_DIMENSIONS ({Total}) != BASE ({Base} = {Expected})",
                        EmbeddingConstants.TOTAL_EMBEDDING_DIMENSIONS,
                        EmbeddingConstants.BASE_TEXT_EMBEDDING_DIMENSIONS,
                        expectedTotal);
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception validating constants consistency");
                return false;
            }
        }

        private static GameEmbeddingInput CreateTestGameInput()
        {
            return new GameEmbeddingInput
            {
                Name = "Test Game for Dimension Validation",
                // Summary = "This is a test game used to validate embedding dimensions during system startup",
                // Storyline = "A comprehensive test to ensure all embedding systems work correctly",
                Genres = ["Action", "Adventure"],
                Platforms = ["PC", "PlayStation 5"],
                GameModes = ["Single-player", "Multiplayer"],
                PlayerPerspectives = ["Third person"],
                Rating = 8,
                ReleaseDate = long.Parse("1704067200"), // Jan 1, 2024
            };
        }

        private static UserPreferenceInput CreateTestUserInput()
        {
            var testGame = CreateTestGameInput();
            return new UserPreferenceInput
            {
                FavoriteGames = [testGame],
                LikedGames = [testGame],
                LikedReviewTexts = ["Great game with excellent graphics and gameplay"],
                LikedGameListDescriptions = ["Best action games of 2023"],
                FollowedUsersFavorites = []
            };
        }

        private class ValidationResult
        {
            public bool IsValid { get; set; }
            public string ErrorMessage { get; set; } = string.Empty;
        }
    }
}