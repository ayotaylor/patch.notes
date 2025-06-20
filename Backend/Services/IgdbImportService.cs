// using System.Text;
// using System.Text.Json;
// using Backend.Data;
// using Backend.Models.Game.Associations;
// using Backend.Models.Game.ReferenceModels;
// using Backend.Models.Game.Relationships;
// using Microsoft.EntityFrameworkCore;

// public class IgdbImportService
// {
//     private readonly HttpClient _httpClient;
//     private readonly ApplicationDbContext _context;
//     private readonly string _clientId;
//     private readonly string _accessToken;
    
//     public IgdbImportService(HttpClient httpClient, ApplicationDbContext context, string clientId, string accessToken)
//     {
//         _httpClient = httpClient;
//         _context = context;
//         _clientId = clientId;
//         _accessToken = accessToken;
        
//         // Configure HttpClient for IGDB API
//         _httpClient.DefaultRequestHeaders.Add("Client-ID", _clientId);
//         _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessToken}");
//     }

//     public async Task ImportAllGamesAsync()
//     {
//         const int batchSize = 500; // IGDB max limit
//         int offset = 0;
//         bool hasMoreData = true;

//         while (hasMoreData)
//         {
//             Console.WriteLine($"Fetching games batch: offset {offset}");
            
//             var games = await FetchGamesBatchAsync(offset, batchSize);
            
//             if (games?.Any() == true)
//             {
//                 await ProcessGamesBatchAsync(games);
//                 offset += batchSize;
                
//                 // Rate limiting - IGDB allows 4 requests per second
//                 await Task.Delay(250);
//             }
//             else
//             {
//                 hasMoreData = false;
//             }
//         }
//     }

//     private async Task<List<IgdbGame>> FetchGamesBatchAsync(int offset, int limit)
//     {
//         // Comprehensive IGDB query to get all game data and relationships
//         var query = $@"
//             fields 
//                 id, name, slug, storyline, summary, first_release_date, hypes, rating,
//                 genres.id, genres.name, genres.slug,
//                 age_ratings.id, age_ratings.category, age_ratings.rating, age_ratings.rating_cover_url,
//                 age_ratings.content_descriptions.id, age_ratings.content_descriptions.category, age_ratings.content_descriptions.description,
//                 alternative_names.id, alternative_names.name, alternative_names.comment,
//                 cover.id, cover.url, cover.width, cover.height,
//                 screenshots.id, screenshots.url, screenshots.width, screenshots.height,
//                 release_dates.id, release_dates.date, release_dates.human, release_dates.platform.id, release_dates.platform.name,
//                 franchises.id, franchises.name, franchises.slug,
//                 game_modes.id, game_modes.name, game_modes.slug,
//                 category,
//                 involved_companies.id, involved_companies.company.id, involved_companies.company.name, 
//                 involved_companies.developer, involved_companies.publisher, involved_companies.porting, involved_companies.supporting,
//                 platforms.id, platforms.name, platforms.abbreviation, platforms.slug,
//                 player_perspectives.id, player_perspectives.name, player_perspectives.slug,
//                 dlcs, expansions, ports, remakes, remasters, similar_games;
//             where category = (0,4,8,9,10,11) & version_parent = null;
//             sort id asc;
//             limit {limit};
//             offset {offset};";

//         try
//         {
//             var content = new StringContent(query, Encoding.UTF8, "text/plain");
//             var response = await _httpClient.PostAsync("https://api.igdb.com/v4/games", content);
            
//             if (response.IsSuccessStatusCode)
//             {
//                 var jsonResponse = await response.Content.ReadAsStringAsync();
//                 var games = JsonSerializer.Deserialize<List<IgdbGame>>(jsonResponse, new JsonSerializerOptions
//                 {
//                     PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
//                 });
                
//                 return games ?? new List<IgdbGame>();
//             }
//             else
//             {
//                 Console.WriteLine($"API Error: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
//                 return new List<IgdbGame>();
//             }
//         }
//         catch (Exception ex)
//         {
//             Console.WriteLine($"Exception fetching games: {ex.Message}");
//             return new List<IgdbGame>();
//         }
//     }

//     private async Task ProcessGamesBatchAsync(List<IgdbGame> igdbGames)
//     {
//         using var transaction = await _context.Database.BeginTransactionAsync();
        
//         try
//         {
//             // Step 1: Insert/Update basic game entities first
//             await ProcessBasicGameEntitiesAsync(igdbGames);
            
//             // Step 2: Process reference data (genres, platforms, etc.)
//             await ProcessReferenceDataAsync(igdbGames);
            
//             // Step 3: Process associations (game-genre, game-platform, etc.)
//             await ProcessGameAssociationsAsync(igdbGames);
            
//             // Step 4: Process game relationships (DLCs, expansions, etc.)
//             await ProcessGameRelationshipsAsync(igdbGames);
            
