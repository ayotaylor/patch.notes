using Backend.Models.DTO.Game;

namespace Backend.Models.DTO.Social
{
    public class LikeDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid GameId { get; set; }
        public DateTime CreatedAt { get; set; }
        public GameDto? Game { get; set; }
    }
}