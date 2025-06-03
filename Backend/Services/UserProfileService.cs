using System.Linq.Expressions;
using Backend.Data;
using Backend.Data.Configuration.Mapping;
using Microsoft.EntityFrameworkCore;

public class UserProfileService : IUserProfileService
{
    private readonly ApplicationDbContext _context;

    private readonly ILogger<UserProfileService> _logger;

    public UserProfileService(ApplicationDbContext context,
        ILogger<UserProfileService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<UserProfileDto> GetUserProfileAsync(string userId)
    {
        // var profile = await _context.Users.
        //     .Include(p => p.UserProfile)
        //     .FirstOrDefaultAsync(p => p.UserId == userId);
        var profile = await _context.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile == null)
        {
            // Create a default profile if none exists
            profile = await CreateDefaultProfileAsync(userId);
        }

        return UserProfileMapper.ToDto(profile);
    }

    public async Task<UserProfileDto> UpdateUserProfileAsync(string userId, UpdateUserProfileDto updateDto)
    {
        var profile = await _context.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile == null)
        {
            return await CreateUserProfileAsync(userId, updateDto);
        }

        // Update the profile with the new data
        UserProfileMapper.updateUserProfile(updateDto, profile);
        profile.UpdatedAt = DateTime.UtcNow;
        profile.IsProfileUpdated = true;
 
        await _context.SaveChangesAsync();

        // TODO: maybe change this to return the updated profile directly or just a success message/object
        _logger.LogInformation("User profile updated successfully for user ID: {UserId}", userId);
        return await GetUserProfileAsync(userId);
    }

    public async Task<UserProfileDto> CreateUserProfileAsync(string userId, UpdateUserProfileDto createDto)
    {
        // Check if a profile already exists for the user
        var existingProfile = await _context.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId);
        if (existingProfile != null)
        {
            _logger.LogWarning("User profile already exists for user ID: {UserId}", userId);
            return await GetUserProfileAsync(userId);
        }
        // Create a new profile if none exists  
        var user = await _context.Users.FindAsync(userId);
        var profile = UserProfileMapper.FromUpdateDto(createDto);
        if (profile == null)
        {
            _logger.LogError("Failed to create user profile from DTO. --> DTO conversion error.");
            throw new ArgumentException("Invalid user profile data.");
        }
        profile.UserId = userId;
        profile.FirstName = user?.FirstName ?? "";
        profile.LastName = user?.LastName ?? "";
        profile.Email = user?.Email ?? "";
        profile.CreatedAt = DateTime.UtcNow;
        profile.UpdatedAt = DateTime.UtcNow;
        // in the case the user is already registered but does not have a profile yet
        if (createDto.IsProfileUpdated.GetValueOrDefault() == true)
        {
            profile.IsProfileUpdated = true;
        }

        _context.UserProfiles.Add(profile);
        await _context.SaveChangesAsync();
        return await GetUserProfileAsync(userId);
    }

    private async Task<UserProfile> CreateDefaultProfileAsync(string userId)
    {
        var user = await _context.Users.FindAsync(userId);
        var profile = new UserProfile
        {
            UserId = userId,
            DisplayName = user?.UserName ?? "User",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.UserProfiles.Add(profile);
        await _context.SaveChangesAsync();

        return profile;
    }
}