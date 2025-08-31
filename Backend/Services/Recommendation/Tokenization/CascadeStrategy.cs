using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Backend.Services.Recommendation.Tokenization
{
    /// <summary>
    /// Cascade tokenization strategy that tries multiple approaches in order
    /// </summary>
    public class CascadeStrategy : TokenizationStrategyBase
    {
        private readonly List<ITokenizationStrategy> _strategies;
        private readonly ILogger _logger;

        public override string StrategyName => "Cascade";
        public override bool IsAvailable => _strategies.Any(s => s.IsAvailable);

        public CascadeStrategy(IConfiguration configuration, ILogger logger)
        {
            _logger = logger;
            _strategies = new List<ITokenizationStrategy>
            {
                new TokenizerJsonStrategy(configuration, logger),
                new VocabTxtStrategy(configuration, logger),
                new FallbackStrategy(logger)
            };
        }

        public override (long[] inputIds, long[] attentionMask) TokenizeForBert(string text, int maxLength)
        {
            foreach (var strategy in _strategies)
            {
                if (strategy.IsAvailable)
                {
                    _logger.LogDebug("Using {StrategyName} tokenization strategy (cascade)", strategy.StrategyName);
                    return strategy.TokenizeForBert(text, maxLength);
                }
            }

            // This should never happen since FallbackStrategy is always available
            _logger.LogError("No tokenization strategy available (this should not happen)");
            return CreateFallbackInputs(text, maxLength);
        }
    }
}