//             await transaction.CommitAsync();
//             Console.WriteLine($"Successfully processed batch of {igdbGames.Count} games");
//         }
//         catch (Exception ex)
//         {
//             await transaction.RollbackAsync();
//             Console.WriteLine($"Error processing batch: {ex.Message}");
//             throw;
//         }
//     }

//     private async Task ProcessBasicGameEntitiesAsync(List<IgdbGame> igdbGames)
//     {
//         var existingGames = await _context.Games
//             .Where(g => igdbGames.Select(ig => ig.Id).Contains(g.IgdbId.Value))
//             .ToListAsync();

//         var existingGameIds = existingGames.Select(g => g.IgdbId.Value).ToHashSet();

//         var newGames = igdbGames
//             .Where(ig => !existingGameIds.Contains(ig.Id))
//             .Select(ig => new Backend.Models.Game.Game
//             {
//                 IgdbId = ig.Id,
//                 Name = ig.Name ?? "",
//                 Slug = ig.Slug ?? "",
//                 Storyline = ig.Storyline,
//                 Summary = ig.Summary,
//                 FirstReleaseDate = ig.FirstReleaseDate,
//                 Hypes = ig.Hypes ?? 0,
//                 IgdbRating = ig.Rating.HasValue ? (decimal)ig.Rating.Value : null
//             })
//             .ToList();

//         if (newGames.Any())
//         {
//             await _context.Games.AddRangeAsync(newGames);
//             await _context.SaveChangesAsync();
//         }

//         // Update existing games
//         foreach (var existingGame in existingGames)
//         {
//             var igdbGame = igdbGames.First(ig => ig.Id == existingGame.IgdbId);
//             existingGame.Name = igdbGame.Name ?? existingGame.Name;
//             existingGame.Storyline = igdbGame.Storyline;
//             existingGame.Summary = igdbGame.Summary;
//             existingGame.FirstReleaseDate = igdbGame.FirstReleaseDate;
//             existingGame.Hypes = igdbGame.Hypes ?? existingGame.Hypes;
//             existingGame.IgdbRating = igdbGame.Rating.HasValue ? (decimal)igdbGame.Rating.Value : existingGame.IgdbRating;
//         }

//         await _context.SaveChangesAsync();
//     }

//     private async Task ProcessReferenceDataAsync(List<IgdbGame> igdbGames)
//     {
//         // Process Genres
//         var allGenres = igdbGames
//             .Where(g => g.Genres?.Any() == true)
//             .SelectMany(g => g.Genres)
//             .GroupBy(g => g.Id)
//             .Select(g => g.First())
//             .ToList();

//         await UpsertReferenceData(allGenres, _context.Genres, 
//             g => new Genre { IgdbId = g.Id, Name = g.Name, Slug = g.Slug });

//         // Process Platforms (from release dates)
//         var allPlatforms = igdbGames
//             .Where(g => g.ReleaseDates?.Any() == true)
//             .SelectMany(g => g.ReleaseDates)
//             .Where(rd => rd.Platform != null)
//             .Select(rd => rd.Platform)
//             .GroupBy(p => p.Id)
//             .Select(p => p.First())
//             .ToList();

//         await UpsertReferenceData(allPlatforms, _context.Platforms,
//             p => new Platform { IgdbId = p.Id, Name = p.Name, Abbreviation = p.Abbreviation, Slug = p.Slug });

//         // Process Companies
//         var allCompanies = igdbGames
//             .Where(g => g.InvolvedCompanies?.Any() == true)
//             .SelectMany(g => g.InvolvedCompanies)
//             .Where(ic => ic.Company != null)
//             .Select(ic => ic.Company)
//             .GroupBy(c => c.Id)
//             .Select(c => c.First())
//             .ToList();

//         await UpsertReferenceData(allCompanies, _context.Companies,
//             c => new Company { IgdbId = c.Id, Name = c.Name });

//         // Add similar methods for other reference data (Franchises, GameModes, etc.)
//     }

//     private async Task UpsertReferenceData<TIgdb, TEntity>(
//         List<TIgdb> igdbItems, 
//         DbSet<TEntity> dbSet, 
//         Func<TIgdb, TEntity> createEntity)
//         where TEntity : class, IHasIgdbId
//         where TIgdb : IHasId
//     {
//         if (!igdbItems.Any()) return;

//         var igdbIds = igdbItems.Select(i => i.Id).ToList();
//         var existingEntities = await dbSet
//             .Where(e => igdbIds.Contains(e.IgdbId.Value))
//             .ToListAsync();

//         var existingIds = existingEntities.Select(e => e.IgdbId.Value).ToHashSet();
//         var newEntities = igdbItems
//             .Where(i => !existingIds.Contains(i.Id))
//             .Select(createEntity)
//             .ToList();

