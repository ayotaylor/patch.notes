namespace Backend.Models.DTO.Game
{
    public class GameCoverDto
    {
        public Guid Id { get; set; }
        public string Url { get; set; } = string.Empty;
        public string? IgdbImageId { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }
    }
}

