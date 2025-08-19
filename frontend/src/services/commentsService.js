import apiClient from "./apiClient";

export const commentsService = {
  // Get comments for a review
  async getReviewComments(reviewId, page = 1, pageSize = 10) {
    try {
      if (!reviewId || (typeof reviewId !== "string" && typeof reviewId !== "number")) {
        throw new Error("Review ID must be a non-empty string or number");
      }

      const response = await apiClient.get(
        `/comments/review/${reviewId}?page=${page}&pageSize=${pageSize}`
      );
      
      const result = response.data.data || {
        data: [],
        page: 1,
        pageSize: pageSize,
        totalCount: 0,
        totalPages: 0,
        hasNextPage: false,
        hasPreviousPage: false
      };

      return result;
    } catch (error) {
      console.error("Error fetching review comments:", error);
      throw new Error(
        error.response?.data?.message || "Failed to fetch review comments"
      );
    }
  },

  // Add comment to a review
  async addReviewComment(reviewId, commentData) {
    try {
      if (!reviewId || (typeof reviewId !== "string" && typeof reviewId !== "number")) {
        throw new Error("Review ID must be a non-empty string or number");
      }

      if (!commentData || typeof commentData !== "object") {
        throw new Error("Comment data must be an object");
      }

      if (!commentData.content || typeof commentData.content !== "string" || commentData.content.trim().length === 0) {
        throw new Error("Comment content is required");
      }

      const response = await apiClient.post(`/comments/review/${reviewId}`, {
        content: commentData.content.trim()
      });

      return response.data.data;
    } catch (error) {
      console.error("Error adding review comment:", error);
      throw new Error(
        error.response?.data?.message || "Failed to add review comment"
      );
    }
  },

  // Get comments for a list
  async getListComments(gameListId, page = 1, pageSize = 10) {
    try {
      if (!gameListId || (typeof gameListId !== "string" && typeof gameListId !== "number")) {
        throw new Error("Game list ID must be a non-empty string or number");
      }

      const response = await apiClient.get(
        `/comments/list/${gameListId}?page=${page}&pageSize=${pageSize}`
      );
      
      const result = response.data.data || {
        data: [],
        page: 1,
        pageSize: pageSize,
        totalCount: 0,
        totalPages: 0,
        hasNextPage: false,
        hasPreviousPage: false
      };

      return result;
    } catch (error) {
      console.error("Error fetching list comments:", error);
      throw new Error(
        error.response?.data?.message || "Failed to fetch list comments"
      );
    }
  },

  // Add comment to a list
  async addListComment(gameListId, commentData) {
    try {
      if (!gameListId || (typeof gameListId !== "string" && typeof gameListId !== "number")) {
        throw new Error("Game list ID must be a non-empty string or number");
      }

      if (!commentData || typeof commentData !== "object") {
        throw new Error("Comment data must be an object");
      }

      if (!commentData.content || typeof commentData.content !== "string" || commentData.content.trim().length === 0) {
        throw new Error("Comment content is required");
      }

      const response = await apiClient.post(`/comments/list/${gameListId}`, {
        content: commentData.content.trim()
      });

      return response.data.data;
    } catch (error) {
      console.error("Error adding list comment:", error);
      throw new Error(
        error.response?.data?.message || "Failed to add list comment"
      );
    }
  },

  // Get replies for a comment
  async getCommentReplies(commentId, page = 1, pageSize = 10) {
    try {
      if (!commentId || (typeof commentId !== "string" && typeof commentId !== "number")) {
        throw new Error("Comment ID must be a non-empty string or number");
      }

      const response = await apiClient.get(
        `/comments/${commentId}/replies?page=${page}&pageSize=${pageSize}`
      );
      
      const result = response.data.data || {
        data: [],
        page: 1,
        pageSize: pageSize,
        totalCount: 0,
        totalPages: 0,
        hasNextPage: false,
        hasPreviousPage: false
      };

      return result;
    } catch (error) {
      console.error("Error fetching comment replies:", error);
      throw new Error(
        error.response?.data?.message || "Failed to fetch comment replies"
      );
    }
  },

  // Get single comment
  async getComment(commentId) {
    try {
      if (!commentId || (typeof commentId !== "string" && typeof commentId !== "number")) {
        throw new Error("Comment ID must be a non-empty string or number");
      }

      const response = await apiClient.get(`/comments/${commentId}`);
      return response.data.data;
    } catch (error) {
      console.error("Error fetching comment:", error);
      throw new Error(
        error.response?.data?.message || "Failed to fetch comment"
      );
    }
  },

  // Update a comment
  async updateComment(commentId, commentData) {
    try {
      if (!commentId || (typeof commentId !== "string" && typeof commentId !== "number")) {
        throw new Error("Comment ID must be a non-empty string or number");
      }

      if (!commentData || typeof commentData !== "object") {
        throw new Error("Comment data must be an object");
      }

      if (!commentData.content || typeof commentData.content !== "string" || commentData.content.trim().length === 0) {
        throw new Error("Comment content is required");
      }

      const response = await apiClient.put(`/comments/${commentId}`, {
        content: commentData.content.trim()
      });

      return response.data.data;
    } catch (error) {
      console.error("Error updating comment:", error);
      throw new Error(
        error.response?.data?.message || "Failed to update comment"
      );
    }
  },

  // Delete a comment
  async deleteComment(commentId) {
    try {
      if (!commentId || (typeof commentId !== "string" && typeof commentId !== "number")) {
        throw new Error("Comment ID must be a non-empty string or number");
      }

      const response = await apiClient.delete(`/comments/${commentId}`);
      return response.data.data;
    } catch (error) {
      console.error("Error deleting comment:", error);
      throw new Error(
        error.response?.data?.message || "Failed to delete comment"
      );
    }
  },

  // Add reply to a comment (this assumes replies are also comments with a parentId)
  async addReply(parentCommentId, replyData) {
    try {
      if (!parentCommentId || (typeof parentCommentId !== "string" && typeof parentCommentId !== "number")) {
        throw new Error("Parent comment ID must be a non-empty string or number");
      }

      if (!replyData || typeof replyData !== "object") {
        throw new Error("Reply data must be an object");
      }

      if (!replyData.content || typeof replyData.content !== "string" || replyData.content.trim().length === 0) {
        throw new Error("Reply content is required");
      }

      // First get the parent comment to determine if it's a review or list comment
      const parentComment = await this.getComment(parentCommentId);
      
      // Determine the endpoint based on parent comment type
      let endpoint;
      if (parentComment.reviewId) {
        endpoint = `/comments/review/${parentComment.reviewId}`;
      } else if (parentComment.gameListId) {
        endpoint = `/comments/list/${parentComment.gameListId}`;
      } else {
        throw new Error("Unable to determine comment type for reply");
      }

      const response = await apiClient.post(endpoint, {
        content: replyData.content.trim(),
        parentCommentId: parentCommentId
      });

      return response.data.data;
    } catch (error) {
      console.error("Error adding reply:", error);
      throw new Error(
        error.response?.data?.message || "Failed to add reply"
      );
    }
  },

  // Get comment count for a review (lightweight call)
  async getReviewCommentCount(reviewId) {
    try {
      if (!reviewId || (typeof reviewId !== "string" && typeof reviewId !== "number")) {
        throw new Error("Review ID must be a non-empty string or number");
      }

      const response = await apiClient.get(`/comments/review/${reviewId}/count`);
      return response.data.data?.totalCount || 0;
    } catch (error) {
      // If the endpoint doesn't exist, fallback to getting first page to get count
      try {
        const result = await this.getReviewComments(reviewId, 1, 1);
        return result.totalCount || 0;
      } catch (fallbackError) {
        console.error("Error fetching review comment count:", error);
        return 0;
      }
    }
  },

  // Load comment counts for multiple reviews
  async loadCommentCountsForReviews(reviews) {
    try {
      if (!Array.isArray(reviews) || reviews.length === 0) {
        return reviews;
      }

      const reviewsWithCounts = await Promise.all(
        reviews.map(async (review) => {
          try {
            const commentCount = await this.getReviewCommentCount(review.id);
            return {
              ...review,
              commentCount
            };
          } catch (error) {
            console.error(`Error loading comment count for review ${review.id}:`, error);
            return {
              ...review,
              commentCount: 0
            };
          }
        })
      );

      return reviewsWithCounts;
    } catch (error) {
      console.error("Error loading comment counts for reviews:", error);
      return reviews;
    }
  }
};