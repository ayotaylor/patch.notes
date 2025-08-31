using Microsoft.Extensions.Logging;

namespace Backend.Services.Recommendation.Tokenization
{
    /// <summary>
    /// Fallback tokenization strategy when no proper tokenizer files are available
    /// </summary>
    public class FallbackStrategy : TokenizationStrategyBase
    {
        private readonly ILogger _logger;

        public override string StrategyName => "Fallback";
        public override bool IsAvailable => true; // Always available as fallback

        public FallbackStrategy(ILogger logger)
        {
            _logger = logger;
        }

        public override (long[] inputIds, long[] attentionMask) TokenizeForBert(string text, int maxLength)
        {
            _logger.LogDebug("Using fallback tokenization strategy");
            return CreateFallbackInputs(text, maxLength);
        }
    }
}