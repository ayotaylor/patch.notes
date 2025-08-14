using Backend.Models.DTO.Social;
using Backend.Models.Social;

namespace Backend.Mapping
{
    public static class SocialMapper
    {
        public static FavoriteDto ToDto(this Favorite favorite)
        {
            return new FavoriteDto
            {
                UserId = favorite.UserId,
                //GameId = favorite.GameId,
                Game = favorite.Game?.ToDto(),
            };
        }

        public static LikeDto ToDto(this Like like)
        {
            return new LikeDto
            {
                UserId = like.UserId,
                //GameId = like.GameId,
                Game = like.Game?.ToDto(),
            };
        }

        public static ReviewDto ToDto(this Review review)
        {
            return new ReviewDto
            {
                Id = review.Id,
                UserId = review.UserId,
                GameId = review.GameId,
                Rating = review.Rating,
                ReviewText = review.ReviewText,
                ReviewDate = review.ReviewDate,
                CreatedAt = review.CreatedAt,
                UpdatedAt = review.UpdatedAt,
                UserDisplayName = review.User?.DisplayName ?? review.User?.FirstName + " " + review.User?.LastName,
                GameTitle = review.Game?.Name
            };
        }
    }
}