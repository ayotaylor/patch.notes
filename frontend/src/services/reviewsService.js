import apiClient from "./apiClient";
import { Game } from "@/models/Game";

// Helper function to process reviews and convert game objects
const processReviews = (reviews) => {
  if (!Array.isArray(reviews)) return reviews;

  return reviews.map(review => ({
    ...review,
    // Convert game object to Game instance if it exists
    game: review.game ? new Game(review.game) : null
  }));
};

export const reviewsService = {
  // Get reviews for a game
  async getGameReviews(gameId, page = 1, pageSize = 10) {
    try {
      if (!gameId || (typeof gameId !== "string" && typeof gameId !== "number")) {
        throw new Error("Game ID must be a non-empty string or number");
      }

      const response = await apiClient.get(
        `/reviews/game/${gameId}?page=${page}&pageSize=${pageSize}`
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

      // Process reviews to convert game objects to Game instances
      if (result.data) {
        result.data = processReviews(result.data);
      }

      return result;
    } catch (error) {
      console.error("Error fetching game reviews:", error);
      throw new Error(
        error.response?.data?.message || "Failed to fetch game reviews"
      );
    }
  },

  // Get latest reviews across all games
  async getLatestReviews(page = 1, pageSize = 10) {
    try {
      if (typeof pageSize !== "number" || pageSize <= 0) {
        throw new Error("Limit must be a positive number");
      }

      const response = await apiClient.get(`/reviews/latest?page=${page}&pageSize=${pageSize}`);
      
      // Handle both direct array response and paginated response structure
      let reviews;
      if (response.data.data && Array.isArray(response.data.data)) {
        // Direct array response
        reviews = response.data.data;
      } else if (response.data.data && response.data.data.data && Array.isArray(response.data.data.data)) {
        // Paginated response structure
        reviews = response.data.data.data;
      } else {
        // Fallback to empty array
        reviews = [];
      }
      
      return processReviews(reviews);
    } catch (error) {
      console.error("Error fetching latest reviews:", error);
      throw new Error(
        error.response?.data?.message || "Failed to fetch latest reviews"
      );
    }
  },

  // Get reviews by user
  async getUserReviews(userId, page = 1, pageSize = 10) {
    try {
      if (!userId || (typeof userId !== "string" && typeof userId !== "number")) {
        throw new Error("User ID must be a non-empty string or number");
      }

      const response = await apiClient.get(
        `/reviews/user/${userId}?page=${page}&pageSize=${pageSize}`
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

      // Process reviews to convert game objects to Game instances
      if (result.data) {
        result.data = processReviews(result.data);
      }

      return result;
    } catch (error) {
      console.error("Error fetching user reviews:", error);
      throw new Error(
        error.response?.data?.message || "Failed to fetch user reviews"
      );
    }
  },

  // Add a new review
  async addReview(reviewData) {
    try {
      if (!reviewData || typeof reviewData !== "object") {
        throw new Error("Review data must be an object");
      }

      if (!reviewData.gameId) {
        throw new Error("Game ID is required");
      }

      if (!reviewData.rating || reviewData.rating < 1 || reviewData.rating > 5) {
        throw new Error("Rating must be between 1 and 5");
      }

      if (!reviewData.reviewText || typeof reviewData.reviewText !== "string" || reviewData.reviewText.trim().length === 0) {
        throw new Error("Review text is required");
      }

      const response = await apiClient.post("/reviews", {
        gameId: reviewData.gameId,
        rating: reviewData.rating,
        reviewText: reviewData.reviewText.trim()
      });

      const review = response.data.data;
      // Process single review if it has a game object
      if (review && review.game) {
        review.game = new Game(review.game);
      }
      return review;
    } catch (error) {
      console.error("Error adding review:", error);
      throw new Error(
        error.response?.data?.message || "Failed to add review"
      );
    }
  },

  // Update an existing review
  async updateReview(reviewId, reviewData) {
    try {
      if (!reviewId || (typeof reviewId !== "string" && typeof reviewId !== "number")) {
        throw new Error("Review ID must be a non-empty string or number");
      }

      if (!reviewData || typeof reviewData !== "object") {
        throw new Error("Review data must be an object");
      }

      if (reviewData.rating && (reviewData.rating < 1 || reviewData.rating > 5)) {
        throw new Error("Rating must be between 1 and 5");
      }

      if (reviewData.reviewText && (typeof reviewData.reviewText !== "string" || reviewData.reviewText.trim().length === 0)) {
        throw new Error("Review text cannot be empty");
      }

      const updateData = {};
      if (reviewData.rating) updateData.rating = reviewData.rating;
      if (reviewData.reviewText) updateData.reviewText = reviewData.reviewText.trim();

      const response = await apiClient.put(`/reviews/${reviewId}`, updateData);
      const review = response.data.data;
      // Process single review if it has a game object
      if (review && review.game) {
        review.game = new Game(review.game);
      }
      return review;
    } catch (error) {
      console.error("Error updating review:", error);
      throw new Error(
        error.response?.data?.message || "Failed to update review"
      );
    }
  },

  // Delete a review
  async deleteReview(reviewId) {
    try {
      if (!reviewId || (typeof reviewId !== "string" && typeof reviewId !== "number")) {
        throw new Error("Review ID must be a non-empty string or number");
      }

      const response = await apiClient.delete(`/reviews/${reviewId}`);
      return response.data.data;
    } catch (error) {
      console.error("Error deleting review:", error);
      throw new Error(
        error.response?.data?.message || "Failed to delete review"
      );
    }
  },

  // Get review statistics for a game
  async getGameReviewStats(gameId) {
    try {
      if (!gameId || (typeof gameId !== "string" && typeof gameId !== "number")) {
        throw new Error("Game ID must be a non-empty string or number");
      }

      const response = await apiClient.get(`/reviews/game/${gameId}/stats`);
      return response.data.data || {
        averageRating: 0,
        totalReviews: 0,
        ratingDistribution: {
          1: 0,
          2: 0,
          3: 0,
          4: 0,
          5: 0
        }
      };
    } catch (error) {
      console.error("Error fetching review stats:", error);
      throw new Error(
        error.response?.data?.message || "Failed to fetch review statistics"
      );
    }
  },

  // Get a single review by ID
  async getReview(reviewId) {
    try {
      if (!reviewId || (typeof reviewId !== "string" && typeof reviewId !== "number")) {
        throw new Error("Review ID must be a non-empty string or number");
      }

      const response = await apiClient.get(`/reviews/${reviewId}`);
      const review = response.data.data;
      // Process single review if it has a game object
      if (review && review.game) {
        review.game = new Game(review.game);
      }
      return review;
    } catch (error) {
      console.error("Error fetching review:", error);
      throw new Error(
        error.response?.data?.message || "Failed to fetch review"
      );
    }
  },

  // Check if user has reviewed a game (by gameId)
  async getUserGameReview(userId, gameId) {
    try {
      if (!userId || (typeof userId !== "string" && typeof userId !== "number")) {
        throw new Error("User ID must be a non-empty string or number");
      }

      if (!gameId || (typeof gameId !== "string" && typeof gameId !== "number")) {
        throw new Error("Game ID must be a non-empty string or number");
      }

      const response = await apiClient.get(`/reviews/user/${userId}/game/${gameId}`);
      const review = response.data.data;
      // Process single review if it has a game object
      if (review && review.game) {
        review.game = new Game(review.game);
      }
      return review;
    } catch (error) {
      if (error.response?.status === 404) {
        return null; // No review found
      }
      console.error("Error fetching user game review:", error);
      throw new Error(
        error.response?.data?.message || "Failed to fetch user review"
      );
    }
  },

  // Get user's review for a game by username and slug
  async getUserGameReviewBySlug(username, gameSlug) {
    try {
      if (!username || typeof username !== "string") {
        throw new Error("Username must be a non-empty string");
      }

      if (!gameSlug || typeof gameSlug !== "string") {
        throw new Error("Game slug must be a non-empty string");
      }

      const response = await apiClient.get(`/reviews/user/${username}/game/${gameSlug}`);
      const review = response.data.data;
      // Process single review if it has a game object
      if (review && review.game) {
        review.game = new Game(review.game);
      }
      return review;
    } catch (error) {
      if (error.response?.status === 404) {
        return null; // No review found
      }
      console.error("Error fetching user game review by slug:", error);
      throw new Error(
        error.response?.data?.message || "Failed to fetch user review"
      );
    }
  }
};