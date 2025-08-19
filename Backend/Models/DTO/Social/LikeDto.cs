using System.Text.Json.Serialization;

namespace Backend.Models.DTO.Social
{
    public class LikeDto
    {
        public int GameId { get; set; }
        public DateTime CreatedAt { get; set; }
        public GameSummaryDto? Game { get; set; }
    }
}