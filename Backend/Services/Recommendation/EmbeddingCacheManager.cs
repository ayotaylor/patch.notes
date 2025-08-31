using System.Collections.Concurrent;

namespace Backend.Services.Recommendation
{
    /// <summary>
    /// Manages caching of embeddings and tokens with LRU-style eviction and thread safety
    /// </summary>
    public class EmbeddingCacheManager
    {
        private readonly ConcurrentDictionary<string, float[]> _embeddingCache = new();
        private readonly ConcurrentDictionary<string, List<int>> _tokenCache = new();
        private readonly ConcurrentDictionary<string, DateTime> _embeddingAccessTimes = new();
        private readonly ConcurrentDictionary<string, DateTime> _tokenAccessTimes = new();
        private readonly object _cleanupLock = new();
        
        private readonly int _maxEmbeddingCacheSize;
        private readonly int _maxTokenCacheSize;
        private readonly TimeSpan _cacheExpiry;
        
        public EmbeddingCacheManager(
            int maxEmbeddingCacheSize = 10000,
            int maxTokenCacheSize = 50000,
            TimeSpan? cacheExpiry = null)
        {
            _maxEmbeddingCacheSize = maxEmbeddingCacheSize;
            _maxTokenCacheSize = maxTokenCacheSize;
            _cacheExpiry = cacheExpiry ?? TimeSpan.FromHours(2);
        }

        /// <summary>
        /// Try to get a cached embedding
        /// </summary>
        public bool TryGetEmbedding(string key, out float[] embedding)
        {
            if (_embeddingCache.TryGetValue(key, out embedding!))
            {
                _embeddingAccessTimes.TryAdd(key, DateTime.UtcNow);
                return true;
            }
            
            embedding = Array.Empty<float>();
            return false;
        }

        /// <summary>
        /// Cache an embedding with automatic cleanup if needed
        /// </summary>
        public void CacheEmbedding(string key, float[] embedding)
        {
            if (string.IsNullOrEmpty(key) || embedding == null || embedding.Length == 0)
                return;

            // Check if we need cleanup
            if (_embeddingCache.Count >= _maxEmbeddingCacheSize)
            {
                CleanupEmbeddingCache();
            }

            _embeddingCache.TryAdd(key, embedding);
            _embeddingAccessTimes.TryAdd(key, DateTime.UtcNow);
        }

        /// <summary>
        /// Try to get cached tokens
        /// </summary>
        public bool TryGetTokens(string key, out List<int> tokens)
        {
            if (_tokenCache.TryGetValue(key, out tokens!))
            {
                _tokenAccessTimes.TryAdd(key, DateTime.UtcNow);
                return true;
            }
            
            tokens = new List<int>();
            return false;
        }

        /// <summary>
        /// Cache tokens with automatic cleanup if needed
        /// </summary>
        public void CacheTokens(string key, List<int> tokens)
        {
            if (string.IsNullOrEmpty(key) || tokens == null || tokens.Count == 0)
                return;

            // Check if we need cleanup
            if (_tokenCache.Count >= _maxTokenCacheSize)
            {
                CleanupTokenCache();
            }

            _tokenCache.TryAdd(key, new List<int>(tokens)); // Defensive copy
            _tokenAccessTimes.TryAdd(key, DateTime.UtcNow);
        }

        /// <summary>
        /// Clear all caches
        /// </summary>
        public void Clear()
        {
            _embeddingCache.Clear();
            _tokenCache.Clear();
            _embeddingAccessTimes.Clear();
            _tokenAccessTimes.Clear();
        }

        /// <summary>
        /// Get cache statistics
        /// </summary>
        public CacheStats GetStats()
        {
            return new CacheStats
            {
                EmbeddingCacheSize = _embeddingCache.Count,
                TokenCacheSize = _tokenCache.Count,
                MaxEmbeddingCacheSize = _maxEmbeddingCacheSize,
                MaxTokenCacheSize = _maxTokenCacheSize,
                EmbeddingHitRate = CalculateHitRate(_embeddingCache.Count, _maxEmbeddingCacheSize),
                TokenHitRate = CalculateHitRate(_tokenCache.Count, _maxTokenCacheSize)
            };
        }

        private void CleanupEmbeddingCache()
        {
            lock (_cleanupLock)
            {
                if (_embeddingCache.Count < _maxEmbeddingCacheSize) return;

                var now = DateTime.UtcNow;
                var itemsToRemove = new List<string>();

                // Remove expired items first
                foreach (var kvp in _embeddingAccessTimes)
                {
                    if (now - kvp.Value > _cacheExpiry)
                    {
                        itemsToRemove.Add(kvp.Key);
                    }
                }

                // If still over limit, remove least recently used items
                if (_embeddingCache.Count - itemsToRemove.Count >= _maxEmbeddingCacheSize)
                {
                    var lruItems = _embeddingAccessTimes
                        .Where(kvp => !itemsToRemove.Contains(kvp.Key))
                        .OrderBy(kvp => kvp.Value)
                        .Take(_embeddingCache.Count - _maxEmbeddingCacheSize + itemsToRemove.Count + 1000) // Remove extra for efficiency
                        .Select(kvp => kvp.Key);

                    itemsToRemove.AddRange(lruItems);
                }

                // Remove items
                foreach (var key in itemsToRemove)
                {
                    _embeddingCache.TryRemove(key, out _);
                    _embeddingAccessTimes.TryRemove(key, out _);
                }
            }
        }

        private void CleanupTokenCache()
        {
            lock (_cleanupLock)
            {
                if (_tokenCache.Count < _maxTokenCacheSize) return;

                var now = DateTime.UtcNow;
                var itemsToRemove = new List<string>();

                // Remove expired items first
                foreach (var kvp in _tokenAccessTimes)
                {
                    if (now - kvp.Value > _cacheExpiry)
                    {
                        itemsToRemove.Add(kvp.Key);
                    }
                }

                // If still over limit, remove least recently used items
                if (_tokenCache.Count - itemsToRemove.Count >= _maxTokenCacheSize)
                {
                    var lruItems = _tokenAccessTimes
                        .Where(kvp => !itemsToRemove.Contains(kvp.Key))
                        .OrderBy(kvp => kvp.Value)
                        .Take(_tokenCache.Count - _maxTokenCacheSize + itemsToRemove.Count + 5000) // Remove extra for efficiency
                        .Select(kvp => kvp.Key);

                    itemsToRemove.AddRange(lruItems);
                }

                // Remove items
                foreach (var key in itemsToRemove)
                {
                    _tokenCache.TryRemove(key, out _);
                    _tokenAccessTimes.TryRemove(key, out _);
                }
            }
        }

        private static float CalculateHitRate(int currentSize, int maxSize)
        {
            if (maxSize == 0) return 0f;
            return Math.Min(1f, (float)currentSize / maxSize);
        }
    }

    public class CacheStats
    {
        public int EmbeddingCacheSize { get; set; }
        public int TokenCacheSize { get; set; }
        public int MaxEmbeddingCacheSize { get; set; }
        public int MaxTokenCacheSize { get; set; }
        public float EmbeddingHitRate { get; set; }
        public float TokenHitRate { get; set; }
    }
}