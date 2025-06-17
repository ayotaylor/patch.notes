namespace Backend.Models.DTO.Game
{
    public class GameReleaseDateDto
    {
        public Guid Id { get; set; }
        public PlatformDto Platform { get; set; } = new();
        public RegionDto Region { get; set; } = new();
        public long Date { get; set; }
    }
}