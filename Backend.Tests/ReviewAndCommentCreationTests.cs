using Microsoft.EntityFrameworkCore;
using Backend.Models.Social;
using Backend.Data;
using Backend.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Backend.Tests.Models;
using System.Text.Json;

namespace Backend.Tests;

/// <summary>
/// Test class for creating reviews and comments from game-reviews.json file.
/// Uses ReviewService and CommentService to create realistic test data.
/// </summary>
public class ReviewAndCommentCreationTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private readonly List<Guid> _createdReviewIds = new();
    private readonly List<Guid> _createdCommentIds = new();
    private IReviewService? _reviewService;
    private ICommentService? _commentService;

    public ReviewAndCommentCreationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync()
    {
        // Setup review service
        var reviewLogger = NullLogger<ReviewService>.Instance;
        _reviewService = new ReviewService(_fixture.Context, reviewLogger);

        // Setup comment service
        var commentLogger = NullLogger<CommentService>.Instance;
        _commentService = new CommentService(_fixture.Context, commentLogger);

        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        // Clean up specific reviews and comments created during tests
        if (_createdReviewIds.Count > 0 || _createdCommentIds.Count > 0)
        {
            await CleanupReviewsAndCommentsAsync(_createdReviewIds, _createdCommentIds);
        }
    }

    /// <summary>
    /// Creates reviews and comments from game-reviews.json file.
    /// Reviews are distributed evenly among @test and @reviewtester.dev users.
    /// Comments are distributed evenly among other users (not the review author).
    /// Ratings support 0.5 increments on a 1.0-5.0 scale.
    /// </summary>
    [Fact(Skip = "Manual execution only - run after creating test users and seeding games")]
    public async Task CreateReviewsAndCommentsFromJson()
    {
        if (_reviewService == null || _commentService == null)
        {
            Assert.Fail("Services not initialized");
            return;
        }

        var context = _fixture.Context;

        // Get all unique users with @test or @reviewtester.dev in their email
        var testUsers = await context.Users
            .Where(u => u.Email!.Contains("@test") || u.Email!.Contains("@reviewtester.dev"))
            .Include(u => u.UserProfile)
            .ToListAsync();

        if (testUsers.Count == 0)
        {
            Assert.Fail("No test users found. Create test users first.");
            return;
        }

        var userProfileIds = testUsers
            .Where(u => u.UserProfile != null)
            .Select(u => u.UserProfile!.Id)
            .ToList();

        if (userProfileIds.Count == 0)
        {
            Assert.Fail("No user profiles found for test users.");
            return;
        }

        Console.WriteLine($"Found {userProfileIds.Count} test users for review creation");

        // Read and deserialize game-reviews.json
        var jsonPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "game-reviews.json");
        if (!File.Exists(jsonPath))
        {
            Assert.Fail($"game-reviews.json not found at: {jsonPath}");
            return;
        }

        var jsonContent = await File.ReadAllTextAsync(jsonPath);
        var gameReviews = JsonSerializer.Deserialize<List<GameReviewData>>(jsonContent);

        if (gameReviews == null || gameReviews.Count == 0)
        {
            Assert.Fail("Failed to deserialize game-reviews.json or file is empty");
            return;
        }

        Console.WriteLine($"Loaded {gameReviews.Count} reviews from game-reviews.json");

        await CreateReviewsAndCommentsFromData(gameReviews, userProfileIds);

        // Verify reviews were created
        var createdReviews = await context.Reviews
            .Where(r => _createdReviewIds.Contains(r.Id))
            .CountAsync();

        Console.WriteLine($"\n=== SUMMARY: Created {createdReviews} reviews and {_createdCommentIds.Count} comments ===");

        Assert.True(createdReviews > 0, "Expected at least some reviews to be created");
    }

    /// <summary>
    /// Helper method to create reviews and comments from the game review data.
    /// Public so it can be called from other test classes or manually.
    /// </summary>
    /// <param name="gameReviews">List of game review data from JSON</param>
    /// <param name="userProfileIds">List of UserProfile GUIDs to distribute reviews among</param>
    public async Task CreateReviewsAndCommentsFromData(List<GameReviewData> gameReviews, List<Guid> userProfileIds)
    {
        if (_reviewService == null || _commentService == null)
        {
            throw new InvalidOperationException("Services not initialized");
        }

        var context = _fixture.Context;
        int reviewsCreated = 0;
        int reviewsSkipped = 0;
        int commentsCreated = 0;

        // Evenly distribute reviews among users using simple round-robin
        for (int i = 0; i < gameReviews.Count; i++)
        {
            var reviewData = gameReviews[i];
            var userProfileId = userProfileIds[i % userProfileIds.Count]; // Round-robin distribution

            // Get game by IgdbId
            var game = await context.Games
                .FirstOrDefaultAsync(g => g.IgdbId == reviewData.IgdbId);

            if (game == null)
            {
                Console.WriteLine($"⚠ Skipping review for '{reviewData.GameName}' (IgdbId: {reviewData.IgdbId}) - game not found in database");
                reviewsSkipped++;
                continue;
            }

            // Check if user has already reviewed this game
            var existingReview = await _reviewService.GetUserReviewForGameAsync(userProfileId, game.IgdbId);
            if (existingReview != null)
            {
                Console.WriteLine($"⚠ Skipping review for '{reviewData.GameName}' - user already reviewed this game");
                reviewsSkipped++;
                continue;
            }

            // Round review score to nearest 0.5 and clamp to 1.0-5.0 range
            var rating = Math.Round(reviewData.ReviewScore * 2) / 2; // Round to nearest 0.5
            rating = Math.Clamp(rating, 1.0, 5.0); // Ensure rating is between 1.0 and 5.0

            try
            {
                // Create the review using ReviewService
                var createdReview = await _reviewService.CreateReviewAsync(
                    userProfileId,
                    game.IgdbId,
                    rating,
                    reviewData.Review
                );

                if (createdReview == null)
                {
                    Console.WriteLine($"✗ Failed to create review for '{reviewData.GameName}'");
                    continue;
                }

                _createdReviewIds.Add(createdReview.Id);
                reviewsCreated++;

                Console.WriteLine($"✓ Review #{reviewsCreated}: '{reviewData.GameName}' - {rating:F1}/5.0 stars ({reviewData.ReviewComments.Count} comments to add)");

                // Create comments for this review
                // Distribute comments evenly among users, excluding the review author
                var availableCommenters = userProfileIds.Where(id => id != userProfileId).ToList();

                if (availableCommenters.Count == 0)
                {
                    Console.WriteLine($"  ⚠ No available users to comment on this review (need at least 2 users)");
                    continue;
                }

                for (int j = 0; j < reviewData.ReviewComments.Count; j++)
                {
                    var commentContent = reviewData.ReviewComments[j];
                    var commenterId = availableCommenters[j % availableCommenters.Count]; // Round-robin distribution

                    try
                    {
                        var createdComment = await _commentService.CreateReviewCommentAsync(
                            commenterId,
                            createdReview.Id,
                            commentContent
                        );

                        if (createdComment != null)
                        {
                            _createdCommentIds.Add(createdComment.Id);
                            commentsCreated++;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"    ✗ Failed to create comment: {ex.Message}");
                    }
                }

                Console.WriteLine($"    Added {reviewData.ReviewComments.Count} comments");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Failed to create review for '{reviewData.GameName}': {ex.Message}");
            }
        }

        Console.WriteLine($"\n=== RESULTS ===");
        Console.WriteLine($"Reviews created: {reviewsCreated}");
        Console.WriteLine($"Reviews skipped: {reviewsSkipped}");
        Console.WriteLine($"Comments created: {commentsCreated}");
    }

    /// <summary>
    /// Cleans up reviews and comments by their IDs
    /// </summary>
    private async Task CleanupReviewsAndCommentsAsync(List<Guid> reviewIds, List<Guid> commentIds)
    {
        var context = _fixture.Context;

        // Remove comments first (due to foreign key constraints)
        if (commentIds.Count > 0)
        {
            var commentsToDelete = await context.Comments
                .Where(c => commentIds.Contains(c.Id))
                .ToListAsync();

            if (commentsToDelete.Count > 0)
            {
                context.Comments.RemoveRange(commentsToDelete);
            }
        }

        // Then remove reviews
        if (reviewIds.Count > 0)
        {
            var reviewsToDelete = await context.Reviews
                .Where(r => reviewIds.Contains(r.Id))
                .ToListAsync();

            if (reviewsToDelete.Count > 0)
            {
                context.Reviews.RemoveRange(reviewsToDelete);
            }
        }

        await context.SaveChangesAsync();
        Console.WriteLine($"Cleaned up {reviewIds.Count} reviews and {commentIds.Count} comments");
    }

    /// <summary>
    /// Utility method to clean up all reviews and comments created from game-reviews.json.
    /// Run this if you need to remove all reviews/comments created by this test.
    /// </summary>
    [Fact(Skip = "Manual execution only - run when you need to clean up all JSON-sourced reviews")]
    public async Task CleanupAllJsonSourcedReviews_ForManualExecution()
    {
        var context = _fixture.Context;

        // Get all test users
        var testUsers = await context.Users
            .Where(u => u.Email!.Contains("@test") || u.Email!.Contains("@reviewtester.dev"))
            .Select(u => u.Id)
            .ToListAsync();

        var testUserProfileIds = await context.UserProfiles
            .Where(p => testUsers.Contains(p.UserId))
            .Select(p => p.Id)
            .ToListAsync();

        // Read game-reviews.json to get the IgdbIds we're looking for
        var jsonPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "game-reviews.json");
        if (!File.Exists(jsonPath))
        {
            Console.WriteLine($"game-reviews.json not found at: {jsonPath}");
            return;
        }

        var jsonContent = await File.ReadAllTextAsync(jsonPath);
        var gameReviews = JsonSerializer.Deserialize<List<GameReviewData>>(jsonContent);

        if (gameReviews == null || gameReviews.Count == 0)
        {
            Console.WriteLine("Failed to load game-reviews.json");
            return;
        }

        // Get the game IDs from the JSON
        var igdbIds = gameReviews.Select(r => r.IgdbId).Distinct().ToList();
        var gameIds = await context.Games
            .Where(g => igdbIds.Contains(g.IgdbId))
            .Select(g => g.Id)
            .ToListAsync();

        // Find reviews by test users for these games
        var reviewsToDelete = await context.Reviews
            .Where(r => testUserProfileIds.Contains(r.UserId) && gameIds.Contains(r.GameId))
            .ToListAsync();

        if (reviewsToDelete.Count > 0)
        {
            var reviewIds = reviewsToDelete.Select(r => r.Id).ToList();

            // Remove comments on these reviews
            var commentsToDelete = await context.Comments
                .Where(c => c.ReviewId.HasValue && reviewIds.Contains(c.ReviewId.Value))
                .ToListAsync();

            if (commentsToDelete.Count > 0)
            {
                context.Comments.RemoveRange(commentsToDelete);
            }

            // Remove the reviews
            context.Reviews.RemoveRange(reviewsToDelete);
            await context.SaveChangesAsync();

            Console.WriteLine($"Cleaned up {reviewsToDelete.Count} reviews and {commentsToDelete.Count} comments");

            // Verify cleanup
            var remainingReviews = await context.Reviews
                .Where(r => testUserProfileIds.Contains(r.UserId) && gameIds.Contains(r.GameId))
                .CountAsync();

            Assert.Equal(0, remainingReviews);
        }
        else
        {
            Console.WriteLine("No reviews found to clean up");
        }
    }
}
