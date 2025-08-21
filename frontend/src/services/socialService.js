import apiClient from "./apiClient";

export const socialService = {
  // Get user's favorites
  async getUserFavorites(userId, page = 1, pageSize = 10) {
    try {
      if (
        !userId ||
        (typeof userId !== "string" && typeof userId !== "number")
      ) {
        throw new Error("User ID must be a non-empty string or number");
      }

      const response = await apiClient.get(
        `/social/favorites/${userId}?page=${page}&pageSize=${pageSize}`
      );

      return (
        response.data.data || {
          data: [],
          page: 1,
          pageSize: pageSize,
          totalCount: 0,
          totalPages: 0,
          hasNextPage: false,
          hasPreviousPage: false,
        }
      );
    } catch (error) {
      console.error("Error fetching user favorites:", error);
      throw new Error(
        error.response?.data?.message || "Failed to fetch user favorites"
      );
    }
  },

  // Add to favorites
  async addToFavorites(gameId) {
    try {
      // if (
      //   !userId ||
      //   (typeof userId !== "string" && typeof userId !== "number")
      // ) {
      //   throw new Error("User ID must be a non-empty string or number");
      // }
      if (
        !gameId ||
        (typeof gameId !== "string" && typeof gameId !== "number")
      ) {
        throw new Error("Game ID must be a non-empty string or number");
      }

      const response = await apiClient.post("/social/favorites", {
        //userId: userId,
        gameId: gameId,
      });

      return response.data.data;
    } catch (error) {
      console.error("Error adding to favorites:", error);
      throw new Error(
        error.response?.data?.message || "Failed to add to favorites"
      );
    }
  },

  // Remove from favorites
  async removeFromFavorites(gameId) {
    try {
      // if (
      //   !userId ||
      //   (typeof userId !== "string" && typeof userId !== "number")
      // ) {
      //   throw new Error("User ID must be a non-empty string or number");
      // }
      if (
        !gameId ||
        (typeof gameId !== "string" && typeof gameId !== "number")
      ) {
        throw new Error("Game ID must be a non-empty string or number");
      }

      const response = await apiClient.delete("/social/favorites", {
        data: {
          //userId: userId,
          gameId: gameId,
        },
      });

      return response.data.data;
    } catch (error) {
      console.error("Error removing from favorites:", error);
      throw new Error(
        error.response?.data?.message || "Failed to remove from favorites"
      );
    }
  },

  // Get user's likes
  async getUserLikes(userId, page = 1, pageSize = 10) {
    try {
      if (
        !userId ||
        (typeof userId !== "string" && typeof userId !== "number")
      ) {
        throw new Error("User ID must be a non-empty string or number");
      }

      const response = await apiClient.get(
        `/social/likes/${userId}?page=${page}&pageSize=${pageSize}`
      );

      return (
        response.data.data || {
          data: [],
          page: 1,
          pageSize: pageSize,
          totalCount: 0,
          totalPages: 0,
          hasNextPage: false,
          hasPreviousPage: false,
        }
      );
    } catch (error) {
      console.error("Error fetching user likes:", error);
      throw new Error(
        error.response?.data?.message || "Failed to fetch user likes"
      );
    }
  },

  // Add general like
  async likeGame(gameId) {
    try {
      // if (
      //   !userId ||
      //   (typeof userId !== "string" && typeof userId !== "number")
      // ) {
      //   throw new Error("User ID must be a non-empty string or number");
      // }
      // if (!itemType || typeof itemType !== "string") {
      //   throw new Error("Item type must be a non-empty string");
      // }
      if (
        !gameId ||
        (typeof gameId !== "string" && typeof gameId !== "number")
      ) {
        throw new Error("Item ID must be a non-empty string or number");
      }

      const response = await apiClient.post("/social/likes", {
        //userId: userId,
        gameId: gameId,
      });

      return response.data.data;
    } catch (error) {
      console.error("Error adding like:", error);
      throw new Error(error.response?.data?.message || "Failed to add like");
    }
  },

  // Remove general like
  async removeGameLike(gameId) {
    try {
      // if (
      //   !userId ||
      //   (typeof userId !== "string" && typeof userId !== "number")
      // ) {
      //   throw new Error("User ID must be a non-empty string or number");
      // }
      // if (!itemType || typeof itemType !== "string") {
      //   throw new Error("Item type must be a non-empty string");
      // }
      if (
        !gameId ||
        (typeof gameId !== "string" && typeof gameId !== "number")
      ) {
        throw new Error("Item ID must be a non-empty string or number");
      }

      const response = await apiClient.delete("/social/likes", {
        data: {
          //userId: userId,
          gameId: gameId,
        },
      });

      return response.data.data;
    } catch (error) {
      console.error("Error removing like:", error);
      throw new Error(error.response?.data?.message || "Failed to remove like");
    }
  },

  // Check if game is favorited
  async isGameFavorited(gameId) {
    try {
      if (
        !gameId ||
        (typeof gameId !== "string" && typeof gameId !== "number")
      ) {
        throw new Error("Game ID must be a non-empty string or number");
      }

      const response = await apiClient.get(`/social/favorites/games/${gameId}/status`);
      return response.data.data;
    } catch (error) {
      console.error("Error checking if game is favorited:", error);
      throw new Error(
        error.response?.data?.message || "Failed to check favorite status"
      );
    }
  },

  // Check if game is liked
  async isGameLiked(gameId) {
    try {
      if (
        !gameId ||
        (typeof gameId !== "string" && typeof gameId !== "number")
      ) {
        throw new Error("Game ID must be a non-empty string or number");
      }

      const response = await apiClient.get(`/social/likes/games/${gameId}/status`);
      return response.data.data;
    } catch (error) {
      console.error("Error checking if game is liked:", error);
      throw new Error(
        error.response?.data?.message || "Failed to check like status"
      );
    }
  },

  // Like a review
  async likeReview(reviewId) {
    try {
      if (
        !reviewId ||
        (typeof reviewId !== "string" && typeof reviewId !== "number")
      ) {
        throw new Error("Review ID must be a non-empty string or number");
      }

      const response = await apiClient.post(`/social/reviews/${reviewId}/like`);
      return response.data.data;
    } catch (error) {
      console.error("Error liking review:", error);
      throw new Error(error.response?.data?.message || "Failed to like review");
    }
  },

  // Check if review is liked
  async isReviewLiked(reviewId) {
    try {
      if (
        !reviewId ||
        (typeof reviewId !== "string" && typeof reviewId !== "number")
      ) {
        throw new Error("Review ID must be a non-empty string or number");
      }

      const response = await apiClient.get(`/social/reviews/${reviewId}/liked`);
      return response.data.data;
    } catch (error) {
      console.error("Error checking if review is liked:", error);
      throw new Error(
        error.response?.data?.message || "Failed to check review like status"
      );
    }
  },

  // Unlike a review
  async unlikeReview(reviewId) {
    try {
      if (
        !reviewId ||
        (typeof reviewId !== "string" && typeof reviewId !== "number")
      ) {
        throw new Error("Review ID must be a non-empty string or number");
      }

      const response = await apiClient.delete(
        `/social/reviews/${reviewId}/like`
      );
      return response.data.data;
    } catch (error) {
      console.error("Error unliking review:", error);
      throw new Error(
        error.response?.data?.message || "Failed to unlike review"
      );
    }
  },

  // Like a list
  async likeList(gameListId) {
    try {
      if (
        !gameListId ||
        (typeof gameListId !== "string" && typeof gameListId !== "number")
      ) {
        throw new Error("Game list ID must be a non-empty string or number");
      }

      const response = await apiClient.post(`/social/lists/${gameListId}/like`);
      return response.data.data;
    } catch (error) {
      console.error("Error liking list:", error);
      throw new Error(error.response?.data?.message || "Failed to like list");
    }
  },

  // Check if list is liked
  async isListLiked(gameListId) {
    try {
      if (
        !gameListId ||
        (typeof gameListId !== "string" && typeof gameListId !== "number")
      ) {
        throw new Error("Game list ID must be a non-empty string or number");
      }

      const response = await apiClient.get(`/social/lists/${gameListId}/liked`);
      return response.data.data;
    } catch (error) {
      console.error("Error checking if list is liked:", error);
      throw new Error(
        error.response?.data?.message || "Failed to check list like status"
      );
    }
  },

  // Unlike a list
  async unlikeList(gameListId) {
    try {
      if (
        !gameListId ||
        (typeof gameListId !== "string" && typeof gameListId !== "number")
      ) {
        throw new Error("Game list ID must be a non-empty string or number");
      }

      const response = await apiClient.delete(
        `/social/lists/${gameListId}/like`
      );
      return response.data.data;
    } catch (error) {
      console.error("Error unliking list:", error);
      throw new Error(error.response?.data?.message || "Failed to unlike list");
    }
  },

  // Like a comment
  async likeComment(commentId) {
    try {
      if (
        !commentId ||
        (typeof commentId !== "string" && typeof commentId !== "number")
      ) {
        throw new Error("Comment ID must be a non-empty string or number");
      }

      const response = await apiClient.post(
        `/social/comments/${commentId}/like`
      );
      return response.data.data;
    } catch (error) {
      console.error("Error liking comment:", error);
      throw new Error(
        error.response?.data?.message || "Failed to like comment"
      );
    }
  },

  // Check if comment is liked
  async isCommentLiked(commentId) {
    try {
      if (
        !commentId ||
        (typeof commentId !== "string" && typeof commentId !== "number")
      ) {
        throw new Error("Comment ID must be a non-empty string or number");
      }

      const response = await apiClient.get(`/social/comments/${commentId}/liked`);
      return response.data.data;
    } catch (error) {
      console.error("Error checking if comment is liked:", error);
      throw new Error(
        error.response?.data?.message || "Failed to check comment like status"
      );
    }
  },

  // Unlike a comment
  async unlikeComment(commentId) {
    try {
      if (
        !commentId ||
        (typeof commentId !== "string" && typeof commentId !== "number")
      ) {
        throw new Error("Comment ID must be a non-empty string or number");
      }

      const response = await apiClient.delete(
        `/social/comments/${commentId}/like`
      );
      return response.data.data;
    } catch (error) {
      console.error("Error unliking comment:", error);
      throw new Error(
        error.response?.data?.message || "Failed to unlike comment"
      );
    }
  },

  // Follow functionality
  async followUser(userId) {
    try {
      if (
        !userId ||
        (typeof userId !== "string" && typeof userId !== "number")
      ) {
        throw new Error("User ID must be a non-empty string or number");
      }

      const response = await apiClient.post(`/follow`, {
        followId: userId,
      });
      return response.data.data;
    } catch (error) {
      console.error("Error following user:", error);
      throw new Error(
        error.response?.data?.message || "Failed to follow user"
      );
    }
  },

  async unfollowUser(userId) {
    try {
      if (
        !userId ||
        (typeof userId !== "string" && typeof userId !== "number")
      ) {
        throw new Error("User ID must be a non-empty string or number");
      }

      const response = await apiClient.delete(`/follow`, {
        data: {
          followId: userId,
        },
      });
      return response.data.data;
    } catch (error) {
      console.error("Error unfollowing user:", error);
      throw new Error(
        error.response?.data?.message || "Failed to unfollow user"
      );
    }
  },

  async isUserFollowed(userId) {
    try {
      if (
        !userId ||
        (typeof userId !== "string" && typeof userId !== "number")
      ) {
        throw new Error("User ID must be a non-empty string or number");
      }

      const response = await apiClient.get(`/follow/${userId}/is-following`);
      return response.data.data;
    } catch (error) {
      console.error("Error checking if user is followed:", error);
      throw new Error(
        error.response?.data?.message || "Failed to check follow status"
      );
    }
  },

  async getFollowing(userId, page = 1, pageSize = 20) {
    try {
      if (
        !userId ||
        (typeof userId !== "string" && typeof userId !== "number")
      ) {
        throw new Error("User ID must be a non-empty string or number");
      }

      const response = await apiClient.get(
        `/follow/${userId}/following?page=${page}&pageSize=${pageSize}`
      );
      return (
        response.data.data || {
          data: [],
          page: 1,
          pageSize: pageSize,
          totalCount: 0,
          totalPages: 0,
          hasNextPage: false,
          hasPreviousPage: false,
        }
      );
    } catch (error) {
      console.error("Error fetching following:", error);
      throw new Error(
        error.response?.data?.message || "Failed to fetch following"
      );
    }
  },

  async getFollowers(userId, page = 1, pageSize = 20) {
    try {
      if (
        !userId ||
        (typeof userId !== "string" && typeof userId !== "number")
      ) {
        throw new Error("User ID must be a non-empty string or number");
      }

      const response = await apiClient.get(
        `/follow/${userId}/followers?page=${page}&pageSize=${pageSize}`
      );
      return (
        response.data.data || {
          data: [],
          page: 1,
          pageSize: pageSize,
          totalCount: 0,
          totalPages: 0,
          hasNextPage: false,
          hasPreviousPage: false,
        }
      );
    } catch (error) {
      console.error("Error fetching followers:", error);
      throw new Error(
        error.response?.data?.message || "Failed to fetch followers"
      );
    }
  },

  // Get all users with pagination
  async getAllUsers(page = 1, pageSize = 20) {
    try {
      const response = await apiClient.get(
        `/users?page=${page}&pageSize=${pageSize}`
      );
      return (
        response.data.data || {
          data: [],
          page: 1,
          pageSize: pageSize,
          totalCount: 0,
          totalPages: 0,
          hasNextPage: false,
          hasPreviousPage: false,
        }
      );
    } catch (error) {
      console.error("Error fetching users:", error);
      throw new Error(
        error.response?.data?.message || "Failed to fetch users"
      );
    }
  },

  // Get featured users
  async getFeaturedUsers(limit = 5) {
    try {
      const response = await apiClient.get(
        `/users/featured?limit=${limit}`
      );
      return response.data.data || [];
    } catch (error) {
      console.error("Error fetching featured users:", error);
      throw new Error(
        error.response?.data?.message || "Failed to fetch featured users"
      );
    }
  },

  // Get popular users
  async getPopularUsers(limit = 5) {
    try {
      const response = await apiClient.get(
        `/users/popular?limit=${limit}`
      );
      return response.data.data || [];
    } catch (error) {
      console.error("Error fetching popular users:", error);
      throw new Error(
        error.response?.data?.message || "Failed to fetch popular users"
      );
    }
  },
};
