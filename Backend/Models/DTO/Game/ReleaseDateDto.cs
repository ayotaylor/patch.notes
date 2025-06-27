namespace Backend.Models.DTO.Game
{
    public class ReleaseDateDto
    {
        public Guid Id { get; set; }
        public PlatformDto Platform { get; set; } = new();
        public RegionDto Region { get; set; } = new();
        public DateTime? Date { get; set; }
    }
}