using Microsoft.EntityFrameworkCore;
using Backend.Models.Social;
using Backend.Data;
using Backend.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Backend.Tests.Helpers;

namespace Backend.Tests;

/// <summary>
/// Test class for review creation functionality.
/// Contains methods to create reviews for test users using popular games.
/// </summary>
public class ReviewCreationTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private readonly List<Guid> _createdReviewIds = new();
    private GameService? _gameService;

    public ReviewCreationTests(DatabaseFixture fixture)
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
        // Clean up specific reviews created during tests
        if (_createdReviewIds.Count > 0)
        {
            await CleanupReviewsAsync(_createdReviewIds);
        }
    }

    /// <summary>
    /// Creates 5 reviews for each of the provided user IDs using popular games.
    /// Uses realistic ratings and varied review text.
    /// </summary>
    /// <param name="userIds">List of user IDs (as strings from Identity) to create reviews for</param>
    [Fact/*(Skip = "Manual execution only - run after creating test users")*/]
    public async Task CreateReviewsForTestUsers()
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

        // Create reviews
        await CreateReviewsForUsers(userProfileIds);

        // Verify reviews were created
        var createdReviews = await context.Reviews
            .Where(r => userProfileIds.Contains(r.UserId))
            .CountAsync();

        Assert.True(createdReviews >= userProfileIds.Count * 5,
            $"Expected at least {userProfileIds.Count * 5} reviews, found {createdReviews}");
    }

    /// <summary>
    /// Helper method to create 5 reviews for each user in the provided list.
    /// Public so it can be called from other test classes or manually.
    /// </summary>
    /// <param name="userProfileIds">List of UserProfile GUIDs to create reviews for</param>
    public async Task CreateReviewsForUsers(List<Guid> userProfileIds)
    {
        if (_gameService == null)
        {
            throw new InvalidOperationException("GameService not initialized");
        }

        var context = _fixture.Context;
        var random = new Random(42); // Fixed seed for reproducible results

        // Get popular games
        var popularGames = await _gameService.GetPopularGamesAsync(20);

        if (popularGames.Count == 0)
        {
            Assert.Fail("No popular games found in database. Ensure games are seeded.");
            return;
        }

        Console.WriteLine($"Found {popularGames.Count} popular games to review");
        Console.WriteLine($"Creating reviews for {userProfileIds.Count} users");

        int totalReviewsCreated = 0;

        // Create 5 reviews for each user
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

            Console.WriteLine($"\nCreating reviews for user: {userProfile.DisplayName} ({userProfile.Email})");

            // Randomly select 5 games from popular games
            var selectedGameIndices = new HashSet<int>();
            while (selectedGameIndices.Count < 5 && selectedGameIndices.Count < popularGames.Count)
            {
                selectedGameIndices.Add(random.Next(popularGames.Count));
            }

            int reviewCount = 0;
            foreach (var gameIndex in selectedGameIndices)
            {
                var gameDto = popularGames[gameIndex];

                var gameId = await context.Games
                    .Where(g => g.IgdbId == gameDto.IgdbId)
                    .Select(g => g.Id)
                    .FirstOrDefaultAsync();

                // Check if user has already reviewed this game
                var existingReview = await context.Reviews
                    .FirstOrDefaultAsync(r => r.UserId == userProfileId && r.GameId == gameId);

                if (existingReview != null)
                {
                    Console.WriteLine($"  User already reviewed {gameDto.Name}, skipping");
                    continue;
                }

                // Generate realistic rating
                var rating = TestDataGenerator.GenerateRealisticRating();
                var reviewText = TestDataGenerator.GenerateReviewText(rating);

                // Create review
                var review = new Review
                {
                    UserId = userProfileId,
                    GameId = gameId,
                    Rating = rating,
                    ReviewText = reviewText,
                    ReviewDate = DateTime.UtcNow.AddDays(-random.Next(0, 365)),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                try
                {
                    await context.Reviews.AddAsync(review);
                    await context.SaveChangesAsync();

                    _createdReviewIds.Add(review.Id);
                    reviewCount++;
                    totalReviewsCreated++;

                    Console.WriteLine($"  ✓ Review #{reviewCount}: {gameDto.Name} - {rating}/5 stars");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  ✗ Failed to create review for {gameDto.Name}: {ex.Message}");
                }
            }

            Console.WriteLine($"  Created {reviewCount} reviews for {userProfile.DisplayName}");
        }

        Console.WriteLine($"\n=== TOTAL: Created {totalReviewsCreated} reviews for {userProfileIds.Count} users ===");
    }

    /// <summary>
    /// Test that creates reviews and then cleans them up.
    /// This test will NOT persist reviews in the database.
    /// </summary>
    [Fact]
    public async Task CreateAndCleanupReviewsForTestUsers_ShouldSucceed()
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
            // Create temporary users for this test
            Assert.Fail("No test users found. Run CreateThirtyTestUsers_ShouldSucceed first or this test with other tests.");
            return;
        }

        var userProfileIds = users
            .Where(u => u.UserProfile != null)
            .Select(u => u.UserProfile!.Id)
            .ToList();

        var initialReviewCount = await context.Reviews
            .Where(r => userProfileIds.Contains(r.UserId))
            .CountAsync();

        // Create reviews
        await CreateReviewsForUsers(userProfileIds);

        // Verify reviews were created
        var afterCreationCount = await context.Reviews
            .Where(r => userProfileIds.Contains(r.UserId))
            .CountAsync();

        Assert.True(afterCreationCount > initialReviewCount,
            $"Expected more reviews after creation. Before: {initialReviewCount}, After: {afterCreationCount}");

        // Cleanup will happen automatically in DisposeAsync
    }

    /// <summary>
    /// Cleans up reviews by their IDs
    /// </summary>
    private async Task CleanupReviewsAsync(List<Guid> reviewIds)
    {
        if (reviewIds.Count == 0) return;

        var context = _fixture.Context;
        var reviewsToDelete = await context.Reviews.Where(r => reviewIds.Contains(r.Id)).ToListAsync();

        if (reviewsToDelete.Count > 0)
        {
            context.Reviews.RemoveRange(reviewsToDelete);
            await context.SaveChangesAsync();
            Console.WriteLine($"Cleaned up {reviewsToDelete.Count} reviews");
        }
    }

    /// <summary>
    /// Utility method to clean up all reviews for review tester users.
    /// Run this if you need to remove all reviews created by test users.
    /// </summary>
    [Fact(Skip = "Manual execution only - run when you need to clean up all test reviews")]
    public async Task CleanupAllReviewTesterReviews_ForManualExecution()
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

        // Find and remove all reviews by these users
        var reviewsToDelete = context.Reviews
            .Where(r => reviewTesterProfileIds.Contains(r.UserId))
            .ToList();

        if (reviewsToDelete.Count > 0)
        {
            context.Reviews.RemoveRange(reviewsToDelete);
            await context.SaveChangesAsync();

            Console.WriteLine($"Cleaned up {reviewsToDelete.Count} reviews from review tester users");

            // Verify cleanup
            var remainingReviews = await context.Reviews
                .Where(r => reviewTesterProfileIds.Contains(r.UserId))
                .CountAsync();

            Assert.Equal(0, remainingReviews);
        }
    }
}
