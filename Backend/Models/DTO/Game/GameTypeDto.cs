namespace Backend.Models.DTO.Game
{
    public class GameTypeDto
    {
        public Guid Id { get; set; }
        public int? IgdbId { get; set; }
        public string Type { get; set; } = string.Empty;
    }
}
