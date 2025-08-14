using Backend.Models.DTO.Social;

namespace Backend.Services
{
    public interface IReviewService
    {
        Task<List<ReviewDto>> GetGameReviewsAsync(int gameId, int page = 1, int pageSize = 20);
        Task<List<ReviewDto>> GetUserReviewsAsync(Guid userId, int page = 1, int pageSize = 20);
        Task<ReviewDto?> GetReviewAsync(Guid reviewId);
        Task<ReviewDto?> GetUserReviewForGameAsync(Guid userId, int gameId);
        Task<ReviewDto?> CreateReviewAsync(Guid userId, int gameId, int rating, string? reviewText = null);
        Task<ReviewDto?> UpdateReviewAsync(Guid reviewId, int rating, string? reviewText = null);
        Task<bool> DeleteReviewAsync(Guid reviewId);
        Task<double> GetGameAverageRatingAsync(int gameId);
        Task<int> GetGameReviewsCountAsync(int gameId);
        Task<bool> HasUserReviewedGameAsync(Guid userId, int gameId);
    }
}