namespace Backend.Models.DTO.Social
{
    public class FollowDto
    {
        public Guid Id { get; set; }
        public UserSummaryDto Follower { get; set; } = null!;
        public UserSummaryDto Following { get; set; } = null!;
        public DateTime FollowedAt { get; set; }
    }

    public class CreateFollowDto
    {
        public Guid FollowingId { get; set; }
    }

    public class FollowStatsDto
    {
        public int FollowersCount { get; set; }
        public int FollowingCount { get; set; }
        public bool IsFollowing { get; set; }
    }
}