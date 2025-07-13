namespace Backend.Models.DTO.Game
{
    public class GameCoverDto
    {
        public string Url { get; set; } = string.Empty;
        public string? ImageId { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }
    }
}

