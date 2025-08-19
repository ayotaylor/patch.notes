using System.Text.Json.Serialization;

namespace Backend.Models.DTO.Social
{
    public class FavoriteDto
    {
        public int GameId { get; set; }
        public DateTime AddedAt { get; set; }
        public GameSummaryDto? Game { get; set; }
    }
}