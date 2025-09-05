using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Backend.Services.Recommendation.Tokenization
{
    /// <summary>
    /// Tokenization strategy using tokenizer.json configuration file
    /// </summary>
    public class TokenizerJsonStrategy(IConfiguration configuration, ILogger logger) : TokenizationStrategyBase
    {
        private readonly IConfiguration _configuration = configuration;
        private readonly ILogger _logger = logger;
        private static TokenizerConfig? _tokenizerConfig;
        private static readonly object _configLock = new();

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public override string StrategyName => "TokenizerJson";

        public override bool IsAvailable
        {
            get
            {
                var config = GetTokenizerConfig();
                return config?.Model?.Vocab != null && config.Model.Vocab.Count > 0;
            }
        }

        public override (long[] inputIds, long[] attentionMask) TokenizeForBert(string text, int maxLength)
        {
            var config = GetTokenizerConfig();
            if (config?.Model == null)
            {
                _logger.LogWarning("TokenizerJson config not available, falling back to simple tokenization");
                return CreateFallbackInputs(text, maxLength);
            }

            var tokens = TokenizeWithTokenizerJson(text, config);
            return CreateBertInputs(tokens, maxLength);
        }

        private TokenizerConfig? GetTokenizerConfig()
        {
            if (_tokenizerConfig != null)
                return _tokenizerConfig;

            lock (_configLock)
            {
                if (_tokenizerConfig != null)
                    return _tokenizerConfig;

                try
                {
                    var tokenizerPath = _configuration["EmbeddingModel:TokenizerPath"];
                    string? tokenizerJsonPath = null;

                    if (!string.IsNullOrEmpty(tokenizerPath))
                    {
                        var directory = Path.GetDirectoryName(tokenizerPath);
                        if (!string.IsNullOrEmpty(directory))
                        {
                            tokenizerJsonPath = Path.Combine(directory, "tokenizer.json");
                        }
                    }

                    if (!string.IsNullOrEmpty(tokenizerJsonPath) && File.Exists(tokenizerJsonPath))
                    {
                        var jsonContent = File.ReadAllText(tokenizerJsonPath);
                        var config = JsonSerializer.Deserialize<TokenizerConfig>(jsonContent, JsonOptions);

                        if (config?.Model?.Vocab != null && config.Model.Vocab.Count > 0)
                        {
                            _tokenizerConfig = config;
                            _logger.LogInformation("Loaded tokenizer.json with {TokenCount} tokens from {Path}",
                                config.Model.Vocab.Count, tokenizerJsonPath);
                            return _tokenizerConfig;
                        }
                    }

                    _logger.LogDebug("tokenizer.json not found or invalid");
                    return null;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load tokenizer.json");
                    return null;
                }
            }
        }

        /// <summary>
        /// Advanced WordPiece tokenization using tokenizer.json configuration
        /// </summary>
        private static List<int> TokenizeWithTokenizerJson(string text, TokenizerConfig config)
        {
            var tokens = new List<int>();

            // Apply BERT normalization
            var normalizedText = ApplyBertNormalization(text,
                config.Normalizer.CleanText,
                config.Normalizer.Lowercase,
                config.Normalizer.HandleChineseChars);

            // Pre-tokenization: split on whitespace and punctuation (BERT style)
            var preTokens = BertPreTokenize(normalizedText);

            foreach (var token in preTokens)
            {
                if (string.IsNullOrWhiteSpace(token)) continue;

                // Apply WordPiece algorithm
                var wordPieceTokens = WordPieceTokenize(token, config.Model);
                tokens.AddRange(wordPieceTokens);
            }

            return tokens;
        }

        /// <summary>
        /// WordPiece tokenization algorithm (proper implementation)
        /// </summary>
        private static List<int> WordPieceTokenize(string word, TokenizerModel model)
        {
            var tokens = new List<int>();

            if (word.Length > model.MaxInputCharsPerWord)
            {
                // Word too long, use UNK token
                if (model.Vocab.TryGetValue(model.UnkToken, out var unkId))
                {
                    tokens.Add(unkId);
                }
                return tokens;
            }

            // Try to find the word in vocabulary first
            if (model.Vocab.TryGetValue(word, out var wholeWordId))
            {
                tokens.Add(wholeWordId);
                return tokens;
            }

            // WordPiece algorithm: greedy longest-match first
            int start = 0;
            while (start < word.Length)
            {
                int end = word.Length;
                bool found = false;

                // Try progressively shorter substrings
                while (start < end)
                {
                    string substr = word.Substring(start, end - start);

                    // Add ## prefix for continuation tokens (except at word start)
                    if (start > 0)
                    {
                        substr = model.ContinuingSubwordPrefix + substr;
                    }

                    if (model.Vocab.TryGetValue(substr, out var tokenId))
                    {
                        tokens.Add(tokenId);
                        start = end;
                        found = true;
                        break;
                    }

                    end--;
                }

                // If no match found, use UNK and move forward
                if (!found)
                {
                    if (model.Vocab.TryGetValue(model.UnkToken, out var unkId))
                    {
                        tokens.Add(unkId);
                    }
                    start++;
                }
            }

            return tokens;
        }
    }

    // Configuration classes for parsing tokenizer.json
    public class TokenizerConfig
    {
        public string Version { get; set; } = "";
        public TokenizerModel Model { get; set; } = new();
        public TokenizerNormalizer Normalizer { get; set; } = new();
        public List<AddedToken> AddedTokens { get; set; } = new();
    }

    public class TokenizerModel
    {
        public string Type { get; set; } = "";
        public Dictionary<string, int> Vocab { get; set; } = new();
        public string UnkToken { get; set; } = "[UNK]";
        public string ContinuingSubwordPrefix { get; set; } = "##";
        public int MaxInputCharsPerWord { get; set; } = 100;
    }

    public class TokenizerNormalizer
    {
        public string Type { get; set; } = "";
        public bool CleanText { get; set; } = true;
        public bool HandleChineseChars { get; set; } = true;
        public bool Lowercase { get; set; } = true;
    }

    public class AddedToken
    {
        public int Id { get; set; }
        public string Content { get; set; } = "";
        public bool Special { get; set; }
    }
}