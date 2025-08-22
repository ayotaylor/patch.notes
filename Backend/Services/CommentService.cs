using Backend.Data;
using Backend.Mapping;
using Backend.Models.DTO.Social;
using Backend.Models.DTO.Response;
using Backend.Models.Social;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services
{
    public class CommentService : ICommentService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CommentService> _logger;

        public CommentService(ApplicationDbContext context, ILogger<CommentService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<PagedResponse<CommentDto>> GetReviewCommentsAsync(Guid reviewId, int page = 1, int pageSize = 20)
        {
            var totalCount = await _context.Comments
                .Where(c => c.ReviewId == reviewId)
                .CountAsync();

            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var comments = await _context.Comments
                .Include(c => c.User)
                .Include(c => c.Replies)
                    .ThenInclude(r => r.User)
                .Include(c => c.Likes)
                .Where(c => c.ReviewId == reviewId && c.ParentCommentId == null)
                .OrderBy(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResponse<CommentDto>
            {
                Data = comments.Select(c => c.ToDto()).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                HasNextPage = page < totalPages,
                HasPreviousPage = page > 1
            };
        }

        public async Task<PagedResponse<CommentDto>> GetGameListCommentsAsync(Guid gameListId, int page = 1, int pageSize = 20)
        {
            var totalCount = await _context.Comments
                .Where(c => c.GameListId == gameListId)
                .CountAsync();

            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var comments = await _context.Comments
                .Include(c => c.User)
                .Include(c => c.Replies)
                    .ThenInclude(r => r.User)
                .Include(c => c.Likes)
                .Where(c => c.GameListId == gameListId && c.ParentCommentId == null)
                .OrderBy(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResponse<CommentDto>
            {
                Data = comments.Select(c => c.ToDto()).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                HasNextPage = page < totalPages,
                HasPreviousPage = page > 1
            };
        }

        public async Task<PagedResponse<CommentDto>> GetCommentRepliesAsync(Guid commentId, int page = 1, int pageSize = 20)
        {
            var totalCount = await _context.Comments
                .Where(c => c.ParentCommentId == commentId)
                .CountAsync();

            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var replies = await _context.Comments
                .Include(c => c.User)
                .Include(c => c.Likes)
                .Where(c => c.ParentCommentId == commentId)
                .OrderBy(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResponse<CommentDto>
            {
                Data = replies.Select(c => c.ToDto()).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                HasNextPage = page < totalPages,
                HasPreviousPage = page > 1
            };
        }

        public async Task<CommentDto?> GetCommentAsync(Guid commentId)
        {
            var comment = await _context.Comments
                .Include(c => c.User)
                .Include(c => c.Replies)
                    .ThenInclude(r => r.User)
                .Include(c => c.Likes)
                .FirstOrDefaultAsync(c => c.Id == commentId);

            return comment?.ToDto();
        }

        public async Task<CommentDto?> CreateReviewCommentAsync(Guid userId, Guid reviewId, string content, Guid? parentCommentId = null)
        {
            var userProfileId = await _context.UserProfiles
                .Where(u => u.UserId == userId.ToString())
                .Select(u => u.Id)
                .FirstOrDefaultAsync();

            if (userProfileId == Guid.Empty)
            {
                return null;
            }

            if (!await _context.Reviews.AnyAsync(r => r.Id == reviewId))
            {
                return null;
            }

            if (parentCommentId.HasValue && !await _context.Comments.AnyAsync(c => c.Id == parentCommentId.Value && c.ReviewId == reviewId))
            {
                return null;
            }

            var comment = new Comment
            {
                UserId = userProfileId,
                ReviewId = reviewId,
                Content = content,
                ParentCommentId = parentCommentId
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            return await GetCommentAsync(comment.Id);
        }

        public async Task<CommentDto?> CreateGameListCommentAsync(Guid userId, Guid gameListId, string content, Guid? parentCommentId = null)
        {
            var userProfileId = await _context.UserProfiles
                .Where(u => u.UserId == userId.ToString())
                .Select(u => u.Id)
                .FirstOrDefaultAsync();

            if (userProfileId == Guid.Empty)
            {
                return null;
            }

            if (!await _context.GameLists.AnyAsync(gl => gl.Id == gameListId))
            {
                return null;
            }

            if (parentCommentId.HasValue && !await _context.Comments.AnyAsync(c => c.Id == parentCommentId.Value && c.GameListId == gameListId))
            {
                return null;
            }

            var comment = new Comment
            {
                UserId = userProfileId,
                GameListId = gameListId,
                Content = content,
                ParentCommentId = parentCommentId
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            return await GetCommentAsync(comment.Id);
        }

        public async Task<CommentDto?> UpdateCommentAsync(Guid commentId, string content)
        {
            var comment = await _context.Comments.FindAsync(commentId);
            if (comment == null)
            {
                return null;
            }

            comment.Content = content;
            await _context.SaveChangesAsync();

            return await GetCommentAsync(commentId);
        }

        public async Task<bool> DeleteCommentAsync(Guid commentId)
        {
            var comment = await _context.Comments.FindAsync(commentId);
            if (comment == null)
            {
                return false;
            }

            // Delete related entities first to avoid foreign key constraint violations
            var commentLikes = await _context.CommentLikes
                .Where(cl => cl.CommentId == commentId)
                .ToListAsync();
            _context.CommentLikes.RemoveRange(commentLikes);

            var childComments = await _context.Comments
                .Where(c => c.ParentCommentId == commentId)
                .ToListAsync();
            _context.Comments.RemoveRange(childComments);

            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> IsUserCommentOwnerAsync(Guid commentId, Guid userId)
        {
            var userProfileId = await _context.UserProfiles
                .Where(u => u.UserId == userId.ToString())
                .Select(u => u.Id)
                .FirstOrDefaultAsync();

            if (userProfileId == Guid.Empty)
            {
                return false;
            }

            return await _context.Comments
                .AnyAsync(c => c.Id == commentId && c.UserId == userProfileId);
        }

        public async Task<int> GetReviewCommentCountAsync(Guid reviewId)
        {
            // Count all comments for this review (both parent comments and replies)
            return await _context.Comments
                .Where(c => c.ReviewId == reviewId)
                .CountAsync();
        }

        public async Task<int> GetGameListCommentCountAsync(Guid gameListId)
        {
            return await _context.Comments
                .Where(c => c.GameListId == gameListId)
                .CountAsync();
        }
    }
}