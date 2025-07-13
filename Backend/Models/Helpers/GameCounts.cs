namespace Backend.Models.Helpers
{
    public class GameCounts
    {
        public Dictionary<Guid, int> LikesCount { get; set; } = new();
        public Dictionary<Guid, int> FavoritesCount { get; set; } = new();
    }
}