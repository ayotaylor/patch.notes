using Backend.Models;
using Backend.Models.Auth;
using Backend.Models.Game.Social;

public class UserProfile : BaseEntity
{
    public string UserId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? DisplayName { get; set; }
    public string? Bio { get; set; }
    public string? ProfileUrlImageUrl { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public bool IsProfileUpdated { get; set; } = false;
    public bool IsPublic { get; set; } = true; // Default to public profile
    // Navigation Properties
    public User User { get; set; } = null!;
    public virtual ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
    public virtual ICollection<Like> Likes { get; set; } = new List<Like>();
}