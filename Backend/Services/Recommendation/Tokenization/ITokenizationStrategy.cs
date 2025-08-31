namespace Backend.Services.Recommendation.Tokenization
{
    /// <summary>
    /// Strategy interface for different tokenization approaches
    /// </summary>
    public interface ITokenizationStrategy
    {
        /// <summary>
        /// Tokenize text and return BERT-compatible input arrays
        /// </summary>
        (long[] inputIds, long[] attentionMask) TokenizeForBert(string text, int maxLength);
        
        /// <summary>
        /// Name of the tokenization strategy for logging/debugging
        /// </summary>
        string StrategyName { get; }
        
        /// <summary>
        /// Whether this strategy is available (e.g., required files exist)
        /// </summary>
        bool IsAvailable { get; }
    }

    /// <summary>
    /// Base class with common tokenization utilities
    /// </summary>
    public abstract class TokenizationStrategyBase : ITokenizationStrategy
    {
        protected const int CLS_TOKEN_ID = 101;
        protected const int SEP_TOKEN_ID = 102;
        protected const string UNK_TOKEN = "[UNK]";

        public abstract (long[] inputIds, long[] attentionMask) TokenizeForBert(string text, int maxLength);
        public abstract string StrategyName { get; }
        public abstract bool IsAvailable { get; }

        /// <summary>
        /// Create BERT input arrays from tokenizer output
        /// </summary>
        public static (long[] inputIds, long[] attentionMask) CreateBertInputs(IList<int> tokens, int maxLength)
        {
            var inputIds = new long[maxLength];
            var attentionMask = new long[maxLength];

            // Add [CLS] token at start
            inputIds[0] = CLS_TOKEN_ID;
            attentionMask[0] = 1;

            // Add tokens (reserve space for [SEP])
            var tokenCount = Math.Min(tokens.Count, maxLength - 2);
            for (int i = 0; i < tokenCount; i++)
            {
                inputIds[i + 1] = tokens[i];
                attentionMask[i + 1] = 1;
            }

            // Add [SEP] token at end
            if (tokenCount + 1 < maxLength)
            {
                inputIds[tokenCount + 1] = SEP_TOKEN_ID;
                attentionMask[tokenCount + 1] = 1;
            }

            return (inputIds, attentionMask);
        }

        /// <summary>
        /// Create fallback BERT inputs when tokenizer library is unavailable
        /// </summary>
        public static (long[] inputIds, long[] attentionMask) CreateFallbackInputs(string text, int maxLength)
        {
            var inputIds = new long[maxLength];
            var attentionMask = new long[maxLength];

            // Add [CLS] token
            inputIds[0] = CLS_TOKEN_ID;
            attentionMask[0] = 1;

            // Simple word-based tokenization
            var words = text.ToLowerInvariant()
                .Split(new[] { ' ', '\t', '\n', '\r', '.', ',', '!', '?', ';', ':', '-' },
                       StringSplitOptions.RemoveEmptyEntries)
                .Take(maxLength - 2);

            int position = 1;
            foreach (var word in words)
            {
                if (position >= maxLength - 1) break;

                inputIds[position] = GetStableHash(word);
                attentionMask[position] = 1;
                position++;
            }

            // Add [SEP] token
            if (position < maxLength)
            {
                inputIds[position] = SEP_TOKEN_ID;
                attentionMask[position] = 1;
            }

            return (inputIds, attentionMask);
        }

        /// <summary>
        /// Generate stable hash for consistent tokenization across runs
        /// </summary>
        protected static long GetStableHash(string word)
        {
            var hash = 0;
            foreach (char c in word)
            {
                hash = ((hash << 5) + hash) + c;
            }
            return Math.Abs(hash % 100000) + 1000;
        }

        /// <summary>
        /// Apply BERT-style text normalization
        /// </summary>
        protected static string ApplyBertNormalization(string text, bool cleanText = true, bool lowercase = true, bool handleChineseChars = true)
        {
            if (cleanText)
            {
                // Clean up whitespace
                text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();
            }

            if (lowercase)
            {
                text = text.ToLowerInvariant();
            }

            // Handle Chinese characters if needed (basic implementation)
            if (handleChineseChars)
            {
                // Add spaces around Chinese characters for better tokenization
                text = System.Text.RegularExpressions.Regex.Replace(text, @"([\u4e00-\u9fff])", " $1 ");
                text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();
            }

            return text;
        }

        /// <summary>
        /// BERT-style pre-tokenization: split on whitespace and punctuation
        /// </summary>
        protected static List<string> BertPreTokenize(string text)
        {
            var tokens = new List<string>();
            var currentToken = "";

            foreach (char c in text)
            {
                if (char.IsWhiteSpace(c))
                {
                    if (!string.IsNullOrEmpty(currentToken))
                    {
                        tokens.Add(currentToken);
                        currentToken = "";
                    }
                }
                else if (char.IsPunctuation(c))
                {
                    if (!string.IsNullOrEmpty(currentToken))
                    {
                        tokens.Add(currentToken);
                        currentToken = "";
                    }
                    tokens.Add(c.ToString());
                }
                else
                {
                    currentToken += c;
                }
            }

            if (!string.IsNullOrEmpty(currentToken))
            {
                tokens.Add(currentToken);
            }

            return tokens;
        }
    }
}