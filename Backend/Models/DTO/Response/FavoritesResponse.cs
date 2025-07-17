using Backend.Models.DTO.Game;

namespace Backend.Models.DTO.Response
{
    public class FavoritesResponse
    {
        public Guid UserId { get; set; }
        public DateTime AddedAt { get; set; }
        public GameDto? Game { get; set; }
    }
}