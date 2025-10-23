# Test Users and Reviews - Usage Guide

This guide explains how to use the test suite to create test users and reviews for the Patch.Notes application.

## Overview

The test suite provides two main test classes:

1. **UserCreationTests** - Creates test users with fully populated profiles
2. **ReviewCreationTests** - Creates realistic reviews for test users using popular games

## Prerequisites

- MySQL database running and accessible
- Connection string configured in `TestSetup.cs` (default: `Server=localhost;Port=3306;Database=patchnotesdb;Uid=root;Pwd=password;`)
- Database seeded with games (for review creation)

## Quick Start

### Step 1: Create Test Users

Run the test method that creates 10 permanent test users:

```csharp
// In UserCreationTests.cs
[Fact(Skip = "Manual execution only")]
public async Task Create10PermanentTestUsers_WithCredentialsFile()
```

**To run this test:**
1. Open `UserCreationTests.cs`
2. Remove the `Skip` attribute or comment it out
3. Run the test
4. Re-add the `Skip` attribute after running (to prevent accidental re-runs)

**What it does:**
- Creates 10 users with email pattern: `reviewer01@reviewtester.dev`, `reviewer02@reviewtester.dev`, etc.
- Each user has a unique password: `TestPass1!`, `TestPass2!`, etc.
- Each user has a fully populated profile (DisplayName, Bio, DateOfBirth, PhoneNumber, etc.)
- Writes credentials to `Backend/testusers/test_users_YYYYMMDD_HHMMSS.txt`

**Example credentials file content:**
```
=== TEST USERS FOR PATCH.NOTES ===
Generated: 2025-10-22 14:30:00 UTC
Total Users: 10

c7b8a9d0-1234-5678-9abc-def012345678
  Email: reviewer01@reviewtester.dev
  Password: TestPass1!
  Name: Alex Smith
  Display Name: Alex456
  Bio: Gaming enthusiast and speedrunner...
  ...
```

### Step 2: Create Reviews for Test Users

After creating test users, run the review creation test:

```csharp
// In ReviewCreationTests.cs
[Fact(Skip = "Manual execution only")]
public async Task CreateReviewsForTestUsers()
```

**To run this test:**
1. Open `ReviewCreationTests.cs`
2. Remove the `Skip` attribute or comment it out
3. Run the test
4. Re-add the `Skip` attribute after running

**What it does:**
- Finds all users with `@reviewtester.dev` email domain
- Gets 20 popular games from the database (using `GetPopularGamesAsync`)
- Creates 5 reviews per user with:
  - Realistic rating distribution (bell curve, favoring 3-4 stars)
  - Varied, contextual review text based on rating
  - Random review dates within the past year
- Prevents duplicate reviews (one review per user per game)

**Example output:**
```
Found 20 popular games to review
Creating reviews for 10 users

Creating reviews for user: Alex456 (reviewer01@reviewtester.dev)
  ✓ Review #1: The Witcher 3 - 5/5 stars
  ✓ Review #2: Elden Ring - 4/5 stars
  ✓ Review #3: Cyberpunk 2077 - 3/5 stars
  ✓ Review #4: Red Dead Redemption 2 - 5/5 stars
  ✓ Review #5: God of War - 4/5 stars
  Created 5 reviews for Alex456
...

=== TOTAL: Created 50 reviews for 10 users ===
```

## Test Methods Reference

### UserCreationTests.cs

| Method | Purpose | Cleanup |
|--------|---------|---------|
| `Create10PermanentTestUsers_WithCredentialsFile()` | Creates 10 test users with credentials file | Manual (see below) |
| `CreatePermanentTestUsers_ForManualTesting()` | Creates 30 test users (legacy) | Manual (see below) |
| `CreateThirtyTestUsers_ShouldSucceed()` | Creates 30 users for automated testing | Automatic |
| `CleanupReviewTesterUsers_ForManualExecution()` | Removes `@reviewtester.dev` users | N/A |
| `CleanupPermanentTestUsers_ForManualExecution()` | Removes `@testusers.dev` users | N/A |

### ReviewCreationTests.cs

| Method | Purpose | Cleanup |
|--------|---------|---------|
| `CreateReviewsForTestUsers()` | Creates 5 reviews per `@reviewtester.dev` user | Manual (see below) |
| `CreateAndCleanupReviewsForTestUsers_ShouldSucceed()` | Creates and auto-cleans reviews | Automatic |
| `CreateReviewsForUsers(List<Guid>)` | Helper method to create reviews for specific users | N/A |
| `CleanupAllReviewTesterReviews_ForManualExecution()` | Removes all reviews by test users | N/A |

## Cleanup

### Remove Test Users from Database

To remove the 10 review tester users:

```csharp
// In UserCreationTests.cs
[Fact(Skip = "Manual execution only")]
public async Task CleanupReviewTesterUsers_ForManualExecution()
```

1. Remove the `Skip` attribute
2. Run the test
3. Re-add the `Skip` attribute

