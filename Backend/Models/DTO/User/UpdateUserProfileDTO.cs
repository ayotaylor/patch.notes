public class UpdateUserProfileDto
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? DisplayName { get; set; }
    public string? Bio { get; set; }
    public string? ProfileImageUrl { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? PhoneNumber { get; set; }

    public string? Email { get; set; }

    public bool? IsProfileUpdated { get; set; } 
}