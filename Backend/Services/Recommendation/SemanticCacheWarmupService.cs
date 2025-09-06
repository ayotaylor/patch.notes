using Backend.Services.Recommendation.Interfaces;

namespace Backend.Services.Recommendation
{
    /// <summary>
    /// Background service that warms up the semantic keyword cache after application startup
    /// </summary>
    public class SemanticCacheWarmupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SemanticCacheWarmupService> _logger;

        public SemanticCacheWarmupService(
            IServiceProvider serviceProvider,
            ILogger<SemanticCacheWarmupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Wait a bit for application to fully start and database connections to be ready
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

            _logger.LogInformation("Starting semantic cache warmup...");

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var cache = scope.ServiceProvider.GetRequiredService<ISemanticKeywordCache>();

                if (!cache.IsInitialized)
                {
                    var success = await cache.InitializeCacheAsync();
                    if (success)
                    {
                        _logger.LogInformation("Semantic cache warmup completed successfully");
                    }
                    else
                    {
                        _logger.LogWarning("Semantic cache warmup failed - cache will initialize on first use");
                    }
                }
                else
                {
                    _logger.LogInformation("Semantic cache was already initialized");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during semantic cache warmup - cache will initialize on first use");
            }
        }
    }
}