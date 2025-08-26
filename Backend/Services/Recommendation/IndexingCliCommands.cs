// Usage with a CLI tool or console application
// dotnet run -- reindex-games
// dotnet run -- reindex-games --batch-size 100 --force

using Backend.Data;
using Backend.Services.Recommendation;
using Microsoft.EntityFrameworkCore;

namespace Backend.CLI
{
    public class IndexingCliCommands
    {
        private readonly GameIndexingService _indexingService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<IndexingCliCommands> _logger;

        public IndexingCliCommands(
            GameIndexingService indexingService,
            ApplicationDbContext context,
            ILogger<IndexingCliCommands> logger)
        {
            _indexingService = indexingService;
            _context = context;
            _logger = logger;
        }

        public async Task<int> ReindexGamesCommand(string[] args)
        {
            try
            {
                var batchSize = GetArgValue(args, "--batch-size", "50");
                var force = HasArg(args, "--force");
                var dryRun = HasArg(args, "--dry-run");

                _logger.LogInformation("Starting game reindexing...");
                _logger.LogInformation("Batch Size: {BatchSize}", batchSize);
                _logger.LogInformation("Force: {Force}", force);
                _logger.LogInformation("Dry Run: {DryRun}", dryRun);

                if (dryRun)
                {
                    var gameCount = await _context.Games.CountAsync();
                    _logger.LogInformation("Would index {Count} games", gameCount);
                    return 0;
                }

                // Initialize collection
                var initialized = await _indexingService.InitializeCollectionAsync();
                if (!initialized)
                {
                    _logger.LogError("Failed to initialize vector database collection");
                    return 1;
                }

                // Check if collection already has data
                if (!force)
                {
                    // Add method to check if collection has data
                    _logger.LogWarning("Collection may already contain data. Use --force to reindex anyway");
                }

                // Perform indexing
                var indexedCount = await _indexingService.IndexAllGamesAsync();

                _logger.LogInformation("Successfully indexed {Count} games", indexedCount);
                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during game reindexing");
                return 1;
            }
        }

        public async Task<int> ValidateIndexCommand(string[] args)
        {
            try
            {
                _logger.LogInformation("Validating game index...");

                var dbGameCount = await _context.Games.CountAsync();
                // Add method to get vector count from collection
                // var vectorCount = await _indexingService.GetIndexedGameCountAsync();

                _logger.LogInformation("Games in database: {DbCount}", dbGameCount);
                // _logger.LogInformation("Games in vector index: {VectorCount}", vectorCount);

                // Add validation logic here
                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during index validation");
                return 1;
            }
        }

        private string GetArgValue(string[] args, string argName, string defaultValue)
        {
            var index = Array.IndexOf(args, argName);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : defaultValue;
        }

        private bool HasArg(string[] args, string argName)
        {
            return args.Contains(argName);
        }
    }
}