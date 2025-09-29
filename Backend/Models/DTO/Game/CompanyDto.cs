namespace Backend.Models.DTO.Game
{
    public class CompanyDto
    {
        public int? IgdbId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? Country { get; set; }
        public string? Description { get; set; }
        public string? Url { get; set; }
        public List<string> Roles { get; set; } = []; // Developer or Publisher
    }
}