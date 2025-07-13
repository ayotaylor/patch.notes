namespace Backend.Models.Helpers
{
    public class UserInteractions
    {
        public HashSet<Guid> UserLikes { get; set; } = new();
        public HashSet<Guid> UserFavorites { get; set; } = new();
    }
}