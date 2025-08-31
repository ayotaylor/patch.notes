using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Backend.Services.Recommendation.Tokenization
{
    /// <summary>
    /// Tokenization strategy using vocab.txt file
    /// </summary>
    public class VocabTxtStrategy : TokenizationStrategyBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;
        private static readonly Dictionary<string, int> _vocabCache = new();
        private static readonly object _vocabCacheLock = new();

        public override string StrategyName => "VocabTxt";

        public override bool IsAvailable
        {
            get
            {
                var vocabulary = GetVocabulary();
                return vocabulary != null && vocabulary.Count > 0;
            }
        }

        public VocabTxtStrategy(IConfiguration configuration, ILogger logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public override (long[] inputIds, long[] attentionMask) TokenizeForBert(string text, int maxLength)
        {
            var vocabulary = GetVocabulary();
            if (vocabulary == null)
            {
                _logger.LogWarning("VocabTxt not available, falling back to simple tokenization");
                return CreateFallbackInputs(text, maxLength);
            }

            var tokens = TokenizeWithVocab(text, vocabulary);
            return CreateBertInputs(tokens, maxLength);
        }

        /// <summary>
        /// Get or create the BERT tokenizer using vocab.txt file
        /// </summary>
        private Dictionary<string, int>? GetVocabulary()
        {
            if (_vocabCache.Count > 0)
                return _vocabCache;

            lock (_vocabCacheLock)
            {
                if (_vocabCache.Count > 0)
                    return _vocabCache;

                try
                {
                    var vocabPath = _configuration["EmbeddingModel:VocabularyPath"];

                    if (!string.IsNullOrEmpty(vocabPath) && File.Exists(vocabPath))
                    {
                        var lines = File.ReadAllLines(vocabPath);
                        for (int i = 0; i < lines.Length; i++)
                        {
                            var token = lines[i].Trim();
                            if (!string.IsNullOrEmpty(token))
                            {
                                _vocabCache[token] = i;
                            }
                        }

                        _logger.LogInformation("Loaded BERT vocabulary with {TokenCount} tokens from {Path}",
                            _vocabCache.Count, vocabPath);
                        return _vocabCache;
                    }

                    _logger.LogWarning("Vocabulary file not found at {Path}", vocabPath);
                    return null;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load BERT vocabulary");
                    return null;
                }
            }
        }

        /// <summary>
        /// Tokenize text using the loaded BERT vocabulary with basic subword tokenization
        /// </summary>
        private List<int> TokenizeWithVocab(string text, Dictionary<string, int> vocabulary)
        {
            var tokens = new List<int>();

            // Convert to lowercase for case-insensitive matching (BERT is typically uncased)
            text = text.ToLowerInvariant();

            // Split into words and handle basic punctuation
            var words = System.Text.RegularExpressions.Regex.Split(text, @"(\s+|[^\w\s])")
                .Where(w => !string.IsNullOrWhiteSpace(w))
                .ToList();

            foreach (var word in words)
            {
                if (string.IsNullOrWhiteSpace(word)) continue;

                // Try exact match first
                if (vocabulary.TryGetValue(word, out var tokenId))
                {
                    tokens.Add(tokenId);
                }
                // Try with ## prefix for subwords (BERT WordPiece tokenization)
                else if (vocabulary.TryGetValue($"###{word}", out var subwordId))
                {
                    tokens.Add(subwordId);
                }
                // Basic subword splitting - try to find partial matches
                else
                {
                    var subwordTokens = TokenizeSubword(word, vocabulary);
                    tokens.AddRange(subwordTokens);
                }
            }

            return tokens;
        }

        /// <summary>
        /// Basic subword tokenization for unknown words
        /// </summary>
        private static List<int> TokenizeSubword(string word, Dictionary<string, int> vocabulary)
        {
            var tokens = new List<int>();

            // Try progressively smaller substrings
            int start = 0;
            while (start < word.Length)
            {
                bool found = false;

                // Try longest possible match first
                for (int end = word.Length; end > start; end--)
                {
                    var substring = word[start..end];
                    var tokenKey = start == 0 ? substring : $"###{substring}";

                    if (vocabulary.TryGetValue(tokenKey, out var tokenId))
                    {
                        tokens.Add(tokenId);
                        start = end;
                        found = true;
                        break;
                    }
                }

                // If no match found, use [UNK] token and move forward
                if (!found)
                {
                    if (vocabulary.TryGetValue(UNK_TOKEN, out var unkToken))
                    {
                        tokens.Add(unkToken);
                    }
                    start++;
                }
            }

            return tokens;
        }
    }
}