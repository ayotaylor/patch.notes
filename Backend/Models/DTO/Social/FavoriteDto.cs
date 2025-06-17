using Backend.Models.DTO.Game;

namespace Backend.Models.DTO.Social
{
    public class FavoriteDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid GameId { get; set; }
        public DateTime AddedAt { get; set; }
        public GameDto? Game { get; set; }
    }
}