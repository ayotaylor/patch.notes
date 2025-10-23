using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Backend.Models.Auth;
using Backend.Data;

namespace Backend.Tests;

/// <summary>
/// Test class for user creation functionality.
/// Contains:
/// - CreateThirtyTestUsers_ShouldSucceed: Automated test that creates and cleans up test users
/// - CreatePermanentTestUsers_ForManualTesting: Creates permanent users for manual testing (not cleaned up)
/// - CleanupPermanentTestUsers_ForManualExecution: Utility to clean up permanent test users
/// </summary>
public class UserCreationTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private readonly List<string> _createdUserIds = new();

    public UserCreationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync()
    {
        // Clean up any existing test data before tests
        return _fixture.CleanupTestUsersAsync();
    }

    public async Task DisposeAsync()
    {
        // Clean up specific users created during tests
        if (_createdUserIds.Count > 0)
        {
            await _fixture.CleanupSpecificUsersAsync(_createdUserIds);
        }
    }
    [Fact(Skip = "Manual execution only - uncomment and run when you need to create permanent test users")]
    public async Task CreateThirtyTestUsers_ShouldSucceed()
    {
        // Get services from fixture
        var userManager = _fixture.UserManager;
        var context = _fixture.Context;

        var testUsers = new List<TestUserData>();
        var random = new Random(42); // Fixed seed for reproducible results

        var firstNames = new[]
        {
            "Alex", "Emma", "James", "Olivia", "William", "Sophia", "Benjamin", "Isabella",
            "Lucas", "Mia", "Henry", "Charlotte", "Alexander", "Amelia", "Michael", "Harper",
            "Ethan", "Evelyn", "Daniel", "Abigail", "Matthew", "Emily", "Jackson", "Ella",
            "Sebastian", "Madison", "David", "Scarlett", "Joseph", "Victoria"
        };

        var lastNames = new[]
        {
            "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis",
            "Rodriguez", "Martinez", "Hernandez", "Lopez", "Gonzalez", "Wilson", "Anderson",
            "Thomas", "Taylor", "Moore", "Jackson", "Martin", "Lee", "Perez", "Thompson",
            "White", "Harris", "Sanchez", "Clark", "Ramirez", "Lewis", "Robinson"
        };

        var bios = new[]
        {
            "Gaming enthusiast and speedrunner",
            "Love RPGs and story-driven games",
            "Competitive FPS player",
            "Indie game developer and player",
            "Retro gaming collector",
            "Casual gamer who loves puzzles",
            "JRPG fan and anime lover",
            "Strategy game mastermind",
            "Co-op gaming with friends",
            "VR gaming pioneer",
            "Mobile gaming on the go",
            "Fighting game tournament player",
            "Simulation game addict",
            "Platformer speedrun enthusiast",
            "Open world exploration lover"
        };

        for (int i = 1; i <= 30; i++)
        {
            var firstName = firstNames[random.Next(firstNames.Length)];
            var lastName = lastNames[random.Next(lastNames.Length)];
            var email = $"testuser{i:D2}@patch.notes";
            var displayName = $"{firstName}{random.Next(100, 999)}";
            var bio = bios[random.Next(bios.Length)];
            var password = "TestPass123!";

            // Use a transaction to ensure both user and profile are created together
            using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                // Create the user
                var user = new User
                {
                    UserName = email,
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    Provider = "Local",
                    CreatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 365)),
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(user, password);

                if (result.Succeeded)
                {
                    // Create user profile in the same transaction
                    var profile = new UserProfile
                    {
                        UserId = user.Id,
                        FirstName = firstName,
                        LastName = lastName,
                        DisplayName = displayName,
                        Email = email,
                        Bio = bio,
                        DateOfBirth = DateTime.Now.AddYears(-random.Next(18, 50)).AddDays(-random.Next(0, 365)),
                        PhoneNumber = $"+1{random.Next(200, 999)}{random.Next(200, 999)}{random.Next(1000, 9999)}",
                        IsProfileUpdated = true,
                        IsPublic = random.Next(0, 10) > 1, // 90% public profiles
                        CreatedAt = user.CreatedAt,
                        UpdatedAt = DateTime.UtcNow
                    };

                    context.UserProfiles.Add(profile);
                    await context.SaveChangesAsync();

                    // Commit transaction
                    await transaction.CommitAsync();

                    var testUserData = new TestUserData
                    {
                        Id = user.Id,
                        Email = email,
                        Password = password,
                        FirstName = firstName,
                        LastName = lastName,
                        DisplayName = displayName,
                        Bio = bio,
                        DateOfBirth = profile.DateOfBirth,
                        PhoneNumber = profile.PhoneNumber,
                        IsPublic = profile.IsPublic,
                        CreatedAt = user.CreatedAt
                    };

                    testUsers.Add(testUserData);
                    _createdUserIds.Add(user.Id);
                }
                else
                {
                    await transaction.RollbackAsync();
                    Assert.Fail($"Failed to create user {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Assert.Fail($"Exception creating user {email}: {ex.Message}");
            }
        }

        // Verify we created 30 users
        Assert.Equal(30, testUsers.Count);

        // Verify users exist in database
        var createdUsers = await context.Users.CountAsync();
        Assert.True(createdUsers >= 30, $"Expected at least 30 users in database, found {createdUsers}");

        var createdProfiles = await context.UserProfiles.CountAsync();
        Assert.True(createdProfiles >= 30, $"Expected at least 30 profiles in database, found {createdProfiles}");
    }

    /// <summary>
    /// Creates 10 permanent test users for manual testing purposes. These users will NOT be cleaned up automatically.
    /// Use this method once to generate test users that persist in the database for development/testing.
    /// Credentials will be written to Backend/testusers folder.
    /// </summary>
    [Fact/*Skip = "Manual execution only - uncomment and run when you need to create permanent test users")*/]
    public async Task Create10PermanentTestUsers_WithCredentialsFile()
    {
        // Get services from fixture
        var userManager = _fixture.UserManager;
        var context = _fixture.Context;

        var testUsers = new List<TestUserData>();
        var random = new Random(42); // Fixed seed for reproducible results

        var firstNames = new[]
        {
            "Alex", "Emma", "James", "Olivia", "William",
            "Sophia", "Benjamin", "Isabella", "Lucas", "Mia"
        };

        var lastNames = new[]
        {
            "Smith", "Johnson", "Williams", "Brown", "Jones",
            "Garcia", "Miller", "Davis", "Rodriguez", "Martinez"
        };

        var bios = new[]
        {
            "Gaming enthusiast and speedrunner. Love challenging myself with difficult games.",
            "RPG lover with a passion for story-driven games and character development.",
            "Competitive FPS player. Always looking for the next tournament.",
            "Indie game developer and player. Supporting small studios!",
            "Retro gaming collector with over 500 classic titles.",
            "Casual gamer who enjoys relaxing puzzle and adventure games.",
            "JRPG fan and anime lover. Turn-based combat is my jam!",
            "Strategy game mastermind. Chess, Civ, Total War - I play them all.",
            "Co-op gaming enthusiast. Gaming is better with friends!",
            "Open world exploration lover. If I can climb it, I will."
        };

        // Use different email domain to distinguish from test users that get cleaned up
        var emailDomain = "@reviewtester.dev";

        for (int i = 1; i <= 10; i++)
        {
            var firstName = firstNames[i - 1];
            var lastName = lastNames[i - 1];
            var email = $"reviewer{i:D2}{emailDomain}";
            var displayName = $"{firstName}{random.Next(100, 999)}";
            var bio = bios[i - 1];
            var password = $"TestPass{i}!";

            // Use a transaction to ensure both user and profile are created together
            using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                // Create the user
                var user = new User
                {
                    UserName = email,
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    Provider = "Local",
                    CreatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 365)),
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(user, password);

                if (result.Succeeded)
                {
                    // Create fully populated user profile
                    var profile = new UserProfile
                    {
                        UserId = user.Id,
                        FirstName = firstName,
                        LastName = lastName,
                        DisplayName = displayName,
                        Email = email,
                        Bio = bio,
                        DateOfBirth = DateTime.Now.AddYears(-random.Next(18, 50)).AddDays(-random.Next(0, 365)),
                        PhoneNumber = $"+1{random.Next(200, 999)}{random.Next(200, 999)}{random.Next(1000, 9999)}",
                        IsProfileUpdated = true,
                        IsPublic = true, // All profiles public for testing
                        CreatedAt = user.CreatedAt,
                        UpdatedAt = DateTime.UtcNow
                    };

                    context.UserProfiles.Add(profile);
                    await context.SaveChangesAsync();

                    // Commit transaction
                    await transaction.CommitAsync();

                    var testUserData = new TestUserData
                    {
                        Id = user.Id,
                        Email = email,
                        Password = password,
                        FirstName = firstName,
                        LastName = lastName,
                        DisplayName = displayName,
                        Bio = bio,
                        DateOfBirth = profile.DateOfBirth,
                        PhoneNumber = profile.PhoneNumber,
                        IsPublic = profile.IsPublic,
                        CreatedAt = user.CreatedAt
                    };

                    testUsers.Add(testUserData);
                    // NOTE: Not adding to _createdUserIds so these users won't be cleaned up
                }
                else
                {
                    await transaction.RollbackAsync();
                    Assert.Fail($"Failed to create permanent user {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Assert.Fail($"Exception creating permanent user {email}: {ex.Message}");
            }
        }

        // Verify we created 10 users
        Assert.Equal(10, testUsers.Count);

        // Write to file in Backend/testusers folder
        var testUsersFolder = GetTestUsersFolder();
        Directory.CreateDirectory(testUsersFolder);
        var fileName = $"test_users_{DateTime.UtcNow:yyyyMMdd_HHmmss}.txt";
        var filePath = Path.Combine(testUsersFolder, fileName);
        await WriteTestUsersToFile(testUsers, filePath);

        // Verify users exist in database
        var permanentUsers = await context.Users.Where(u => u.Email!.EndsWith(emailDomain)).CountAsync();
        Assert.Equal(10, permanentUsers);

        var permanentProfiles = await context.UserProfiles.Where(p => p.Email!.EndsWith(emailDomain)).CountAsync();
        Assert.Equal(10, permanentProfiles);
    }

    /// <summary>
    /// Creates permanent test users for manual testing purposes. These users will NOT be cleaned up automatically.
    /// Use this method once to generate test users that persist in the database for development/testing.
    /// </summary>
    [Fact(Skip = "Manual execution only - uncomment and run when you need to create permanent test users")]
    public async Task CreatePermanentTestUsers_ForManualTesting()
    {
        // Get services from fixture
        var userManager = _fixture.UserManager;
        var context = _fixture.Context;

        var testUsers = new List<TestUserData>();
        var random = new Random(42); // Fixed seed for reproducible results

        var firstNames = new[]
        {
            "Alex", "Emma", "James", "Olivia", "William", "Sophia", "Benjamin", "Isabella",
            "Lucas", "Mia", "Henry", "Charlotte", "Alexander", "Amelia", "Michael", "Harper",
            "Ethan", "Evelyn", "Daniel", "Abigail", "Matthew", "Emily", "Jackson", "Ella",
            "Sebastian", "Madison", "David", "Scarlett", "Joseph", "Victoria"
        };

        var lastNames = new[]
        {
            "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis",
            "Rodriguez", "Martinez", "Hernandez", "Lopez", "Gonzalez", "Wilson", "Anderson",
            "Thomas", "Taylor", "Moore", "Jackson", "Martin", "Lee", "Perez", "Thompson",
            "White", "Harris", "Sanchez", "Clark", "Ramirez", "Lewis", "Robinson"
        };

        var bios = new[]
        {
            "Gaming enthusiast and speedrunner",
            "Love RPGs and story-driven games",
            "Competitive FPS player",
            "Indie game developer and player",
            "Retro gaming collector",
            "Casual gamer who loves puzzles",
            "JRPG fan and anime lover",
            "Strategy game mastermind",
            "Co-op gaming with friends",
            "VR gaming pioneer",
            "Mobile gaming on the go",
            "Fighting game tournament player",
            "Simulation game addict",
            "Platformer speedrun enthusiast",
            "Open world exploration lover"
        };

        // Use different email domain to distinguish from test users that get cleaned up
        var emailDomain = "@testusers.dev";

        for (int i = 1; i <= 30; i++)
        {
            var firstName = firstNames[random.Next(firstNames.Length)];
            var lastName = lastNames[random.Next(lastNames.Length)];
            var email = $"user{i:D2}{emailDomain}";
            var displayName = $"{firstName}{random.Next(100, 999)}";
            var bio = bios[random.Next(bios.Length)];
            var password = "TestPass123!";

            // Use a transaction to ensure both user and profile are created together
            using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                // Create the user
                var user = new User
                {
                    UserName = email,
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    Provider = "Local",
                    CreatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 365)),
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(user, password);

                if (result.Succeeded)
                {
                    // Create user profile in the same transaction
                    var profile = new UserProfile
                    {
                        UserId = user.Id,
                        FirstName = firstName,
                        LastName = lastName,
                        DisplayName = displayName,
                        Email = email,
                        Bio = bio,
                        DateOfBirth = DateTime.Now.AddYears(-random.Next(18, 50)).AddDays(-random.Next(0, 365)),
                        PhoneNumber = $"+1{random.Next(200, 999)}{random.Next(200, 999)}{random.Next(1000, 9999)}",
                        IsProfileUpdated = true,
                        IsPublic = random.Next(0, 10) > 1, // 90% public profiles
                        CreatedAt = user.CreatedAt,
                        UpdatedAt = DateTime.UtcNow
                    };

                    context.UserProfiles.Add(profile);
                    await context.SaveChangesAsync();

                    // Commit transaction
                    await transaction.CommitAsync();

                    var testUserData = new TestUserData
                    {
                        Id = user.Id,
                        Email = email,
                        Password = password,
                        FirstName = firstName,
                        LastName = lastName,
                        DisplayName = displayName,
                        Bio = bio,
                        DateOfBirth = profile.DateOfBirth,
                        PhoneNumber = profile.PhoneNumber,
                        IsPublic = profile.IsPublic,
                        CreatedAt = user.CreatedAt
                    };

                    testUsers.Add(testUserData);
                    // NOTE: Not adding to _createdUserIds so these users won't be cleaned up
                }
                else
                {
                    await transaction.RollbackAsync();
                    Assert.Fail($"Failed to create permanent user {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Assert.Fail($"Exception creating permanent user {email}: {ex.Message}");
            }
        }

        // Verify we created 30 users
        Assert.Equal(30, testUsers.Count);

        // Write to file in parent directory with a different filename
        var currentDirectory = Directory.GetCurrentDirectory();
        var parentDirectory = Directory.GetParent(currentDirectory)?.FullName;
        if (parentDirectory != null)
        {
            await WriteTestUsersToFile(testUsers, Path.Combine(parentDirectory, "permanent_test_users.txt"));
        }

        // Verify users exist in database
        var permanentUsers = await context.Users.Where(u => u.Email!.EndsWith(emailDomain)).CountAsync();
        Assert.Equal(30, permanentUsers);

        var permanentProfiles = await context.UserProfiles.Where(p => p.Email!.EndsWith(emailDomain)).CountAsync();
        Assert.Equal(30, permanentProfiles);
    }

    /// <summary>
    /// Utility method to clean up the 10 review tester users when needed.
    /// Run this if you need to remove the review tester users from the database.
    /// </summary>
    [Fact(Skip = "Manual execution only - run when you need to clean up review tester users")]
    public async Task CleanupReviewTesterUsers_ForManualExecution()
    {
        // Get services from fixture
        var context = _fixture.Context;
        var emailDomain = "@reviewtester.dev";

        // Find and remove review tester users
        var reviewTesterUsers = context.Users.Where(u => u.Email!.EndsWith(emailDomain)).ToList();
        var reviewTesterProfiles = context.UserProfiles.Where(p => p.Email!.EndsWith(emailDomain)).ToList();

        if (reviewTesterUsers.Count > 0 || reviewTesterProfiles.Count > 0)
        {
            context.UserProfiles.RemoveRange(reviewTesterProfiles);
            context.Users.RemoveRange(reviewTesterUsers);
            await context.SaveChangesAsync();

            // Verify cleanup
            var remainingUsers = await context.Users.Where(u => u.Email!.EndsWith(emailDomain)).CountAsync();
            var remainingProfiles = await context.UserProfiles.Where(p => p.Email!.EndsWith(emailDomain)).CountAsync();

            Assert.Equal(0, remainingUsers);
            Assert.Equal(0, remainingProfiles);
        }
    }

    /// <summary>
    /// Utility method to clean up permanent test users when needed.
    /// Run this if you need to remove the permanent test users from the database.
    /// </summary>
    [Fact(Skip = "Manual execution only - run when you need to clean up permanent test users")]
    public async Task CleanupPermanentTestUsers_ForManualExecution()
    {
        // Get services from fixture
        var context = _fixture.Context;
        var emailDomain = "@testusers.dev";

        // Find and remove permanent test users
        var permanentUsers = context.Users.Where(u => u.Email!.EndsWith(emailDomain)).ToList();
        var permanentProfiles = context.UserProfiles.Where(p => p.Email!.EndsWith(emailDomain)).ToList();

        if (permanentUsers.Count > 0 || permanentProfiles.Count > 0)
        {
            context.UserProfiles.RemoveRange(permanentProfiles);
            context.Users.RemoveRange(permanentUsers);
            await context.SaveChangesAsync();

            // Verify cleanup
            var remainingUsers = await context.Users.Where(u => u.Email!.EndsWith(emailDomain)).CountAsync();
            var remainingProfiles = await context.UserProfiles.Where(p => p.Email!.EndsWith(emailDomain)).CountAsync();

            Assert.Equal(0, remainingUsers);
            Assert.Equal(0, remainingProfiles);
        }
    }

    private static string GetTestUsersFolder()
    {
        // Navigate to Backend folder from test project
        var currentDirectory = Directory.GetCurrentDirectory();
        var backendFolder = currentDirectory;

        // Find Backend folder
        while (backendFolder != null && !Directory.Exists(Path.Combine(backendFolder, "Backend")))
        {
            backendFolder = Directory.GetParent(backendFolder)?.FullName;
        }

        if (backendFolder == null)
        {
            throw new DirectoryNotFoundException("Could not find Backend folder");
        }

        return Path.Combine(backendFolder, "Backend", "testusers");
    }

    private static async Task WriteTestUsersToFile(List<TestUserData> users, string filePath)
    {
        var lines = new List<string>
        {
            "=== TEST USERS FOR PATCH.NOTES ===",
            $"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC",
            $"Total Users: {users.Count}",
            "",
            "FORMAT: ID | Email | Password | Name | Display Name | Bio | Phone | Public | Created",
            new string('=', 100),
            ""
        };

        foreach (var user in users.OrderBy(u => u.Email))
        {
            var createdDate = user.CreatedAt.ToString("yyyy-MM-dd");
            var fullName = $"{user.FirstName} {user.LastName}";
            var publicStatus = user.IsPublic ? "Public" : "Private";
            var birthYear = user.DateOfBirth?.Year.ToString() ?? "N/A";

            lines.Add($"{user.Id}");
            lines.Add($"  Email: {user.Email}");
            lines.Add($"  Password: {user.Password}");
            lines.Add($"  Name: {fullName}");
            lines.Add($"  Display Name: {user.DisplayName}");
            lines.Add($"  Bio: {user.Bio}");
            lines.Add($"  Phone: {user.PhoneNumber}");
            lines.Add($"  Birth Year: {birthYear}");
            lines.Add($"  Profile: {publicStatus}");
            lines.Add($"  Created: {createdDate}");
            lines.Add("");
        }

        lines.Add("");
        lines.Add("=== LOGIN CREDENTIALS SUMMARY ===");
        foreach (var user in users.OrderBy(u => u.Email))
        {
            lines.Add($"{user.Email} | {user.Password}");
        }

        await File.WriteAllLinesAsync(filePath, lines);
    }
}

public class TestUserData
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsPublic { get; set; }
    public DateTime CreatedAt { get; set; }
}