This will delete all users with `@reviewtester.dev` email domain and their associated profiles.

### Remove Test Reviews from Database

To remove all reviews created by test users:

```csharp
// In ReviewCreationTests.cs
[Fact(Skip = "Manual execution only")]
public async Task CleanupAllReviewTesterReviews_ForManualExecution()
```

1. Remove the `Skip` attribute
2. Run the test
3. Re-add the `Skip` attribute

## Customization

### Test Data Generator

The `TestDataGenerator` class in `Helpers/TestDataGenerator.cs` contains:

- **User Data**: First names, last names, bios, profile templates
- **Review Data**: Rating distributions, review templates, adjectives
- **Methods**:
  - `GenerateRegisterRequest(int userIndex)` - Creates registration data
  - `GenerateUserProfile(...)` - Creates profile data
  - `GenerateReviewText(int rating)` - Creates rating-appropriate review text
  - `GenerateRealisticRating()` - Bell curve rating distribution

You can modify these to customize the generated data.

### Review Text Examples

Reviews are generated with contextual text based on ratings:

**5 stars:**
```
This game is absolutely amazing! The gameplay mechanics are polished to perfection
and the story kept me engaged throughout. Highly recommended!
```

**3 stars:**
```
I found this to be a decent experience. The graphics are acceptable with room for
improvement and the sound design really adds to the atmosphere. Worth a try if
you're interested.
```

**1 star:**
```
After spending many hours with this game, I can say it's disappointing. The
fundamentally broken gameplay loop is what keeps me from recommending it. Cannot recommend.
```

## Database Schema

### Users
- Email: `reviewer01@reviewtester.dev` through `reviewer10@reviewtester.dev`
- Passwords: `TestPass1!` through `TestPass10!`
- Provider: "Local"
- EmailConfirmed: true

### UserProfiles
- All fields populated (FirstName, LastName, DisplayName, Bio, DateOfBirth, PhoneNumber)
- IsProfileUpdated: true
- IsPublic: true

### Reviews
- Rating: 1-5 (bell curve distribution)
- ReviewText: ~50-150 words, contextual to rating
- ReviewDate: Random date within past year
- One review per user per game (enforced)

## Troubleshooting

### "No popular games found in database"
- Ensure your database has games seeded
- Games need `Hypes > 0` and `Rating > 0` to be considered popular
- Check `GameService.GetPopularGamesAsync()` returns games

### "No review tester users found"
- Run `Create10PermanentTestUsers_WithCredentialsFile` first
- Check database for users with `@reviewtester.dev` emails

### "Failed to create user"
- Check database connection in `TestSetup.cs`
- Ensure MySQL is running
- Check for duplicate emails in database

### Test keeps getting skipped
- Remove or comment out the `Skip` attribute from the `[Fact]` attribute
- Example: Change `[Fact(Skip = "...")]` to `[Fact]`

## Best Practices

1. **Always re-add Skip attribute** after manual test execution to prevent accidental re-runs
2. **Check for existing users** before creating new ones to avoid duplicates
3. **Clean up test data** when done testing to keep database clean
4. **Backup credentials file** if you need to preserve login information
5. **Use different email domains** for different test scenarios to easily identify and clean up

## Example Workflow

```bash
# 1. Create test users
# Remove Skip from Create10PermanentTestUsers_WithCredentialsFile, run test, re-add Skip

# 2. Check credentials file
cat Backend/testusers/test_users_*.txt

# 3. Create reviews
# Remove Skip from CreateReviewsForTestUsers, run test, re-add Skip

# 4. Verify in database
mysql -u root -p patchnotesdb
SELECT COUNT(*) FROM Users WHERE Email LIKE '%@reviewtester.dev%';
SELECT COUNT(*) FROM Reviews WHERE UserId IN (
  SELECT Id FROM UserProfiles WHERE Email LIKE '%@reviewtester.dev%'
);

# 5. Test login with credentials from file

# 6. When done, cleanup
# Remove Skip from cleanup methods, run tests, re-add Skip
```

## Advanced Usage

### Creating Reviews for Specific Users

You can programmatically create reviews for specific users:

```csharp
var reviewTests = new ReviewCreationTests(fixture);
await reviewTests.InitializeAsync();

var userProfileIds = new List<Guid>
{
    Guid.Parse("your-user-profile-id-1"),
    Guid.Parse("your-user-profile-id-2")
};

await reviewTests.CreateReviewsForUsers(userProfileIds);
```

### Custom Number of Reviews per User

Modify the loop in `CreateReviewsForUsers`:

```csharp
// Change from 5 to desired number
var selectedGameIndices = new HashSet<int>();
while (selectedGameIndices.Count < 10 && selectedGameIndices.Count < popularGames.Count)
{
    selectedGameIndices.Add(random.Next(popularGames.Count));
}
```

## Support

For issues or questions:
1. Check database connection and configuration
2. Verify test prerequisites are met
3. Check console output for detailed error messages
4. Review this README for common solutions
