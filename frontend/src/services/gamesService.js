import apiClient from "./apiClient";

export const gamesService = {
  // Search games
  async searchGames(query, page = 1, pageSize = 20) {
    try {
      if (!query || typeof query !== "string") {
        throw new Error("Query must be a non-empty string");
      }
      if (query.trim() === "") {
        return {
          data: [],
          page: 1,
          pageSize: pageSize,
          totalCount: 0,
          totalPages: 0,
          hasNextPage: false,
          hasPreviousPage: false
        };
      }
      query = query.trim();
      const response = await apiClient.get(
        `/games/search?Search=${encodeURIComponent(query)}&Page=${page}&PageSize=${pageSize}`
      );
      return response.data.data || {
        data: [],
        page: 1,
        pageSize: pageSize,
        totalCount: 0,
        totalPages: 0,
        hasNextPage: false,
        hasPreviousPage: false
      };
    } catch (error) {
      console.log(error);
      throw new Error(
        error.response?.data?.message || "Failed to search games"
      );
    }
  },

  // Get popular games
  async getPopularGames(limit = 10) {
    try {
      if (typeof limit !== "number" || limit <= 0) {
        throw new Error("Limit must be a positive number");
      }
      const response = await apiClient.get(`/games/popular?limit=${limit}`);
      return response.data.data || [];
    } catch (error) {
      throw new Error(
        error.response?.data?.message || "Failed to fetch popular games"
      );
    }
  },

  // Get game details
  async getGameDetails(identifier) {
    try {
      if (
        !identifier ||
        (typeof identifier !== "string" && typeof identifier !== "number")
      ) {
        throw new Error("Game identifier must be a non-empty string or number");
      }
      console.log(
        "gamesService.getGameDetails: Making API call with identifier:",
        identifier,
        typeof identifier
      );
      const response = await apiClient.get(`/games/${identifier}`);
      const gameData = response.data.data || response.data || null;
      console.log("gamesService.getGameDetails: API response:", response.data);

      if (!gameData) {
        console.warn(
          "gamesService.getGameDetails: No game data returned from API"
        );
        return null;
      }

      // Make sure we're not returning a Promise
      if (gameData instanceof Promise) {
        console.error(
          "gamesService.getGameDetails: WARNING - API returned a Promise!"
        );
        const resolvedData = await gameData;
        console.log(
          "gamesService.getGameDetails: Resolved Promise to:",
          resolvedData
        );
        return resolvedData;
      }
      console.log(
        "gamesService.getGameDetails: Returning game data:",
        gameData
      );
      console.log(
        "gamesService.getGameDetails: Game data type:",
        typeof gameData
      );
      return gameData;
    } catch (error) {
      throw new Error(
        error.response?.data?.message || "Failed to fetch game details"
      );
    }
  },

  // async getGameDetailsBySlug(slug) {
  //     try {
  //         if (!slug || typeof slug !== 'string') {
  //             throw new Error("Game ID must be a non-empty string")
  //         }
  //         const response = await apiClient.get(`/games/${encodeURIComponent(slug)}`)
  //         return response.data.data || null
  //     } catch (error) {
  //         throw new Error(
  //             error.response?.data?.message || "Failed to fetch game details"
  //         )
  //     }
  // },

  async getSimilarGames(gameId, limit) {
    try {
      if (
        !gameId ||
        (typeof gameId !== "string" && typeof gameId !== "number")
      ) {
        throw new Error("Game ID must be a non-empty string or number");
      }
      const response = await apiClient.get(
        `/games/${gameId}/similar?limit=${limit}`
      );

      // Try different possible response structures
      const gameData =
        response.data.data ||
        response.data.similarGames ||
        response.data ||
        response.similarGames ||
        [];

      // Make sure it's an array
      if (!Array.isArray(gameData)) {
        console.warn(
          "gamesService.getSimilarGames: API did not return an array, got:",
          typeof gameData,
          gameData
        );
        return [];
      }

      console.log("gamesService.getSimilarGames: Returning:", gameData);
      return gameData;
    } catch (error) {
      throw new Error(
        error.response?.data?.message || "Failed to fetch similar games"
      );
    }
  },

  // Get new games
  async getNewGames(limit = 10) {
    try {
      if (typeof limit !== "number" || limit <= 0) {
        throw new Error("Limit must be a positive number");
      }
      const response = await apiClient.get(`/games/new?limit=${limit}`);
      return response.data.data || [];
    } catch (error) {
      throw new Error(
        error.response?.data?.message || "Failed to fetch new games"
      );
    }
  },

  // Get latest reviewed games
  // TODO: change implementation to get the latest reviewd games from API - review controller
  async getLatestReviewedGames(limit = 10) {
    try {
      if (typeof limit !== "number" || limit <= 0) {
        throw new Error("Limit must be a positive number");
      }
      const response = await apiClient.get(`/games/popular?limit=${limit}`);
      return response.data.data || [];
    } catch (error) {
      throw new Error(
        error.response?.data?.message || "Failed to fetch latest reviewed games"
      );
    }
  },

};
