namespace Backend.Models.DTO
{
    public class UserInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}