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
          itemId: gameId,
        },
      });

      return response.data.data;
    } catch (error) {
      console.error("Error removing like:", error);
      throw new Error(error.response?.data?.message || "Failed to remove like");
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
};
