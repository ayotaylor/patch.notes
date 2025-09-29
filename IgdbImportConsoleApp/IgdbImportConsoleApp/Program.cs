using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Backend.Config;
using Backend.Data;
using Backend.Models.Game;
using Backend.Models.Game.Associations;
using Backend.Models.Game.ReferenceModels;
using Backend.Models.Game.Relationships;
using Game.Models.ReferenceModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;

namespace IgdbImportConsoleApp
{
    public class OptimizedIgdbImportService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IServiceProvider _serviceProvider;
        private readonly IgdbSettings _igdbSettings;
        private readonly ILogger<OptimizedIgdbImportService> _logger;
        private readonly SemaphoreSlim _rateLimitSemaphore;

        // Batch-level caching for reference data
        private readonly Dictionary<int, Guid> _gameTypeCache = new();
        private readonly Dictionary<int, Guid> _genreCache = new();
        private readonly Dictionary<int, Guid> _companyCache = new();
        private readonly Dictionary<int, Guid> _platformCache = new();
        private readonly Dictionary<int, Guid> _franchiseCache = new();
        private readonly Dictionary<int, Guid> _gameModeCache = new();
        private readonly Dictionary<int, Guid> _playerPerspectiveCache = new();
        private readonly Dictionary<int, Guid> _themeCache = new();
        private readonly Dictionary<int, Guid> _regionCache = new();
        private readonly Dictionary<int, Guid> _ageRatingCache = new();

        // Duplicate checking optimization
        private readonly HashSet<int> _existingGameIds = new();
        private readonly HashSet<int> _existingAltNameIds = new();
        private readonly HashSet<int> _existingCoverIds = new();
        private readonly HashSet<int> _existingScreenshotIds = new();
        private readonly HashSet<int> _existingReleaseDateIds = new();

        // Game ID mapping cache (IgdbId -> DatabaseId)
        private readonly Dictionary<int, Guid> _gameIdCache = new();

        // Relationship caches to prevent duplicates
        private readonly HashSet<string> _existingRelationships = new();

