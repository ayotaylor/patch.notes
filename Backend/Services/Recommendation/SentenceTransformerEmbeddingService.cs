using Backend.Services.Recommendation.Interfaces;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Backend.Configuration;
using System.Text.Json;

namespace Backend.Services.Recommendation
{
    public class SentenceTransformerEmbeddingService : IEmbeddingService, IDisposable
    {
        private readonly InferenceSession? _session;
        private readonly ILogger<SentenceTransformerEmbeddingService> _logger;
        private readonly IConfiguration _configuration;
        private readonly bool _useOnnxModel;
        private readonly EmbeddingDimensions _dimensions;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public int EmbeddingDimensions => _dimensions.TotalDimensions;

        public SentenceTransformerEmbeddingService(IConfiguration configuration, ILogger<SentenceTransformerEmbeddingService> logger)
        {
            _logger = logger;
            _configuration = configuration;

            // Initialize standardized dimensions
            _dimensions = LoadEmbeddingDimensions();

            // Check if ONNX model should be used
            var modelPath = _configuration["EmbeddingModel:Path"];
            _useOnnxModel = _configuration.GetValue<bool>("EmbeddingModel:UseOnnx", false)
                            && !string.IsNullOrEmpty(modelPath)
                            && File.Exists(modelPath);

            try
            {
                if (_useOnnxModel)
                {
                    var sessionOptions = new Microsoft.ML.OnnxRuntime.SessionOptions();
                    _session = new InferenceSession(modelPath!, sessionOptions);
                    _logger.LogInformation("Embedding service initialized with ONNX model: {ModelPath}", modelPath);
                }
                else
                {
                    _logger.LogInformation("Embedding service initialized with simple embedding method");
                    if (!string.IsNullOrEmpty(modelPath) && !File.Exists(modelPath))
                    {
                        _logger.LogWarning("ONNX model path specified but file not found: {ModelPath}", modelPath);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize ONNX model, falling back to simple embeddings");
                _useOnnxModel = false;
                _session?.Dispose();
                _session = null;
            }
        }

        private EmbeddingDimensions LoadEmbeddingDimensions()
        {
            try
            {
                var configPath = Path.Combine(AppContext.BaseDirectory, "Configuration", "DefaultSemanticKeywordMappings.json");
                if (File.Exists(configPath))
                {
                    var jsonContent = File.ReadAllText(configPath);
                    var config = JsonSerializer.Deserialize<SemanticKeywordConfig>(jsonContent, JsonOptions);

                    if (config?.Dimensions != null)
                    {
                        _logger.LogInformation("Loaded embedding dimensions from configuration: {BaseTextEmbedding} + {StructuredFeatures} = {TotalDimensions}",
                            config.Dimensions.BaseTextEmbedding, config.Dimensions.StructuredFeatures, config.Dimensions.TotalDimensions);
                        return config.Dimensions;
                    }
                }

                _logger.LogWarning("Could not load embedding dimensions from configuration, using defaults");
                return new EmbeddingDimensions();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading embedding dimensions configuration, using defaults");
                return new EmbeddingDimensions();
            }
        }

        public async Task<float[]> GenerateEmbeddingAsync(string text)
        {
            return await GenerateEmbeddingAsync(text, null);
        }

        public async Task<float[]> GenerateEmbeddingAsync(string text, GameEmbeddingInput? gameInput)
        {
            return await Task.Run(() =>
            {
                try
                {
                    float[] baseEmbedding;
                    if (_useOnnxModel && _session != null)
                    {
                        baseEmbedding = GenerateOnnxEmbedding(text, gameInput);
                    }
                    else
                    {
                        baseEmbedding = GenerateSimpleEmbedding(text, gameInput);
                    }

                    // Pad text embedding to match game embedding dimensions
                    return PadEmbeddingToGameSize(baseEmbedding);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating embedding for text: {Text}", text.Substring(0, Math.Min(50, text.Length)));
                    return new float[EmbeddingDimensions];
                }
            });
        }

        // Production ONNX implementation - activated when UseOnnx=true and model files exist
        private float[] GenerateOnnxEmbedding(string text, GameEmbeddingInput? gameInput = null)
        {
            try
            {
                if (_session == null)
                    throw new InvalidOperationException("ONNX session not initialized");

                // PRODUCTION IMPLEMENTATION NOTES:
                // 1. Download model files from: https://huggingface.co/sentence-transformers/all-MiniLM-L6-v2
                //    - model.onnx
                //    - tokenizer.json
                //    - config.json
                // 2. Install proper tokenizer package: Microsoft.ML.Tokenizers or HuggingFace.NET
                // 3. Implement proper tokenization below

                // Simplified ONNX inference (replace with actual tokenization)
                var tokens = SimpleTokenize(text, 512); // Max sequence length

                var inputTensor = new DenseTensor<long>(tokens, new[] { 1, tokens.Length });
                var attentionMask = new DenseTensor<long>(
                    tokens.Select(t => t > 0 ? 1L : 0L).ToArray(),
                    new[] { 1, tokens.Length });

                var inputs = new List<NamedOnnxValue>
                {
                    NamedOnnxValue.CreateFromTensor("input_ids", inputTensor),
                    NamedOnnxValue.CreateFromTensor("attention_mask", attentionMask)
                };

                using var results = _session.Run(inputs);
                var output = results.First().AsTensor<float>();

                // Extract and pool embeddings (mean pooling)
                var embeddings = new float[_dimensions.BaseTextEmbedding];
                var seqLength = Math.Min(tokens.Length, (int)output.Dimensions[1]);
                var validTokens = tokens.Count(t => t > 0);

                for (int i = 0; i < _dimensions.BaseTextEmbedding; i++)
                {
                    float sum = 0;
                    for (int j = 0; j < seqLength; j++)
                    {
                        if (tokens[j] > 0) // Only non-padded tokens
                        {
                            sum += output[0, j, i];
                        }
                    }
                    embeddings[i] = validTokens > 0 ? sum / validTokens : 0;
                }

                return NormalizeVector(embeddings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ONNX embedding generation, falling back to simple method");
                return GenerateSimpleEmbedding(text, gameInput);
            }
        }

        // Simple tokenization for ONNX (replace with proper tokenizer in production)
        private static long[] SimpleTokenize(string text, int maxLength)
        {
            // Simplified tokenization - replace with proper tokenizer in production
            var tokens = new long[maxLength];
            var words = text.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);

            tokens[0] = 101; // [CLS] token
            var pos = 1;

            foreach (var word in words.Take(maxLength - 2))
            {
                // Simple hash-based token ID (replace with actual vocabulary)
                tokens[pos++] = Math.Abs(word.GetHashCode()) % 30000 + 1000;
            }

            if (pos < maxLength)
            {
                tokens[pos] = 102; // [SEP] token
            }

            return tokens;
        }

        public async Task<float[]> GenerateGameEmbeddingAsync(GameEmbeddingInput gameInput)
        {
            var gameText = BuildGameText(gameInput);
            var textEmbedding = await GenerateEmbeddingAsync(gameText, gameInput);
            var structuredFeatures = ExtractStructuredFeatures(gameInput);
            return CombineEmbeddings(textEmbedding, structuredFeatures);
        }

        public async Task<float[]> GenerateUserPreferenceEmbeddingAsync(UserPreferenceInput userInput)
        {
            var allEmbeddings = new List<float[]>();

            // Generate embeddings for favorite games (limit for performance)
            foreach (var game in userInput.FavoriteGames.Take(10))
            {
                var embedding = await GenerateGameEmbeddingAsync(game);
                allEmbeddings.Add(embedding);
            }

            // Generate embeddings for liked games
            foreach (var game in userInput.LikedGames.Take(10))
            {
                var embedding = await GenerateGameEmbeddingAsync(game);
                allEmbeddings.Add(embedding);
            }

            // Generate embeddings for liked review texts
            foreach (var reviewText in userInput.LikedReviewTexts.Take(20))
            {
                var embedding = await GenerateEmbeddingAsync(reviewText);
                allEmbeddings.Add(embedding);
            }

            // Generate embeddings for liked game list descriptions
            foreach (var listDescription in userInput.LikedGameListDescriptions.Take(10))
            {
                var embedding = await GenerateEmbeddingAsync(listDescription);
                allEmbeddings.Add(embedding);
            }

            // Generate embeddings for followed users' favorites
            foreach (var game in userInput.FollowedUsersFavorites.Take(20))
            {
                var embedding = await GenerateGameEmbeddingAsync(game);
                allEmbeddings.Add(embedding);
            }

            return AverageEmbeddings(allEmbeddings);
        }

        private static string BuildGameText(GameEmbeddingInput gameInput)
        {
            var parts = new List<string>
            {
                $"Game: {gameInput.Name}",
                $"Summary: {gameInput.Summary}",
                !string.IsNullOrEmpty(gameInput.Storyline) ? $"Storyline: {gameInput.Storyline}" : "",
                gameInput.Genres.Count > 0 ? $"Genres: {string.Join(", ", gameInput.Genres)}" : "",
                gameInput.Platforms.Count > 0 ? $"Platforms: {string.Join(", ", gameInput.Platforms)}" : "",
                gameInput.GameModes.Count > 0 ? $"Game Modes: {string.Join(", ", gameInput.GameModes)}" : "",
                gameInput.PlayerPerspectives.Count > 0 ? $"Perspectives: {string.Join(", ", gameInput.PlayerPerspectives)}" : "",
                gameInput.Rating.HasValue ? $"Rating: {gameInput.Rating:F1}" : "",
                gameInput.ReleaseDate.HasValue ? $"Released: {gameInput.ReleaseDate:yyyy}" : ""
            };

            // Enhance text with extracted semantic keywords for richer context
            AddSemanticKeywordsToText(parts, gameInput);

            return string.Join(". ", parts.Where(p => !string.IsNullOrEmpty(p)));
        }

        private static void AddSemanticKeywordsToText(List<string> parts, GameEmbeddingInput gameInput)
        {
            // Add semantic keywords if they were extracted by the GameIndexingService
            if (HasEnhancedSemanticKeywords(gameInput))
            {
                // Add genre-specific descriptors
                if (gameInput.ExtractedGenreKeywords.Count > 0)
                {
                    var genreDescriptors = string.Join(", ", gameInput.ExtractedGenreKeywords.Take(5));
                    parts.Add($"Genre Characteristics: {genreDescriptors}");
                }

                // Add gameplay mechanics
                if (gameInput.ExtractedMechanicKeywords.Count > 0)
                {
                    var mechanicDescriptors = string.Join(", ", gameInput.ExtractedMechanicKeywords.Take(5));
                    parts.Add($"Gameplay: {mechanicDescriptors}");
                }

                // Add thematic elements
                if (gameInput.ExtractedThemeKeywords.Count > 0)
                {
                    var themeDescriptors = string.Join(", ", gameInput.ExtractedThemeKeywords.Take(4));
                    parts.Add($"Themes: {themeDescriptors}");
                }

                // Add mood and atmosphere
                if (gameInput.ExtractedMoodKeywords.Count > 0)
                {
                    var moodDescriptors = string.Join(", ", gameInput.ExtractedMoodKeywords.Take(4));
                    parts.Add($"Atmosphere: {moodDescriptors}");
                }

                // Add art style if available
                if (gameInput.ExtractedArtStyleKeywords.Count > 0)
                {
                    var artDescriptors = string.Join(", ", gameInput.ExtractedArtStyleKeywords.Take(3));
                    parts.Add($"Art Style: {artDescriptors}");
                }

                // Add target audience information
                if (gameInput.ExtractedAudienceKeywords.Count > 0)
                {
                    var audienceDescriptors = string.Join(", ", gameInput.ExtractedAudienceKeywords.Take(3));
                    parts.Add($"Target Audience: {audienceDescriptors}");
                }

                // Add hierarchical boost information for additional context
                if (gameInput.HierarchicalBoosts.Count > 0)
                {
                    var boostDescriptors = string.Join(", ", gameInput.HierarchicalBoosts.Take(3));
                    parts.Add($"Special Characteristics: {boostDescriptors}");
                }
            }
        }

        private float[] ExtractStructuredFeatures(GameEmbeddingInput gameInput)
        {
            var features = new List<float>
            {
                // Rating feature (normalized 0-1)
                gameInput.Rating.HasValue ? (float)gameInput.Rating.Value / 10f : 0f,
            };

            // Release year feature (normalized, recent = higher)
            if (gameInput.ReleaseDate.HasValue)
            {
                var year = gameInput.ReleaseDate.Value.Year;
                var normalizedYear = Math.Max(0, Math.Min(1, (year - 1980) / (DateTime.Now.Year - 1980 + 5f)));
                features.Add(normalizedYear);
            }
            else
            {
                features.Add(0f);
            }

            // Genre diversity and platform availability
            features.Add(Math.Min(1f, gameInput.Genres.Count / 5f));
            features.Add(Math.Min(1f, gameInput.Platforms.Count / 10f));

            // Enhanced semantic features if available
            if (HasEnhancedSemanticKeywords(gameInput))
            {
                // Semantic keyword richness (normalized)
                var totalKeywords = gameInput.ExtractedGenreKeywords.Count +
                                  gameInput.ExtractedMechanicKeywords.Count +
                                  gameInput.ExtractedThemeKeywords.Count +
                                  gameInput.ExtractedMoodKeywords.Count +
                                  gameInput.ExtractedArtStyleKeywords.Count +
                                  gameInput.ExtractedAudienceKeywords.Count;
                features.Add(Math.Min(1f, totalKeywords / 20f)); // Normalize to max 20 keywords

                // Cross-category boost strength
                var boostStrength = gameInput.HierarchicalBoosts.Count > 0 ?
                    Math.Min(1f, gameInput.HierarchicalBoosts.Count / 10f) : 0f;
                features.Add(boostStrength);

                // Semantic weight distribution balance (measure of how balanced the keyword distribution is)
                if (gameInput.SemanticWeights.Count > 0)
                {
                    var weightVariance = CalculateWeightVariance(gameInput.SemanticWeights);
                    features.Add(Math.Min(1f, weightVariance));
                }
                else
                {
                    features.Add(0f);
                }

                // Category coverage (how many semantic categories are represented)
                var categoryCount = 0;
                if (gameInput.ExtractedGenreKeywords.Count > 0) categoryCount++;
                if (gameInput.ExtractedMechanicKeywords.Count > 0) categoryCount++;
                if (gameInput.ExtractedThemeKeywords.Count > 0) categoryCount++;
                if (gameInput.ExtractedMoodKeywords.Count > 0) categoryCount++;
                if (gameInput.ExtractedArtStyleKeywords.Count > 0) categoryCount++;
                if (gameInput.ExtractedAudienceKeywords.Count > 0) categoryCount++;
                features.Add(categoryCount / 6f); // Normalize to 0-1
            }
            else
            {
                // Add zeros for missing semantic features to maintain consistent size
                features.Add(0f); // keyword richness
                features.Add(0f); // boost strength
                features.Add(0f); // weight variance
                features.Add(0f); // category coverage
            }

            // Pad to fixed size using configured structured features count
            while (features.Count < _dimensions.StructuredFeatures) features.Add(0f);
            return [.. features.Take(_dimensions.StructuredFeatures)];
        }

        private static float CalculateWeightVariance(Dictionary<string, float> weights)
        {
            if (weights.Count == 0) return 0f;

            var validWeights = weights.Values.Where(w => w > 0).ToList();
            if (validWeights.Count <= 1) return 0f;

            var mean = validWeights.Average();
            var variance = validWeights.Sum(w => Math.Pow(w - mean, 2)) / validWeights.Count;

            return (float)Math.Sqrt(variance); // Return standard deviation normalized
        }

        private static float[] CombineEmbeddings(float[] textEmbedding, float[] structuredFeatures)
        {
            var combined = new float[textEmbedding.Length + structuredFeatures.Length];
            Array.Copy(textEmbedding, 0, combined, 0, textEmbedding.Length);
            Array.Copy(structuredFeatures, 0, combined, textEmbedding.Length, structuredFeatures.Length);
            return NormalizeVector(combined);
        }

        private float[] AverageEmbeddings(List<float[]> embeddings)
        {
            if (embeddings.Count == 0) return new float[_dimensions.TotalDimensions];

            var avgEmbedding = new float[_dimensions.TotalDimensions];

            foreach (var embedding in embeddings)
            {
                for (int i = 0; i < Math.Min(embedding.Length, avgEmbedding.Length); i++)
                {
                    avgEmbedding[i] += embedding[i];
                }
            }

            for (int i = 0; i < avgEmbedding.Length; i++)
            {
                avgEmbedding[i] /= embeddings.Count;
            }

            return avgEmbedding;
        }

        private float[] GenerateSimpleEmbedding(string text, GameEmbeddingInput? gameInput = null)
        {
            // Start with a zero embedding for purely semantic-based generation
            var embedding = new float[_dimensions.BaseTextEmbedding];
            
            // Apply semantic features as the primary signal source (preserve their relative strength)
            ApplySemanticKeywords(embedding, text, gameInput);
            
            // Calculate semantic signal strength before adding text features
            var semanticMagnitude = CalculateMagnitude(embedding);
            
            // Add text-based semantic features using TF-IDF-like approach
            AddTextBasedFeatures(embedding, text);
            
            // Apply magnitude-preserving normalization that maintains semantic signal ratios
            return ApplySemanticPreservingNormalization(embedding, semanticMagnitude);
        }

        private void AddTextBasedFeatures(float[] embedding, string text)
        {
            var words = text.ToLowerInvariant().Split(new[] { ' ', '.', ',', '!', '?', ';', ':' }, StringSplitOptions.RemoveEmptyEntries);
            var ranges = _dimensions.CategoryRanges;
            
            // Calculate word frequency and importance
            var wordCounts = words.GroupBy(w => w).ToDictionary(g => g.Key, g => g.Count());
            var totalWords = words.Length;
            
            foreach (var (word, count) in wordCounts)
            {
                if (word.Length < 3) continue; // Skip short words
                
                // Calculate TF (term frequency) 
                var tf = (float)count / totalWords;
                
                // Simple IDF approximation based on word length and rarity indicators
                var idf = word.Length > 6 ? 1.5f : 1.0f; // Longer words get higher weight
                var weight = tf * idf * 0.3f; // Scale down to avoid overwhelming semantic signals
                
                // Map word to embedding position using hash
                var hash = Math.Abs(word.GetHashCode());
                var basePosition = hash % _dimensions.BaseTextEmbedding;
                
                // Distribute across multiple positions for better representation
                for (int i = 0; i < 3; i++)
                {
                    var position = (basePosition + i * 17) % _dimensions.BaseTextEmbedding; // Prime number spreading
                    if (position < embedding.Length)
                    {
                        embedding[position] += weight / (i + 1); // Decay weight for spread positions
                    }
                }
            }
        }

        private void ApplySemanticKeywords(float[] embedding, string text, GameEmbeddingInput? gameInput = null)
        {
            var lowerText = text.ToLowerInvariant();
            var keywordMatches = new List<(string keyword, float weight, int position)>();

            // If we have enhanced semantic keywords from GameIndexingService, use them
            if (gameInput != null && HasEnhancedSemanticKeywords(gameInput))
            {
                ApplyEnhancedSemanticKeywords(embedding, gameInput);
                return;
            }

            // Fallback: Apply basic semantic keywords using configuration
            ApplyConfigurableSemanticKeywords(embedding, lowerText);
        }

        private static bool HasEnhancedSemanticKeywords(GameEmbeddingInput gameInput)
        {
            return gameInput.ExtractedGenreKeywords.Count > 0 ||
                   gameInput.ExtractedMechanicKeywords.Count > 0 ||
                   gameInput.ExtractedThemeKeywords.Count > 0 ||
                   gameInput.ExtractedMoodKeywords.Count > 0 ||
                   gameInput.ExtractedArtStyleKeywords.Count > 0 ||
                   gameInput.ExtractedAudienceKeywords.Count > 0;
        }

        private void ApplyEnhancedSemanticKeywords(float[] embedding, GameEmbeddingInput gameInput)
        {
            var keywordMatches = new List<(string keyword, float weight, int position)>();

            // Get semantic weights from the game input
            var weights = gameInput.SemanticWeights;
            var genreWeight = weights.GetValueOrDefault("genre", 0.25f);
            var mechanicsWeight = weights.GetValueOrDefault("mechanics", 0.20f);
            var themeWeight = weights.GetValueOrDefault("theme", 0.15f);
            var moodWeight = weights.GetValueOrDefault("mood", 0.15f);
            var artStyleWeight = weights.GetValueOrDefault("artstyle", 0.10f);
            var audienceWeight = weights.GetValueOrDefault("audience", 0.05f);
            var crossCategoryBoost = weights.GetValueOrDefault("cross_category_boost", 1.5f);

            // Apply keywords using standardized position ranges
            var ranges = _dimensions.CategoryRanges;

            ApplyKeywordCategory(gameInput.ExtractedGenreKeywords, genreWeight, ranges.Genre.Start, ranges.Genre.End, keywordMatches);
            ApplyKeywordCategory(gameInput.ExtractedMechanicKeywords, mechanicsWeight, ranges.Mechanics.Start, ranges.Mechanics.End, keywordMatches);
            ApplyKeywordCategory(gameInput.ExtractedThemeKeywords, themeWeight, ranges.Theme.Start, ranges.Theme.End, keywordMatches);
            ApplyKeywordCategory(gameInput.ExtractedMoodKeywords, moodWeight, ranges.Mood.Start, ranges.Mood.End, keywordMatches);
            ApplyKeywordCategory(gameInput.ExtractedArtStyleKeywords, artStyleWeight, ranges.ArtStyle.Start, ranges.ArtStyle.End, keywordMatches);
            ApplyKeywordCategory(gameInput.ExtractedAudienceKeywords, audienceWeight, ranges.Audience.Start, ranges.Audience.End, keywordMatches);

            // Apply hierarchical boosts using standardized range
            foreach (var boost in gameInput.HierarchicalBoosts)
            {
                var boostRange = ranges.HierarchicalBoosts;
                var boostPosition = Math.Abs(boost.GetHashCode()) % boostRange.Size + boostRange.Start;
                if (boostPosition < embedding.Length)
                {
                    keywordMatches.Add((boost, crossCategoryBoost * 0.3f, boostPosition));
                }
            }

            // Apply all matched keywords to embedding
            ApplyKeywordMatchesToEmbedding(embedding, keywordMatches);
        }

        private static void ApplyKeywordCategory(
            List<string> keywords,
            float baseWeight,
            int startPosition,
            int endPosition,
            List<(string keyword, float weight, int position)> keywordMatches)
        {
            if (keywords.Count == 0) return;

            var positionRange = endPosition - startPosition;
            var positionStep = Math.Max(1, positionRange / Math.Max(keywords.Count, 1));

            for (int i = 0; i < keywords.Count; i++)
            {
                var position = startPosition + (i * positionStep);
                var weight = baseWeight * (1.0f + (0.1f * (keywords.Count - i))); // Boost earlier keywords slightly
                keywordMatches.Add((keywords[i], weight, Math.Min(position, endPosition - 1)));
            }
        }

        private void ApplyConfigurableSemanticKeywords(float[] embedding, string lowerText)
        {
            // Load basic semantic keywords from configuration or defaults
            var config = LoadBasicSemanticConfig();
            var ranges = _dimensions.CategoryRanges;
            
            // Apply genre keywords
            ApplyKeywordCategory(config.GenreKeywords, lowerText, embedding, ranges.Genre.Start, ranges.Genre.Size, 1.2f);
            
            // Apply mechanics keywords with platform-aware enhancements
            var enhancedMechanics = EnhanceMechanicsWithPlatformKeywords(config.MechanicsKeywords, lowerText);
            ApplyKeywordCategory(enhancedMechanics, lowerText, embedding, ranges.Mechanics.Start, ranges.Mechanics.Size, 1.0f);
            
            // Apply theme keywords
            ApplyKeywordCategory(config.ThemeKeywords, lowerText, embedding, ranges.Theme.Start, ranges.Theme.Size, 0.8f);
            
            // Apply mood keywords
            ApplyKeywordCategory(config.MoodKeywords, lowerText, embedding, ranges.Mood.Start, ranges.Mood.Size, 0.7f);
            
            // Apply art style keywords
            ApplyKeywordCategory(config.ArtStyleKeywords, lowerText, embedding, ranges.ArtStyle.Start, ranges.ArtStyle.Size, 0.6f);
            
            // Apply audience keywords
            ApplyKeywordCategory(config.AudienceKeywords, lowerText, embedding, ranges.Audience.Start, ranges.Audience.Size, 0.5f);
        }

        private static List<string> EnhanceMechanicsWithPlatformKeywords(List<string> mechanicsKeywords, string text)
        {
            var enhanced = new List<string>(mechanicsKeywords);
            
            // Extract potential platform names from text and add their semantic keywords
            var potentialPlatforms = ExtractPlatformNamesFromText(text);
            foreach (var platform in potentialPlatforms)
            {
                var platformKeywords = PlatformAliasService.GetPlatformSemanticKeywords(platform);
                // Add platform-specific semantic keywords to mechanics
                enhanced.AddRange(platformKeywords.Where(k => 
                    k.Contains("controller") || k.Contains("portable") || k.Contains("touchscreen") || 
                    k.Contains("motion-controls") || k.Contains("backwards-compatible") || k.Contains("mods")));
            }
            
            return enhanced.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static List<string> ExtractPlatformNamesFromText(string text)
        {
            var platforms = new List<string>();
            var lowerText = text.ToLowerInvariant();
            
            // Common platform keywords that might appear in game text
            var platformIndicators = new[]
            {
                "pc", "ps5", "ps4", "xbox", "switch", "nintendo", "playstation", "mobile", 
                "android", "ios", "steam", "windows", "mac", "linux", "console"
            };
            
            foreach (var indicator in platformIndicators)
            {
                if (lowerText.Contains(indicator))
                {
                    platforms.Add(indicator);
                }
            }
            
            return platforms.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static void ApplyKeywordCategory(List<string> keywords, string text, float[] embedding, int startPos, int rangeSize, float baseWeight)
        {
            if (keywords.Count == 0) return;
            
            var positionStep = Math.Max(1, rangeSize / Math.Max(keywords.Count, 1));
            
            for (int i = 0; i < keywords.Count; i++)
            {
                if (text.Contains(keywords[i], StringComparison.OrdinalIgnoreCase))
                {
                    var position = startPos + (i * positionStep);
                    if (position < embedding.Length)
                    {
                        var weight = baseWeight * (1.0f + (0.2f * (keywords.Count - i) / keywords.Count));
                        embedding[position] += weight;
                        
                        // Spread influence to nearby positions for better semantic clustering
                        for (int j = 1; j <= 2; j++)
                        {
                            var decayWeight = weight * (0.4f / j);
                            if (position + j < embedding.Length) embedding[position + j] += decayWeight;
                            if (position - j >= 0) embedding[position - j] += decayWeight;
                        }
                    }
                }
            }
        }

        private BasicSemanticConfig LoadBasicSemanticConfig()
        {
            try
            {
                var configPath = Path.Combine(AppContext.BaseDirectory, "Configuration", "BasicSemanticKeywords.json");
                if (File.Exists(configPath))
                {
                    var jsonContent = File.ReadAllText(configPath);
                    var config = JsonSerializer.Deserialize<BasicSemanticConfig>(jsonContent, JsonOptions);
                    return config ?? GetDefaultBasicSemanticConfig();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading basic semantic configuration, using defaults");
            }
            
            return GetDefaultBasicSemanticConfig();
        }

        private static BasicSemanticConfig GetDefaultBasicSemanticConfig()
        {
            return new BasicSemanticConfig
            {
                GenreKeywords = ["rpg", "action", "adventure", "strategy", "simulation", "puzzle", "racing", "sports", "fighting", "shooter", "fps", "platformer", "roguelike", "mmorpg", "rts", "tower defense", "horror", "survival", "sandbox", "battle royale", "moba", "visual novel"],
                MechanicsKeywords = ["multiplayer", "co-op", "single-player", "open world", "crafting", "building", "exploration", "stealth", "combat", "leveling", "character progression", "skill tree", "inventory", "quest", "boss battles", "permadeath", "real-time", "turn-based"],
                ThemeKeywords = ["fantasy", "sci-fi", "cyberpunk", "steampunk", "post-apocalyptic", "medieval", "modern", "futuristic", "space", "underwater", "mythology", "anime", "realistic", "dystopian", "military", "pirate", "detective", "supernatural"],
                MoodKeywords = ["dark", "light-hearted", "serious", "comedic", "atmospheric", "intense", "relaxing", "mysterious", "epic", "emotional", "thrilling", "suspenseful", "peaceful", "chaotic", "nostalgic", "uplifting", "creepy", "energetic"],
                ArtStyleKeywords = ["pixel art", "8-bit", "16-bit", "hand-drawn", "3d", "realistic", "stylized", "minimalist", "retro", "colorful", "cel-shaded", "low-poly", "cartoon", "beautiful", "detailed", "artistic"],
                AudienceKeywords = ["casual", "hardcore", "family", "children", "mature", "beginner", "challenging", "difficult", "accessible", "complex", "competitive", "social"]
            };
        }


        private static void ApplyKeywordMatchesToEmbedding(float[] embedding, List<(string keyword, float weight, int position)> keywordMatches)
        {
            foreach (var (_, weight, position) in keywordMatches)
            {
                if (position < embedding.Length)
                {
                    embedding[position] += weight;

                    // Spread influence to nearby positions for semantic clustering
                    var spreadRange = 3;
                    for (int i = 1; i <= spreadRange; i++)
                    {
                        var decayWeight = weight * (0.3f / i);
                        if (position + i < embedding.Length) embedding[position + i] += decayWeight;
                        if (position - i >= 0) embedding[position - i] += decayWeight;
                    }
                }
            }
        }


        private float[] PadEmbeddingToGameSize(float[] textEmbedding)
        {
            // Add structured features to match game embedding size using total dimensions
            var paddedEmbedding = new float[_dimensions.TotalDimensions];
            Array.Copy(textEmbedding, 0, paddedEmbedding, 0, Math.Min(textEmbedding.Length, _dimensions.BaseTextEmbedding));
            // Remaining elements are already initialized to 0
            return paddedEmbedding;
        }

        private static float CalculateMagnitude(float[] vector)
        {
            return (float)Math.Sqrt(vector.Sum(x => x * x));
        }

        private static float[] ApplySemanticPreservingNormalization(float[] embedding, float semanticMagnitude)
        {
            var currentMagnitude = CalculateMagnitude(embedding);
            
            if (currentMagnitude == 0) return embedding;
            
            // Preserve semantic signal strength while applying gentle normalization
            // This approach maintains the relative importance of semantic signals
            // while preventing the embedding from becoming too extreme in magnitude
            
            var semanticRatio = semanticMagnitude > 0 ? semanticMagnitude / currentMagnitude : 0f;
            var targetMagnitude = Math.Max(0.5f, Math.Min(1.5f, semanticRatio)); // Bounded normalization
            
            var scalingFactor = targetMagnitude / currentMagnitude;
            
            for (int i = 0; i < embedding.Length; i++)
            {
                embedding[i] *= scalingFactor;
            }
            
            return embedding;
        }

        private static float[] NormalizeVector(float[] vector)
        {
            var magnitude = CalculateMagnitude(vector);
            if (magnitude > 0)
            {
                for (int i = 0; i < vector.Length; i++)
                {
                    vector[i] /= magnitude;
                }
            }
            return vector;
        }



        public void Dispose()
        {
            _session?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}