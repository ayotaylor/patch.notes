using Backend.Models.DTO.Game;
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
                //UserId = favorite.UserId,
                GameId = favorite.Game?.IgdbId ?? 0,
                AddedAt = favorite.AddedAt,
                Game = favorite.Game?.ToSummaryDto(),
            };
        }

        public static LikeDto ToDto(this Like like)
        {
            return new LikeDto
            {
                //UserId = like.UserId,
                GameId = like.Game?.IgdbId ?? 0,
                CreatedAt = like.CreatedAt,
                Game = like.Game?.ToSummaryDto(),
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
                GameTitle = review.Game?.Name,
                LikeCount = review.Likes?.Count,
                User = review.User != null ? new UserSummaryDto
                {
                    Id = Guid.Parse(review.User.UserId),
                    DisplayName = review.User.DisplayName ?? review.User.FirstName + " " + review.User.LastName,
                    ProfileUrlImageUrl = review.User.ProfileUrlImageUrl,
                } : null,
                Game = review.Game != null ? new GameSummaryDto
                {
                    IgdbId = review.Game.IgdbId,
                    Name = review.Game.Name,
                    Slug = review.Game.Slug,
                    CoverUrl = review.Game.Covers?.FirstOrDefault()?.Url
                } : null
            };
        }

        public static GameListDto ToDto(this GameList gameList)
        {
            return new GameListDto
            {
                Id = gameList.Id,
                UserId = gameList.UserId,
                Title = gameList.Name,
                Description = gameList.Description,
                IsPublic = gameList.IsPublic,
                CreatedAt = gameList.CreatedAt,
                UpdatedAt = gameList.UpdatedAt,
                UserDisplayName = gameList.User?.DisplayName ?? gameList.User?.FirstName + " " + gameList.User?.LastName,
                GameCount = gameList.GameListItems?.Count ?? 0,
                CommentsCount = gameList.Comments?.Count ?? 0,
                LikesCount = gameList.Likes?.Count ?? 0,
                User = gameList.User != null ? new UserSummaryDto
                {
                    Id = Guid.Parse(gameList.User.UserId),
                    DisplayName = gameList.User.DisplayName ?? gameList.User.FirstName + " " + gameList.User.LastName,
                    ProfileUrlImageUrl = gameList.User.ProfileUrlImageUrl,
                } : null,
                Games = gameList.GameListItems?.Select(gli => gli.ToDto()).ToList(),
                Comments = gameList.Comments?.Select(c => c.ToSummaryDto()).ToList()
            };
        }

        public static GameListItemDto ToDto(this GameListItem gameListItem)
        {
            return new GameListItemDto
            {
                Id = gameListItem.Id,
                GameListId = gameListItem.GameListId,
                GameId = gameListItem.GameId,
                Order = gameListItem.Order,
                Note = gameListItem.Note,
                CreatedAt = gameListItem.CreatedAt,
                Game = gameListItem.Game != null ? new GameSummaryDto
                {
                    IgdbId = gameListItem.Game.IgdbId,
                    Name = gameListItem.Game.Name,
                    Slug = gameListItem.Game.Slug,
                    CoverUrl = gameListItem.Game.Covers?.FirstOrDefault()?.Url
                } : null
            };
        }

        public static CommentDto ToDto(this Comment comment)
        {
            return new CommentDto
            {
                Id = comment.Id,
                UserId = comment.UserId,
                Content = comment.Content,
                ReviewId = comment.ReviewId,
                GameListId = comment.GameListId,
                ParentCommentId = comment.ParentCommentId,
                CreatedAt = comment.CreatedAt,
                UpdatedAt = comment.UpdatedAt,
                UserDisplayName = comment.User?.DisplayName ?? comment.User?.FirstName + " " + comment.User?.LastName,
                LikeCount = comment.Likes?.Count ?? 0,
                ReplyCount = comment.Replies?.Count ?? 0,
                User = comment.User != null ? new UserSummaryDto
                {
                    Id = Guid.Parse(comment.User.UserId),
                    DisplayName = comment.User.DisplayName ?? comment.User.FirstName + " " + comment.User.LastName,
                    ProfileUrlImageUrl = comment.User.ProfileUrlImageUrl,
                } : null,
                Replies = comment.Replies?.Select(r => r.ToSummaryDto()).ToList()
            };
        }

        public static CommentSummaryDto ToSummaryDto(this Comment comment)
        {
            return new CommentSummaryDto
            {
                Id = comment.Id,
                Content = comment.Content,
                CreatedAt = comment.CreatedAt,
                UserDisplayName = comment.User?.DisplayName ?? comment.User?.FirstName + " " + comment.User?.LastName,
                LikesCount = comment.Likes?.Count ?? 0,
                ReplyCount = comment.Replies?.Count ?? 0
            };
        }
    }
}