        // Connection pooling optimization
        private ApplicationDbContext? _reusableContext;
        private readonly object _contextLock = new();

        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString, // Add this
            AllowTrailingCommas = true, // Add this
            ReadCommentHandling = JsonCommentHandling.Skip // Add this
        };

        public OptimizedIgdbImportService(
            IHttpClientFactory httpClientFactory,
            IServiceProvider serviceProvider,
            IOptions<IgdbSettings> igdbSettings,
            ILogger<OptimizedIgdbImportService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _serviceProvider = serviceProvider;
            _igdbSettings = igdbSettings.Value;
            _logger = logger;

            // Allow up to 4 concurrent requests (IGDB rate limit)
            _rateLimitSemaphore = new SemaphoreSlim(4, 4);
        }

        public async Task ImportAllGamesOptimizedAsync()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            _logger.LogInformation("Starting optimized IGDB import");

            try
            {
                // Step 1: Get total count and plan batches
                var totalGames = await GetTotalGameCountAsync();
                var totalBatches = (int)Math.Ceiling((double)totalGames / _igdbSettings.BatchSize);

                _logger.LogInformation("Planning to import {TotalGames} games in {TotalBatches} batches",
                    totalGames, totalBatches);

                // Step 2: PRE-POPULATE ALL REFERENCE DATA SEQUENTIALLY
                //await PrePopulateAllReferenceDataAsync(totalBatches);
                await PrePopulateAllReferenceDataAsync();  

                // Step 2: Process games in parallel with controlled concurrency
                var batchTasks = new List<Task>();
                var maxConcurrentBatches = 4; // Adjust based on your database capacity
                var semaphore = new SemaphoreSlim(1, maxConcurrentBatches);

                for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
                {
                    var offset = batchIndex * _igdbSettings.BatchSize;

                    var batchTask = ProcessBatchWithSemaphoreAsync(semaphore, offset, batchIndex, totalBatches);
                    batchTasks.Add(batchTask);
                }

                await Task.WhenAll(batchTasks);

                // Step 3: Process relationships after all games are imported
                //await ProcessAllRelationshipsAsync();
            }
            finally
            {
                stopwatch.Stop();
                _logger.LogInformation("IGDB import completed in {ElapsedTime}", stopwatch.Elapsed);
            }
        }

        private async Task PrePopulateAllReferenceDataAsync()
        {
            _logger.LogInformation("Pre-populating reference data for all batches...");

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            await ProcessGameTypes(context);
            await ProcessGenres(context);
            await ProcessCompanies(context);
            await ProcessPlatforms(context);
            await ProcessFranchises(context);
            await ProcessGameModes(context);
            await ProcessPlayerPerspectives(context);
            await ProcessThemes(context);
            await ProcessRegions(context);

            //Save independent entities first
            await context.SaveChangesAsync();

            // Now process dependent entities in order
            await ProcessRatingOrganizations(context);
            //await context.SaveChangesAsync(); // Save RatingOrganizations before AgeRatingCategories

            await ProcessAgeRatingCategories(context);
            //await context.SaveChangesAsync(); // Save AgeRatingCategories before AgeRatings

            await ProcessAgeRatings(context);
            //await context.SaveChangesAsync(); // Final save for AgeRatings

            _logger.LogInformation("✓ Reference data pre-population completed");
        }

        private async Task ProcessGameTypes(ApplicationDbContext context)
        {
            var endpoint = "game_types";
            var total = await GetTotalCountAsync($"{endpoint}/count");
            var totalBatches = (int)Math.Ceiling((double)total / _igdbSettings.BatchSize);

            for (int i = 0; i < totalBatches; i++)
            {
                var offset = i * _igdbSettings.BatchSize;
                var result = await FetchBatchFromGameSourceAsync<IgdbGameType>(ApiQueries.IGDB_GAME_TYPES,
                    endpoint, offset, _igdbSettings.BatchSize);
                if (result.Count > 0)
                {
                    await BulkUpsertGameTypes(context, result);
                    _logger.LogInformation("Pre-populated reference data for {endpoint} -> {CurrentBatch} of {TotalBatches} batches",
                        endpoint, i+1, totalBatches);
                }
            }
        }

        // private async Task ProcessReferenceType<T>(ApplicationDbContext context,
        //     Func<ApplicationDbContext, List<T>, Task> func, string endpoint, string query)
        // {
        //     //var endpoint = "genres";
        //     var total = await GetTotalCountAsync($"{endpoint}/count");
        //     var totalBatches = (int)Math.Ceiling((double)total / _igdbSettings.BatchSize);
        //     var result = new List<IgdbGenre>();

        //     for (int i = 0; i < totalBatches; i++)
        //     {
        //         var offset = i * _igdbSettings.BatchSize;
        //         result = await FetchBatchFromGameSourceAsync<IgdbGenre>(query,
        //             endpoint, offset, _igdbSettings.BatchSize);
        //     }

        //     if (result.Count > 0)
        //     {
        //         await BulkUpsertGenres(context, result);
        //         await func(context, result);
        //         _logger.LogInformation("Pre-populated reference data for {endpoint} in {TotalBatches} batches",
        //             endpoint, totalBatches);
        //     }
        // }

        private async Task ProcessGenres(ApplicationDbContext context)
        {
            var endpoint = "genres";
            var total = await GetTotalCountAsync($"{endpoint}/count");
            var totalBatches = (int)Math.Ceiling((double)total / _igdbSettings.BatchSize);

            for (int i = 0; i < totalBatches; i++)
            {
                var offset = i * _igdbSettings.BatchSize;
                var result = await FetchBatchFromGameSourceAsync<IgdbGenre>(ApiQueries.IGDB_GENRES,
                    endpoint, offset, _igdbSettings.BatchSize);
                if (result.Count > 0)
                {
                    await BulkUpsertGenres(context, result);
                    _logger.LogInformation("Pre-populated reference data for {endpoint} -> {CurrentBatch} of {TotalBatches} batches",
                        endpoint, i+1, totalBatches);
                }
            }
        }

        private async Task ProcessCompanies(ApplicationDbContext context)
        {
            var endpoint = "involved_companies";
            var total = await GetTotalCountAsync($"{endpoint}/count");
            var totalBatches = (int)Math.Ceiling((double)total / _igdbSettings.BatchSize);

            for (int i = 0; i < totalBatches; i++)
            {
                var offset = i * _igdbSettings.BatchSize;
                var result = await FetchBatchFromGameSourceAsync<IgdbInvolvedCompany>(ApiQueries.IGDB_INVOLVED_COMPANIES,
                    endpoint, offset, _igdbSettings.BatchSize);
                if (result.Count > 0)
                {
                    await BulkUpsertCompanies(context, result);
                    _logger.LogInformation("Pre-populated reference data for {endpoint} -> {CurrentBatch} of {TotalBatches} batches",
                        endpoint, i+1, totalBatches);
                }
            }
        }

        private async Task ProcessPlatforms(ApplicationDbContext context)
        {
            var endpoint = "platforms";
            var total = await GetTotalCountAsync($"{endpoint}/count");
            var totalBatches = (int)Math.Ceiling((double)total / _igdbSettings.BatchSize);

            for (int i = 0; i < totalBatches; i++)
            {
                var offset = i * _igdbSettings.BatchSize;
                var result = await FetchBatchFromGameSourceAsync<IgdbPlatform>(ApiQueries.IGDB_PLATFORMS,
                    endpoint, offset, _igdbSettings.BatchSize);
                if (result.Count > 0)
                {
                    await BulkUpsertPlatforms(context, result);
                    _logger.LogInformation("Pre-populated reference data for {endpoint} -> {CurrentBatch} of {TotalBatches} batches",
                        endpoint, i+1, totalBatches);
                }
            }
        }

        private async Task ProcessRatingOrganizations(ApplicationDbContext context)
        {
            var endpoint = "age_rating_organizations";
            var total = await GetTotalCountAsync($"{endpoint}/count");
            var totalBatches = (int)Math.Ceiling((double)total / _igdbSettings.BatchSize);

            for (int i = 0; i < totalBatches; i++)
            {
                var offset = i * _igdbSettings.BatchSize;
                var result = await FetchBatchFromGameSourceAsync<IgdbAgeRatingOrganization>(ApiQueries.IGDB_RATING_ORGANIZATIONS,
                    endpoint, offset, _igdbSettings.BatchSize);
                if (result.Count > 0)
                {
                    await BulkUpsertRatingOrganizations(context, result);
                    _logger.LogInformation("Pre-populated reference data for {endpoint} -> {CurrentBatch} of {TotalBatches} batches",
                        endpoint, i+1, totalBatches);
                }
            }
        }

        private async Task ProcessAgeRatingCategories(ApplicationDbContext context)
        {
            var endpoint = "age_rating_categories";
            var total = await GetTotalCountAsync($"{endpoint}/count");
            var totalBatches = (int)Math.Ceiling((double)total / _igdbSettings.BatchSize);

            for (int i = 0; i < totalBatches; i++)
            {
                var offset = i * _igdbSettings.BatchSize;
                var result = await FetchBatchFromGameSourceAsync<IgdbAgeRatingCategory>(ApiQueries.IGDB_AGE_RATING_CATEGORIES,
                    endpoint, offset, _igdbSettings.BatchSize);
                if (result.Count > 0)
                {
                    await BulkUpsertAgeRatingCategories(context, result);
                    _logger.LogInformation("Pre-populated reference data for {endpoint} -> {CurrentBatch} of {TotalBatches} batches",
                            endpoint, i+1, totalBatches);
                }
            }
        }

        private async Task ProcessAgeRatings(ApplicationDbContext context)
        {
            var endpoint = "age_ratings";
            var total = await GetTotalCountAsync($"{endpoint}/count");
            var totalBatches = (int)Math.Ceiling((double)total / _igdbSettings.BatchSize);
                var result = new List<IgdbAgeRating>();

            for (int i = 0; i < totalBatches; i++)
            {
                var offset = i * _igdbSettings.BatchSize;
                result = await FetchBatchFromGameSourceAsync<IgdbAgeRating>(ApiQueries.IGDB_AGE_RATINGS,
                    endpoint, offset, _igdbSettings.BatchSize);
                if (result.Count > 0)
                {
                    await BulkUpsertAgeRatings(context, result);
                    _logger.LogInformation("Pre-populated reference data for {endpoint} -> {CurrentBatch} of {TotalBatches} batches",
                            endpoint, i+1, totalBatches);
                }
            }
        }

        private async Task ProcessFranchises(ApplicationDbContext context)
        {
            var endpoint = "franchises";
            var total = await GetTotalCountAsync($"{endpoint}/count");
            var totalBatches = (int)Math.Ceiling((double)total / _igdbSettings.BatchSize);

            for (int i = 0; i < totalBatches; i++)
            {
                var offset = i * _igdbSettings.BatchSize;
                var result = await FetchBatchFromGameSourceAsync<IgdbFranchise>(ApiQueries.IGDB_FRANCHISES,
                    endpoint, offset, _igdbSettings.BatchSize);
                if (result.Count > 0)
                {
                    await BulkUpsertFranchises(context, result);
                    _logger.LogInformation("Pre-populated reference data for {endpoint} -> {CurrentBatch} of {TotalBatches} batches",
                        endpoint, i + 1, totalBatches);
                }
            }
        }

        private async Task ProcessGameModes(ApplicationDbContext context)
        {
            var endpoint = "game_modes";
            var total = await GetTotalCountAsync($"{endpoint}/count");
            var totalBatches = (int)Math.Ceiling((double)total / _igdbSettings.BatchSize);

            for (int i = 0; i < totalBatches; i++)
            {
                var offset = i * _igdbSettings.BatchSize;
                var result = await FetchBatchFromGameSourceAsync<IgdbGameMode>(ApiQueries.IGDB_GAME_MODES,
                    endpoint, offset, _igdbSettings.BatchSize);
                if (result.Count > 0)
                {
                    await BulkUpsertGameModes(context, result);
                    _logger.LogInformation("Pre-populated reference data for {endpoint} -> {CurrentBatch} of {TotalBatches} batches",
                        endpoint, i + 1, totalBatches);
                }
            }
        }

        private async Task ProcessPlayerPerspectives(ApplicationDbContext context)
        {
            var endpoint = "player_perspectives";
            var total = await GetTotalCountAsync($"{endpoint}/count");
            var totalBatches = (int)Math.Ceiling((double)total / _igdbSettings.BatchSize);

            for (int i = 0; i < totalBatches; i++)
            {
                var offset = i * _igdbSettings.BatchSize;
                var result = await FetchBatchFromGameSourceAsync<IgdbPlayerPerspective>(ApiQueries.IGDB_PLAYER_PERSPECTIVES,
                    endpoint, offset, _igdbSettings.BatchSize);
                if (result.Count > 0)
                {
                    await BulkUpsertPlayerPerspectives(context, result);
                    _logger.LogInformation("Pre-populated reference data for {endpoint} -> {CurrentBatch} of {TotalBatches} batches",
                        endpoint, i + 1, totalBatches);
                }
            }
        }

        private async Task ProcessThemes(ApplicationDbContext context)
        {
            var endpoint = "themes";
            var total = await GetTotalCountAsync($"{endpoint}/count");
            var totalBatches = (int)Math.Ceiling((double)total / _igdbSettings.BatchSize);

            for (int i = 0; i < totalBatches; i++)
            {
                var offset = i * _igdbSettings.BatchSize;
                var result = await FetchBatchFromGameSourceAsync<IgdbTheme>(ApiQueries.IGDB_THEMES,
                    endpoint, offset, _igdbSettings.BatchSize);
                if (result.Count > 0)
                {
                    await BulkUpsertThemes(context, result);
                    _logger.LogInformation("Pre-populated reference data for {endpoint} -> {CurrentBatch} of {TotalBatches} batches",
                        endpoint, i + 1, totalBatches);
                }
            }
        }

        private async Task ProcessRegions(ApplicationDbContext context)
        {
            var endpoint = "release_date_regions";
            var total = await GetTotalCountAsync($"{endpoint}/count");
            var totalBatches = (int)Math.Ceiling((double)total / _igdbSettings.BatchSize);

            for (int i = 0; i < totalBatches; i++)
            {
                var offset = i * _igdbSettings.BatchSize;
                var result = await FetchBatchFromGameSourceAsync<IgdbRegion>(ApiQueries.IGDB_RELEASE_DATE_REGIONS,
                    endpoint, offset, _igdbSettings.BatchSize);
                if (result.Count > 0)
                {
                    await BulkUpsertRegions(context, result);
                    _logger.LogInformation("Pre-populated reference data for {endpoint} -> {CurrentBatch} of {TotalBatches} batches",
                        endpoint, i + 1, totalBatches);
                }
            }
        }

        // private async Task BulkInsertAllReferenceDataAsync(List<IgdbGame> igdbGames)
        // {
        //     using var scope = _serviceProvider.CreateScope();
        //     var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        //     using var transaction = await context.Database.BeginTransactionAsync();

        //     try
        //     {
        //         // Process all reference data types
        //         await BulkUpsertGameTypes(context, igdbGames);
        //         await BulkUpsertGenres(context, igdbGames);
        //         await BulkUpsertCompanies(context, igdbGames);
        //         await BulkUpsertPlatforms(context, igdbGames);
        //         await BulkUpsertRatingOrganizations(context, igdbGames);
        //         await BulkUpsertAgeRatingCategories(context, igdbGames);
        //         await BulkUpsertAgeRatings(context, igdbGames);
        //         await BulkUpsertFranchises(context, igdbGames);
        //         await BulkUpsertGameModes(context, igdbGames);
        //         await BulkUpsertPlayerPerspectives(context, igdbGames);
        //         await BulkUpsertThemes(context, igdbGames);
        //         await BulkUpsertRegions(context, igdbGames);

        //         await context.SaveChangesAsync();
        //         await transaction.CommitAsync();
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogWarning(ex, "Error during reference data bulk upsert - rolling back");
        //         await transaction.RollbackAsync();
        //         // Don't throw - let the process continue, individual batch processing will handle missing data
        //     }
        // }

        private async Task BulkUpsertGameTypes(ApplicationDbContext context, List<IgdbGameType> igdbGameTypes)
        {
            var uniqueGameTypes = igdbGameTypes
                .GroupBy(gt => gt.Id)
                .Select(g => g.First())
                .ToList();

            if (uniqueGameTypes.Count == 0) return;

            try
            {
                var igdbIds = uniqueGameTypes.Select(g => g.Id).ToList();
                var existingGameTypes = await context.GameTypes
                    .Where(g => igdbIds.Contains(g.IgdbId))
                    .ToDictionaryAsync(g => g.IgdbId);

                var gameTypesToAdd = uniqueGameTypes
                    .Where(g => !existingGameTypes.ContainsKey(g.Id))
                    .Select(g => new GameType
                    {
                        IgdbId = g.Id,
                        Type = g.Type ?? "",
                    }).ToList();

                if (gameTypesToAdd.Count > 0)
                {
                    await context.GameTypes.AddRangeAsync(gameTypesToAdd);
                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error executing bulk upsert for GameTypes");
            }
        }

        private async Task BulkUpsertGenres(ApplicationDbContext context, List<IgdbGenre> igdbGenres)
        {
            var uniqueGenres = igdbGenres
                .GroupBy(gt => gt.Id)
                .Select(g => g.First())
                .ToList();

            if (uniqueGenres.Count == 0) return;

            try
            {
                var igdbIds = uniqueGenres.Select(g => g.Id).ToList();
                var existingGenres = await context.Genres
                    .Where(g => igdbIds.Contains(g.IgdbId))
                    .ToDictionaryAsync(g => g.IgdbId);

                var genresToAdd = uniqueGenres
                    .Where(g => !existingGenres.ContainsKey(g.Id))
                    .Select(g => new Genre
                    {
                        IgdbId = g.Id,
                        Name = g.Name,
                        Slug = g.Slug ?? "",
                    }).ToList();

                if (genresToAdd.Count > 0)
                {
                    await context.Genres.AddRangeAsync(genresToAdd);
                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error executing bulk upsert for Genres");
            }
        }

        private async Task BulkUpsertCompanies(ApplicationDbContext context, List<IgdbInvolvedCompany> igdbInvolvedCompanies)
        {
            // Extract, Group, and De-duplicate the companies from the incoming list
            var uniqueIgdbCompanies = igdbInvolvedCompanies
                .Where(ic => ic.Company != null)
                .Select(ic => ic.Company)
                .GroupBy(c => c.Id)
                .Select(g => g.First())
                .ToList();

            if (uniqueIgdbCompanies.Count == 0) return;

            try
            {
                var igdbIds = uniqueIgdbCompanies.Select(c => c.Id).ToList();
                var existingCompanies = await context.Companies
                    .Where(c => igdbIds.Contains(c.IgdbId))
                    .ToDictionaryAsync(c => c.IgdbId);

                var companiesToAdd = uniqueIgdbCompanies
                    .Where(c => !existingCompanies.ContainsKey(c.Id))
                    .Select(g => new Company
                    {
                        IgdbId = g.Id,
                        Name = g.Name,
                        CountryCode = g.Country,
                        Description = g.Description ?? "",
                        Slug = g.Slug ?? "",
                        Url = g.Url ?? "",
                    }).ToList();

                if (companiesToAdd.Count > 0)
                {
                    await context.Companies.AddRangeAsync(companiesToAdd);
                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error executing bulk upsert for Companies");
            }
        }

        private async Task BulkUpsertPlatforms(ApplicationDbContext context, List<IgdbPlatform> igdbPlatforms)
        {
            var uniquePlatforms = igdbPlatforms
                .GroupBy(p => p.Id)
                .Select(p => p.First())
                .ToList();

            if (uniquePlatforms.Count == 0) return;

            try
            {
                var igdbIds = uniquePlatforms.Select(p => p.Id).ToList();
                var existingPlatforms = await context.Platforms
                    .Where(p => igdbIds.Contains(p.IgdbId))
                    .ToDictionaryAsync(p => p.IgdbId);

                var platformsToAdd = uniquePlatforms
                    .Where(p => !existingPlatforms.ContainsKey(p.Id))
                    .Select(g => new Platform
                    {
                        IgdbId = g.Id,
                        Name = g.Name,
                        Abbreviation = g.Abbreviation,
                        Slug = g.Slug ?? "",
                    }).ToList();

                if (platformsToAdd.Count > 0)
                {
                    await context.Platforms.AddRangeAsync(platformsToAdd);
                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error executing bulk upsert for Platforms");
            }
        }

        private async Task BulkUpsertFranchises(ApplicationDbContext context, List<IgdbFranchise> igdbFranchises)
        {
            var uniqueFranchises = igdbFranchises
                .GroupBy(p => p.Id)
                .Select(p => p.First())
                .ToList();

            if (uniqueFranchises.Count == 0) return;

            try
            {
                var igdbIds = uniqueFranchises.Select(f => f.Id).ToList();
                var existingFranchises = await context.Franchises
                    .Where(f => igdbIds.Contains(f.IgdbId))
                    .ToDictionaryAsync(f => f.IgdbId);

                var franchisesToAdd = uniqueFranchises
                    .Where(f => !existingFranchises.ContainsKey(f.Id))
                    .Select(g => new Franchise
                    {
                        IgdbId = g.Id,
                        Name = g.Name ?? "",
                        Slug = g.Slug ?? "",
                    }).ToList();

                if (franchisesToAdd.Count > 0)
                {
                    await context.Franchises.AddRangeAsync(franchisesToAdd);
                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error executing bulk upsert for Franchises");
            }
        }

        private async Task BulkUpsertGameModes(ApplicationDbContext context, List<IgdbGameMode> igdbGamesModes)
        {
            var uniqueGameModes = igdbGamesModes
                .GroupBy(p => p.Id)
                .Select(p => p.First())
                .ToList();

            if (uniqueGameModes.Count == 0) return;

            try
            {
                var igdbIds = uniqueGameModes.Select(gm => gm.Id).ToList();
                var existingGameModes = await context.GameModes
                    .Where(gm => igdbIds.Contains(gm.IgdbId))
                    .ToDictionaryAsync(gm => gm.IgdbId);

                var gameModesToAdd = uniqueGameModes
                    .Where(gm => !existingGameModes.ContainsKey(gm.Id))
                    .Select(g => new GameMode
                    {
                        IgdbId = g.Id,
                        Name = g.Name ?? "",
                        Slug = g.Slug ?? "",
                    }).ToList();

                if (gameModesToAdd.Count > 0)
                {
                    await context.GameModes.AddRangeAsync(gameModesToAdd);
                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error executing bulk upsert for GameModes");
            }
        }

        private async Task BulkUpsertPlayerPerspectives(ApplicationDbContext context, List<IgdbPlayerPerspective> igdbPlayerPerspectives)
        {
            var uniquePlayerPerspectives = igdbPlayerPerspectives
                .GroupBy(p => p.Id)
                .Select(p => p.First())
                .ToList();

            if (uniquePlayerPerspectives.Count == 0) return;

            try
            {
                var igdbIds = uniquePlayerPerspectives.Select(pp => pp.Id).ToList();
                var existingPlayerPerspectives = await context.PlayerPerspectives
                    .Where(pp => igdbIds.Contains(pp.IgdbId))
                    .ToDictionaryAsync(pp => pp.IgdbId);

                var ppToAdd = uniquePlayerPerspectives
                    .Where(pp => !existingPlayerPerspectives.ContainsKey(pp.Id))
                    .Select(g => new PlayerPerspective
                    {
                        IgdbId = g.Id,
                        Name = g.Name ?? "",
                        Slug = g.Slug ?? "",
                    }).ToList();

                if (ppToAdd.Count > 0)
                {
                    await context.PlayerPerspectives.AddRangeAsync(ppToAdd);
                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error executing bulk upsert for PlayerPerspectives");
            }
        }

        private async Task BulkUpsertRegions(ApplicationDbContext context, List<IgdbRegion> igdbRegions)
        {
            var uniqueRegions = igdbRegions
                .GroupBy(r => r.Id)
                .Select(r => r.First())
                .ToList();

            if (uniqueRegions.Count == 0) return;

            try
            {
                var igdbIds = uniqueRegions.Select(r => r.Id).ToList();
                var existingRegions = await context.Regions
                    .Where(r => igdbIds.Contains(r.IgdbId))
                    .ToDictionaryAsync(r => r.IgdbId);

                var regionToAdd = uniqueRegions
                    .Where(r => !existingRegions.ContainsKey(r.Id))
                    .Select(g => new ReleaseDateRegion
                    {
                        IgdbId = g.Id,
                        Region = g.Region ?? "",
                    }).ToList();

                if (regionToAdd.Count > 0)
                {
                    await context.Regions.AddRangeAsync(regionToAdd);
                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error executing bulk upsert for Regions");
            }
        }

        private async Task BulkUpsertThemes(ApplicationDbContext context, List<IgdbTheme> igdbThemes)
        {
            var uniqueThemes = igdbThemes
                .GroupBy(p => p.Id)
                .Select(p => p.First())
                .ToList();

            if (uniqueThemes.Count == 0) return;

            try
            {
                var igdbIds = uniqueThemes.Select(t => t.Id).ToList();
                var existingThemes = await context.Themes
                    .Where(t => igdbIds.Contains(t.IgdbId))
                    .ToDictionaryAsync(t => t.IgdbId);

                var themesToAdd = uniqueThemes
                    .Where(t => !existingThemes.ContainsKey(t.Id))
                    .Select(g => new Theme
                    {
                        IgdbId = g.Id,
                        Name = g.Name ?? "",
                        Slug = g.Slug ?? "",
                    }).ToList();

                if (themesToAdd.Count > 0)
                {
                    await context.Themes.AddRangeAsync(themesToAdd);
                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error executing bulk upsert for Themes");
            }
        }

        private async Task BulkUpsertRatingOrganizations(ApplicationDbContext context, List<IgdbAgeRatingOrganization> igdbOrganizations)
        {
            var uniqueOrganizations = igdbOrganizations
                .GroupBy(o => o.Id)
                .Select(g => g.First())
                .ToList();

            if (uniqueOrganizations.Count == 0) return;

            try
            {
                var igdbIds = uniqueOrganizations.Select(o => o.Id).ToList();
                var existingOrganizations = await context.RatingOrganizations
                    .Where(ro => igdbIds.Contains(ro.IgdbId))
                    .ToDictionaryAsync(ro => ro.IgdbId);

                var orgsToAdd = uniqueOrganizations
                    .Where(o => !existingOrganizations.ContainsKey(o.Id))
                    .Select(g => new RatingOrganization
                    {
                        IgdbId = g.Id,
                        Name = g.Name ?? "",
                    }).ToList();

                if (orgsToAdd.Count > 0)
                {
                    await context.RatingOrganizations.AddRangeAsync(orgsToAdd);
                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error executing bulk upsert for RatingOrganizations");
            }
        }

        private async Task BulkUpsertAgeRatingCategories(ApplicationDbContext context, List<IgdbAgeRatingCategory> igdbAgeRatingCategory)
        {
            var uniqueCategories = igdbAgeRatingCategory
                .GroupBy(c => c.Id)
                .Select(g => g.First())
                .ToList();

            if (uniqueCategories.Count == 0) return;

            try
            {
                var igdbIds = uniqueCategories.Select(c => c.Id).ToList();
                var existingCategories = await context.AgeRatingCategories
                    .Where(c => igdbIds.Contains(c.IgdbId))
                    .ToDictionaryAsync(c => c.IgdbId);

                // Get organization mappings for foreign key relationships
                var organizationIgdbIds = uniqueCategories
                    .Where(c => c.Organization != null && !existingCategories.ContainsKey(c.Id))
                    .Select(c => c.Organization.Id)
                    .Distinct()
                    .ToList();

                var organizationMappings = await context.RatingOrganizations
                    .Where(ro => organizationIgdbIds.Contains(ro.IgdbId))
                    .ToDictionaryAsync(ro => ro.IgdbId, ro => ro.Id);

                var categoriesToAdd = uniqueCategories
                    .Where(c => !existingCategories.ContainsKey(c.Id))
                    .Select(g => new AgeRatingCategory
                    {
                        IgdbId = g.Id,
                        Rating = g.Rating ?? "",
                        RatingOrganizationId = g.Organization != null && organizationMappings.TryGetValue(g.Organization.Id, out var orgId)
                            ? orgId : Guid.Empty,
                    }).ToList();

                if (categoriesToAdd.Count > 0)
                {
                    await context.AgeRatingCategories.AddRangeAsync(categoriesToAdd);
                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error executing bulk upsert for AgeRatingCategories");
            }
        }

        private async Task BulkUpsertAgeRatings(ApplicationDbContext context, List<IgdbAgeRating> igdbAgeRatings)
        {
            var uniqueAgeRatings = igdbAgeRatings
                .GroupBy(ar => ar.Id)
                .Select(g => g.First())
                .ToList();

            if (uniqueAgeRatings.Count == 0) return;

            try
            {
                var igdbIds = uniqueAgeRatings.Select(ar => ar.Id).ToList();
                var existingAgeRatings = await context.AgeRatings
                    .Where(ar => igdbIds.Contains(ar.IgdbId))
                    .ToDictionaryAsync(ar => ar.IgdbId);

                // Get category mappings for foreign key relationships
                var categoryIgdbIds = uniqueAgeRatings
                    .Where(ar => ar.RatingCategory != null && !existingAgeRatings.ContainsKey(ar.Id))
                    .Select(ar => ar.RatingCategory.Id)
                    .Distinct()
                    .ToList();

                var categoryMappings = await context.AgeRatingCategories
                    .Where(arc => categoryIgdbIds.Contains(arc.IgdbId))
                    .ToDictionaryAsync(arc => arc.IgdbId, arc => arc.Id);

                var ageRatingsToAdd = uniqueAgeRatings
                    .Where(ar => !existingAgeRatings.ContainsKey(ar.Id))
                    .Select(g => new AgeRating
                    {
                        IgdbId = g.Id,
                        AgeRatingCategoryId = g.RatingCategory != null && categoryMappings.TryGetValue(g.RatingCategory.Id, out var catId)
                            ? catId : Guid.Empty
                    }).ToList();

                if (ageRatingsToAdd.Count > 0)
                {
                    await context.AgeRatings.AddRangeAsync(ageRatingsToAdd);
                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error executing bulk upsert for AgeRatings");
            }
        }

        private async Task BulkInsertGameSpecificDataAsync(ApplicationDbContext context, List<IgdbGame> igdbGames)
        {
            // Use cache for game ID mapping instead of querying database
            var gameIds = new Dictionary<int, Guid>();
            foreach (var game in igdbGames)
            {
                if (_gameIdCache.TryGetValue(game.Id, out var databaseId))
                {
                    gameIds[game.Id] = databaseId;
                }
            }

            // Process Alt Names using cache
            await BulkUpsertAltNames(context, igdbGames, gameIds);

            // Process Covers using cache
            await BulkUpsertCovers(context, igdbGames, gameIds);

            // Process Screenshots using cache
            await BulkUpsertScreenshots(context, igdbGames, gameIds);

            // Process Release Dates using cache
            await BulkInsertReleaseDates(context, igdbGames, gameIds);
        }

        private async Task BulkUpsertAltNames(ApplicationDbContext context, List<IgdbGame> igdbGames, Dictionary<int, Guid> gameIds)
        {
            var allAltNames = igdbGames
                .Where(g => g.AlternativeNames?.Count > 0)
                .SelectMany(g => g.AlternativeNames)
                .Where(an => gameIds.ContainsKey(an.Game))
                .GroupBy(an => an.Id)
                .Select(an => an.First())
                .ToList();

            if (allAltNames.Count == 0) return;

            try
            {
                // Use cache for duplicate checking - only query for existing alt names that need updates
                var newAltNames = allAltNames
                    .Where(ian => !_existingAltNameIds.Contains(ian.Id))
                    .ToList();

                var existingAltNamesToUpdate = allAltNames
                    .Where(ian => _existingAltNameIds.Contains(ian.Id))
                    .ToList();

                // Only query database for alt names that need updates
                Dictionary<int, AltName> existingAltNames = new();
                if (existingAltNamesToUpdate.Count > 0)
                {
                    var existingIds = existingAltNamesToUpdate.Select(an => an.Id).ToList();
                    existingAltNames = await context.AltNames
                        .Where(an => existingIds.Contains(an.IgdbId))
                        .ToDictionaryAsync(an => an.IgdbId);
                }

                // Prepare new alt names that don't exist yet
                var altNamesToAdd = newAltNames
                    .Select(ian => new AltName
                    {
                        IgdbId = ian.Id,
                        Name = ian.Name ?? "",
                        Comment = ian.Comment,
                        GameId = gameIds[ian.Game]
                    }).ToList();

                if (altNamesToAdd.Count > 0)
                {
                    await context.AltNames.AddRangeAsync(altNamesToAdd);

                    // Update cache with new alt name IDs
                    foreach (var altName in altNamesToAdd)
                    {
                        _existingAltNameIds.Add(altName.IgdbId);
                    }
                }

                // Update existing alt names using cached data
                foreach (var existingPair in existingAltNames)
                {
                    var igdbAltName = existingAltNamesToUpdate.FirstOrDefault(ian => ian.Id == existingPair.Key);
                    if (igdbAltName == null) continue;

                    var existingAltName = existingPair.Value;
                    existingAltName.Name = igdbAltName.Name ?? "";
                    existingAltName.Comment = igdbAltName.Comment;
                    existingAltName.GameId = gameIds[igdbAltName.Game];
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error executing bulk upsert for AltNames");
                throw;
            }
        }

        private async Task BulkUpsertCovers(ApplicationDbContext context, List<IgdbGame> igdbGames, Dictionary<int, Guid> gameIds)
        {
            var allCovers = igdbGames
                .Where(g => g.Cover != null && gameIds.ContainsKey(g.Cover.Game))
                .Select(g => g.Cover)
                .GroupBy(c => c.Id)
                .Select(c => c.First())
                .ToList();

            if (allCovers.Count == 0) return;

            try
            {
                // Use cache for duplicate checking
                var newCovers = allCovers
                    .Where(ic => !_existingCoverIds.Contains(ic.Id))
                    .ToList();

                var existingCoversToUpdate = allCovers
                    .Where(ic => _existingCoverIds.Contains(ic.Id))
                    .ToList();

                // Only query database for covers that need updates
                Dictionary<int, Cover> existingCovers = new();
                if (existingCoversToUpdate.Count > 0)
                {
                    var existingIds = existingCoversToUpdate.Select(c => c.Id).ToList();
                    existingCovers = await context.Covers
                        .Where(c => existingIds.Contains(c.IgdbId))
                        .ToDictionaryAsync(c => c.IgdbId);
                }

                // Prepare new covers that don't exist yet
                var coversToAdd = newCovers
                    .Select(ic => new Cover
                    {
                        IgdbId = ic.Id,
                        Url = ic.Url,
                        Width = ic.Width > 0 ? ic.Width : 0,
                        Height = ic.Height > 0 ? ic.Height : 0,
                        GameId = gameIds[ic.Game],
                        ImageId = ic.ImageId
                    }).ToList();

                if (coversToAdd.Count > 0)
                {
                    await context.Covers.AddRangeAsync(coversToAdd);

                    // Update cache with new cover IDs
                    foreach (var cover in coversToAdd)
                    {
                        _existingCoverIds.Add(cover.IgdbId);
                    }
                }

                // Update existing covers using cached data
                foreach (var existingPair in existingCovers)
                {
                    var igdbCover = existingCoversToUpdate.FirstOrDefault(ic => ic.Id == existingPair.Key);
                    if (igdbCover == null) continue;

                    var existingCover = existingPair.Value;
                    existingCover.Url = igdbCover.Url;
                    existingCover.Width = igdbCover.Width > 0 ? igdbCover.Width : 0;
                    existingCover.Height = igdbCover.Height > 0 ? igdbCover.Height : 0;
                    existingCover.GameId = gameIds[igdbCover.Game];
                    existingCover.ImageId = igdbCover.ImageId;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error executing bulk upsert for Covers");
                throw;
            }
        }

        private async Task BulkUpsertScreenshots(ApplicationDbContext context, List<IgdbGame> igdbGames, Dictionary<int, Guid> gameIds)
        {
            var allScreenshots = igdbGames
                .Where(g => g.Screenshots?.Count > 0)
                .SelectMany(g => g.Screenshots)
                .Where(s => gameIds.ContainsKey(s.Game))
                .GroupBy(s => s.Id)
                .Select(s => s.First())
                .ToList();

            if (allScreenshots.Count == 0) return;

            try
            {
                // Use cache for duplicate checking
                var newScreenshots = allScreenshots
                    .Where(iss => !_existingScreenshotIds.Contains(iss.Id))
                    .ToList();

                var existingScreenshotsToUpdate = allScreenshots
                    .Where(iss => _existingScreenshotIds.Contains(iss.Id))
                    .ToList();

                // Only query database for screenshots that need updates
                Dictionary<int, Screenshot> existingScreenshots = new();
                if (existingScreenshotsToUpdate.Count > 0)
                {
                    var existingIds = existingScreenshotsToUpdate.Select(s => s.Id).ToList();
                    existingScreenshots = await context.Screenshots
                        .Where(s => existingIds.Contains(s.IgdbId))
                        .ToDictionaryAsync(s => s.IgdbId);
                }

                // Prepare new screenshots that don't exist yet
                var screenshotsToAdd = newScreenshots
                    .Select(iss => new Screenshot
                    {
                        IgdbId = iss.Id,
                        Url = iss.Url ?? "",
                        Width = iss.Width > 0 ? iss.Width : 0,
                        Height = iss.Height > 0 ? iss.Height : 0,
                        GameId = gameIds[iss.Game],
                        ImageId = iss.ImageId
                    }).ToList();

                if (screenshotsToAdd.Count > 0)
                {
                    await context.Screenshots.AddRangeAsync(screenshotsToAdd);

                    // Update cache with new screenshot IDs
                    foreach (var screenshot in screenshotsToAdd)
                    {
                        _existingScreenshotIds.Add(screenshot.IgdbId);
                    }
                }

                // Update existing screenshots using cached data
                foreach (var existingPair in existingScreenshots)
                {
                    var igdbScreenshot = existingScreenshotsToUpdate.FirstOrDefault(iss => iss.Id == existingPair.Key);
                    if (igdbScreenshot == null) continue;

                    var existingScreenshot = existingPair.Value;
                    existingScreenshot.Url = igdbScreenshot.Url ?? "";
                    existingScreenshot.Width = igdbScreenshot.Width > 0 ? igdbScreenshot.Width : 0;
                    existingScreenshot.Height = igdbScreenshot.Height > 0 ? igdbScreenshot.Height : 0;
                    existingScreenshot.GameId = gameIds[igdbScreenshot.Game];
                    existingScreenshot.ImageId = igdbScreenshot.ImageId;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error executing bulk upsert for Screenshots");
                throw;
            }
        }

        // Add the missing BulkInsertReleaseDates method
        private async Task BulkInsertReleaseDates(ApplicationDbContext context, List<IgdbGame> igdbGames, Dictionary<int, Guid> gameIds)
        {
            var allReleaseDates = igdbGames
                .Where(g => g.ReleaseDates?.Count > 0)
                .SelectMany(g => g.ReleaseDates)
                .Where(rd => gameIds.ContainsKey(rd.Game))
                .GroupBy(rd => rd.Id)
                .Select(rd => rd.First())
                .ToList();

            if (allReleaseDates.Count == 0) return;

            try
            {
                // Use cache for duplicate checking
                var newReleaseDates = allReleaseDates
                    .Where(ird => !_existingReleaseDateIds.Contains(ird.Id))
                    .ToList();

                var existingReleaseDatesToUpdate = allReleaseDates
                    .Where(ird => _existingReleaseDateIds.Contains(ird.Id))
                    .ToList();

                // Only query database for release dates that need updates
                Dictionary<int, ReleaseDate> existingReleaseDates = new();
                if (existingReleaseDatesToUpdate.Count > 0)
                {
                    var existingIds = existingReleaseDatesToUpdate.Select(rd => rd.Id).ToList();
                    existingReleaseDates = await context.ReleaseDates
                        .Where(rd => existingIds.Contains(rd.IgdbId))
                        .ToDictionaryAsync(rd => rd.IgdbId);
                }

                // Prepare new release dates that don't exist yet (using cache for platform and region mappings)
                var releaseDatesToAdd = newReleaseDates
                    .Where(ird => ird.Platform != null && _platformCache.ContainsKey(ird.Platform.Id) &&
                            ird.ReleaseRegion != null && _regionCache.ContainsKey(ird.ReleaseRegion.Id))
                    .Select(ird => new ReleaseDate
                    {
                        IgdbId = ird.Id,
                        Date = ird.Date,
                        GameId = gameIds[ird.Game],
                        PlatformId = _platformCache[ird.Platform.Id],
                        RegionId = _regionCache[ird.ReleaseRegion.Id]
                    }).ToList();

                if (releaseDatesToAdd.Count > 0)
                {
                    await context.ReleaseDates.AddRangeAsync(releaseDatesToAdd);

                    // Update cache with new release date IDs
                    foreach (var releaseDate in releaseDatesToAdd)
                    {
                        _existingReleaseDateIds.Add(releaseDate.IgdbId);
                    }
                }

                // Update existing release dates using cached data
                foreach (var existingPair in existingReleaseDates)
                {
                    var igdbReleaseDate = existingReleaseDatesToUpdate.FirstOrDefault(ird => ird.Id == existingPair.Key);
                    if (igdbReleaseDate == null) continue;

                    // Only update if we have valid platform and region mappings in cache
                    if (igdbReleaseDate.Platform == null || !_platformCache.ContainsKey(igdbReleaseDate.Platform.Id) ||
                        igdbReleaseDate.ReleaseRegion == null || !_regionCache.ContainsKey(igdbReleaseDate.ReleaseRegion.Id))
                        continue;

                    var existingReleaseDate = existingPair.Value;
                    existingReleaseDate.Date = igdbReleaseDate.Date;
                    existingReleaseDate.GameId = gameIds[igdbReleaseDate.Game];
                    existingReleaseDate.PlatformId = _platformCache[igdbReleaseDate.Platform.Id];
                    existingReleaseDate.RegionId = _regionCache[igdbReleaseDate.ReleaseRegion.Id];
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error executing bulk upsert for ReleaseDates");
                throw;
            }
        }

        private async Task ProcessBatchWithSemaphoreAsync(SemaphoreSlim semaphore, int offset, int batchIndex, int totalBatches, CancellationToken cancellationToken = default)
        {
            var displayBatchIndex = batchIndex + 1;

            await semaphore.WaitAsync(cancellationToken);
            try
            {
                _logger.LogInformation("Processing batch {BatchIndex}/{TotalBatches} (offset: {Offset})",
                    displayBatchIndex, totalBatches, offset);

                var games = await FetchGamesBatchWithRetryAsync(offset, _igdbSettings.BatchSize);

                if (games.Count == 0)
                {
                    _logger.LogInformation("Skipping empty batch {BatchIndex}/{TotalBatches}",
                        displayBatchIndex, totalBatches);
                    return;
                }

                try
                {
                    await ProcessGamesBatchOptimizedAsync(games);
                    _logger.LogInformation("Completed batch {BatchIndex}/{TotalBatches} - {GameCount} games",
                        displayBatchIndex, totalBatches, games.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process batch {BatchIndex}/{TotalBatches} with {GameCount} games. Offset: {Offset}",
                        displayBatchIndex, totalBatches, games.Count, offset);
                    throw; // Re-throw to maintain existing error handling behavior
                }
            }
            finally
            {
                semaphore.Release();
            }
        }

        public async Task NonConcurrentImportAsync()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            _logger.LogInformation("Starting non-concurrent IGDB import");

            try
            {
                // Step 1: Get total count and plan batches
                var totalGames = await GetTotalGameCountAsync();
                var totalBatches = (int)Math.Ceiling((double)totalGames / _igdbSettings.BatchSize);
                _logger.LogInformation("Planning to import {TotalGames} games in {TotalBatches} batches",
                    totalGames, totalBatches);

                // Step 2: Pre-populate reference data and initialize caches
                //await PrePopulateAllReferenceDataAsync();
                await InitializeCachesAsync();

                // Step 3: Process each batch sequentially
                for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
                {
                    var offset = batchIndex * _igdbSettings.BatchSize;

                    _logger.LogInformation("Processing batch {BatchIndex}/{TotalBatches} (offset: {Offset})",
                        batchIndex + 1, totalBatches, offset);

                    var games = await FetchGamesBatchWithRetryAsync(offset, _igdbSettings.BatchSize);

                    if (games.Count > 0)
                    {
                        await ProcessGamesBatchOptimizedAsync(games);
                        _logger.LogInformation("Completed batch {BatchIndex}/{TotalBatches} - {GameCount} games",
                            batchIndex + 1, totalBatches, games.Count);
                    }
                }
            }
            finally
            {
                stopwatch.Stop();
                _logger.LogInformation("Non-concurrent IGDB import completed in {ElapsedTime}", stopwatch.Elapsed);
            }
        }

        private async Task InitializeCachesAsync()
        {
            _logger.LogInformation("Initializing reference data caches...");

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Load all reference data into cache dictionaries for fast lookup
            var gameTypes = await context.GameTypes.ToDictionaryAsync(x => x.IgdbId, x => x.Id);
            var genres = await context.Genres.ToDictionaryAsync(x => x.IgdbId, x => x.Id);
            var companies = await context.Companies.ToDictionaryAsync(x => x.IgdbId, x => x.Id);
            var platforms = await context.Platforms.ToDictionaryAsync(x => x.IgdbId, x => x.Id);
            var franchises = await context.Franchises.ToDictionaryAsync(x => x.IgdbId, x => x.Id);
            var gameModes = await context.GameModes.ToDictionaryAsync(x => x.IgdbId, x => x.Id);
            var playerPerspectives = await context.PlayerPerspectives.ToDictionaryAsync(x => x.IgdbId, x => x.Id);
            var themes = await context.Themes.ToDictionaryAsync(x => x.IgdbId, x => x.Id);
            var regions = await context.Regions.ToDictionaryAsync(x => x.IgdbId, x => x.Id);
            var ageRatings = await context.AgeRatings.ToDictionaryAsync(x => x.IgdbId, x => x.Id);

            // Populate cache dictionaries
            foreach (var item in gameTypes) _gameTypeCache[item.Key] = item.Value;
            foreach (var item in genres) _genreCache[item.Key] = item.Value;
            foreach (var item in companies) _companyCache[item.Key] = item.Value;
            foreach (var item in platforms) _platformCache[item.Key] = item.Value;
            foreach (var item in franchises) _franchiseCache[item.Key] = item.Value;
            foreach (var item in gameModes) _gameModeCache[item.Key] = item.Value;
            foreach (var item in playerPerspectives) _playerPerspectiveCache[item.Key] = item.Value;
            foreach (var item in themes) _themeCache[item.Key] = item.Value;
            foreach (var item in regions) _regionCache[item.Key] = item.Value;
            foreach (var item in ageRatings) _ageRatingCache[item.Key] = item.Value;

            // Load existing IDs for duplicate checking and game ID mapping
            var gameIdMappings = await context.Games.ToDictionaryAsync(g => g.IgdbId, g => g.Id);
            var existingAltNameIds = await context.AltNames.Select(an => an.IgdbId).ToListAsync();
            var existingCoverIds = await context.Covers.Select(c => c.IgdbId).ToListAsync();
            var existingScreenshotIds = await context.Screenshots.Select(s => s.IgdbId).ToListAsync();
            var existingReleaseDateIds = await context.ReleaseDates.Select(rd => rd.IgdbId).ToListAsync();

            foreach (var mapping in gameIdMappings)
            {
                _existingGameIds.Add(mapping.Key);
                _gameIdCache[mapping.Key] = mapping.Value;
            }
            foreach (var id in existingAltNameIds) _existingAltNameIds.Add(id);
            foreach (var id in existingCoverIds) _existingCoverIds.Add(id);
            foreach (var id in existingScreenshotIds) _existingScreenshotIds.Add(id);
            foreach (var id in existingReleaseDateIds) _existingReleaseDateIds.Add(id);

            // Load existing relationships to prevent duplicates
            await LoadExistingRelationshipsAsync(context);

            _logger.LogInformation("Cache initialized with {GameTypeCount} game types, {GenreCount} genres, {CompanyCount} companies, {PlatformCount} platforms, {ExistingGameCount} existing games, {AltNameCount} alt names, {CoverCount} covers, {RelationshipCount} relationships",
                _gameTypeCache.Count, _genreCache.Count, _companyCache.Count, _platformCache.Count, _existingGameIds.Count, _existingAltNameIds.Count, _existingCoverIds.Count, _existingRelationships.Count);
        }

        private async Task LoadExistingRelationshipsAsync(ApplicationDbContext context)
        {
            // Load existing relationships into cache
            var dlcs = await context.GameDlcs.Select(d => $"GameDlc|{d.ParentGameId}|{d.DlcGameId}").ToListAsync();
            var expansions = await context.GameExpansions.Select(e => $"GameExpansion|{e.ParentGameId}|{e.ExpansionGameId}").ToListAsync();
            var ports = await context.GamePorts.Select(p => $"GamePort|{p.OriginalGameId}|{p.PortGameId}").ToListAsync();
            var remakes = await context.GameRemakes.Select(r => $"GameRemake|{r.OriginalGameId}|{r.RemakeGameId}").ToListAsync();
            var remasters = await context.GameRemasters.Select(r => $"GameRemaster|{r.OriginalGameId}|{r.RemasterGameId}").ToListAsync();
            var similars = await context.SimilarGames.Select(s => $"SimilarGame|{s.GameId}|{s.SimilarGameId}").ToListAsync();

            foreach (var relationship in dlcs.Concat(expansions).Concat(ports).Concat(remakes).Concat(remasters).Concat(similars))
            {
                _existingRelationships.Add(relationship);
            }
        }

        private async Task<List<IgdbGame>> FetchGamesBatchWithRetryAsync(int offset, int limit)
        {
            var retryPolicy = Policy
                .Handle<HttpRequestException>()
                .Or<TaskCanceledException>()
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        _logger.LogWarning("Retry {RetryCount} for offset {Offset} after {Delay}ms",
                            retryCount, offset, timespan.TotalMilliseconds);
                    });

            return await retryPolicy.ExecuteAsync(async () =>
            {
                await _rateLimitSemaphore.WaitAsync();
                try
                {
                    return await FetchGamesBatchAsync(offset, limit);
                }
                finally
                {
                    _rateLimitSemaphore.Release();
                    await Task.Delay(_igdbSettings.RateLimitDelayMs); // Rate limiting
                }
            });
        }

        private async Task<List<IgdbGame>> FetchGamesBatchAsync(int offset, int limit)
        {
            using var httpClient = CreateConfiguredHttpClient();

            // Optimized query - only essential fields for initial import
            var query = $@"
            fields 
                id, name, slug, storyline, summary, first_release_date, hypes, rating,
                genres.id, genres.name, genres.slug,
                age_ratings.id, age_ratings.rating_category.id, 
                age_ratings.rating_category.rating, 
                age_ratings.rating_category.organization.id, 
                age_ratings.rating_category.organization.name,  
                alternative_names.id, alternative_names.name, alternative_names.comment,
                alternative_names.game,
                cover.id, cover.url, cover.width, cover.height, cover.game, cover.image_id,
                screenshots.id, screenshots.url, screenshots.width, screenshots.height,
                screenshots.game, screenshots.image_id,
                release_dates.id, release_dates.date, release_dates.game, 
                release_dates.platform.id, release_dates.platform.name,
                release_dates.release_region.id, release_dates.release_region.region,
                franchises.id, franchises.name, franchises.slug,
                game_modes.id, game_modes.name, game_modes.slug,
                game_type.id, game_type.type,
                involved_companies.id, involved_companies.company.id, 
                involved_companies.company.name, involved_companies.company.country,
                involved_companies.company.description, 
                involved_companies.company.slug, involved_companies.company.url,
                involved_companies.developer, involved_companies.publisher, 
                involved_companies.porting, involved_companies.supporting,
                platforms.id, platforms.name, platforms.abbreviation, platforms.slug,
                player_perspectives.id, player_perspectives.name, player_perspectives.slug,
                dlcs, expansions, ports, remakes, remasters, similar_games, 
                themes.id, themes.name, themes.slug, total_rating, total_rating_count;
            where version_parent = null;
            sort id asc;
            limit {limit};
            offset {offset};";

            var content = new StringContent(query, Encoding.UTF8, "text/plain");
            var response = await httpClient.PostAsync("games", content);

            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var games = JsonSerializer.Deserialize<List<IgdbGame>>(jsonResponse, _jsonOptions);

            return games ?? [];
        }

        private async Task<List<T>> FetchBatchFromGameSourceAsync<T>(string query, string endpoint, int offset, int limit)
        {
            using var httpClient = CreateConfiguredHttpClient();

            var finalQuery = $"{query} limit {limit}; offset {offset};";

            var content = new StringContent(finalQuery, Encoding.UTF8, "text/plain");
            var response = await httpClient.PostAsync(endpoint, content);

            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<List<T>>(jsonResponse, _jsonOptions);

            return result ?? [];
        }

        private async Task ProcessGamesBatchOptimizedAsync(List<IgdbGame> igdbGames)
        {
            // Use a separate DbContext per batch to avoid tracking issues
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Disable change tracking for better performance
            context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
            context.ChangeTracker.AutoDetectChangesEnabled = false;

            using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                // Step 1: Insert games first and save to get database IDs
                await BulkInsertGamesAsync(context, igdbGames);
                await context.SaveChangesAsync(); // Save games first so their IDs are available

                // Step 2-4: Insert all dependent data in one batch to reduce SaveChanges calls
                await BulkInsertGameSpecificDataAsync(context, igdbGames); // Alt names, covers, screenshots, release dates
                await BulkInsertAssociationsAsync(context, igdbGames);
                await ProcessGameRelationshipsAsync(context, igdbGames);

                // Single SaveChanges for all dependent data
                await context.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch (DbUpdateException ex)
            {
                // Check if it's a duplicate key error
                if (ex.InnerException is MySqlConnector.MySqlException mysqlEx &&
                    mysqlEx.ErrorCode == MySqlConnector.MySqlErrorCode.DuplicateKeyEntry)
                {
                    _logger.LogWarning("Duplicate key detected in batch processing: {Message}. Rolling back transaction and continuing...", mysqlEx.Message);
                    await transaction.RollbackAsync();
                    // Continue processing other batches
                    return;
                }
                else if (ex.Message.Contains("Duplicate entry") || ex.InnerException?.Message.Contains("Duplicate entry") == true)
                {
                    _logger.LogWarning("Duplicate entry detected (alternative check): {Message}. Rolling back transaction and continuing...", ex.Message);
                    await transaction.RollbackAsync();
                    // Continue processing other batches
                    return;
                }
                else
                {
                    // Re-throw if it's not a duplicate key error
                    _logger.LogError(ex, "Database update error during batch processing");
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error processing batch");
                await transaction.RollbackAsync();
                throw;
            }
        }

        // private async Task BulkInsertGamesAsync(ApplicationDbContext context, List<IgdbGame> igdbGames)
        // {
        //     // Step 1: Extract all unique GameType IGDB IDs from the games
        //     var gameTypeIgdbIds = igdbGames
        //         .Where(g => g.GameType != null)
        //         .Select(g => g.GameType.Id)
        //         .Distinct()
        //         .ToList();

        //     // Get all GameType mappings from database
        //     var gameTypeIdMappings = await context.GameTypes
        //         .Where(gt => gameTypeIgdbIds.Contains(gt.IgdbId))
        //         .ToDictionaryAsync(gt => gt.IgdbId, gt => gt.Id);

        //     // Step 2: Get existing GameTypes from database
        //     var existingGameTypes = await context.GameTypes
        //         .Where(gt => gameTypeIgdbIds.Contains(gt.IgdbId))
        //         .ToDictionaryAsync(gt => gt.IgdbId, gt => gt.Id);

        //     // Step 3: Create missing GameTypes
        //     var missingGameTypeIds = gameTypeIgdbIds
        //         .Where(id => !existingGameTypes.ContainsKey(id))
        //         .ToList();

        //     if (missingGameTypeIds.Count > 0)
        //     {
        //         Console.WriteLine($"Creating {missingGameTypeIds.Count} missing GameTypes...");

        //         var newGameTypes = missingGameTypeIds.Select(igdbId => new GameType
        //         {
        //             IgdbId = igdbId,
        //             Type = igdbGames.Select(g => g.GameType)
        //                 .FirstOrDefault(gt => gt != null && gt.Id == igdbId)?.Type ?? ""
        //         }).ToList();

        //         await context.GameTypes.AddRangeAsync(newGameTypes);
        //         //await context.SaveChangesAsync();

        //         // Update the lookup dictionary with newly created GameTypes
        //         foreach (var gameType in newGameTypes)
        //         {
        //             existingGameTypes[gameType.IgdbId] = gameType.Id;
        //         }

        //         Console.WriteLine($"✓ Created {newGameTypes.Count} GameTypes");
        //     }

        //     // Step 4: Prepare new games and update existing ones
        //     var igdbIds = igdbGames.Select(g => g.Id).ToList();
        //     var existingGames = await context.Games
        //         .Where(g => igdbIds.Contains(g.IgdbId))
        //         .ToListAsync();
        //     var existingGameIds = existingGames.Select(g => g.IgdbId).ToHashSet();

        //     var newGames = igdbGames
        //         .Where(ig => existingGameIds.Count <= 0 || !existingGameIds.Contains(ig.Id))
        //         .Select(ig => new Backend.Models.Game.Game
        //         {
        //             IgdbId = ig.Id,
        //             Name = ig.Name ?? "",
        //             Slug = ig.Slug ?? "",
        //             Storyline = ig.Storyline,
        //             Summary = ig.Summary,
        //             FirstReleaseDate = ig.FirstReleaseDate,
        //             Hypes = ig.Hypes ?? 0,
        //             Rating = ig.Rating.HasValue ? (decimal)ig.Rating.Value : null,
        //             GameTypeId = existingGameTypes.TryGetValue(ig.GameType.Id, out var gameTypeId) ? gameTypeId : Guid.Empty,
        //         })
        //         .ToList();

        //     if (newGames.Count > 0)
        //     {
        //         // Use bulk insert if available (EF Core 7+)
        //         await context.Games.AddRangeAsync(newGames);
        //     }

        //     // Update existing games
        //     foreach (var existingGame in existingGames)
        //     {
        //         // if (existingGame.IgdbId == 0)
        //         //     continue;
        //         var igdbGame = igdbGames.FirstOrDefault(ig => ig.Id == existingGame.IgdbId);
        //         if (igdbGame == null) continue;
        //         existingGame.Name = igdbGame.Name ?? existingGame.Name;
        //         existingGame.Storyline = igdbGame.Storyline ?? existingGame.Storyline;
        //         existingGame.Summary = igdbGame.Summary ?? existingGame.Summary;
        //         existingGame.FirstReleaseDate = igdbGame.FirstReleaseDate ?? existingGame.FirstReleaseDate;
        //         existingGame.Hypes = igdbGame.Hypes ?? existingGame.Hypes;
        //         existingGame.Rating = igdbGame.Rating.HasValue ? (decimal)igdbGame.Rating.Value : existingGame.Rating;
        //         existingGame.GameTypeId = igdbGame.GameType != null &&
        //             gameTypeIdMappings.TryGetValue(igdbGame.GameType.Id, out var gameTypeId) ?
        //                 gameTypeId : existingGame.GameTypeId;
        //     }

        //     //await context.SaveChangesAsync();
        // }

        private async Task BulkInsertGamesAsync(ApplicationDbContext context, List<IgdbGame> igdbGames)
        {
            // Deduplicate input data first
            var uniqueGames = igdbGames
                .GroupBy(g => g.Id)
                .Select(g => g.First())
                .ToList();

            if (uniqueGames.Count == 0) return;

            try
            {
                // Use cache for duplicate checking - no database query needed
                var newGames = uniqueGames
                    .Where(ig => !_existingGameIds.Contains(ig.Id))
                    .ToList();

                var existingGamesToUpdate = uniqueGames
                    .Where(ig => _existingGameIds.Contains(ig.Id))
                    .ToList();

                // Only query database for games that need updates
                Dictionary<int, Backend.Models.Game.Game> existingGamesDict = new();
                if (existingGamesToUpdate.Count > 0)
                {
                    var existingIds = existingGamesToUpdate.Select(g => g.Id).ToList();
                    existingGamesDict = await context.Games
                        .Where(g => existingIds.Contains(g.IgdbId))
                        .ToDictionaryAsync(g => g.IgdbId);
                }

                // Prepare new games that don't exist yet - using cache for GameType lookup
                var gamesToAdd = newGames
                    .Select(ig => new Backend.Models.Game.Game
                    {
                        IgdbId = ig.Id,
                        Name = ig.Name ?? "",
                        Slug = ig.Slug ?? "",
                        Storyline = ig.Storyline,
                        Summary = ig.Summary,
                        FirstReleaseDate = ig.FirstReleaseDate,
                        Hypes = ig.Hypes ?? 0,
                        Rating = ig.Rating.HasValue && !double.IsNaN(ig.Rating.Value) && !double.IsInfinity(ig.Rating.Value)
                            ? (decimal)ig.Rating.Value : null,
                        TotalRating = ig.TotalRating.HasValue && !double.IsNaN(ig.TotalRating.Value) && !double.IsInfinity(ig.TotalRating.Value)
                            ? (decimal)ig.TotalRating.Value : null,
                        TotalRatingCount = ig.TotalRatingCount ?? 0,
                        GameTypeId = ig.GameType != null && _gameTypeCache.TryGetValue(ig.GameType.Id, out var gameTypeId)
                            ? gameTypeId : null,
                    }).ToList();

                if (gamesToAdd.Count > 0)
                {
                    await context.Games.AddRangeAsync(gamesToAdd);

                    // Update caches with new game IDs and mappings
                    foreach (var game in gamesToAdd)
                    {
                        _existingGameIds.Add(game.IgdbId);
                        _gameIdCache[game.IgdbId] = game.Id;
                    }
                }

                // Update existing games using cached data
                foreach (var existingPair in existingGamesDict)
                {
                    var igdbGame = existingGamesToUpdate.FirstOrDefault(ig => ig.Id == existingPair.Key);
                    if (igdbGame == null) continue;

                    var existingGame = existingPair.Value;

                    // Update existing game properties using cache
                    existingGame.Name = igdbGame.Name ?? "";
                    existingGame.Slug = igdbGame.Slug ?? "";
                    existingGame.Storyline = igdbGame.Storyline;
                    existingGame.Summary = igdbGame.Summary;
                    existingGame.FirstReleaseDate = igdbGame.FirstReleaseDate;
                    existingGame.Hypes = igdbGame.Hypes ?? 0;
                    existingGame.Rating = igdbGame.Rating.HasValue && !double.IsNaN(igdbGame.Rating.Value) && !double.IsInfinity(igdbGame.Rating.Value)
                        ? (decimal)igdbGame.Rating.Value : null;
                    existingGame.TotalRating = igdbGame.TotalRating.HasValue && !double.IsNaN(igdbGame.TotalRating.Value) && !double.IsInfinity(igdbGame.TotalRating.Value)
                        ? (decimal)igdbGame.TotalRating.Value : null;
                    existingGame.TotalRatingCount = igdbGame.TotalRatingCount ?? 0;
                    existingGame.GameTypeId = igdbGame.GameType != null && _gameTypeCache.TryGetValue(igdbGame.GameType.Id, out var gameTypeId)
                        ? gameTypeId : null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error executing bulk upsert for Games");
                throw;
            }
        }

        private async Task UpsertGameTypesWithRawSQL(ApplicationDbContext context, List<IgdbGameType> gameTypes)
        {
            if (gameTypes.Count == 0) return;

            var valuesList = new List<string>();
            foreach (var gt in gameTypes)
            {
                var id = Guid.NewGuid();
                var type = EscapeSqlString(gt.Type ?? "NULL");
                valuesList.Add($"('{id}', {gt.Id}, '{type}', NOW(), NOW())");
            }

            var sql = $@"
        INSERT IGNORE INTO GameTypes (Id, IgdbId, Type, CreatedAt, UpdatedAt)
        VALUES {string.Join(",", valuesList)}";

            try
            {
                await context.Database.ExecuteSqlRawAsync(sql);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error executing raw SQL for GameTypes upsert");
                // This is expected in concurrent scenarios - MySQL will just ignore duplicates
            }
        }

        // Helper method to escape SQL strings
        private string EscapeSqlString(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            return input.Replace("\\", "\\\\")  // Must be first - escape backslashes
                .Replace("'", "''")      // Escape single quotes  
                .Replace("\"", "\\\"")   // Escape double quotes
                .Replace("\0", "\\0")    // Escape null bytes
                .Replace("\n", "\\n")    // Escape newlines
                .Replace("\r", "\\r")    // Escape carriage returns
                .Replace("\x1a", "\\Z")  // Escape substitute character
                .Replace("\t", "\\t");   // Escape tabs
        }

        private async Task UpsertGameTypesExplicitly(ApplicationDbContext context, List<IgdbGameType> gameTypes)
        {
            var igdbIds = gameTypes.Select(gt => gt.Id).ToList();

            // // Get existing GameTypes
            // var existingGameTypes = await context.GameTypes
            //     .Where(gt => igdbIds.Contains(gt.IgdbId))
            //     .ToListAsync();

            // var existingIgdbIds = existingGameTypes.Select(gt => gt.IgdbId).ToHashSet();

            // Use a separate context to check existing GameTypes to avoid transaction isolation issues
            using var checkScope = _serviceProvider.CreateScope();
            var checkContext = checkScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var existingIgdbIds = await checkContext.GameTypes
                .Where(gt => igdbIds.Contains(gt.IgdbId))
                .Select(gt => gt.IgdbId)
                .ToListAsync();

            var existingIgdbIdsSet = existingIgdbIds.ToHashSet();

            // Only create new ones
            var newGameTypes = gameTypes
                .Where(gt => !existingIgdbIds.Contains(gt.Id))
                .Select(gt => new GameType
                {
                    IgdbId = gt.Id,
                    Type = gt.Type
                })
                .ToList();

            if (newGameTypes.Count > 0)
            {
                await context.GameTypes.AddRangeAsync(newGameTypes);
            }
        }

        private async Task BulkInsertReferenceDataAsync(ApplicationDbContext context, List<IgdbGame> igdbGames)
        {
            var currentGameIgdbIds = igdbGames.Select(g => g.Id).ToList();

            // Process Genres
            var allGenres = igdbGames
                .Where(g => g.Genres?.Any() == true)
                .SelectMany(g => g.Genres)
                .GroupBy(g => g.Id)
                .Select(g => g.First())
                .ToList();

            await UpsertReferenceData(allGenres, context, context.Genres,
                g => new Genre { IgdbId = g.Id, Name = g.Name, Slug = g.Slug });

            // Process Age Ratings
            var allAgeRatings = igdbGames
                .Where(g => g.AgeRatings?.Any() == true)
                .SelectMany(g => g.AgeRatings)
                .GroupBy(ar => ar.Id)
                .Select(ar => ar.First())
                .ToList();
            var allAgeRatingCategories = allAgeRatings
                .Select(ar => ar.RatingCategory)
                .Where(c => c != null)
                .GroupBy(c => c.Id)
                .Select(c => c.First())
                .ToList();
            var allRatingOrganizations = allAgeRatingCategories
                .Select(c => c.Organization)
                .Where(o => o != null)
                .GroupBy(o => o!.Id)
                .Select(o => o.First())
                .ToList();
            await UpsertReferenceData(
                allRatingOrganizations!,
                context,
                context.RatingOrganizations,
                o => new RatingOrganization
                {
                    IgdbId = o.Id,
                    Name = o.Name ?? ""
                });
            await context.SaveChangesAsync();

            var ageRatingOrganizationIds = await context.RatingOrganizations
                .Where(o => o.IgdbId > 0)
                .GroupBy(o => o.IgdbId)
                .ToDictionaryAsync(o => o.Key, o => o.First().Id);
            await UpsertReferenceData(allAgeRatingCategories,
                context,
                context.AgeRatingCategories,
                c => new AgeRatingCategory
                {
                    IgdbId = c.Id,
                    Rating = c.Rating ?? "",
                    RatingOrganizationId = c.Organization != null && c.Organization.Id > 0 &&
                        ageRatingOrganizationIds.TryGetValue(c.Organization.Id, out var orgId) ?
                        orgId : Guid.Empty
                });
            await context.SaveChangesAsync();

            var ageRatingCategoryIds = await context.AgeRatingCategories
                .Where(c => c.IgdbId > 0)
                .GroupBy(c => c.IgdbId)
                .ToDictionaryAsync(c => c.Key, c => c.First().Id);
            await UpsertReferenceData(allAgeRatings, context, context.AgeRatings,
                ar => new AgeRating
                {
                    IgdbId = ar.Id,
                    AgeRatingCategoryId = ar.RatingCategory != null &&
                        ageRatingCategoryIds.TryGetValue(ar.RatingCategory.Id, out var categoryId) ?
                        categoryId : Guid.Empty
                });
            await context.SaveChangesAsync();

            // get game ids
            var gameIds = await context.Games
                .Where(g => currentGameIgdbIds.Contains(g.IgdbId))
                .GroupBy(g => g.IgdbId)
                .ToDictionaryAsync(g => g.Key, g => g.First().Id);

            // Process Alternative Names
            var allAltNames = igdbGames
                .Where(g => g.AlternativeNames?.Count > 0)
                .SelectMany(g => g.AlternativeNames)
                .GroupBy(an => an.Id)
                .Select(an => an.First())
                .ToList();

            await UpsertReferenceData(allAltNames, context, context.AltNames,
                an => new AltName
                {
                    IgdbId = an.Id,
                    Name = an.Name ?? "",
                    Comment = an.Comment ?? "",
                    GameId = an.Game > 0 && gameIds.TryGetValue(an.Game, out var gameId) ? gameId : Guid.Empty
                });

            // Process Covers
            var allCovers = igdbGames
                .Where(g => g.Cover != null)
                .Select(g => g.Cover)
                .GroupBy(c => c.Id)
                .Select(c => c.First())
                .ToList();
            await UpsertReferenceData(allCovers, context, context.Covers,
                c => new Cover
                {
                    IgdbId = c.Id,
                    Url = c.Url ?? "",
                    Width = c.Width,
                    Height = c.Height,
                    GameId = c.Game > 0 && gameIds.TryGetValue(c.Game, out var gameId) ? gameId : Guid.Empty,
                    ImageId = c.ImageId ?? ""
                });

            // Process Screenshots
            var allScreenshots = igdbGames
                .Where(g => g.Screenshots?.Any() == true)
                .SelectMany(g => g.Screenshots)
                .GroupBy(s => s.Id)
                .Select(s => s.First())
                .ToList();
            await UpsertReferenceData(allScreenshots, context, context.Screenshots,
                s => new Screenshot
                {
                    IgdbId = s.Id,
                    Url = s.Url ?? "",
                    Width = s.Width,
                    Height = s.Height,
                    GameId = s.Game > 0 && gameIds.TryGetValue(s.Game, out var gameId) ? gameId : Guid.Empty,
                    ImageId = s.ImageId ?? ""
                });

            // Process Release Dates
            var allReleaseDates = igdbGames
                .Where(g => g.ReleaseDates?.Any() == true)
                .SelectMany(g => g.ReleaseDates)
                .GroupBy(rd => rd.Id)
                .Select(rd => rd.First())
                .ToList();
            // Process Platforms (from release dates)
            var allPlatforms = igdbGames
                .Where(g => g.ReleaseDates?.Any() == true)
                .SelectMany(g => g.ReleaseDates)
                .Where(rd => rd.Platform != null)
                .Select(rd => rd.Platform)
                .GroupBy(p => p.Id)
                .Select(p => p.First())
                .ToList();

            await UpsertReferenceData(allPlatforms, context, context.Platforms,
                p => new Platform
                {
                    IgdbId = p.Id,
                    Name = p.Name,
                    Abbreviation = p.Abbreviation,
                    Slug = p.Slug ?? "",
                });
            // Process Release Regions (from release dates)
            var allReleaseRegions = igdbGames
                .Where(g => g.ReleaseDates?.Any() == true)
                .SelectMany(g => g.ReleaseDates)
                .Where(rd => rd.ReleaseRegion != null)
                .Select(rd => rd.ReleaseRegion)
                .GroupBy(r => r.Id)
                .Select(r => r.First())
                .ToList();
            await UpsertReferenceData(allReleaseRegions, context, context.Regions,
                r => new ReleaseDateRegion
                {
                    IgdbId = r.Id,
                    Region = r.Region ?? "",
                });
            var releaseRegionIds = await context.Regions
                .Where(r => r.IgdbId > 0)
                .GroupBy(r => r.IgdbId)
                .ToDictionaryAsync(r => r.Key, r => r.First().Id);
            var platformIds = await context.Platforms
                .Where(p => p.IgdbId > 0)
                .GroupBy(r => r.IgdbId)
                .ToDictionaryAsync(r => r.Key, r => r.First().Id);
            // Upsert Release Dates 
            await UpsertReferenceData(allReleaseDates, context, context.ReleaseDates,
                rd => new ReleaseDate
                {
                    IgdbId = rd.Id,
                    Date = rd.Date ?? 0,
                    GameId = rd.Game > 0 && gameIds.TryGetValue(rd.Game, out var gameId) ? gameId : Guid.Empty,
                    PlatformId = rd.Platform != null &&
                        platformIds.TryGetValue(rd.Platform.Id, out var platformId) ?
                            platformId : Guid.Empty,
                    RegionId = rd.ReleaseRegion != null &&
                        releaseRegionIds.TryGetValue(rd.ReleaseRegion.Id, out var regionId) ?
                            regionId : Guid.Empty
                });

            // Process Franchises
            var allFranchises = igdbGames
                .Where(g => g.Franchises?.Any() == true)
                .SelectMany(g => g.Franchises)
                .GroupBy(f => f.Id)
                .Select(f => f.First())
                .ToList();
            await UpsertReferenceData(allFranchises, context, context.Franchises,
                f => new Franchise
                {
                    IgdbId = f.Id,
                    Name = f.Name ?? "",
                    Slug = f.Slug ?? ""
                });

            // Process Game Modes
            var allGameModes = igdbGames
                .Where(g => g.GameModes?.Any() == true)
                .SelectMany(g => g.GameModes)
                .GroupBy(gm => gm.Id)
                .Select(gm => gm.First())
                .ToList();
            await UpsertReferenceData(allGameModes, context, context.GameModes,
                gm => new GameMode
                {
                    IgdbId = gm.Id,
                    Name = gm.Name ?? "",
                    Slug = gm.Slug ?? ""
                });

            // Process Companies
            var allCompanies = igdbGames
                .Where(g => g.InvolvedCompanies?.Any() == true)
                .SelectMany(g => g.InvolvedCompanies)
                .Where(ic => ic.Company != null)
                .Select(ic => ic.Company)
                .GroupBy(c => c.Id)
                .Select(c => c.First())
                .ToList();

            await UpsertReferenceData(allCompanies, context, context.Companies,
                c => new Company
                {
                    IgdbId = c.Id,
                    Name = c.Name,
                    CountryCode = c.Country,
                    Description = c.Description,
                    Slug = c.Slug,
                    Url = c.Url
                });

            // Process Platforms from Game Objects
            var allPlatformsFromGames = igdbGames
                .Where(g => g.Platforms?.Any() == true)
                .SelectMany(g => g.Platforms)
                .GroupBy(p => p.Id)
                .Select(p => p.First())
                .ToList();
            await UpsertReferenceData(allPlatformsFromGames, context, context.Platforms,
                p => new Platform
                {
                    IgdbId = p.Id,
                    Name = p.Name ?? "",
                    Abbreviation = p.Abbreviation,
                    Slug = p.Slug ?? ""
                });

            // Process Player Perspectives
            var allPlayerPerspectives = igdbGames
                .Where(g => g.PlayerPerspectives?.Any() == true)
                .SelectMany(g => g.PlayerPerspectives)
                .GroupBy(pp => pp.Id)
                .Select(pp => pp.First())
                .ToList();
            await UpsertReferenceData(allPlayerPerspectives, context, context.PlayerPerspectives,
                pp => new PlayerPerspective
                {
                    IgdbId = pp.Id,
                    Name = pp.Name ?? "",
                    Slug = pp.Slug ?? ""
                });

            // Process Themes
            var allThemes = igdbGames
                .Where(g => g.Themes?.Count > 0)
                .SelectMany(g => g.Themes)
                .GroupBy(t => t.Id)
                .Select(t => t.First())
                .ToList();
            await UpsertReferenceData(allThemes, context, context.Themes,
                pp => new Theme
                {
                    IgdbId = pp.Id,
                    Name = pp.Name ?? "",
                    Slug = pp.Slug ?? ""
                });
        }

        private async Task UpsertReferenceData<TIgdb, TEntity>(
            List<TIgdb> igdbItems,
            ApplicationDbContext context,
            DbSet<TEntity> dbSet,
            Func<TIgdb, TEntity> createEntity)
            where TEntity : class, IHasIgdbId
            where TIgdb : IHasId
        {
            if (igdbItems.Count <= 0) return;

            var igdbIds = igdbItems.Select(i => i.Id).ToList();

            // Use a separate context to check existing entities to avoid concurrent issues
            HashSet<int> existingIgdbIds;
            using (var checkScope = _serviceProvider.CreateScope())
            {
                var checkContext = checkScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                existingIgdbIds = (await checkContext.Set<TEntity>()
                    .Where(e => igdbIds.Contains(e.IgdbId))
                    .Select(e => e.IgdbId)
                    .ToListAsync())
                    .ToHashSet();
            }
            // var existingEntities = await dbSet
            //     .Where(e => igdbIds.Contains(e.IgdbId))
            //     .ToListAsync();

            //var existingIds = existingEntities.Select(e => e.IgdbId).ToHashSet();
            var newEntities = igdbItems
                .Where(i => !existingIgdbIds.Contains(i.Id))
                .Select(createEntity)
                .ToList();

            if (newEntities.Count > 0)
            {
                try
                {
                    await dbSet.AddRangeAsync(newEntities);
                    // IMPORTANT: Don't call SaveChangesAsync here - let the main transaction handle it
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error adding {EntityType} entities. This might be expected due to concurrent processing.", typeof(TEntity).Name);

                    // Clear any problematic entities from the context
                    foreach (var entity in newEntities)
                    {
                        context.Entry(entity).State = EntityState.Detached;
                    }
                }
                //await context.SaveChangesAsync();
            }
        }

        private async Task BulkInsertAssociationsAsync(ApplicationDbContext context, List<IgdbGame> igdbGames)
        {
            // Use cache for game ID mapping instead of querying database
            var gameIds = new Dictionary<int, Guid>();
            foreach (var game in igdbGames)
            {
                if (_gameIdCache.TryGetValue(game.Id, out var databaseId))
                {
                    gameIds[game.Id] = databaseId;
                }
            }

            // Process Game Genres
            await ProcessGameGenreAssociationsAsync(context, igdbGames, gameIds);
            // Process Game Age Ratings
            await ProcessGameAgeRatingAssociationsAsync(context, igdbGames, gameIds);
            // Process Game Franchises
            await ProcessGameFranchiseAssociationsAsync(context, igdbGames, gameIds);
            // Process Game Modes
            await ProcessGameModeAssociationsAsync(context, igdbGames, gameIds);
            // Process Game Companies
            await ProcessGameCompanyAssociationsAsync(context, igdbGames, gameIds);
            // Process Game Platforms
            await ProcessGamePlatformAssociationsAsync(context, igdbGames, gameIds);
            // Process Game Player Perspectives
            await ProcessGamePlayerPerspectiveAssociationsAsync(context, igdbGames, gameIds);
            // Process Game Themes
            await ProcessGameThemesAssociationAsync(context, igdbGames, gameIds);
        }

        private async Task ProcessGameGenreAssociationsAsync(
            ApplicationDbContext context,
            List<IgdbGame> igdbGames,
            Dictionary<int, Guid> gameIds)
        {
            var allGenreIgdbIds = igdbGames
                .Where(g => g.Genres?.Count > 0)
                .SelectMany(g => g.Genres.Select(genre => genre.Id))
                .Distinct()
                .ToList();

            if (allGenreIgdbIds.Count == 0) return;

            // Use cache for genre ID mapping instead of querying database
            var genreIds = new Dictionary<int, Guid>();
            foreach (var igdbId in allGenreIgdbIds)
            {
                if (_genreCache.TryGetValue(igdbId, out var genreId))
                {
                    genreIds[igdbId] = genreId;
                }
            }

            var gameGenres = new List<GameGenre>();
            foreach (var igdbGame in igdbGames.Where(g => g.Genres?.Count > 0))
            {
                if (!gameIds.TryGetValue(igdbGame.Id, out var gameId) || igdbGame.Genres == null || igdbGame.Genres.Count <= 0)
                    continue;

                foreach (var genre in igdbGame.Genres)
                {
                    if (genreIds.TryGetValue(genre.Id, out var genreId))
                    {
                        gameGenres.Add(new GameGenre
                        {
                            GameId = gameId,
                            GenreId = genreId
                        });
                    }
                }
            }

            if (gameGenres.Count == 0) return;

            try
            {
                var uniqueGameGenres = gameGenres
                    .GroupBy(gc => new { gc.GameId, gc.GenreId })
                    .Select(g => g.First())
                    .ToList();

                // Get existing associations to avoid duplicates
                var targetGameIds = uniqueGameGenres.Select(gg => gg.GameId).Distinct().ToList();
                var tagetGenreIds = uniqueGameGenres.Select(gg => gg.GenreId).Distinct().ToList();
                var existingAssociations = await context.GameGenres
                    .Where(gg => targetGameIds.Contains(gg.GameId) && tagetGenreIds.Contains(gg.GenreId))
                    .Select(gg => new { gg.GameId, gg.GenreId })
                    .ToListAsync();

                var associationsToAdd = uniqueGameGenres
                    .Where(gg => !existingAssociations.Any(existing =>
                        existing.GameId == gg.GameId && existing.GenreId == gg.GenreId))
                    .ToList();

                if (associationsToAdd.Count > 0)
                {
                    await context.GameGenres.AddRangeAsync(associationsToAdd);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error executing bulk upsert for GameGenre associations");
                throw;
            }
        }

        private async Task ProcessGameAgeRatingAssociationsAsync(
            ApplicationDbContext context,
            List<IgdbGame> igdbGames,
            Dictionary<int, Guid> gameIds)
        {
            var allAgeRatingIgdbIds = igdbGames
                .Where(g => g.AgeRatings?.Count > 0)
                .SelectMany(g => g.AgeRatings.Select(ar => ar.Id))
                .Distinct()
                .ToList();

            if (allAgeRatingIgdbIds.Count == 0) return;

            // Use cache for age rating ID mapping instead of querying database
            var ageRatingIds = new Dictionary<int, Guid>();
            foreach (var igdbId in allAgeRatingIgdbIds)
            {
                if (_ageRatingCache.TryGetValue(igdbId, out var ageRatingId))
                {
                    ageRatingIds[igdbId] = ageRatingId;
                }
            }

            var gameAgeRatings = new List<GameAgeRating>();
            foreach (var igdbGame in igdbGames.Where(g => g.AgeRatings?.Count > 0))
            {
                if (!gameIds.TryGetValue(igdbGame.Id, out var gameId) || igdbGame.AgeRatings == null || igdbGame.AgeRatings.Count <= 0)
                    continue;

                foreach (var ageRating in igdbGame.AgeRatings)
                {
                    if (ageRatingIds.TryGetValue(ageRating.Id, out var ageRatingId))
                    {
                        gameAgeRatings.Add(new GameAgeRating
                        {
                            GameId = gameId,
                            AgeRatingId = ageRatingId
                        });
                    }
                }
            }

            if (gameAgeRatings.Count == 0) return;

            try
            {
                var uniqueGameAgeRatings = gameAgeRatings
                    .GroupBy(gar => new { gar.GameId, gar.AgeRatingId })
                    .Select(g => g.First())
                    .ToList();

                // Get existing associations to avoid duplicates
                var targetGameIds = uniqueGameAgeRatings.Select(gar => gar.GameId).Distinct().ToList();
                var targetAgeRatingIds = uniqueGameAgeRatings.Select(gar => gar.AgeRatingId).Distinct().ToList();
                var existingAssociations = await context.GameAgeRatings
                    .Where(gar => targetGameIds.Contains(gar.GameId) && targetAgeRatingIds.Contains(gar.AgeRatingId))
                    .Select(gar => new { gar.GameId, gar.AgeRatingId })
                    .ToListAsync();

                var associationsToAdd = uniqueGameAgeRatings
                    .Where(gar => !existingAssociations.Any(existing =>
                        existing.GameId == gar.GameId && existing.AgeRatingId == gar.AgeRatingId))
                    .ToList();

                if (associationsToAdd.Count > 0)
                {
                    await context.GameAgeRatings.AddRangeAsync(associationsToAdd);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error executing bulk upsert for GameAgeRating associations");
                throw;
            }

            // Save changes
            //await context.SaveChangesAsync();
        }

        private async Task ProcessGameFranchiseAssociationsAsync(
            ApplicationDbContext context,
            List<IgdbGame> igdbGames,
            Dictionary<int, Guid> gameIds)
        {
            var allFranchiseIgdbIds = igdbGames
                .Where(g => g.Franchises?.Count > 0)
                .SelectMany(g => g.Franchises.Select(f => f.Id))
                .Distinct()
                .ToList();

            if (allFranchiseIgdbIds.Count == 0) return;

            // Use cache for franchise ID mapping instead of querying database
            var franchiseIds = new Dictionary<int, Guid>();
            foreach (var igdbId in allFranchiseIgdbIds)
            {
                if (_franchiseCache.TryGetValue(igdbId, out var franchiseId))
                {
                    franchiseIds[igdbId] = franchiseId;
                }
            }
            var gameFranchises = new List<GameFranchise>();
            foreach (var igdbGame in igdbGames.Where(g => g.Franchises?.Count > 0))
            {
                if (!gameIds.TryGetValue(igdbGame.Id, out var gameId) || igdbGame.Franchises == null || igdbGame.Franchises.Count <= 0)
                    continue;

                foreach (var franchise in igdbGame.Franchises)
                {
                    if (franchiseIds.TryGetValue(franchise.Id, out var franchiseId))
                    {
                        gameFranchises.Add(new GameFranchise
                        {
                            GameId = gameId,
                            FranchiseId = franchiseId
                        });
                    }
                }
            }

            if (gameFranchises.Count == 0) return;

            try
            {
                var uniqueGameFranchises = gameFranchises
                    .GroupBy(gf => new { gf.GameId, gf.FranchiseId })
                    .Select(g => g.First())
                    .ToList();

                // Get existing associations to avoid duplicates
                var targetGameIds = uniqueGameFranchises.Select(gf => gf.GameId).Distinct().ToList();
                var targetFranchiseIds = uniqueGameFranchises.Select(gf => gf.FranchiseId).Distinct().ToList();
                var existingAssociations = await context.GameFranchises
                    .Where(gf => targetGameIds.Contains(gf.GameId) && targetFranchiseIds.Contains(gf.FranchiseId))
                    .Select(gf => new { gf.GameId, gf.FranchiseId })
                    .ToListAsync();

                var associationsToAdd = uniqueGameFranchises
                    .Where(gf => !existingAssociations.Any(existing =>
                        existing.GameId == gf.GameId && existing.FranchiseId == gf.FranchiseId))
                    .ToList();

                if (associationsToAdd.Count > 0)
                {
                    await context.GameFranchises.AddRangeAsync(associationsToAdd);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error executing bulk upsert for GameFranchise associations");
                throw;
            }
            // Save changes
            //await context.SaveChangesAsync();
        }

        private async Task ProcessGameModeAssociationsAsync(
            ApplicationDbContext context,
            List<IgdbGame> igdbGames,
            Dictionary<int, Guid> gameIds)
        {
            var allGameModeIgdbIds = igdbGames
                .Where(g => g.GameModes?.Count > 0)
                .SelectMany(g => g.GameModes.Select(gm => gm.Id))
                .Distinct()
                .ToList();

            if (allGameModeIgdbIds.Count == 0) return;

            // Use cache for game mode ID mapping instead of querying database
            var gameModeIds = new Dictionary<int, Guid>();
            foreach (var igdbId in allGameModeIgdbIds)
            {
                if (_gameModeCache.TryGetValue(igdbId, out var gameModeId))
                {
                    gameModeIds[igdbId] = gameModeId;
                }
            }
            var gameModes = new List<GameModeGame>();
            foreach (var igdbGame in igdbGames.Where(g => g.GameModes?.Count > 0))
            {
                if (!gameIds.TryGetValue(igdbGame.Id, out var gameId) || igdbGame.GameModes == null || igdbGame.GameModes.Count <= 0)
                    continue;

                foreach (var gameMode in igdbGame.GameModes)
                {
                    if (gameModeIds.TryGetValue(gameMode.Id, out var gameModeId))
                    {
                        gameModes.Add(new GameModeGame
                        {
                            GameId = gameId,
                            GameModeId = gameModeId
                        });
                    }
                }
            }

            if (gameModes.Count == 0) return;

            try
            {
                var uniqueGameModes = gameModes
                    .GroupBy(gm => new { gm.GameId, gm.GameModeId })
                    .Select(g => g.First())
                    .ToList();

                // Get existing associations to avoid duplicates
                var targetGameIds = uniqueGameModes.Select(gm => gm.GameId).Distinct().ToList();
                var targetGameModeIds = uniqueGameModes.Select(gm => gm.GameModeId).Distinct().ToList();
                var existingAssociations = await context.GameModeGames
                    .Where(gm => targetGameIds.Contains(gm.GameId) && targetGameModeIds.Contains(gm.GameModeId))
                    .Select(gm => new { gm.GameId, gm.GameModeId })
                    .ToListAsync();

                var associationsToAdd = uniqueGameModes
                    .Where(gm => !existingAssociations.Any(existing =>
                        existing.GameId == gm.GameId && existing.GameModeId == gm.GameModeId))
                    .ToList();

                if (associationsToAdd.Count > 0)
                {
                    await context.GameModeGames.AddRangeAsync(associationsToAdd);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error executing bulk upsert for GameMode associations");
                throw;
            }
            // Save changes
            //await context.SaveChangesAsync();
        }

        private async Task ProcessGameCompanyAssociationsAsync(
            ApplicationDbContext context,
            List<IgdbGame> igdbGames,
            Dictionary<int, Guid> gameIds)
        {
            var allCompanyIgdbIds = igdbGames
                .Where(g => g.InvolvedCompanies?.Count > 0)
                .SelectMany(g => g.InvolvedCompanies.Select(ic => ic.Company.Id))
                .Distinct()
                .ToList();

            if (allCompanyIgdbIds.Count == 0) return;

            // Use cache for company ID mapping instead of querying database
            var companyIds = new Dictionary<int, Guid>();
            foreach (var igdbId in allCompanyIgdbIds)
            {
                if (_companyCache.TryGetValue(igdbId, out var companyId))
                {
                    companyIds[igdbId] = companyId;
                }
            }
            var gameCompanies = new List<GameCompany>();
            foreach (var igdbGame in igdbGames.Where(g => g.InvolvedCompanies?.Count > 0))
            {
                if (!gameIds.TryGetValue(igdbGame.Id, out var gameId)
                    || igdbGame.InvolvedCompanies == null
                    || igdbGame.InvolvedCompanies.Count <= 0)
                    continue;

                foreach (var involvedCompany in igdbGame.InvolvedCompanies)
                {
                    if (companyIds.TryGetValue(involvedCompany.Company.Id, out var companyId))
                    {
                        gameCompanies.Add(new GameCompany
                        {
                            GameId = gameId,
                            CompanyId = companyId,
                            IgdbId = involvedCompany.Id,
                            Developer = involvedCompany.Developer ?? false,
                            Publisher = involvedCompany.Publisher ?? false,
                            Porting = involvedCompany.Porting ?? false,
                            Supporting = involvedCompany.Supporting ?? false
                        });
                    }
                }
            }

            if (gameCompanies.Count == 0) return;

            // Remove existing associations for these games - group only by the actual primary key
            var uniqueGameCompanies = gameCompanies
                .GroupBy(gc => new { gc.GameId, gc.CompanyId })
                .Select(g => g.First())
                .ToList();

            try
            {
                // Get existing associations to avoid duplicates (GameCompany has composite key of GameId, CompanyId)
                var targetGameIds = uniqueGameCompanies.Select(gc => gc.GameId).Distinct().ToList();
                var targetCompanyIds = uniqueGameCompanies.Select(gc => gc.CompanyId).Distinct().ToList();
                var existingAssociations = await context.GameCompanies
                    .Where(gc => targetGameIds.Contains(gc.GameId) && targetCompanyIds.Contains(gc.CompanyId))
                    .Select(gc => new { gc.GameId, gc.CompanyId })
                    .ToListAsync();

                var associationsToAdd = uniqueGameCompanies
                    .Where(gc => !existingAssociations.Any(existing =>
                        existing.GameId == gc.GameId && existing.CompanyId == gc.CompanyId))
                    .ToList();

                if (associationsToAdd.Count > 0)
                {
                    await context.GameCompanies.AddRangeAsync(associationsToAdd);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error executing bulk upsert for GameCompany associations");
                throw;
            }
            // Save changes
            //await context.SaveChangesAsync();
        }

        private async Task ProcessGamePlatformAssociationsAsync(
            ApplicationDbContext context,
            List<IgdbGame> igdbGames,
            Dictionary<int, Guid> gameIds)
        {
            var allPlatformIgdbIds = igdbGames
                .Where(g => g.Platforms?.Count > 0)
                .SelectMany(g => g.Platforms.Select(p => p.Id))
                .Distinct()
                .ToList();

            if (allPlatformIgdbIds.Count == 0) return;

            // Use cache for platform ID mapping instead of querying database
            var platformIds = new Dictionary<int, Guid>();
            foreach (var igdbId in allPlatformIgdbIds)
            {
                if (_platformCache.TryGetValue(igdbId, out var platformId))
                {
                    platformIds[igdbId] = platformId;
                }
            }
            var gamePlatforms = new List<GamePlatform>();
            foreach (var igdbGame in igdbGames.Where(g => g.Platforms?.Count > 0))
            {
                if (!gameIds.TryGetValue(igdbGame.Id, out var gameId)
                    || igdbGame.Platforms == null
                    || igdbGame.Platforms.Count <= 0)
                    continue;

                foreach (var platform in igdbGame.Platforms)
                {
                    if (platformIds.TryGetValue(platform.Id, out var platformId))
                    {
                        gamePlatforms.Add(new GamePlatform
                        {
                            GameId = gameId,
                            PlatformId = platformId
                        });
                    }
                }
            }

            if (gamePlatforms.Count == 0) return;

            try
            {
                var uniqueGamePlatforms = gamePlatforms
                    .GroupBy(gp => new { gp.GameId, gp.PlatformId })
                    .Select(g => g.First())
                    .ToList();

                // Get existing associations to avoid duplicates
                var targetGameIds = uniqueGamePlatforms.Select(gp => gp.GameId).Distinct().ToList();
                var targetPlatformIds = uniqueGamePlatforms.Select(gp => gp.PlatformId).Distinct().ToList();
                var existingAssociations = await context.GamePlatforms
                    .Where(gp => targetGameIds.Contains(gp.GameId) && targetPlatformIds.Contains(gp.PlatformId))
                    .Select(gp => new { gp.GameId, gp.PlatformId })
                    .ToListAsync();

                var associationsToAdd = uniqueGamePlatforms
                    .Where(gp => !existingAssociations.Any(existing =>
                        existing.GameId == gp.GameId && existing.PlatformId == gp.PlatformId))
                    .ToList();

                if (associationsToAdd.Count > 0)
                {
                    await context.GamePlatforms.AddRangeAsync(associationsToAdd);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error executing bulk upsert for GamePlatform associations");
                throw;
            }
            // Save changes
            //await context.SaveChangesAsync();
        }

        private async Task ProcessGamePlayerPerspectiveAssociationsAsync(
            ApplicationDbContext context,
            List<IgdbGame> igdbGames,
            Dictionary<int, Guid> gameIds)
        {
            var allPlayerPerspectiveIgdbIds = igdbGames
                .Where(g => g.PlayerPerspectives?.Count > 0)
                .SelectMany(g => g.PlayerPerspectives.Select(pp => pp.Id))
                .Distinct()
                .ToList();

            if (allPlayerPerspectiveIgdbIds.Count == 0) return;

            // Use cache for player perspective ID mapping instead of querying database
            var playerPerspectiveIds = new Dictionary<int, Guid>();
            foreach (var igdbId in allPlayerPerspectiveIgdbIds)
            {
                if (_playerPerspectiveCache.TryGetValue(igdbId, out var perspectiveId))
                {
                    playerPerspectiveIds[igdbId] = perspectiveId;
                }
            }
            var gamePlayerPerspectives = new List<GamePlayerPerspective>();
            foreach (var igdbGame in igdbGames.Where(g => g.PlayerPerspectives?.Count > 0))
            {
                if (!gameIds.TryGetValue(igdbGame.Id, out var gameId)
                    || igdbGame.PlayerPerspectives == null
                    || igdbGame.PlayerPerspectives.Count <= 0)
                    continue;

                foreach (var playerPerspective in igdbGame.PlayerPerspectives)
                {
                    if (playerPerspectiveIds.TryGetValue(playerPerspective.Id, out var playerPerspectiveId))
                    {
                        gamePlayerPerspectives.Add(new GamePlayerPerspective
                        {
                            GameId = gameId,
                            PlayerPerspectiveId = playerPerspectiveId
                        });
                    }
                }
            }

            if (gamePlayerPerspectives.Count == 0) return;

            try
            {
                var uniqueGamePlayerPerspectives = gamePlayerPerspectives
                    .GroupBy(gpp => new { gpp.GameId, gpp.PlayerPerspectiveId })
                    .Select(g => g.First())
                    .ToList();

                // Get existing associations to avoid duplicates
                var targetGameIds = uniqueGamePlayerPerspectives.Select(gpp => gpp.GameId).Distinct().ToList();
                var targetPlayerPerspectiveIds = uniqueGamePlayerPerspectives.Select(gpp => gpp.PlayerPerspectiveId).Distinct().ToList();
                var existingAssociations = await context.GamePlayerPerspectives
                    .Where(gpp => targetGameIds.Contains(gpp.GameId) && targetPlayerPerspectiveIds.Contains(gpp.PlayerPerspectiveId))
                    .Select(gpp => new { gpp.GameId, gpp.PlayerPerspectiveId })
                    .ToListAsync();

                var associationsToAdd = uniqueGamePlayerPerspectives
                    .Where(gpp => !existingAssociations.Any(existing =>
                        existing.GameId == gpp.GameId && existing.PlayerPerspectiveId == gpp.PlayerPerspectiveId))
                    .ToList();

                if (associationsToAdd.Count > 0)
                {
                    await context.GamePlayerPerspectives.AddRangeAsync(associationsToAdd);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error executing bulk upsert for GamePlayerPerspective associations");
                throw;
            }
            // Save changes
            //await context.SaveChangesAsync();
        }

        private async Task ProcessGameThemesAssociationAsync(
            ApplicationDbContext context,
            List<IgdbGame> igdbGames,
            Dictionary<int, Guid> gameIds)
        {
            var allGameThemeIgdbIds = igdbGames
                .Where(g => g.Themes?.Count > 0)
                .SelectMany(g => g.Themes.Select(theme => theme.Id))
                .Distinct()
                .ToList();

            if (allGameThemeIgdbIds.Count == 0) return;

            // Use cache for theme ID mapping instead of querying database
            var themeIds = new Dictionary<int, Guid>();
            foreach (var igdbId in allGameThemeIgdbIds)
            {
                if (_themeCache.TryGetValue(igdbId, out var themeId))
                {
                    themeIds[igdbId] = themeId;
                }
            }

            var gameThemeAssociations = new List<GameTheme>();
            foreach (var igdbGame in igdbGames.Where(g => g.Themes?.Count > 0))
            {
                if (!gameIds.TryGetValue(igdbGame.Id, out var gameId)
                    || igdbGame.Themes == null
                    || igdbGame.Themes.Count <= 0)
                    continue;

                foreach (var theme in igdbGame.Themes)
                {
                    if (themeIds.TryGetValue(theme.Id, out var themeId))
                    {
                        gameThemeAssociations.Add(new GameTheme
                        {
                            GameId = gameId,
                            ThemeId = themeId
                        });
                    }
                }
            }

            if (gameThemeAssociations.Count == 0) return;

            try
            {
                var uniqueAssociations = gameThemeAssociations
                    .GroupBy(a => new { a.GameId, a.ThemeId })
                    .Select(g => g.First())
                    .ToList();

                // Get existing associations to avoid duplicates
                var targetGameIds = uniqueAssociations.Select(a => a.GameId).Distinct().ToList();
                var targetThemeIds = uniqueAssociations.Select(a => a.ThemeId).Distinct().ToList();
                var existingAssociations = await context.GameThemes
                    .Where(a => targetGameIds.Contains(a.GameId) && targetThemeIds.Contains(a.ThemeId))
                    .Select(a => new { a.GameId, a.ThemeId })
                    .ToListAsync();

                var associationsToAdd = uniqueAssociations
                    .Where(a => !existingAssociations.Any(existing =>
                        existing.GameId == a.GameId && existing.ThemeId == a.ThemeId))
                    .ToList();

                if (associationsToAdd.Count > 0)
                {
                    await context.GameThemes.AddRangeAsync(associationsToAdd);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error executing bulk upsert for GameTheme associations");
                throw;
            }
        }

        private async Task ProcessAllRelationshipsAsync()
        {
            _logger.LogInformation("Processing game relationships...");

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Process relationships in batches to avoid memory issues
            const int relationshipBatchSize = 1000;
            var offset = 0;

            while (true)
            {
                var games = await context.Games
                    .Where(g => g.IgdbId > 0)
                    .Skip(offset)
                    .Take(relationshipBatchSize)
                    .ToListAsync();

                if (games.Count == 0) break;

                // Fetch relationship data for these games from IGDB
                await ProcessRelationshipsBatch(games);

                offset += relationshipBatchSize;
            }
        }

        private async Task ProcessRelationshipsBatch(List<Backend.Models.Game.Game> games)
        {
            // Implementation for processing relationships
            // This would fetch relationship data from IGDB and insert into relationship tables
            // Similar to the previous approach but optimized for bulk operations
        }

        private async Task ProcessGameRelationshipsAsync(
            ApplicationDbContext context,
            List<IgdbGame> igdbGames)
        {
            // Use cache for game ID mapping instead of querying database
            var gameIds = new Dictionary<int, Guid>();
            foreach (var game in igdbGames)
            {
                if (_gameIdCache.TryGetValue(game.Id, out var databaseId))
                {
                    gameIds[game.Id] = databaseId;
                }
            }

            // Process DLC relationships
            await ProcessRelationship<GameDlc>(context, igdbGames, gameIds,
                game => game.Dlcs,
                (parentId, childId) => new GameDlc { ParentGameId = parentId, DlcGameId = childId });

            // Process Expansion relationships
            await ProcessRelationship<GameExpansion>(context, igdbGames, gameIds,
                game => game.Expansions,
                (parentId, childId) => new GameExpansion { ParentGameId = parentId, ExpansionGameId = childId });

            // Process Port relationships
            await ProcessRelationship<GamePort>(context, igdbGames, gameIds,
                game => game.Ports,
                (parentId, childId) => new GamePort { OriginalGameId = parentId, PortGameId = childId });

            // Process Remake relationships
            await ProcessRelationship<GameRemake>(context, igdbGames, gameIds,
                game => game.Remakes,
                (parentId, childId) => new GameRemake { OriginalGameId = parentId, RemakeGameId = childId });

            // Process Remaster relationships
            await ProcessRelationship<GameRemaster>(context, igdbGames, gameIds,
                game => game.Remasters,
                (parentId, childId) => new GameRemaster { OriginalGameId = parentId, RemasterGameId = childId });

            // Process Similar Games
            await ProcessRelationship<SimilarGame>(context, igdbGames, gameIds,
                game => game.SimilarGames,
                (gameId, similarId) => new SimilarGame { GameId = gameId, SimilarGameId = similarId });

            //await context.SaveChangesAsync();
        }

        private async Task ProcessRelationship<TRelationship>(
            ApplicationDbContext context,
            List<IgdbGame> igdbGames,
            Dictionary<int, Guid> gameIds,
            Func<IgdbGame, List<int>?> getRelatedIds,
            Func<Guid, Guid, TRelationship> createRelationship)
            where TRelationship : class
        {
            var relationships = new List<TRelationship>();
            var dbSet = context.Set<TRelationship>();

            foreach (var igdbGame in igdbGames)
            {
                var relatedIds = getRelatedIds(igdbGame);
                if (relatedIds == null || relatedIds.Count <= 0 || !gameIds.TryGetValue(igdbGame.Id, out var gameId))
                    continue;

                // Find which related games exist in our database using cache
                var existingRelatedGameIds = new List<Guid>();
                foreach (var relatedId in relatedIds)
                {
                    if (_gameIdCache.TryGetValue(relatedId, out var relatedGameId))
                    {
                        existingRelatedGameIds.Add(relatedGameId);
                    }
                }

                foreach (var relatedGameId in existingRelatedGameIds)
                {
                    relationships.Add(createRelationship(gameId, relatedGameId));
                }
            }

            if (relationships.Count > 0)
            {
                // Remove duplicates within the current batch and against existing relationships
                var uniqueRelationships = new List<TRelationship>();
                var relationshipTypeName = typeof(TRelationship).Name;

                foreach (var relationship in relationships)
                {
                    // Create a unique key based on the relationship properties
                    string key = CreateRelationshipKey(relationship, relationshipTypeName);

                    // Check against both existing relationships and current batch
                    if (!_existingRelationships.Contains(key))
                    {
                        uniqueRelationships.Add(relationship);
                        _existingRelationships.Add(key); // Update cache to prevent future duplicates
                    }
                }

                // Add relationships to context - they will be saved later with SaveChanges()
                if (uniqueRelationships.Count > 0)
                {
                    _logger.LogDebug("Adding {Count} unique {Type} relationships to context", uniqueRelationships.Count, relationshipTypeName);
                    await dbSet.AddRangeAsync(uniqueRelationships);
                }
                else
                {
                    _logger.LogDebug("No unique {Type} relationships to add (all were duplicates)", relationshipTypeName);
                }
            }
        }

        private static string CreateRelationshipKey<TRelationship>(TRelationship relationship, string typeName)
        {
            // Use reflection to get the properties and create a unique key
            var props = typeof(TRelationship).GetProperties()
                .Where(p => p.Name.EndsWith("GameId") || p.Name.EndsWith("Id"))
                .OrderBy(p => p.Name)
                .Select(p => p.GetValue(relationship)?.ToString() ?? "")
                .ToArray();

            return $"{typeName}|{string.Join("|", props)}";
        }

        private HttpClient CreateConfiguredHttpClient()
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.BaseAddress = new Uri(_igdbSettings.BaseUrl);
            httpClient.DefaultRequestHeaders.Add("Client-ID", _igdbSettings.ClientId);
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_igdbSettings.ClientSecret}");
            httpClient.Timeout = TimeSpan.FromMinutes(5);

            return httpClient;
        }

        private async Task<int> GetTotalGameCountAsync()
        {
            using var httpClient = CreateConfiguredHttpClient();

            var countQuery = @"
            fields id;
            where version_parent = null;
            limit 1;";

            var content = new StringContent(countQuery, Encoding.UTF8, "text/plain");
            var response = await httpClient.PostAsync("games/count", content);

            response.EnsureSuccessStatusCode();
            var countResponse = await response.Content.ReadAsStringAsync();
            var countResult = JsonSerializer.Deserialize<CountResponse>(
                countResponse,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                });

            return countResult?.Count ?? 0;
        }
        
        private async Task<int> GetTotalCountAsync(string endpoint)
        {
            using var httpClient = CreateConfiguredHttpClient();

            var countQuery = @"
            fields id;";

            var content = new StringContent(countQuery, Encoding.UTF8, "text/plain");
            var response = await httpClient.PostAsync(endpoint, content);

            response.EnsureSuccessStatusCode();
            var countResponse = await response.Content.ReadAsStringAsync();
            var countResult = JsonSerializer.Deserialize<CountResponse>(
                countResponse,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                });

            return countResult?.Count ?? 0;
        }
    }

    public class CountResponse
    {
        public int Count { get; set; }
    }

    // Console Application for fastest import
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            using var scope = host.Services.CreateScope();

            // Test configuration loading
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var connectionString = config.GetConnectionString("mysqldb");
            Console.WriteLine($"Connection String: {connectionString ?? "NOT FOUND"}");

            var igdbSettings = scope.ServiceProvider.GetRequiredService<IOptions<IgdbSettings>>().Value;
            Console.WriteLine($"IGDB Settings loaded: {igdbSettings != null}");

            var importService = scope.ServiceProvider.GetRequiredService<OptimizedIgdbImportService>();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Console.WriteLine("Testing database connection...");
            await context.Database.CanConnectAsync();
            Console.WriteLine("✓ Database connection successful\n");

            try
            {
                await importService.NonConcurrentImportAsync();
                //await importService.ImportAllGamesOptimizedAsync();
                Console.WriteLine("Import completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Import failed: {ex.Message}");
                // Display inner exceptions to see the real database error
                var innerEx = ex.InnerException;
                while (innerEx != null)
                {
                    Console.WriteLine($"Inner Exception: {innerEx.Message}");
                    innerEx = innerEx.InnerException;
                }
                Console.WriteLine(ex.StackTrace);
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
                {
                    // Load configuration from appsettings.json and environment variables
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                        .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true)
                        .AddUserSecrets<Backend.Models.Game.Game>()
                        .AddEnvironmentVariables();
                })
                .ConfigureServices((context, services) =>
                {
                    // Configure services for console app
                    services.Configure<IgdbSettings>(
                        context.Configuration.GetSection(IgdbSettings.SectionName));

                    // Configure DbContext (only once!)
                    var connectionString = context.Configuration.GetConnectionString("mysqldb");
                    if (string.IsNullOrEmpty(connectionString))
                    {
                        throw new InvalidOperationException("MySQL connection string 'mysqldb' not found in configuration.");
                    }
                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
                    });

                    // HTTP Client
                    services.AddHttpClient<OptimizedIgdbImportService>(client =>
                    {
                        client.Timeout = TimeSpan.FromMinutes(5);
                    });
                    services.AddScoped<OptimizedIgdbImportService>();

                    services.AddLogging(builder =>
                    {
                        builder.AddConsole();
                        builder.SetMinimumLevel(LogLevel.Information);
                    });

                    // Logging
                    services.AddLogging(builder =>
                    {
                        builder.AddConsole(options =>
                        {
                            options.IncludeScopes = true;
                            options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
                        });
                    });
                });
    }
}

