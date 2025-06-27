using System.Text;
using System.Text.Json;
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

        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true,
            //WriteIndented = true
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

                // Step 2: Process in parallel with controlled concurrency
                var batchTasks = new List<Task>();
                var maxConcurrentBatches = 4; // Adjust based on your database capacity
                var semaphore = new SemaphoreSlim(maxConcurrentBatches, maxConcurrentBatches);

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

        private async Task ProcessBatchWithSemaphoreAsync(SemaphoreSlim semaphore, int offset, int batchIndex, int totalBatches)
        {
            await semaphore.WaitAsync();
            try
            {
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
            finally
            {
                semaphore.Release();
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
                dlcs, expansions, ports, remakes, remasters, similar_games;
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
                await BulkInsertGamesAsync(context, igdbGames);
                await BulkInsertReferenceDataAsync(context, igdbGames);
                await BulkInsertAssociationsAsync(context, igdbGames);
                await ProcessGameRelationshipsAsync(context, igdbGames);

                await context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task BulkInsertGamesAsync(ApplicationDbContext context, List<IgdbGame> igdbGames)
        {
            // Step 1: Extract all unique GameType IGDB IDs from the games
            var gameTypeIgdbIds = igdbGames
                .Where(g => g.GameType != null)
                .Select(g => g.GameType.Id)
                .Distinct()
                .ToList();

            // Get all GameType mappings from database
            var gameTypeIdMappings = await context.GameTypes
                .Where(gt => gameTypeIgdbIds.Contains(gt.IgdbId))
                .ToDictionaryAsync(gt => gt.IgdbId, gt => gt.Id);

            // Step 2: Get existing GameTypes from database
            var existingGameTypes = await context.GameTypes
                .Where(gt => gameTypeIgdbIds.Contains(gt.IgdbId))
                .ToDictionaryAsync(gt => gt.IgdbId, gt => gt.Id);

            // Step 3: Create missing GameTypes
            var missingGameTypeIds = gameTypeIgdbIds
                .Where(id => !existingGameTypes.ContainsKey(id))
                .ToList();

            if (missingGameTypeIds.Count > 0)
            {
                Console.WriteLine($"Creating {missingGameTypeIds.Count} missing GameTypes...");

                var newGameTypes = missingGameTypeIds.Select(igdbId => new GameType
                {
                    IgdbId = igdbId,
                    Type = igdbGames.Select(g => g.GameType)
                        .FirstOrDefault(gt => gt != null && gt.Id == igdbId)?.Type ?? ""
                }).ToList();

                await context.GameTypes.AddRangeAsync(newGameTypes);
                //await context.SaveChangesAsync();

                // Update the lookup dictionary with newly created GameTypes
                foreach (var gameType in newGameTypes)
                {
                    existingGameTypes[gameType.IgdbId] = gameType.Id;
                }

                Console.WriteLine($"✓ Created {newGameTypes.Count} GameTypes");
            }

            // Step 4: Prepare new games and update existing ones
            var igdbIds = igdbGames.Select(g => g.Id).ToList();
            var existingGames = await context.Games
                .Where(g => igdbIds.Contains(g.IgdbId))
                .ToListAsync();
            var existingGameIds = existingGames.Select(g => g.IgdbId).ToHashSet();

            var newGames = igdbGames
                .Where(ig => existingGameIds.Count <= 0 || !existingGameIds.Contains(ig.Id))
                .Select(ig => new Backend.Models.Game.Game
                {
                    IgdbId = ig.Id,
                    Name = ig.Name ?? "",
                    Slug = ig.Slug ?? "",
                    Storyline = ig.Storyline,
                    Summary = ig.Summary,
                    FirstReleaseDate = ig.FirstReleaseDate,
                    Hypes = ig.Hypes ?? 0,
                    Rating = ig.Rating.HasValue ? (decimal)ig.Rating.Value : null,
                    GameTypeId = existingGameTypes.TryGetValue(ig.GameType.Id, out var gameTypeId) ? gameTypeId : Guid.Empty,
                })
                .ToList();

            if (newGames.Count > 0)
            {
                // Use bulk insert if available (EF Core 7+)
                await context.Games.AddRangeAsync(newGames);
            }

            // Update existing games
            foreach (var existingGame in existingGames)
            {
                // if (existingGame.IgdbId == 0)
                //     continue;
                var igdbGame = igdbGames.FirstOrDefault(ig => ig.Id == existingGame.IgdbId);
                if (igdbGame == null) continue;
                existingGame.Name = igdbGame.Name ?? existingGame.Name;
                existingGame.Storyline = igdbGame.Storyline ?? existingGame.Storyline;
                existingGame.Summary = igdbGame.Summary ?? existingGame.Summary;
                existingGame.FirstReleaseDate = igdbGame.FirstReleaseDate ?? existingGame.FirstReleaseDate;
                existingGame.Hypes = igdbGame.Hypes ?? existingGame.Hypes;
                existingGame.Rating = igdbGame.Rating.HasValue ? (decimal)igdbGame.Rating.Value : existingGame.Rating;
                existingGame.GameTypeId = igdbGame.GameType != null &&
                    gameTypeIdMappings.TryGetValue(igdbGame.GameType.Id, out var gameTypeId) ?
                        gameTypeId : existingGame.GameTypeId;
            }

            //await context.SaveChangesAsync();
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
            var ageRatingCategoryIds = await context.AgeRatingCategories
                .Where(c => c.IgdbId > 0)
                .GroupBy(c => c.IgdbId)
                .ToDictionaryAsync(c => c.Key, c => c.First().Id);
            await UpsertReferenceData(allAgeRatings, context, context.AgeRatings,
                ar => new AgeRating
                {
                    IgdbId = ar.Id,
                    Name = ar.RatingCategory?.Rating ?? "",
                    Slug = ar.RatingCategory?.Rating ?? "",
                    AgeRatingCategoryId = ar.RatingCategory != null &&
                        ageRatingCategoryIds.TryGetValue(ar.RatingCategory.Id, out var categoryId) ?
                        categoryId : Guid.Empty
                });

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
                    Date = rd.Date.HasValue
                        ? (DateTime?)DateTimeOffset.FromUnixTimeSeconds(rd.Date.Value).UtcDateTime
                        : null,
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

            // // Process Game Types
            // var allGameTypes = igdbGames
            //     .Where(g => g.GameType != null)
            //     .Select(g => g.GameType)
            //     .GroupBy(gt => gt.Id)
            //     .Select(gt => gt.FirstOrDefault())
            //     .ToList();
            // await UpsertReferenceData(allGameTypes, context, context.GameTypes,
            //     gt => new GameType
            //     {
            //         IgdbId = gt.Id,
            //         Type = gt.Type ?? ""
            //     });

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
                    Country = c.Country.ToString(),
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
            var existingEntities = await dbSet
                .Where(e => igdbIds.Contains(e.IgdbId))
                .ToListAsync();

            var existingIds = existingEntities.Select(e => e.IgdbId).ToHashSet();
            var newEntities = igdbItems
                .Where(i => !existingIds.Contains(i.Id))
                .Select(createEntity)
                .ToList();

            if (newEntities.Count > 0)
            {
                await dbSet.AddRangeAsync(newEntities);
                await context.SaveChangesAsync();
            }
        }

        private async Task BulkInsertAssociationsAsync(ApplicationDbContext context, List<IgdbGame> igdbGames)
        {
            var currentGameIgdbIds = igdbGames.Select(g => g.Id).ToList();
            var gameIds = await context.Games
                .Where(g => currentGameIgdbIds.Contains(g.IgdbId))
                .GroupBy(g => g.IgdbId)
                .ToDictionaryAsync(g => g.Key, g => g.First().Id);

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

            var genreIds = await context.Genres
                .Where(g => allGenreIgdbIds.Contains(g.IgdbId))
                .ToDictionaryAsync(g => g.IgdbId, g => g.Id);

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

            var uniqueGameGenres = gameGenres
                .GroupBy(gc => new { gc.GameId, gc.GenreId })
                .Select(g => g.First()) // Take first occurrence of each unique combination
                .ToList();
            // Remove existing associations for these games
            var existingGameGenres = await context.GameGenres
                .Where(gg => gameIds.Values.Contains(gg.GameId))
                .ToListAsync();

            if (existingGameGenres != null && existingGameGenres.Count > 0)
            {
                context.GameGenres.RemoveRange(existingGameGenres);
            }
            // replace with new associations
            if (uniqueGameGenres.Count > 0)
            {
                await context.GameGenres.AddRangeAsync(uniqueGameGenres);
            }
            // Save changes
            //await context.SaveChangesAsync();
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

            var ageRatingIds = await context.AgeRatings
                .Where(ar => allAgeRatingIgdbIds.Contains(ar.IgdbId))
                .ToDictionaryAsync(ar => ar.IgdbId, ar => ar.Id);

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

            var uniqueGameAgeRatings = gameAgeRatings
                .GroupBy(gar => new { gar.GameId, gar.AgeRatingId })
                .Select(g => g.First()) // Take first occurrence of each unique combination
                .ToList();

            // Remove existing associations for these games
            var existingGameAgeRatings = await context.GameAgeRatings
                .Where(gar => gameIds.Values.Contains(gar.GameId))
                .ToListAsync();

            if (existingGameAgeRatings != null && existingGameAgeRatings.Count > 0)
            {
                context.GameAgeRatings.RemoveRange(existingGameAgeRatings);
            }

            // replace with new associations
            if (uniqueGameAgeRatings.Count > 0)
            {
                await context.GameAgeRatings.AddRangeAsync(uniqueGameAgeRatings);
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
            var franchiseIds = await context.Franchises
                .Where(f => allFranchiseIgdbIds.Contains(f.IgdbId))
                .ToDictionaryAsync(f => f.IgdbId, f => f.Id);
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

            var uniqueGameFranchises = gameFranchises
                .GroupBy(gf => new { gf.GameId, gf.FranchiseId })
                .Select(g => g.First()) // Take first occurrence of each unique combination
                .ToList();
            // Remove existing associations for these games
            var existingGameFranchises = await context.GameFranchises
                .Where(gf => gameIds.Values.Contains(gf.GameId))
                .ToListAsync();
            if (existingGameFranchises != null && existingGameFranchises.Count > 0)
            {
                context.GameFranchises.RemoveRange(existingGameFranchises);
            }
            // replace with new associations
            if (uniqueGameFranchises.Count > 0)
            {
                await context.GameFranchises.AddRangeAsync(uniqueGameFranchises);
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
            var gameModeIds = await context.GameModes
                .Where(gm => allGameModeIgdbIds.Contains(gm.IgdbId))
                .ToDictionaryAsync(gm => gm.IgdbId, gm => gm.Id);
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

            var uniqueGameModes = gameModes
                .GroupBy(gm => new { gm.GameId, gm.GameModeId })
                .Select(g => g.First()) // Take first occurrence of each unique combination
                .ToList();
            // Remove existing associations for these games
            var existingGameModes = await context.GameModeGames
                .Where(gmg => gameIds.Values.Contains(gmg.GameId))
                .ToListAsync();
            if (existingGameModes != null && existingGameModes.Count > 0)
            {
                context.GameModeGames.RemoveRange(existingGameModes);
            }
            // replace with new associations
            if (uniqueGameModes.Count > 0)
            {
                await context.GameModeGames.AddRangeAsync(uniqueGameModes);
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
            var companyIds = await context.Companies
                .Where(c => allCompanyIgdbIds.Contains(c.IgdbId))
                .ToDictionaryAsync(c => c.IgdbId, c => c.Id);
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
            // Remove existing associations for these games
            var uniqueGameCompanies = gameCompanies
                .GroupBy(gc => new { gc.GameId, gc.CompanyId })
                .Select(g => g.First()) // Take first occurrence of each unique combination
                .ToList();
            var existingGameCompanies = await context.GameCompanies
                .Where(gc => gameIds.Values.Contains(gc.GameId))
                .ToListAsync();
            if (existingGameCompanies != null && existingGameCompanies.Count > 0)
            {
                context.GameCompanies.RemoveRange(existingGameCompanies);
            }
            // replace with new associations
            if (uniqueGameCompanies.Count > 0)
            {
                await context.GameCompanies.AddRangeAsync(uniqueGameCompanies);
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
            var platformIds = await context.Platforms
                .Where(p => allPlatformIgdbIds.Contains(p.IgdbId))
                .ToDictionaryAsync(p => p.IgdbId, p => p.Id);
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
            var uniqueGamePlatforms = gamePlatforms
                .GroupBy(gp => new { gp.GameId, gp.PlatformId })
                .Select(g => g.First()) // Take first occurrence of each unique combination
                .ToList();
            // Remove existing associations for these games
            var existingGamePlatforms = await context.GamePlatforms
                .Where(gp => gameIds.Values.Contains(gp.GameId))
                .ToListAsync();
            if (existingGamePlatforms != null && existingGamePlatforms.Count > 0)
            {
                context.GamePlatforms.RemoveRange(existingGamePlatforms);
            }
            // replace with new associations
            if (uniqueGamePlatforms.Count > 0)
            {
                await context.GamePlatforms.AddRangeAsync(uniqueGamePlatforms);
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
            var playerPerspectiveIds = await context.PlayerPerspectives
                .Where(pp => allPlayerPerspectiveIgdbIds.Contains(pp.IgdbId))
                .ToDictionaryAsync(pp => pp.IgdbId, pp => pp.Id);
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

            var uniqueGamePlayerPerspectives = gamePlayerPerspectives
                .GroupBy(gpp => new { gpp.GameId, gpp.PlayerPerspectiveId })
                .Select(g => g.First()) // Take first occurrence of each unique combination
                .ToList();
            // Remove existing associations for these games
            var existingGamePlayerPerspectives = await context.GamePlayerPerspectives
                .Where(gpp => gameIds.Values.Contains(gpp.GameId))
                .ToListAsync();
            if (existingGamePlayerPerspectives.Count > 0)
            {
                context.GamePlayerPerspectives.RemoveRange(existingGamePlayerPerspectives);
            }
            // replace with new associations
            if (uniqueGamePlayerPerspectives.Count > 0)
            {
                await context.GamePlayerPerspectives.AddRangeAsync(uniqueGamePlayerPerspectives);
            }
            // Save changes
            //await context.SaveChangesAsync();
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

                if (games.Count > 0) break;

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
            var gameIds = await context.Games
                .Where(g => igdbGames.Select(ig => ig.Id).Contains(g.IgdbId))
                .ToDictionaryAsync(g => g.IgdbId, g => g.Id);

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

            foreach (var igdbGame in igdbGames)
            {
                var relatedIds = getRelatedIds(igdbGame);
                if (relatedIds?.Count <= 0 || !gameIds.TryGetValue(igdbGame.Id, out var gameId))
                    continue;

                // Find which related games exist in our database
                var existingRelatedGameIds = await context.Games
                    .Where(g => relatedIds.Contains(g.IgdbId))
                    .Select(g => g.Id)
                    .ToListAsync();

                foreach (var relatedGameId in existingRelatedGameIds)
                {
                    relationships.Add(createRelationship(gameId, relatedGameId));
                }
            }

            if (relationships.Count > 0)
            {
                var dbSet = context.Set<TRelationship>();
                await dbSet.AddRangeAsync(relationships);
            }
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
                await importService.ImportAllGamesOptimizedAsync();
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