//         if (newEntities.Any())
//         {
//             await dbSet.AddRangeAsync(newEntities);
//             await _context.SaveChangesAsync();
//         }
//     }

//     private async Task ProcessGameAssociationsAsync(List<IgdbGame> igdbGames)
//     {
//         var gameIds = await _context.Games
//             .Where(g => igdbGames.Select(ig => ig.Id).Contains(g.IgdbId.Value))
//             .ToDictionaryAsync(g => g.IgdbId.Value, g => g.Id);

//         // Process Game-Genre associations
//         var gameGenres = new List<GameGenre>();
//         foreach (var igdbGame in igdbGames.Where(g => g.Genres?.Any() == true))
//         {
//             if (!gameIds.TryGetValue(igdbGame.Id, out var gameId)) continue;

//             var genreIds = await _context.Genres
//                 .Where(g => igdbGame.Genres.Select(ig => ig.Id).Contains(g.IgdbId.Value))
//                 .ToDictionaryAsync(g => g.IgdbId.Value, g => g.Id);

//             foreach (var igdbGenre in igdbGame.Genres)
//             {
//                 if (genreIds.TryGetValue(igdbGenre.Id, out var genreId))
//                 {
//                     gameGenres.Add(new GameGenre { GameId = gameId, GenreId = genreId });
//                 }
//             }
//         }

//         // Remove existing associations for these games
//         var gameIdsToProcess = gameIds.Values.ToList();
//         var existingGameGenres = await _context.GameGenres
//             .Where(gg => gameIdsToProcess.Contains(gg.GameId))
//             .ToListAsync();
        
//         _context.GameGenres.RemoveRange(existingGameGenres);
//         await _context.GameGenres.AddRangeAsync(gameGenres);
        
//         // Similar processing for other associations (platforms, companies, etc.)
//         await _context.SaveChangesAsync();
//     }

//     private async Task ProcessGameRelationshipsAsync(List<IgdbGame> igdbGames)
//     {
//         var gameIds = await _context.Games
//             .Where(g => igdbGames.Select(ig => ig.Id).Contains(g.IgdbId.Value))
//             .ToDictionaryAsync(g => g.IgdbId.Value, g => g.Id);

//         // Process DLC relationships
//         await ProcessRelationship<GameDlc>(igdbGames, gameIds, 
//             game => game.Dlcs, 
//             (parentId, childId) => new GameDlc { ParentGameId = parentId, DlcGameId = childId });

//         // Process Expansion relationships
//         await ProcessRelationship<GameExpansion>(igdbGames, gameIds,
//             game => game.Expansions,
//             (parentId, childId) => new GameExpansion { ParentGameId = parentId, ExpansionGameId = childId });

//         // Process Port relationships
//         await ProcessRelationship<GamePort>(igdbGames, gameIds,
//             game => game.Ports,
//             (parentId, childId) => new GamePort { OriginalGameId = parentId, PortGameId = childId });

//         // Process Remake relationships
//         await ProcessRelationship<GameRemake>(igdbGames, gameIds,
//             game => game.Remakes,
//             (parentId, childId) => new GameRemake { OriginalGameId = parentId, RemakeGameId = childId });

//         // Process Remaster relationships
//         await ProcessRelationship<GameRemaster>(igdbGames, gameIds,
//             game => game.Remasters,
//             (parentId, childId) => new GameRemaster { OriginalGameId = parentId, RemasterGameId = childId });

//         // Process Similar Games
//         await ProcessRelationship<SimilarGame>(igdbGames, gameIds,
//             game => game.SimilarGames,
//             (gameId, similarId) => new SimilarGame { GameId = gameId, SimilarGameId = similarId });

//         await _context.SaveChangesAsync();
//     }

//     private async Task ProcessRelationship<TRelationship>(
//         List<IgdbGame> igdbGames,
//         Dictionary<int, int> gameIds,
//         Func<IgdbGame, List<int>?> getRelatedIds,
//         Func<int, int, TRelationship> createRelationship)
//         where TRelationship : class
//     {
//         var relationships = new List<TRelationship>();

//         foreach (var igdbGame in igdbGames)
//         {
//             var relatedIds = getRelatedIds(igdbGame);
//             if (relatedIds?.Any() != true || !gameIds.TryGetValue(igdbGame.Id, out var gameId))
//                 continue;

//             // Find which related games exist in our database
//             var existingRelatedGameIds = await _context.Games
//                 .Where(g => relatedIds.Contains(g.IgdbId.Value))
//                 .Select(g => g.Id)
//                 .ToListAsync();

//             foreach (var relatedGameId in existingRelatedGameIds)
//             {
//                 relationships.Add(createRelationship(gameId, relatedGameId));
//             }
//         }

//         if (relationships.Any())
//         {
//             var dbSet = _context.Set<TRelationship>();
//             await dbSet.AddRangeAsync(relationships);
//         }
//     }
// }