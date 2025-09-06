using Backend.Configuration;
using Backend.Services.Recommendation.Interfaces;

namespace Backend.Services.Recommendation
{
    public static class SemanticUtilityService
    {
        public static string? FindBestFuzzyMatch(string input, ICollection<string> candidates)
        {
            if (string.IsNullOrWhiteSpace(input) || input.Length < 3)
                return null;

            var inputLower = input.ToLowerInvariant();
            var bestMatch = "";
            var bestScore = 0.0;
            const double MINIMUM_SIMILARITY_THRESHOLD = 0.6;

            foreach (var candidate in candidates)
            {
                var candidateLower = candidate.ToLowerInvariant();
                var score = CalculateSimilarityScore(inputLower, candidateLower);

                if (score > bestScore && score >= MINIMUM_SIMILARITY_THRESHOLD)
                {
                    bestScore = score;
                    bestMatch = candidate;
                }
            }

            return bestScore >= MINIMUM_SIMILARITY_THRESHOLD ? bestMatch : null;
        }

        public static double CalculateSimilarityScore(string input, string candidate)
        {
            if (input == candidate) return 1.0;
            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(candidate)) return 0.0;

            var inputLower = input.ToLowerInvariant();
            var candidateLower = candidate.ToLowerInvariant();

            // Exact substring matching (bidirectional)
            if (candidateLower.Contains(inputLower) || inputLower.Contains(candidateLower))
            {
                var longerLength = Math.Max(inputLower.Length, candidateLower.Length);
                var shorterLength = Math.Min(inputLower.Length, candidateLower.Length);
                return (double)shorterLength / longerLength;
            }

            // Levenshtein distance for more sophisticated matching
            var distance = CalculateLevenshteinDistance(inputLower, candidateLower);
            var maxLength = Math.Max(inputLower.Length, candidateLower.Length);

            if (maxLength == 0) return 1.0;

            return 1.0 - ((double)distance / maxLength);
        }

        public static int CalculateLevenshteinDistance(string source, string target)
        {
            if (string.IsNullOrEmpty(source)) return target?.Length ?? 0;
            if (string.IsNullOrEmpty(target)) return source.Length;

            var matrix = new int[source.Length + 1, target.Length + 1];

            for (int i = 0; i <= source.Length; i++)
                matrix[i, 0] = i;
            for (int j = 0; j <= target.Length; j++)
                matrix[0, j] = j;

            for (int i = 1; i <= source.Length; i++)
            {
                for (int j = 1; j <= target.Length; j++)
                {
                    var cost = source[i - 1] == target[j - 1] ? 0 : 1;

                    matrix[i, j] = Math.Min(
                        Math.Min(matrix[i - 1, j] + 1,
                                matrix[i, j - 1] + 1),
                        matrix[i - 1, j - 1] + cost);
                }
            }

            return matrix[source.Length, target.Length];
        }

        public static void AddMappingKeywords(
            SemanticCategoryMapping mapping,
            GameEmbeddingInput gameInput,
            HashSet<string> allExtractedKeywords)
        {
            // Add keywords to respective categories
            // Core game properties
            gameInput.ExtractedGenreKeywords.AddRange(mapping.GenreKeywords);
            gameInput.ExtractedMechanicKeywords.AddRange(mapping.MechanicKeywords);
            gameInput.ExtractedThemeKeywords.AddRange(mapping.ThemeKeywords);
            gameInput.ExtractedMoodKeywords.AddRange(mapping.MoodKeywords);
            gameInput.ExtractedArtStyleKeywords.AddRange(mapping.ArtStyleKeywords);
            gameInput.ExtractedAudienceKeywords.AddRange(mapping.AudienceKeywords);
            
            // Platform-specific properties
            gameInput.ExtractedPlatformTypeKeywords.AddRange(mapping.PlatformType);
            gameInput.ExtractedEraKeywords.AddRange(mapping.EraKeywords);
            gameInput.ExtractedCapabilityKeywords.AddRange(mapping.CapabilityKeywords);
            
            // Game mode-specific properties
            gameInput.ExtractedPlayerInteractionKeywords.AddRange(mapping.PlayerInteractionKeywords);
            gameInput.ExtractedScaleKeywords.AddRange(mapping.ScaleKeywords);
            gameInput.ExtractedCommunicationKeywords.AddRange(mapping.CommunicationKeywords);
            
            // Perspective-specific properties
            gameInput.ExtractedViewpointKeywords.AddRange(mapping.ViewpointKeywords);
            gameInput.ExtractedImmersionKeywords.AddRange(mapping.ImmersionKeywords);
            gameInput.ExtractedInterfaceKeywords.AddRange(mapping.InterfaceKeywords);

            // Add to global set for cross-category analysis (all keyword lists)
            var allKeywords = mapping.GenreKeywords.Concat(mapping.MechanicKeywords)
                .Concat(mapping.ThemeKeywords).Concat(mapping.MoodKeywords)
                .Concat(mapping.ArtStyleKeywords).Concat(mapping.AudienceKeywords)
                .Concat(mapping.PlatformType).Concat(mapping.EraKeywords)
                .Concat(mapping.CapabilityKeywords).Concat(mapping.PlayerInteractionKeywords)
                .Concat(mapping.ScaleKeywords).Concat(mapping.CommunicationKeywords)
                .Concat(mapping.ViewpointKeywords).Concat(mapping.ImmersionKeywords)
                .Concat(mapping.InterfaceKeywords);
                
            foreach (var keyword in allKeywords)
            {
                allExtractedKeywords.Add(keyword.ToLowerInvariant());
            }
        }
    }
}