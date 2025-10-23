using Backend.Models.DTO;

namespace Backend.Tests.Helpers;

public static class TestDataGenerator
{
    private static readonly Random Random = new Random();

    private static readonly List<string> FirstNames = new List<string>
    {
        "Alex", "Jordan", "Morgan", "Casey", "Riley",
        "Taylor", "Jamie", "Avery", "Quinn", "Dakota"
    };

    private static readonly List<string> LastNames = new List<string>
    {
        "Smith", "Johnson", "Williams", "Brown", "Jones",
        "Garcia", "Miller", "Davis", "Rodriguez", "Martinez"
    };

    private static readonly List<string> BioTemplates = new List<string>
    {
        "Passionate gamer with a love for RPGs and indie games.",
        "Console enthusiast, always looking for the next great adventure.",
        "Retro gaming fan who appreciates the classics.",
        "Competitive gamer focused on strategy and skill.",
        "Casual player who enjoys story-driven experiences.",
        "Gaming content creator and reviewer.",
        "Long-time gamer exploring new genres and experiences.",
        "Collector of games across all platforms.",
        "Speed runner and achievement hunter.",
        "Gaming is life! Always up for multiplayer sessions."
    };

    private static readonly List<string> ReviewPositiveAdjectives = new List<string>
    {
        "amazing", "fantastic", "incredible", "outstanding", "excellent",
        "brilliant", "superb", "phenomenal", "wonderful", "exceptional"
    };

    private static readonly List<string> ReviewNegativeAdjectives = new List<string>
    {
        "disappointing", "mediocre", "lackluster", "underwhelming", "frustrating",
        "boring", "tedious", "repetitive", "flawed", "problematic"
    };

    private static readonly List<string> ReviewTemplates = new List<string>
    {
        "This game is absolutely {adjective}! The gameplay mechanics are {detail} and the story kept me engaged throughout. {recommendation}",
        "I found this to be a {adjective} experience. The graphics are {detail} and the sound design really adds to the atmosphere. {recommendation}",
        "After spending many hours with this game, I can say it's {adjective}. The {detail} gameplay loop is what keeps me coming back. {recommendation}",
        "{adjective} title that {detail}. The developers really nailed the core mechanics. {recommendation}",
        "This game offers a {adjective} experience with {detail} gameplay. {recommendation}",
        "I was {emotion} by this game. The {aspect} is {adjective} and really stands out. {recommendation}",
        "A {adjective} entry in the series. The {aspect} has been {detail}. {recommendation}",
        "The game's {aspect} is {adjective}. {detail}. {recommendation}"
    };

    private static readonly Dictionary<int, List<string>> RatingSpecificDetails = new Dictionary<int, List<string>>
    {
        { 5, new List<string> { "polished to perfection", "incredibly well-designed", "addictive and rewarding", "immersive and engaging" } },
        { 4, new List<string> { "well-implemented", "mostly solid", "engaging for the most part", "well-crafted" } },
        { 3, new List<string> { "decent but not groundbreaking", "average with some highlights", "acceptable with room for improvement", "mixed with both strengths and weaknesses" } },
        { 2, new List<string> { "lacking polish", "in need of improvement", "hampered by technical issues", "not living up to potential" } },
        { 1, new List<string> { "severely lacking", "plagued with issues", "fundamentally broken", "riddled with problems" } }
    };

    private static readonly Dictionary<int, List<string>> RatingRecommendations = new Dictionary<int, List<string>>
    {
        { 5, new List<string> { "Highly recommended!", "A must-play!", "Don't miss this one!", "Absolutely worth your time!" } },
        { 4, new List<string> { "Definitely worth checking out.", "I'd recommend this to fans of the genre.", "Worth playing if it interests you.", "A solid choice." } },
        { 3, new List<string> { "Worth a try if you're interested.", "Might appeal to some players.", "Consider it if you're a fan.", "Your mileage may vary." } },
        { 2, new List<string> { "Hard to recommend.", "Wait for patches or a sale.", "Proceed with caution.", "Maybe skip this one." } },
        { 1, new List<string> { "Cannot recommend.", "Save your money.", "Avoid unless drastically improved.", "Not worth it." } }
    };

    private static readonly List<string> GameAspects = new List<string>
    {
        "combat system", "level design", "narrative", "character development",
        "multiplayer experience", "progression system", "art direction",
        "soundtrack", "replayability", "world building"
    };

    private static readonly List<string> Emotions = new List<string>
    {
        "impressed", "surprised", "captivated", "disappointed", "thrilled",
        "let down", "blown away", "underwhelmed", "delighted", "frustrated"
    };

    public static RegisterRequest GenerateRegisterRequest(int userIndex)
    {
        var firstName = FirstNames[userIndex % FirstNames.Count];
        var lastName = LastNames[userIndex % LastNames.Count];
        var username = $"{firstName.ToLower()}{lastName.ToLower()}{userIndex}";
        var email = $"{username}@testuser.com";
        var password = $"TestPass{userIndex}!";

        return new RegisterRequest
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Password = password,
            ConfirmPassword = password
        };
    }

    public static UpdateUserProfileDto GenerateUserProfile(string firstName, string lastName, int userIndex)
    {
        var displayName = $"{firstName} {lastName}";
        var bio = BioTemplates[userIndex % BioTemplates.Count];
        var dateOfBirth = DateTime.UtcNow.AddYears(-Random.Next(18, 45)).AddDays(-Random.Next(0, 365));

        return new UpdateUserProfileDto
        {
            FirstName = firstName,
            LastName = lastName,
            DisplayName = displayName,
            Bio = bio,
            DateOfBirth = dateOfBirth,
            //IsPublic = true
        };
    }

    public static string GenerateReviewText(int rating)
    {
        var template = ReviewTemplates[Random.Next(ReviewTemplates.Count)];
        var adjective = rating >= 4
            ? ReviewPositiveAdjectives[Random.Next(ReviewPositiveAdjectives.Count)]
            : rating <= 2
                ? ReviewNegativeAdjectives[Random.Next(ReviewNegativeAdjectives.Count)]
                : Random.Next(2) == 0
                    ? ReviewPositiveAdjectives[Random.Next(ReviewPositiveAdjectives.Count)]
                    : ReviewNegativeAdjectives[Random.Next(ReviewNegativeAdjectives.Count)];

        var detail = RatingSpecificDetails[rating][Random.Next(RatingSpecificDetails[rating].Count)];
        var recommendation = RatingRecommendations[rating][Random.Next(RatingRecommendations[rating].Count)];
        var aspect = GameAspects[Random.Next(GameAspects.Count)];
        var emotion = Emotions[Random.Next(Emotions.Count)];

        var reviewText = template
            .Replace("{adjective}", adjective)
            .Replace("{detail}", detail)
            .Replace("{recommendation}", recommendation)
            .Replace("{aspect}", aspect)
            .Replace("{emotion}", emotion);

        return reviewText;
    }

    public static int GenerateRealisticRating()
    {
        // Generate ratings with a bell curve distribution, favoring 3-4 stars
        var random = Random.NextDouble();

        if (random < 0.10) return 1; // 10%
        if (random < 0.20) return 2; // 10%
        if (random < 0.50) return 3; // 30%
        if (random < 0.80) return 4; // 30%
        return 5; // 20%
    }
}
