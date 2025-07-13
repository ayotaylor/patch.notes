namespace Backend.Models.DTO.Game
{
    public class PlayerPerspectiveDto
    {
        public int? IgdbId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
    }
}