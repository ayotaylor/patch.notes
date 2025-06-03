using Microsoft.AspNetCore.Identity;

namespace Backend.Models.Auth
{
    public class User : IdentityUser
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? Provider { get; set; }  // db, google, facebook
        public string? ProviderId { get; set; }

        // navigation properties
        public UserProfile? UserProfile { get; set; }
    }
}