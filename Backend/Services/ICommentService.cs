using Backend.Models.DTO.Social;
using Backend.Models.DTO.Response;

namespace Backend.Services
{
    public interface ICommentService
    {
        Task<PagedResponse<CommentDto>> GetReviewCommentsAsync(Guid reviewId, int page = 1, int pageSize = 20);
        Task<PagedResponse<CommentDto>> GetGameListCommentsAsync(Guid gameListId, int page = 1, int pageSize = 20);
        Task<PagedResponse<CommentDto>> GetCommentRepliesAsync(Guid commentId, int page = 1, int pageSize = 20);
        Task<CommentDto?> GetCommentAsync(Guid commentId);
        Task<CommentDto?> CreateReviewCommentAsync(Guid userId, Guid reviewId, string content, Guid? parentCommentId = null);
        Task<CommentDto?> CreateGameListCommentAsync(Guid userId, Guid gameListId, string content, Guid? parentCommentId = null);
        Task<CommentDto?> UpdateCommentAsync(Guid commentId, string content);
        Task<bool> DeleteCommentAsync(Guid commentId);
        Task<bool> IsUserCommentOwnerAsync(Guid commentId, Guid userId);
    }
}