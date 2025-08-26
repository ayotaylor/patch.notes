using Backend.Services.Recommendation.Interfaces;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace Backend.Services.Recommendation
{
    public class SentenceTransformerEmbeddingService : IEmbeddingService, IDisposable
    {
        private readonly InferenceSession? _session;
        private readonly ILogger<SentenceTransformerEmbeddingService> _logger;
        private readonly IConfiguration _configuration;
        private readonly bool _useOnnxModel;
        
        public int EmbeddingDimensions => 384; // all-MiniLM-L6-v2 dimensions

        public SentenceTransformerEmbeddingService(IConfiguration configuration, ILogger<SentenceTransformerEmbeddingService> logger)
        {
            _logger = logger;
            _configuration = configuration;
            
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

        public async Task<float[]> GenerateEmbeddingAsync(string text)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (_useOnnxModel && _session != null)
                    {
                        return GenerateOnnxEmbedding(text);
                    }
                    else
                    {
                        return GenerateSimpleEmbedding(text);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating embedding for text: {Text}", text.Substring(0, Math.Min(50, text.Length)));
                    return new float[EmbeddingDimensions];
                }
            });
        }

        // Production ONNX implementation - activated when UseOnnx=true and model files exist
        private float[] GenerateOnnxEmbedding(string text)
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
                var embeddings = new float[384];
                var seqLength = Math.Min(tokens.Length, (int)output.Dimensions[1]);
                var validTokens = tokens.Count(t => t > 0);
                
                for (int i = 0; i < 384; i++)
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
                return GenerateSimpleEmbedding(text);
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
            var textEmbedding = await GenerateEmbeddingAsync(gameText);
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
            
            return string.Join(". ", parts.Where(p => !string.IsNullOrEmpty(p)));
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
            
            // Pad to fixed size
            while (features.Count < 10) features.Add(0f);
            return [.. features.Take(10)];
        }

        private static float[] CombineEmbeddings(float[] textEmbedding, float[] structuredFeatures)
        {
            var combined = new float[textEmbedding.Length + structuredFeatures.Length];
            Array.Copy(textEmbedding, 0, combined, 0, textEmbedding.Length);
            Array.Copy(structuredFeatures, 0, combined, textEmbedding.Length, structuredFeatures.Length);
            return NormalizeVector(combined);
        }

        private static float[] AverageEmbeddings(List<float[]> embeddings)
        {
            if (embeddings.Count == 0) return new float[384 + 10]; // Default size
            
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

        private static float[] GenerateSimpleEmbedding(string text)
        {
            // Deterministic embedding based on text content
            var embedding = new float[384];
            var hash = text.GetHashCode();
            var random = new Random(hash);
            
            for (int i = 0; i < 384; i++)
            {
                embedding[i] = (float)(random.NextDouble() * 2 - 1); // Range: -1 to 1
            }
            
            // Add semantic features based on keywords
            var keywords = new[] { "rpg", "action", "adventure", "strategy", "horror", "happy", "dark", "fantasy", "sci-fi", "multiplayer" };
            var lowerText = text.ToLowerInvariant();
            
            for (int i = 0; i < Math.Min(keywords.Length, 20); i++)
            {
                if (lowerText.Contains(keywords[i], StringComparison.OrdinalIgnoreCase))
                {
                    embedding[i] += 0.5f;
                }
            }
            
            return NormalizeVector(embedding);
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

        public void Dispose()
        {
            _session?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}