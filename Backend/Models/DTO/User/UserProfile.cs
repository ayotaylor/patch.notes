using Backend.Models.Auth;

public class UserProfile
{
    public int Id { get; set; }
    public string UserId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? DisplayName { get; set; }
    public string? Bio { get; set; }
    public string? ProfileUrlImageUrl { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public User User { get; set; } = null!;
    public string? Email { get; set; }
    public bool IsProfileUpdated { get; set; } = false;
    public bool IsPublic { get; set; } = true; // Default to public profile
}