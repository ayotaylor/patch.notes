using Backend.Models.DTO.Game;

namespace Backend.Models.DTO.Social
{
    public class LikeDto
    {
        public Guid UserId { get; set; }
        public int GameId { get; set; }
        public DateTime CreatedAt { get; set; }
        public GameDto? Game { get; set; }
    }
}