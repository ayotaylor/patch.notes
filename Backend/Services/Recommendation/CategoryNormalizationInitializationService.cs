namespace Backend.Services.Recommendation
{
    /// <summary>
    /// Background service that initializes the CategoryNormalizationService cache at application startup
    /// </summary>
    public class CategoryNormalizationInitializationService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CategoryNormalizationInitializationService> _logger;

        public CategoryNormalizationInitializationService(
            IServiceProvider serviceProvider,
            ILogger<CategoryNormalizationInitializationService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting category normalization cache initialization...");

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var normalizationService = scope.ServiceProvider.GetRequiredService<CategoryNormalizationService>();
                
                await normalizationService.InitializeCacheAsync();
                
                _logger.LogInformation("Category normalization cache initialization completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize category normalization cache. The service will fall back to database queries.");
                // Don't throw - allow the application to start even if cache initialization fails
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Category normalization initialization service stopping");
            return Task.CompletedTask;
        }
    }
}