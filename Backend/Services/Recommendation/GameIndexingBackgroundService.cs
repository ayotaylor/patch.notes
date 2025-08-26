using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Backend.Services.Recommendation
{
    public class GameIndexingBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<GameIndexingBackgroundService> _logger;
        private readonly IConfiguration _configuration;

        public GameIndexingBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<GameIndexingBackgroundService> logger,
            IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Wait for application to fully start
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var indexingService = scope.ServiceProvider.GetRequiredService<GameIndexingService>();

                    // Check if we need to initialize or reindex
                    var shouldReindex = _configuration.GetValue<bool>("RecommendationEngine:AutoReindex", false);
                    var reindexInterval = _configuration.GetValue<int>("RecommendationEngine:ReindexIntervalHours", 24);

                    if (shouldReindex)
                    {
                        _logger.LogInformation("Starting scheduled reindexing of games");

                        var initialized = await indexingService.InitializeCollectionAsync();
                        if (initialized)
                        {
                            var count = await indexingService.IndexAllGamesAsync();
                            _logger.LogInformation("Scheduled reindex completed: {Count} games", count);
                        }
                    }

                    // Wait for next scheduled run
                    await Task.Delay(TimeSpan.FromHours(reindexInterval), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during scheduled game indexing");
                    // Wait 1 hour before retrying on error
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
            }
        }
    }
}