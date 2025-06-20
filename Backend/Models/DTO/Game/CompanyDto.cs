namespace Backend.Models.DTO.Game
{
    public class CompanyDto
    {
        public Guid Id { get; set; }
        public int? IgdbId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Country { get; set; }
        public string? Description { get; set; }
        public string? Url { get; set; }
        public List<string> Roles { get; set; } = []; // Developer or Publisher
    }
}