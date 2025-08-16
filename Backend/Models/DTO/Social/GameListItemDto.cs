using Backend.Models.DTO.Game;

namespace Backend.Models.DTO.Social
{
    public class GameListItemDto
    {
        public Guid Id { get; set; }
        public Guid GameListId { get; set; }
        public Guid GameId { get; set; }
        public int Order { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }

        public GameSummaryDto? Game { get; set; }
    }
}