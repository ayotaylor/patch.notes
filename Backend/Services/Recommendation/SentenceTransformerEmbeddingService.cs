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
        private readonly Lazy<int> _embeddingDimensions;
        private readonly EmbeddingDimensions _dimensions;

        public int EmbeddingDimensions => _embeddingDimensions.Value;

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

            // Initialize embedding dimensions lazily by generating a sample embedding
            _embeddingDimensions = new Lazy<int>(() => CalculateEmbeddingDimensions());

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
                    var config = JsonSerializer.Deserialize<SemanticKeywordConfig>(jsonContent, new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true 
                    });
                    
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

        private static float[] ExtractStructuredFeatures(GameEmbeddingInput gameInput)
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

            // Pad to fixed size (should be exactly 20 now to match total dimensions of 404)
            while (features.Count < 20) features.Add(0f);
            return [.. features.Take(20)];
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

            var avgEmbedding = new float[embeddings.First().Length];

            foreach (var embedding in embeddings)
            {
                for (int i = 0; i < embedding.Length; i++)
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
            // Deterministic embedding based on text content
            var embedding = new float[_dimensions.BaseTextEmbedding];
            var hash = text.GetHashCode();
            var random = new Random(hash);

            for (int i = 0; i < embedding.Length; i++)
            {
                embedding[i] = (float)(random.NextDouble() * 2 - 1); // Range: -1 to 1
            }

            // Add semantic features based on comprehensive keyword taxonomy
            ApplySemanticKeywords(embedding, text, gameInput);

            return NormalizeVector(embedding);
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

            // Fallback to original keyword matching for backward compatibility
            ApplyLegacyKeywordMatching(embedding, lowerText, keywordMatches);
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

        private static void ApplyLegacyKeywordMatching(float[] embedding, string lowerText, List<(string keyword, float weight, int position)> keywordMatches)
        {
            // Core Genres (High weight - defines primary gameplay)
            foreach (var (keyword, weight, position) in GenreKeywords)
            {
                if (lowerText.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    keywordMatches.Add((keyword, weight, position));
                }
            }

            // Game Mechanics (Medium-high weight - defines how game plays)
            foreach (var (keyword, weight, position) in MechanicKeywords)
            {
                if (lowerText.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    keywordMatches.Add((keyword, weight, position));
                }
            }

            // Themes and Settings (Medium weight - defines context and world)
            foreach (var (keyword, weight, position) in ThemeKeywords)
            {
                if (lowerText.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    keywordMatches.Add((keyword, weight, position));
                }
            }

            // Moods and Tone (Medium weight - defines emotional experience)
            foreach (var (keyword, weight, position) in MoodKeywords)
            {
                if (lowerText.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    keywordMatches.Add((keyword, weight, position));
                }
            }

            // Art Style (Medium-low weight - defines visual presentation)
            foreach (var (keyword, weight, position) in ArtStyleKeywords)
            {
                if (lowerText.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    keywordMatches.Add((keyword, weight, position));
                }
            }

            // Target Audience and Difficulty (Lower weight - defines accessibility)
            foreach (var (keyword, weight, position) in AudienceKeywords)
            {
                if (lowerText.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    keywordMatches.Add((keyword, weight, position));
                }
            }

            // Apply hierarchical boosts for related keywords
            ApplyHierarchicalBoosts(keywordMatches, lowerText);

            // Apply matched keywords to embedding
            ApplyKeywordMatchesToEmbedding(embedding, keywordMatches);
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

        private static void ApplyHierarchicalBoosts(List<(string keyword, float weight, int position)> keywordMatches, string lowerText)
        {
            // Boost RPG subcategories when RPG is present
            if (keywordMatches.Any(k => k.keyword.Contains("rpg")))
            {
                if (lowerText.Contains("character") && lowerText.Contains("level")) 
                    keywordMatches.Add(("character progression", 0.4f, 15));
                if (lowerText.Contains("turn") && lowerText.Contains("based")) 
                    keywordMatches.Add(("tactical combat", 0.4f, 16));
                if (lowerText.Contains("story") || lowerText.Contains("narrative")) 
                    keywordMatches.Add(("story-driven", 0.3f, 17));
            }

            // Boost action subcategories when action is present
            if (keywordMatches.Any(k => k.keyword.Contains("action")))
            {
                if (lowerText.Contains("fast") || lowerText.Contains("quick") || lowerText.Contains("reflex")) 
                    keywordMatches.Add(("fast-paced", 0.4f, 25));
                if (lowerText.Contains("combat") || lowerText.Contains("fight")) 
                    keywordMatches.Add(("combat-focused", 0.4f, 26));
            }

            // Boost multiplayer features when multiplayer detected
            if (keywordMatches.Any(k => k.keyword.Contains("multiplayer")))
            {
                if (lowerText.Contains("team") || lowerText.Contains("coop") || lowerText.Contains("co-op")) 
                    keywordMatches.Add(("cooperative", 0.5f, 35));
                if (lowerText.Contains("versus") || lowerText.Contains("pvp") || lowerText.Contains("competitive")) 
                    keywordMatches.Add(("competitive", 0.5f, 36));
            }

            // Boost atmospheric keywords for horror/dark themes
            if (keywordMatches.Any(k => k.keyword.Contains("horror") || k.keyword.Contains("dark")))
            {
                if (lowerText.Contains("atmosphere") || lowerText.Contains("mood") || lowerText.Contains("tension")) 
                    keywordMatches.Add(("atmospheric", 0.4f, 45));
                if (lowerText.Contains("scary") || lowerText.Contains("frightening")) 
                    keywordMatches.Add(("intense", 0.3f, 46));
            }
        }

        private float[] PadEmbeddingToGameSize(float[] textEmbedding)
        {
            // Add structured features to match game embedding size
            var paddedEmbedding = new float[textEmbedding.Length + _dimensions.StructuredFeatures];
            Array.Copy(textEmbedding, 0, paddedEmbedding, 0, textEmbedding.Length);
            // Remaining elements are already initialized to 0
            return paddedEmbedding;
        }

        private static float[] NormalizeVector(float[] vector)
        {
            var magnitude = Math.Sqrt(vector.Sum(x => x * x));
            if (magnitude > 0)
            {
                for (int i = 0; i < vector.Length; i++)
                {
                    vector[i] = (float)(vector[i] / magnitude);
                }
            }
            return vector;
        }

        private int CalculateEmbeddingDimensions()
        {
            try
            {
                // Generate a sample game embedding to determine the actual dimensions
                var sampleGame = new GameEmbeddingInput
                {
                    Name = "Sample Game",
                    Summary = "A sample game for dimension calculation",
                    Genres = ["Action"],
                    Platforms = ["PC"]
                };

                var sampleEmbedding = GenerateGameEmbeddingAsync(sampleGame).Result;
                _logger.LogInformation("Calculated embedding dimensions: {Dimensions}", sampleEmbedding.Length);
                return sampleEmbedding.Length;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating embedding dimensions, using default");
                return _dimensions.TotalDimensions; // Use standardized dimensions
            }
        }

        // Comprehensive keyword taxonomy with weights and positions
        private static readonly List<(string keyword, float weight, int position)> GenreKeywords = new()
        {
            // Core Genres (High weight - 0.6-0.8)
            ("rpg", 0.8f, 0), ("role playing", 0.8f, 0), ("role-playing", 0.8f, 0),
            ("action", 0.7f, 1), ("adventure", 0.7f, 2), ("strategy", 0.7f, 3),
            ("simulation", 0.7f, 4), ("puzzle", 0.6f, 5), ("racing", 0.6f, 6),
            ("sports", 0.6f, 7), ("fighting", 0.6f, 8), ("shooter", 0.7f, 9),
            
            // RPG Subgenres
            ("jrpg", 0.7f, 10), ("japanese rpg", 0.7f, 10), ("wrpg", 0.7f, 11), ("western rpg", 0.7f, 11),
            ("arpg", 0.7f, 12), ("action rpg", 0.7f, 12), ("mmorpg", 0.7f, 13), ("tactical rpg", 0.6f, 14),
            ("roguelike", 0.6f, 15), ("roguelite", 0.6f, 15),
            
            // Strategy Subgenres
            ("rts", 0.6f, 18), ("real-time strategy", 0.6f, 18), ("turn-based", 0.6f, 19),
            ("4x", 0.6f, 20), ("tower defense", 0.5f, 21), ("city builder", 0.5f, 22),
        };

        private static readonly List<(string keyword, float weight, int position)> MechanicKeywords = new()
        {
            // Core Mechanics (Medium-high weight - 0.4-0.6)
            ("multiplayer", 0.6f, 30), ("single-player", 0.5f, 31), ("singleplayer", 0.5f, 31),
            ("co-op", 0.6f, 32), ("coop", 0.6f, 32), ("cooperative", 0.6f, 32),
            ("competitive", 0.5f, 33), ("pvp", 0.5f, 33), ("versus", 0.5f, 33),
            
            ("open world", 0.6f, 34), ("open-world", 0.6f, 34), ("sandbox", 0.5f, 35),
            ("linear", 0.4f, 36), ("exploration", 0.5f, 37), ("crafting", 0.5f, 38),
            ("building", 0.5f, 39), ("management", 0.5f, 40), ("survival", 0.5f, 41),
            
            ("stealth", 0.5f, 42), ("platformer", 0.5f, 43), ("platforming", 0.5f, 43),
            ("puzzle-solving", 0.5f, 44), ("resource management", 0.4f, 45),
            ("character progression", 0.5f, 46), ("leveling", 0.4f, 47), ("loot", 0.4f, 48),
        };

        private static readonly List<(string keyword, float weight, int position)> ThemeKeywords = new()
        {
            // Themes and Settings (Medium weight - 0.3-0.5)
            ("fantasy", 0.5f, 60), ("sci-fi", 0.5f, 61), ("science fiction", 0.5f, 61),
            ("cyberpunk", 0.4f, 62), ("steampunk", 0.4f, 63), ("post-apocalyptic", 0.4f, 64),
            ("horror", 0.5f, 65), ("western", 0.4f, 66), ("medieval", 0.4f, 67),
            ("modern", 0.3f, 68), ("futuristic", 0.4f, 69), ("historical", 0.4f, 70),
            
            ("space", 0.4f, 71), ("underwater", 0.3f, 72), ("urban", 0.3f, 73),
            ("jungle", 0.3f, 74), ("desert", 0.3f, 75), ("arctic", 0.3f, 76),
            
            ("mythology", 0.4f, 77), ("japanese", 0.3f, 78), ("anime", 0.4f, 79),
            ("cartoon", 0.3f, 80), ("realistic", 0.3f, 81),
        };

        private static readonly List<(string keyword, float weight, int position)> MoodKeywords = new()
        {
            // Moods and Emotional Tone (Medium weight - 0.3-0.5)
            ("dark", 0.4f, 90), ("light-hearted", 0.4f, 91), ("serious", 0.3f, 92),
            ("comedic", 0.4f, 93), ("funny", 0.4f, 93), ("humorous", 0.4f, 93),
            ("atmospheric", 0.4f, 94), ("intense", 0.4f, 95), ("relaxing", 0.4f, 96),
            ("stressful", 0.3f, 97), ("mysterious", 0.4f, 98), ("romantic", 0.3f, 99),
            
            ("nostalgic", 0.3f, 100), ("epic", 0.4f, 101), ("emotional", 0.4f, 102),
            ("cheerful", 0.3f, 103), ("happy", 0.3f, 103), ("melancholic", 0.3f, 104),
            ("dramatic", 0.4f, 105), ("peaceful", 0.3f, 106), ("chaotic", 0.3f, 107),
        };

        private static readonly List<(string keyword, float weight, int position)> ArtStyleKeywords = new()
        {
            // Art and Presentation Style (Medium-low weight - 0.2-0.4)
            ("pixel art", 0.4f, 120), ("pixelart", 0.4f, 120), ("8-bit", 0.3f, 121), ("16-bit", 0.3f, 122),
            ("hand-drawn", 0.3f, 123), ("3d realistic", 0.3f, 124), ("3d stylized", 0.3f, 125),
            ("minimalist", 0.3f, 126), ("retro", 0.3f, 127), ("vintage", 0.3f, 127),
            
            ("colorful", 0.3f, 128), ("monochrome", 0.2f, 129), ("noir", 0.3f, 130),
            ("cel-shaded", 0.3f, 131), ("photorealistic", 0.3f, 132),
            
            // Audio cues
            ("orchestral", 0.2f, 133), ("electronic", 0.2f, 134), ("ambient", 0.2f, 135),
            ("soundtrack", 0.2f, 136), ("music", 0.2f, 136),
        };

        private static readonly List<(string keyword, float weight, int position)> AudienceKeywords = new()
        {
            // Target Audience and Difficulty (Lower weight - 0.2-0.4)
            ("casual", 0.4f, 150), ("hardcore", 0.4f, 151), ("family", 0.3f, 152),
            ("children", 0.3f, 153), ("kids", 0.3f, 153), ("teen", 0.2f, 154),
            ("mature", 0.3f, 155), ("adult", 0.2f, 156),
            
            ("beginner", 0.3f, 157), ("easy", 0.3f, 158), ("challenging", 0.3f, 159),
            ("difficult", 0.3f, 160), ("hard", 0.3f, 160), ("punishing", 0.3f, 161),
            ("accessible", 0.3f, 162), ("complex", 0.3f, 163), ("simple", 0.3f, 164),
            
            ("social", 0.3f, 165), ("community", 0.2f, 166), ("competitive scene", 0.3f, 167),
        };

        public void Dispose()
        {
            _session?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}