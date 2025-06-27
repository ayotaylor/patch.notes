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
                Id = favorite.Id,
                UserId = favorite.UserId,
                GameId = favorite.GameId,
                Game = favorite.Game?.ToDto(),
            };
        }

        public static LikeDto ToDto(this Like like)
        {
            return new LikeDto
            {
                Id = like.Id,
                UserId = like.UserId,
                GameId = like.GameId,
                Game = like.Game?.ToDto(),
            };
        }
    }
}