using Microsoft.EntityFrameworkCore;
using Backend.Models.Social;
using Backend.Data;
using Backend.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Backend.Tests.Helpers;
using Backend.Models.DTO.Game;

namespace Backend.Tests;

/// <summary>
/// Test class for game list creation functionality.
/// Contains methods to create game lists for test users using popular games.
/// </summary>
public class ListCreationTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private readonly List<Guid> _createdListIds = new();
    private GameService? _gameService;

    public ListCreationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync()
    {
        // Setup game service
        var gameLogger = NullLogger<GameService>.Instance;
        _gameService = new GameService(_fixture.Context, gameLogger);

        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        // Clean up specific lists created during tests
        if (_createdListIds.Count > 0)
        {
            await CleanupListsAsync(_createdListIds);
        }
    }

    /// <summary>
    /// Creates 3 game lists (with 5 games each) for each of the provided user IDs.
    /// Uses predefined list names and popular games.
    /// </summary>
    [Fact/*(Skip = "Manual execution only - run after creating test users")*/]
    public async Task CreateListsForTestUsers()
    {
        // Get the review tester users from the database
        var context = _fixture.Context;
        var emailDomain = "@reviewtester.dev";

        var users = await context.Users
            .Where(u => u.Email!.EndsWith(emailDomain))
            .Include(u => u.UserProfile)
            .ToListAsync();

        if (users.Count == 0)
        {
            Assert.Fail("No review tester users found. Run Create10PermanentTestUsers_WithCredentialsFile first.");
            return;
        }

        // Get UserProfile IDs
        var userProfileIds = users
            .Where(u => u.UserProfile != null)
            .Select(u => u.UserProfile!.Id)
            .ToList();

        if (userProfileIds.Count == 0)
        {
            Assert.Fail("No user profiles found for review tester users.");
            return;
        }

        // Create lists
        await CreateListsForUsers(userProfileIds);

        // Verify lists were created
        var createdLists = await context.GameLists
            .Where(l => userProfileIds.Contains(l.UserId))
            .CountAsync();

        Assert.True(createdLists >= userProfileIds.Count * 3,
            $"Expected at least {userProfileIds.Count * 3} lists, found {createdLists}");
    }

    /// <summary>
    /// Helper method to create 3 lists (with 5 games each) for each user in the provided list.
    /// Public so it can be called from other test classes or manually.
    /// </summary>
    /// <param name="userProfileIds">List of UserProfile GUIDs to create lists for</param>
    public async Task CreateListsForUsers(List<Guid> userProfileIds)
    {
        if (_gameService == null)
        {
            throw new InvalidOperationException("GameService not initialized");
        }

        var context = _fixture.Context;
        var random = new Random(42); // Fixed seed for reproducible results

        // Get popular games
        var popularGames = await _gameService.GetPopularGamesAsync(50);

        if (popularGames.Count < 5)
        {
            Assert.Fail("Not enough popular games found in database. Ensure games are seeded.");
            return;
        }

        Console.WriteLine($"Found {popularGames.Count} popular games for lists");
        Console.WriteLine($"Creating lists for {userProfileIds.Count} users");

        int totalListsCreated = 0;

        // Create 3 lists for each user
        foreach (var userProfileId in userProfileIds)
        {
            // Get user info for logging
            var userProfile = await context.UserProfiles
                .FirstOrDefaultAsync(p => p.Id == userProfileId);

            if (userProfile == null)
            {
                Console.WriteLine($"Warning: UserProfile {userProfileId} not found, skipping");
                continue;
            }

            Console.WriteLine($"\nCreating lists for user: {userProfile.DisplayName} ({userProfile.Email})");

            // Get predefined list names
            var listNames = TestDataGenerator.GetPredefinedListNames();

            int listCount = 0;
            foreach (var listName in listNames)
            {
                // Check if user already has a list with this name
                var existingList = await context.GameLists
                    .FirstOrDefaultAsync(l => l.UserId == userProfileId && l.Name == listName);

                if (existingList != null)
                {
                    Console.WriteLine($"  User already has list '{listName}', skipping");
                    continue;
                }

                // Create the list
                var gameList = new GameList
                {
                    UserId = userProfileId,
                    Name = listName,
                    Description = TestDataGenerator.GetListDescription(listName),
                    IsPublic = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                try
                {
                    await context.GameLists.AddAsync(gameList);
                    await context.SaveChangesAsync();

                    _createdListIds.Add(gameList.Id);
                    listCount++;
                    totalListsCreated++;

                    Console.WriteLine($"  ✓ Created list: {listName}");

                    // Add 5 games to the list
                    await AddGamesToList(gameList.Id, popularGames, random);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  ✗ Failed to create list '{listName}': {ex.Message}");
                }
            }

            Console.WriteLine($"  Created {listCount} lists for {userProfile.DisplayName}");
        }

        Console.WriteLine($"\n=== TOTAL: Created {totalListsCreated} lists for {userProfileIds.Count} users ===");
    }

    /// <summary>
    /// Adds 5 random games to a game list
    /// </summary>
    private async Task AddGamesToList(Guid listId, List<GameDto> popularGames, Random random)
    {
        var context = _fixture.Context;

        // Randomly select 5 games
        var selectedGameIndices = new HashSet<int>();
        while (selectedGameIndices.Count < 5 && selectedGameIndices.Count < popularGames.Count)
        {
            selectedGameIndices.Add(random.Next(popularGames.Count));
        }

        int order = 1;
        foreach (var gameIndex in selectedGameIndices)
        {
            var gameDto = popularGames[gameIndex];

            var game = await context.Games
                .FirstOrDefaultAsync(g => g.IgdbId == gameDto.IgdbId);

            if (game == null) continue;

            var gameListItem = new GameListItem
            {
                GameListId = listId,
                GameId = game.Id,
                Order = order++,
                Note = null, // No notes for now
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await context.GameListItems.AddAsync(gameListItem);
        }

        await context.SaveChangesAsync();
        Console.WriteLine($"    Added {selectedGameIndices.Count} games to list");
    }

    /// <summary>
    /// Test that creates lists and then cleans them up.
    /// This test will NOT persist lists in the database.
    /// </summary>
    [Fact]
    public async Task CreateAndCleanupListsForTestUsers_ShouldSucceed()
    {
        if (_gameService == null)
        {
            Assert.Fail("GameService not initialized");
            return;
        }

        var context = _fixture.Context;

        // Get test users (using @patch.notes domain from the standard test)
        var users = await context.Users
            .Where(u => u.Email!.EndsWith("@patch.notes"))
            .Include(u => u.UserProfile)
            .Take(5) // Just use 5 users for this test
            .ToListAsync();

        if (users.Count == 0)
        {
            Assert.Fail("No test users found. Run CreateThirtyTestUsers_ShouldSucceed first or this test with other tests.");
            return;
        }

        var userProfileIds = users
            .Where(u => u.UserProfile != null)
            .Select(u => u.UserProfile!.Id)
            .ToList();

        var initialListCount = await context.GameLists
            .Where(l => userProfileIds.Contains(l.UserId))
            .CountAsync();

        // Create lists
        await CreateListsForUsers(userProfileIds);

        // Verify lists were created
        var afterCreationCount = await context.GameLists
            .Where(l => userProfileIds.Contains(l.UserId))
            .CountAsync();

        Assert.True(afterCreationCount > initialListCount,
            $"Expected more lists after creation. Before: {initialListCount}, After: {afterCreationCount}");

        // Verify game list items were created
        var createdLists = await context.GameLists
            .Where(l => _createdListIds.Contains(l.Id))
            .Include(l => l.GameListItems)
            .ToListAsync();

        foreach (var list in createdLists)
        {
            Assert.True(list.GameListItems.Count > 0,
                $"List '{list.Name}' should have game items");
        }

        // Cleanup will happen automatically in DisposeAsync
    }

    /// <summary>
    /// Cleans up lists by their IDs, including their game list items
    /// </summary>
    private async Task CleanupListsAsync(List<Guid> listIds)
    {
        if (listIds.Count == 0) return;

        var context = _fixture.Context;

        // Remove game list items first (due to foreign key constraints)
        var gameListItems = await context.GameListItems
            .Where(item => listIds.Contains(item.GameListId))
            .ToListAsync();

        if (gameListItems.Count > 0)
        {
            context.GameListItems.RemoveRange(gameListItems);
        }

        // Then remove the lists
        var listsToDelete = await context.GameLists
            .Where(l => listIds.Contains(l.Id))
            .ToListAsync();

        if (listsToDelete.Count > 0)
        {
            context.GameLists.RemoveRange(listsToDelete);
            await context.SaveChangesAsync();
            Console.WriteLine($"Cleaned up {listsToDelete.Count} lists and {gameListItems.Count} list items");
        }
    }

    /// <summary>
    /// Utility method to clean up all lists for review tester users.
    /// Run this if you need to remove all lists created by test users.
    /// </summary>
    [Fact(Skip = "Manual execution only - run when you need to clean up all test lists")]
    public async Task CleanupAllReviewTesterLists_ForManualExecution()
    {
        var context = _fixture.Context;
        var emailDomain = "@reviewtester.dev";

        // Get all review tester user profile IDs
        var reviewTesterUserIds = await context.Users
            .Where(u => u.Email!.EndsWith(emailDomain))
            .Select(u => u.Id)
            .ToListAsync();

        var reviewTesterProfileIds = await context.UserProfiles
            .Where(p => reviewTesterUserIds.Contains(p.UserId))
            .Select(p => p.Id)
            .ToListAsync();

        // Find all lists by these users
        var listsToDelete = await context.GameLists
            .Where(l => reviewTesterProfileIds.Contains(l.UserId))
            .ToListAsync();

        if (listsToDelete.Count > 0)
        {
            var listIds = listsToDelete.Select(l => l.Id).ToList();

            // Remove game list items first
            var gameListItems = await context.GameListItems
                .Where(item => listIds.Contains(item.GameListId))
                .ToListAsync();

            if (gameListItems.Count > 0)
            {
                context.GameListItems.RemoveRange(gameListItems);
            }

            // Then remove lists
            context.GameLists.RemoveRange(listsToDelete);
            await context.SaveChangesAsync();

            Console.WriteLine($"Cleaned up {listsToDelete.Count} lists and {gameListItems.Count} list items from review tester users");

            // Verify cleanup
            var remainingLists = await context.GameLists
                .Where(l => reviewTesterProfileIds.Contains(l.UserId))
                .CountAsync();

            Assert.Equal(0, remainingLists);
        }
        else
        {
            Console.WriteLine("No lists found to clean up");
        }
    }
}
