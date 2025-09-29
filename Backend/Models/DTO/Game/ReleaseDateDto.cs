namespace Backend.Models.DTO.Game
{
    public class ReleaseDateDto
    {
        public PlatformDto Platform { get; set; } = new();
        public RegionDto Region { get; set; } = new();
        public long? Date { get; set; }
    }
}