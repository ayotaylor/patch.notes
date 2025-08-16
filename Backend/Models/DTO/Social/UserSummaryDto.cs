namespace Backend.Models.DTO.Social
{
    public class UserSummaryDto
    {
        public Guid Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string? ProfileUrlImageUrl { get; set; }
    }
}