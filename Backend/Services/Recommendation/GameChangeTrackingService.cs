using Backend.Data;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services.Recommendation
{
    public class GameChangeTrackingService
    {
        private readonly ApplicationDbContext _context;
        private readonly GameIndexingService _indexingService;
        private readonly ILogger<GameChangeTrackingService> _logger;

        public GameChangeTrackingService(
            ApplicationDbContext context,
            GameIndexingService indexingService,
            ILogger<GameChangeTrackingService> logger)
        {
            _context = context;
            _indexingService = indexingService;
            _logger = logger;
        }

        public async Task ProcessGameChangesAsync()
        {
            try
            {
                // Track changes since last run
                var changeTracker = _context.ChangeTracker;
                
                var addedGames = changeTracker.Entries<Backend.Models.Game.Game>()
                    .Where(e => e.State == EntityState.Added)
                    .Select(e => e.Entity)
                    .ToList();

                var modifiedGames = changeTracker.Entries<Backend.Models.Game.Game>()
                    .Where(e => e.State == EntityState.Modified)
                    .Select(e => e.Entity)
                    .ToList();

                var deletedGameIds = changeTracker.Entries<Backend.Models.Game.Game>()
                    .Where(e => e.State == EntityState.Deleted)
                    .Select(e => e.Entity.Id)
                    .ToList();

                // Process additions and modifications
                foreach (var game in addedGames.Concat(modifiedGames))
                {
                    await _indexingService.IndexGameAsync(game);
                    _logger.LogDebug("Updated index for game: {GameName}", game.Name);
                }

                // Process deletions
                foreach (var gameId in deletedGameIds)
                {
                    await _indexingService.RemoveGameFromIndexAsync(gameId);
                    _logger.LogDebug("Removed game from index: {GameId}", gameId);
                }

                if (addedGames.Any() || modifiedGames.Any() || deletedGameIds.Any())
                {
                    _logger.LogInformation("Processed {Added} additions, {Modified} modifications, {Deleted} deletions",
                        addedGames.Count, modifiedGames.Count, deletedGameIds.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing game changes");
            }
        }

        // Call this method in your game service operations
        public async Task HandleGameUpdatedAsync(Guid gameId)
        {
            try
            {
                var success = await _indexingService.UpdateGameIndexAsync(gameId);
                if (success)
                {
                    _logger.LogInformation("Successfully updated index for game: {GameId}", gameId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update index for game: {GameId}", gameId);
            }
        }

        public async Task HandleGameDeletedAsync(Guid gameId)
        {
            try
            {
                var success = await _indexingService.RemoveGameFromIndexAsync(gameId);
                if (success)
                {
                    _logger.LogInformation("Successfully removed game from index: {GameId}", gameId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove game from index: {GameId}", gameId);
            }
        }
    }
}