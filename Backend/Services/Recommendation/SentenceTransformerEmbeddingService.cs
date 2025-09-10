using Backend.Services.Recommendation.Interfaces;
using Backend.Services.Recommendation.Tokenization;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Backend.Configuration;
using System.Buffers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Backend.Services.Recommendation
{
    public class SentenceTransformerEmbeddingService : IEmbeddingService, IDisposable
    {
        private readonly InferenceSession? _session;
        private readonly ILogger<SentenceTransformerEmbeddingService> _logger;
        private readonly IConfiguration _configuration;
        private readonly ISemanticConfigurationService _configService;
        private readonly HybridEmbeddingEnhancer _hybridEnhancer;
        private readonly bool _useOnnxModel;
        private readonly EmbeddingDimensions _dimensions;
        private readonly EmbeddingCacheManager _cacheManager;
        private readonly ITokenizationStrategy _tokenizationStrategy;
        private readonly ArrayPool<long> _longArrayPool = ArrayPool<long>.Shared;
        private readonly ArrayPool<float> _floatArrayPool = ArrayPool<float>.Shared;
        private const int MaxBatchSize = 50;
        private ITokenizationStrategy? _cachedWorkingStrategy;
        
        // GPU Service integration
        private readonly HttpClient? _gpuServiceClient;
        private readonly bool _useGpuService;
        private readonly string _gpuServiceUrl;

        public int EmbeddingDimensions => _dimensions.TotalDimensions;

        public SentenceTransformerEmbeddingService(
            IConfiguration configuration, 
            ILogger<SentenceTransformerEmbeddingService> logger,
            ISemanticConfigurationService configService,
            HybridEmbeddingEnhancer hybridEnhancer)
        {
            _logger = logger;
            _configuration = configuration;
            _configService = configService;
            _hybridEnhancer = hybridEnhancer;

            // Optimize cache sizes for high-throughput indexing
            var cacheConfig = configuration.GetSection("EmbeddingCache");
            var maxEmbeddingCacheSize = cacheConfig.GetValue<int>("MaxEmbeddingCacheSize", 5000);
            var maxTokenCacheSize = cacheConfig.GetValue<int>("MaxTokenCacheSize", 15000);
            var cacheExpiryHours = cacheConfig.GetValue<int>("ExpiryHours", 2);

            _cacheManager = new EmbeddingCacheManager(
                maxEmbeddingCacheSize,
                maxTokenCacheSize,
                TimeSpan.FromHours(cacheExpiryHours));

            // Initialize tokenization strategy
            _tokenizationStrategy = TokenizationStrategyFactory.CreateStrategy(configuration, logger);

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
                    sessionOptions.EnableMemoryPattern = true;
                    sessionOptions.EnableCpuMemArena = true;
                    sessionOptions.ExecutionMode = ExecutionMode.ORT_PARALLEL;
                    sessionOptions.InterOpNumThreads = Environment.ProcessorCount;
                    sessionOptions.IntraOpNumThreads = Math.Max(1, Environment.ProcessorCount / 2);
                    _session = new InferenceSession(modelPath!, sessionOptions);
                    _logger.LogInformation("Embedding service initialized with optimized ONNX model: {ModelPath}", modelPath);
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

            // Initialize GPU service
            _useGpuService = _configuration.GetValue<bool>("EmbeddingService:UseGpuService", false);
            _gpuServiceUrl = _configuration.GetValue<string>("EmbeddingService:GpuServiceUrl", "http://localhost:8001");
            
            if (_useGpuService)
            {
                _gpuServiceClient = new HttpClient
                {
                    BaseAddress = new Uri(_gpuServiceUrl),
                    Timeout = TimeSpan.FromMinutes(5) // Long timeout for large batches
                };
                _logger.LogInformation("GPU service integration enabled at: {Url}", _gpuServiceUrl);
            }
            else
            {
                _logger.LogInformation("GPU service integration disabled, using local embedding generation");
            }
        }

        #region GPU Service Integration

        /// <summary>
        /// Generate embeddings for a batch of texts using GPU acceleration
        /// </summary>
        public async Task<List<float[]>> GenerateEmbeddingsBatchAsync(List<string> texts)
        {
            if (texts == null || texts.Count == 0)
                return new List<float[]>();

            // Use GPU service if enabled and we have multiple texts
            if (_useGpuService && _gpuServiceClient != null && texts.Count > 1)
            {
                try
                {
                    return await CallGpuServiceBatchAsync(texts);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "GPU service failed, falling back to local processing for {Count} texts", texts.Count);
                    // Fall through to local processing
                }
            }

            // Fallback to local processing (individual or ONNX batch)
            if (_useOnnxModel && texts.Count > 1)
            {
                return await ProcessTextBatchWithOnnx(texts);
            }
            else
            {
                // Individual processing for small batches or when ONNX unavailable
                var tasks = texts.Select(text => GenerateEmbeddingAsync(text));
                return (await Task.WhenAll(tasks)).ToList();
            }
        }

        /// <summary>
        /// Generate embeddings for game inputs using GPU acceleration
        /// </summary>
        public async Task<List<float[]>> GenerateGameEmbeddingsBatchAsync(List<GameEmbeddingInput> gameInputs)
        {
            if (gameInputs == null || gameInputs.Count == 0)
                return new List<float[]>();

            // Convert game inputs to texts
            var texts = gameInputs.Select(BuildGameText).ToList();
            
            // Use the batch text processing
            return await GenerateEmbeddingsBatchAsync(texts);
        }

        /// <summary>
        /// Call the Python GPU service for batch embedding generation
        /// </summary>
        private async Task<List<float[]>> CallGpuServiceBatchAsync(List<string> texts)
        {
            if (_gpuServiceClient == null)
                throw new InvalidOperationException("GPU service client not initialized");

            const int maxBatchSize = 100; // Reasonable batch size for HTTP requests
            var allEmbeddings = new List<float[]>();

            // Process in chunks to avoid request size limits
            for (int i = 0; i < texts.Count; i += maxBatchSize)
            {
                var batch = texts.Skip(i).Take(maxBatchSize).ToList();
                var embeddings = await CallGpuServiceSingleBatch(batch);
                allEmbeddings.AddRange(embeddings);
            }

            return allEmbeddings;
        }

        /// <summary>
        /// Call GPU service for a single batch
        /// </summary>
        private async Task<List<float[]>> CallGpuServiceSingleBatch(List<string> texts)
        {
            var startTime = DateTime.UtcNow;
            
            var request = new { texts = texts, batch_size = Math.Min(texts.Count, 64) };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _gpuServiceClient!.PostAsync("/embed/batch", content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"GPU service returned {response.StatusCode}: {errorContent}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<GpuServiceResponse>(responseJson);

            if (result?.Embeddings == null)
                throw new InvalidOperationException("GPU service returned null embeddings");

            var processingTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
            var rate = texts.Count / (processingTime / 1000.0);
            
            _logger.LogDebug("GPU service processed {Count} embeddings in {Time:F0}ms ({Rate:F1} emb/sec) on {Device}", 
                texts.Count, processingTime, rate, result.DeviceUsed ?? "unknown");

            return result.Embeddings.Select(e => e.ToArray()).ToList();
        }

        /// <summary>
        /// Process text batch with local ONNX model (fallback)
        /// </summary>
        private async Task<List<float[]>> ProcessTextBatchWithOnnx(List<string> texts)
        {
            // This would use your existing ONNX processing logic
            // For now, fall back to individual processing
            var tasks = texts.Select(text => GenerateEmbeddingAsync(text));
            return (await Task.WhenAll(tasks)).ToList();
        }

        #endregion

        /// <summary>
        /// Response model for GPU service batch embedding endpoint
        /// </summary>
        private class GpuServiceResponse
        {
            [JsonPropertyName("embeddings")]
            public List<List<float>> Embeddings { get; set; } = new();
            
            [JsonPropertyName("processing_time")]
            public double ProcessingTime { get; set; }
            
            [JsonPropertyName("device_used")]
            public string? DeviceUsed { get; set; }
            
            [JsonPropertyName("batch_size")]
            public int BatchSize { get; set; }
        }

        private EmbeddingDimensions LoadEmbeddingDimensions()
        {
            try
            {
                var config = _configService.SemanticConfig;
                if (config?.Dimensions != null)
                {
                    _logger.LogDebug("Using embedding dimensions from configuration service: {TotalDimensions} dimensions",
                        config.Dimensions.TotalDimensions);
                    return config.Dimensions;
                }

                _logger.LogWarning("Could not load embedding dimensions from configuration service, using EmbeddingConstants defaults");
                return new EmbeddingDimensions
                {
                    BaseTextEmbedding = EmbeddingConstants.BASE_TEXT_EMBEDDING_DIMENSIONS
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading embedding dimensions from configuration service, using EmbeddingConstants defaults");
                return new EmbeddingDimensions
                {
                    BaseTextEmbedding = EmbeddingConstants.BASE_TEXT_EMBEDDING_DIMENSIONS
                };
            }
        }

        public async Task<float[]> GenerateEmbeddingAsync(string text)
        {
            return await GenerateEmbeddingAsync(text, null);
        }

        public async Task<float[]> GenerateEmbeddingAsync(string text, GameEmbeddingInput? gameInput)
        {
            if (_useOnnxModel && _session != null)
            {
                return await GenerateRawTextEmbeddingAsync(text);
            }
            else
            {
                _logger.LogWarning("ONNX model not available, but ONNX-only mode requested");
                throw new InvalidOperationException("ONNX model required but not available");
            }
        }

        /// <summary>
        /// Generates raw text embedding using ONNX model - returns BASE_TEXT_EMBEDDING_DIMENSIONS
        /// </summary>
        private Task<float[]> GenerateRawTextEmbeddingAsync(string text)
        {
            try
            {
                if (_useOnnxModel && _session != null)
                {
                    var embedding = GenerateOnnxEmbedding(text);
                    return Task.FromResult(embedding);
                }
                else
                {
                    throw new InvalidOperationException("ONNX model required but not available");
                }
            }
            catch (Exception ex)
            {
                var textPreview = string.IsNullOrEmpty(text) ? "[null/empty]" :
                    text.Length <= 50 ? text : text.Substring(0, 50) + "...";
                _logger.LogError(ex, "Error generating ONNX embedding for text: {TextPreview}", textPreview);
                return Task.FromException<float[]>(ex);
            }
        }

        /// <summary>
        /// Optimized ONNX embedding generation using proper BERT tokenization
        /// Follows sentence-transformers all-MiniLM-L6-v2 model standards
        /// </summary>
        private float[] GenerateOnnxEmbedding(string text)
        {
            if (_session == null)
                throw new InvalidOperationException("ONNX session not initialized");

            try
            {
                const int maxSequenceLength = 512;

                // Step 1: Tokenize with proper BERT tokenizer
                var (inputIds, attentionMask) = TokenizeForBert(text, maxSequenceLength);

                // Step 2: Create ONNX input tensors (batch_size=1)
                var inputTensor = new DenseTensor<long>(inputIds, new[] { 1, inputIds.Length });
                var maskTensor = new DenseTensor<long>(attentionMask, new[] { 1, attentionMask.Length });

                // Create token_type_ids (all zeros for single sentence)
                var tokenTypeIds = new long[inputIds.Length];
                var tokenTypeTensor = new DenseTensor<long>(tokenTypeIds, new[] { 1, tokenTypeIds.Length });

                var inputs = new List<NamedOnnxValue>
                {
                    NamedOnnxValue.CreateFromTensor("input_ids", inputTensor),
                    NamedOnnxValue.CreateFromTensor("attention_mask", maskTensor),
                    NamedOnnxValue.CreateFromTensor("token_type_ids", tokenTypeTensor)
                };

                // Step 3: Run ONNX inference
                using var results = _session.Run(inputs);
                var lastHiddenState = results.FirstOrDefault()?.AsTensor<float>()
                    ?? throw new InvalidOperationException("ONNX model returned no output");

                // Step 4: Apply mean pooling (sentence-transformers standard)
                var pooledEmbedding = PerformMeanPooling(lastHiddenState, attentionMask);

                // Step 5: L2 normalize (sentence-transformers standard)
                return NormalizeVector(pooledEmbedding);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ONNX embedding generation failed for text: {TextPreview}",
                    text.Length > 100 ? text[..100] + "..." : text);
                throw;
            }
        }

        /// <summary>
        /// Tokenize text for BERT model using the configured strategy with caching
        /// </summary>
        private (long[] inputIds, long[] attentionMask) TokenizeForBert(string text, int maxLength)
        {
            // Check if we have cached tokens for this text
            var cacheKey = $"{text}:{maxLength}";
            if (_cacheManager.TryGetTokens(cacheKey, out var cachedTokens))
            {
                // Convert cached tokens to BERT inputs
                return TokenizationStrategyBase.CreateBertInputs(cachedTokens, maxLength);
            }

            // Use strategy to tokenize
            var result = _tokenizationStrategy.TokenizeForBert(text, maxLength);

            // Extract tokens from result for caching
            var tokensToCache = ExtractTokensFromBertInputs(result.inputIds, result.attentionMask);
            _cacheManager.CacheTokens(cacheKey, tokensToCache);

            return result;
        }

        /// <summary>
        /// Extract token list from BERT input arrays for caching
        /// </summary>
        private static List<int> ExtractTokensFromBertInputs(long[] inputIds, long[] attentionMask)
        {
            var tokens = new List<int>();

            // Skip CLS token at start (index 0) and SEP token at end
            for (int i = 1; i < inputIds.Length && attentionMask[i] == 1; i++)
            {
                if (inputIds[i] != 102) // Not SEP token
                {
                    tokens.Add((int)inputIds[i]);
                }
            }

            return tokens;
        }

        /// <summary>
        /// Perform mean pooling on token embeddings (sentence-transformers standard) with ArrayPool optimization
        /// </summary>
        private float[] PerformMeanPooling(Tensor<float> tokenEmbeddings, long[] attentionMask)
        {
            var sequenceLength = tokenEmbeddings.Dimensions[1];
            var embeddingDim = tokenEmbeddings.Dimensions[2];

            if (embeddingDim != _dimensions.BaseTextEmbedding)
            {
                throw new InvalidOperationException(
                    $"Model embedding dimension {embeddingDim} doesn't match expected {_dimensions.BaseTextEmbedding}");
            }

            var pooledEmbeddingArray = ArrayPool<float>.Shared.Rent(embeddingDim);
            try
            {
                var pooledEmbedding = pooledEmbeddingArray.AsSpan(0, embeddingDim);
                pooledEmbedding.Clear();
                var validTokenCount = 0;

                // Sum embeddings for all valid (non-padded) tokens
                for (int seq = 0; seq < Math.Min(sequenceLength, attentionMask.Length); seq++)
                {
                    if (attentionMask[seq] > 0) // Valid token
                    {
                        validTokenCount++;
                        for (int dim = 0; dim < embeddingDim; dim++)
                        {
                            pooledEmbedding[dim] += tokenEmbeddings[0, seq, dim];
                        }
                    }
                }

                // Compute mean (average over valid tokens)
                if (validTokenCount > 0)
                {
                    for (int dim = 0; dim < embeddingDim; dim++)
                    {
                        pooledEmbedding[dim] /= validTokenCount;
                    }
                }

                return pooledEmbedding.ToArray();
            }
            finally
            {
                ArrayPool<float>.Shared.Return(pooledEmbeddingArray);
            }
        }


        public async Task<float[]> GenerateGameEmbeddingAsync(GameEmbeddingInput gameInput)
        {
            var gameText = BuildGameText(gameInput);

            // Check cache first (cache contains hybrid-enhanced embeddings)
            if (_cacheManager.TryGetEmbedding(gameText, out var cachedEmbedding))
            {
                return cachedEmbedding;
            }

            // Step 1: Generate base text embedding
            var textEmbedding = await GenerateRawTextEmbeddingAsync(gameText);

            // Validate text embedding dimensions
            if (textEmbedding.Length != EmbeddingConstants.BASE_TEXT_EMBEDDING_DIMENSIONS)
            {
                _logger.LogError("Text embedding dimension mismatch: got {Actual}, expected {Expected}. Resizing...",
                    textEmbedding.Length, EmbeddingConstants.BASE_TEXT_EMBEDDING_DIMENSIONS);
                textEmbedding = ResizeEmbedding(textEmbedding, EmbeddingConstants.BASE_TEXT_EMBEDDING_DIMENSIONS);
            }

            // Step 2: Apply positional semantic enhancement
            _hybridEnhancer.ApplyPositionalEnhancement(textEmbedding, gameInput);

            // Step 3: Normalize the final hybrid embedding to maintain vector properties
            var finalEmbedding = NormalizeVector(textEmbedding);

            // Final validation using centralized helper
            var (isValid, errorMessage) = EmbeddingDimensionValidator.ValidateEmbeddingDimensions(finalEmbedding.Length, "final embedding");
            if (!isValid)
            {
                _logger.LogCritical("CRITICAL: {ErrorMessage}", errorMessage);
                throw new InvalidOperationException(errorMessage!);
            }

            // Cache the hybrid-enhanced result
            _cacheManager.CacheEmbedding(gameText, finalEmbedding);

            _logger.LogDebug("Successfully generated hybrid embedding: {TotalDims} dimensions (text + positional)", finalEmbedding.Length);

            return finalEmbedding;
        }

        /// <summary>
        /// Process multiple games efficiently using batch ONNX inference
        /// </summary>
        public async Task<List<float[]>> ProcessGamesInBatch(List<GameEmbeddingInput> gameInputs)
        {
            if (gameInputs == null || gameInputs.Count == 0)
                return new List<float[]>();

            // Increase batch size for better ONNX throughput - ONNX performs much better with larger batches
            const int maxBatchSize = 128;
            if (gameInputs.Count > maxBatchSize)
            {
                _logger.LogWarning("Batch size {BatchSize} exceeds maximum {MaxSize}, processing in chunks",
                    gameInputs.Count, maxBatchSize);

                var allResults = new List<float[]>();
                for (int i = 0; i < gameInputs.Count; i += maxBatchSize)
                {
                    var chunk = gameInputs.Skip(i).Take(maxBatchSize).ToList();
                    var chunkResults = await ProcessGamesInBatch(chunk);
                    allResults.AddRange(chunkResults);
                }
                return allResults;
            }

            var results = new float[gameInputs.Count][];
            var gamesNeedingEmbeddings = new List<(GameEmbeddingInput game, string text, int index)>();

            // Check cache for all games first
            for (int i = 0; i < gameInputs.Count; i++)
            {
                var gameText = BuildGameText(gameInputs[i]);
                if (_cacheManager.TryGetEmbedding(gameText, out var cachedEmbedding))
                {
                    results[i] = cachedEmbedding;
                }
                else
                {
                    gamesNeedingEmbeddings.Add((gameInputs[i], gameText, i));
                }
            }

            // Process uncached games in batches to respect MaxBatchSize limit
            for (int offset = 0; offset < gamesNeedingEmbeddings.Count; offset += MaxBatchSize)
            {
                try
                {
                    var batchSize = Math.Min(MaxBatchSize, gamesNeedingEmbeddings.Count - offset);
                    var batchGames = new List<(GameEmbeddingInput game, string text, int index)>(batchSize);
                    var textsToEmbed = new List<string>(batchSize);

                    for (int i = offset; i < offset + batchSize && i < gamesNeedingEmbeddings.Count; i++)
                    {
                        batchGames.Add(gamesNeedingEmbeddings[i]);
                        textsToEmbed.Add(gamesNeedingEmbeddings[i].text);
                    }

                    var newEmbeddings = await CreateEmbeddingsFromBatch(textsToEmbed);

                    // Validate we got the expected number of embeddings
                    if (newEmbeddings.Count != batchGames.Count)
                    {
                        _logger.LogError("Embedding count mismatch: expected {Expected}, got {Actual}",
                            batchGames.Count, newEmbeddings.Count);
                        continue;
                    }

                    // Put results in correct positions and cache them
                    for (int i = 0; i < batchGames.Count; i++)
                    {
                        var (_, text, originalIndex) = batchGames[i];
                        var embedding = newEmbeddings[i];
                        results[originalIndex] = embedding;
                        _cacheManager.CacheEmbedding(text, embedding);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process embedding batch at offset {Offset}", offset);
                    continue;
                }
            }

            return results.ToList();
        }

        /// <summary>
        /// Create embeddings from multiple texts using single ONNX inference
        /// </summary>
        private Task<List<float[]>> CreateEmbeddingsFromBatch(List<string> texts)
        {
            if (_session == null)
                throw new InvalidOperationException("ONNX session not initialized");

            const int maxSequenceLength = 512;
            var actualBatchSize = Math.Min(texts.Count, MaxBatchSize);
            var totalSize = actualBatchSize * maxSequenceLength;

            // Rent arrays from pool
            var inputIds = _longArrayPool.Rent(totalSize);
            var attentionMasks = _longArrayPool.Rent(totalSize);
            var tokenTypeIds = _longArrayPool.Rent(totalSize);

            try
            {
                // Clear token type ids (should be zeros)
                Array.Clear(tokenTypeIds, 0, totalSize);

                // Tokenize each text and fill batch arrays using spans
                for (int i = 0; i < actualBatchSize; i++)
                {
                    var (textInputIds, textAttentionMask) = GetCachedTokenizationStrategy().TokenizeForBert(texts[i], maxSequenceLength);
                    var startPosition = i * maxSequenceLength;

                    textInputIds.AsSpan().CopyTo(inputIds.AsSpan(startPosition, maxSequenceLength));
                    textAttentionMask.AsSpan().CopyTo(attentionMasks.AsSpan(startPosition, maxSequenceLength));
                }

                // Create ONNX input tensors using rented arrays
                var inputTensor = new DenseTensor<long>(inputIds.AsMemory(0, totalSize), new[] { actualBatchSize, maxSequenceLength });
                var maskTensor = new DenseTensor<long>(attentionMasks.AsMemory(0, totalSize), new[] { actualBatchSize, maxSequenceLength });
                var tokenTypeTensor = new DenseTensor<long>(tokenTypeIds.AsMemory(0, totalSize), new[] { actualBatchSize, maxSequenceLength });

                var onnxInputs = new List<NamedOnnxValue>
                {
                    NamedOnnxValue.CreateFromTensor("input_ids", inputTensor),
                    NamedOnnxValue.CreateFromTensor("attention_mask", maskTensor),
                    NamedOnnxValue.CreateFromTensor("token_type_ids", tokenTypeTensor)
                };

                // Run single ONNX inference for entire batch
                using var onnxResults = _session.Run(onnxInputs);
                var batchOutput = onnxResults[0]?.AsTensor<float>()
                    ?? throw new InvalidOperationException("ONNX model returned no output");

                // Extract individual embeddings from batch results
                var embeddings = new List<float[]>();
                for (int i = 0; i < actualBatchSize; i++)
                {
                    var maskSpan = attentionMasks.AsSpan(i * maxSequenceLength, maxSequenceLength);
                    var embedding = ExtractEmbeddingFromBatchResult(batchOutput, maskSpan, i);
                    embeddings.Add(embedding);
                }

                return Task.FromResult(embeddings);
            }
            finally
            {
                // Return arrays to pool
                _longArrayPool.Return(inputIds);
                _longArrayPool.Return(attentionMasks);
                _longArrayPool.Return(tokenTypeIds);
            }
        }

        /// <summary>
        /// Get cached working tokenization strategy to avoid cascade overhead
        /// </summary>
        private ITokenizationStrategy GetCachedTokenizationStrategy()
        {
            if (_cachedWorkingStrategy != null)
                return _cachedWorkingStrategy;

            // Find the first working strategy and cache it
            if (_tokenizationStrategy is CascadeStrategy cascade)
            {
                var strategies = cascade.GetType().GetField("_strategies", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                    .GetValue(cascade) as List<ITokenizationStrategy>;

                _cachedWorkingStrategy = strategies?.FirstOrDefault(s => s.IsAvailable) ?? _tokenizationStrategy;
            }
            else
            {
                _cachedWorkingStrategy = _tokenizationStrategy;
            }

            return _cachedWorkingStrategy;
        }

        /// <summary>
        /// Extract single embedding from batch ONNX results using mean pooling
        /// </summary>
        private float[] ExtractEmbeddingFromBatchResult(Tensor<float> batchResults, ReadOnlySpan<long> attentionMask, int itemIndex)
        {
            var sequenceLength = batchResults.Dimensions[1];
            var embeddingDim = batchResults.Dimensions[2];

            // Rent pooled embedding array
            var pooledEmbedding = _floatArrayPool.Rent(embeddingDim);
            var validTokenCount = 0;

            try
            {
                // Clear the rented array
                Array.Clear(pooledEmbedding, 0, embeddingDim);

                // Vectorized mean pooling using spans
                for (int seq = 0; seq < sequenceLength && seq < attentionMask.Length; seq++)
                {
                    if (attentionMask[seq] > 0)
                    {
                        validTokenCount++;
                        var embeddingSpan = pooledEmbedding.AsSpan(0, embeddingDim);

                        // Add entire embedding vector at once
                        for (int dim = 0; dim < embeddingDim; dim++)
                        {
                            embeddingSpan[dim] += batchResults[itemIndex, seq, dim];
                        }
                    }
                }

                // Calculate mean in-place
                if (validTokenCount > 0)
                {
                    var embeddingSpan = pooledEmbedding.AsSpan(0, embeddingDim);
                    var invCount = 1.0f / validTokenCount;
                    for (int dim = 0; dim < embeddingDim; dim++)
                    {
                        embeddingSpan[dim] *= invCount;
                    }
                }

                // Copy to final result array
                var result = new float[embeddingDim];
                pooledEmbedding.AsSpan(0, embeddingDim).CopyTo(result);
                return NormalizeVector(result);
            }
            finally
            {
                _floatArrayPool.Return(pooledEmbedding);
            }

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

        private string BuildGameText(GameEmbeddingInput gameInput)
        {
            // Estimate capacity to reduce StringBuilder reallocations
            var estimatedLength = gameInput.Name.Length + 100 +
                (gameInput.Genres.Sum(g => g.Length) + gameInput.Genres.Count * 2) +
                (gameInput.Platforms.Sum(p => p.Length) + gameInput.Platforms.Count * 2) +
                (gameInput.GameModes.Sum(m => m.Length) + gameInput.GameModes.Count * 2) +
                (gameInput.Companies.Sum(c => c.Length) + gameInput.Companies.Count * 2) +
                (gameInput.PlayerPerspectives.Sum(pp => pp.Length) + gameInput.PlayerPerspectives.Count * 2) +
                (gameInput.GameType.Length * 2) +
                (gameInput.ReleaseDate != null ?
                    gameInput.ReleaseDate?.ToString().Length * 2 : 0) +
                (gameInput.Rating != null ?
                    gameInput.Rating?.ToString().Length * 2 : 0);

            var estimatedLengthVal = estimatedLength.Value;

            var sb = new StringBuilder(estimatedLengthVal);

            // Build text directly without intermediate collections
            sb.Append("Game: ").Append(gameInput.Name);

            if (gameInput.Genres.Count > 0)
            {
                sb.Append(". Genres: ");
                for (int i = 0; i < gameInput.Genres.Count; i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.Append(gameInput.Genres[i]);
                }
            }

            if (gameInput.Platforms.Count > 0)
            {
                sb.Append(". Platforms: ");
                // var expandedPlatforms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                
                // // Add platforms directly (normalization handled at filter level)
                // expandedPlatforms.AddRange(gameInput.Platforms);

                bool first = true;
                foreach (var platform in gameInput.Platforms)
                {
                    if (!first) sb.Append(", ");
                    sb.Append(platform);
                    first = false;
                }
            }

            if (gameInput.GameModes.Count > 0)
            {
                sb.Append(". Game Modes: ");
                for (int i = 0; i < gameInput.GameModes.Count; i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.Append(gameInput.GameModes[i]);
                }
            }

            if (gameInput.PlayerPerspectives.Count > 0)
            {
                sb.Append(". Player Perspective: ");
                for (int i = 0; i < gameInput.PlayerPerspectives.Count; i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.Append(gameInput.PlayerPerspectives[i]);
                }
            }

            // if (gameInput.AgeRatings.Count > 0)
            // {
            //     sb.Append(". Age Rating: ");
            //     for (int i = 0; i < gameInput.AgeRatings.Count; i++)
            //     {
            //         if (i > 0) sb.Append(", ");
            //         sb.Append(gameInput.AgeRatings[i]);
            //     }
            // }

            if (gameInput.Companies.Count > 0)
            {
                sb.Append(". Companies: ");
                for (int i = 0; i < gameInput.Companies.Count; i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.Append(gameInput.Companies[i]);
                }
            }

            if (!string.IsNullOrEmpty(gameInput.GameType))
            {
                sb.Append(". Game Type: ").Append(gameInput.GameType);
            }

            if (gameInput.ReleaseDate.HasValue)
            {
                sb.Append(". Released(unix timestamp): ").Append(gameInput.ReleaseDate.Value.ToString());
            }

            if (gameInput.Rating.HasValue)
            {
                sb.Append(". Rating: ").Append(gameInput.Rating.Value.ToString("F1"));
            }

            // Add semantic keywords efficiently
            AddSemanticKeywordsToText(sb, gameInput);

            return sb.ToString();
        }

        private static void AddSemanticKeywordsToText(StringBuilder sb, GameEmbeddingInput gameInput)
        {
            // Add semantic keywords if they were extracted by the GameIndexingService
            // Check if we have any extracted semantic keywords to add
            if (HasAnyExtractedKeywords(gameInput))
            {
                // Add genre-specific descriptors
                if (gameInput.ExtractedGenreKeywords.Count > 0)
                {
                    sb.Append(". Genre Characteristics: ");
                    for (int i = 0; i < Math.Min(5, gameInput.ExtractedGenreKeywords.Count); i++)
                    {
                        if (i > 0) sb.Append(", ");
                        sb.Append(gameInput.ExtractedGenreKeywords[i]);
                    }
                }

                // Add gameplay mechanics
                if (gameInput.ExtractedMechanicKeywords.Count > 0)
                {
                    sb.Append(". Gameplay: ");
                    for (int i = 0; i < Math.Min(5, gameInput.ExtractedMechanicKeywords.Count); i++)
                    {
                        if (i > 0) sb.Append(", ");
                        sb.Append(gameInput.ExtractedMechanicKeywords[i]);
                    }
                }

                // Add thematic elements
                if (gameInput.ExtractedThemeKeywords.Count > 0)
                {
                    sb.Append(". Themes: ");
                    for (int i = 0; i < Math.Min(4, gameInput.ExtractedThemeKeywords.Count); i++)
                    {
                        if (i > 0) sb.Append(", ");
                        sb.Append(gameInput.ExtractedThemeKeywords[i]);
                    }
                }

                // Add mood and atmosphere
                if (gameInput.ExtractedMoodKeywords.Count > 0)
                {
                    sb.Append(". Atmosphere: ");
                    for (int i = 0; i < Math.Min(4, gameInput.ExtractedMoodKeywords.Count); i++)
                    {
                        if (i > 0) sb.Append(", ");
                        sb.Append(gameInput.ExtractedMoodKeywords[i]);
                    }
                }

                // Add art style if available
                if (gameInput.ExtractedArtStyleKeywords.Count > 0)
                {
                    sb.Append(". Art Style: ");
                    for (int i = 0; i < Math.Min(3, gameInput.ExtractedArtStyleKeywords.Count); i++)
                    {
                        if (i > 0) sb.Append(", ");
                        sb.Append(gameInput.ExtractedArtStyleKeywords[i]);
                    }
                }

                // Add target audience information
                if (gameInput.ExtractedAudienceKeywords.Count > 0)
                {
                    sb.Append(". Target Audience: ");
                    for (int i = 0; i < Math.Min(3, gameInput.ExtractedAudienceKeywords.Count); i++)
                    {
                        if (i > 0) sb.Append(", ");
                        sb.Append(gameInput.ExtractedAudienceKeywords[i]);
                    }
                }

                // Add platform-specific keywords
                if (gameInput.ExtractedPlatformTypeKeywords.Count > 0)
                {
                    sb.Append(". Platform Features: ");
                    for (int i = 0; i < Math.Min(3, gameInput.ExtractedPlatformTypeKeywords.Count); i++)
                    {
                        if (i > 0) sb.Append(", ");
                        sb.Append(gameInput.ExtractedPlatformTypeKeywords[i]);
                    }
                }

                if (gameInput.ExtractedEraKeywords.Count > 0)
                {
                    sb.Append(". Era: ");
                    for (int i = 0; i < Math.Min(5, gameInput.ExtractedEraKeywords.Count); i++)
                    {
                        if (i > 0) sb.Append(", ");
                        sb.Append(gameInput.ExtractedEraKeywords[i]);
                    }
                }

                if (gameInput.ExtractedCapabilityKeywords.Count > 0)
                {
                    sb.Append(". Capabilities: ");
                    for (int i = 0; i < Math.Min(3, gameInput.ExtractedCapabilityKeywords.Count); i++)
                    {
                        if (i > 0) sb.Append(", ");
                        sb.Append(gameInput.ExtractedCapabilityKeywords[i]);
                    }
                }

                // Add game mode-specific keywords
                if (gameInput.ExtractedPlayerInteractionKeywords.Count > 0)
                {
                    sb.Append(". Player Interaction: ");
                    for (int i = 0; i < Math.Min(3, gameInput.ExtractedPlayerInteractionKeywords.Count); i++)
                    {
                        if (i > 0) sb.Append(", ");
                        sb.Append(gameInput.ExtractedPlayerInteractionKeywords[i]);
                    }
                }

                if (gameInput.ExtractedScaleKeywords.Count > 0)
                {
                    sb.Append(". Scale: ");
                    for (int i = 0; i < Math.Min(3, gameInput.ExtractedScaleKeywords.Count); i++)
                    {
                        if (i > 0) sb.Append(", ");
                        sb.Append(gameInput.ExtractedScaleKeywords[i]);
                    }
                }

                if (gameInput.ExtractedCommunicationKeywords.Count > 0)
                {
                    sb.Append(". Communication: ");
                    for (int i = 0; i < Math.Min(2, gameInput.ExtractedCommunicationKeywords.Count); i++)
                    {
                        if (i > 0) sb.Append(", ");
                        sb.Append(gameInput.ExtractedCommunicationKeywords[i]);
                    }
                }

                // Add perspective-specific keywords
                if (gameInput.ExtractedViewpointKeywords.Count > 0)
                {
                    sb.Append(". Viewpoint: ");
                    for (int i = 0; i < Math.Min(2, gameInput.ExtractedViewpointKeywords.Count); i++)
                    {
                        if (i > 0) sb.Append(", ");
                        sb.Append(gameInput.ExtractedViewpointKeywords[i]);
                    }
                }

                if (gameInput.ExtractedImmersionKeywords.Count > 0)
                {
                    sb.Append(". Immersion: ");
                    for (int i = 0; i < Math.Min(2, gameInput.ExtractedImmersionKeywords.Count); i++)
                    {
                        if (i > 0) sb.Append(", ");
                        sb.Append(gameInput.ExtractedImmersionKeywords[i]);
                    }
                }

                if (gameInput.ExtractedInterfaceKeywords.Count > 0)
                {
                    sb.Append(". Interface: ");
                    for (int i = 0; i < Math.Min(2, gameInput.ExtractedInterfaceKeywords.Count); i++)
                    {
                        if (i > 0) sb.Append(", ");
                        sb.Append(gameInput.ExtractedInterfaceKeywords[i]);
                    }
                }
            }
        }

        /// <summary>
        /// Checks if the game input has any extracted semantic keywords
        /// </summary>
        private static bool HasAnyExtractedKeywords(GameEmbeddingInput gameInput)
        {
            return gameInput.ExtractedGenreKeywords.Count > 0 ||
                   gameInput.ExtractedMechanicKeywords.Count > 0 ||
                   gameInput.ExtractedThemeKeywords.Count > 0 ||
                   gameInput.ExtractedMoodKeywords.Count > 0 ||
                   gameInput.ExtractedArtStyleKeywords.Count > 0 ||
                   gameInput.ExtractedAudienceKeywords.Count > 0 ||
                   gameInput.ExtractedPlatformTypeKeywords.Count > 0 ||
                   gameInput.ExtractedEraKeywords.Count > 0 ||
                   gameInput.ExtractedCapabilityKeywords.Count > 0 ||
                   gameInput.ExtractedPlayerInteractionKeywords.Count > 0 ||
                   gameInput.ExtractedScaleKeywords.Count > 0 ||
                   gameInput.ExtractedCommunicationKeywords.Count > 0 ||
                   gameInput.ExtractedViewpointKeywords.Count > 0 ||
                   gameInput.ExtractedImmersionKeywords.Count > 0 ||
                   gameInput.ExtractedInterfaceKeywords.Count > 0;
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

        private static float CalculateMagnitude(float[] vector)
        {
            return (float)Math.Sqrt(vector.Sum(x => x * x));
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

        /// <summary>
        /// Resizes an embedding to the target dimension by truncating or padding with zeros
        /// </summary>
        private static float[] ResizeEmbedding(float[] embedding, int targetDimensions)
        {
            if (embedding.Length == targetDimensions)
                return embedding;

            var resized = new float[targetDimensions];
            var copyLength = Math.Min(embedding.Length, targetDimensions);
            Array.Copy(embedding, 0, resized, 0, copyLength);

            // Remaining elements are already initialized to 0 by default
            return resized;
        }

        /// <summary>
        /// Get cache statistics for performance monitoring
        /// </summary>
        public CacheStats GetCacheStats()
        {
            return _cacheManager.GetStats();
        }

        public void Dispose()
        {
            _session?.Dispose();
            _gpuServiceClient?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}