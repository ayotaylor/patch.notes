using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Backend.Services.Recommendation.Tokenization
{
    /// <summary>
    /// Factory for creating tokenization strategies based on configuration
    /// </summary>
    public static class TokenizationStrategyFactory
    {
        public static ITokenizationStrategy CreateStrategy(IConfiguration configuration, ILogger logger)
        {
            var strategy = configuration["EmbeddingModel:TokenizerStrategy"] ?? "cascade";

            return strategy.ToLowerInvariant() switch
            {
                "tokenizer.json" or "json" => new TokenizerJsonStrategy(configuration, logger),
                "vocab.txt" or "vocab" => new VocabTxtStrategy(configuration, logger),
                "fallback" => new FallbackStrategy(logger),
                "cascade" or _ => new CascadeStrategy(configuration, logger)
            };
        }
    }
}