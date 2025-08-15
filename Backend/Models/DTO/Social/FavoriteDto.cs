using System.Text.Json.Serialization;
using Backend.Models.DTO.Game;

namespace Backend.Models.DTO.Social
{
    public class FavoriteDto
    {
        public Guid UserId { get; set; }
        public int GameId { get; set; }
        public DateTime AddedAt { get; set; }
        public GameDto? Game { get; set; }
    }
}