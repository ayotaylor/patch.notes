using System.Text.Json.Serialization;

namespace Backend.Models.DTO.Social
{
    public class FavoriteDto
    {
        public Guid UserId { get; set; }
        public int GameId { get; set; }
        public DateTime AddedAt { get; set; }
        public GameSummaryDto? Game { get; set; }
    }
}