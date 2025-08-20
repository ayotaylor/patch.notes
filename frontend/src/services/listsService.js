import apiClient from "./apiClient";
import { Game } from "@/models/Game";

// Helper function to process lists and convert game objects
const processLists = (lists) => {
  if (!Array.isArray(lists)) return lists;

  return lists.map(list => ({
    ...list,
    // Convert game objects to Game instances if they exist
    games: list.games ? list.games.map(gameListItem => {
      // GameListItemDto has a nested Game property (GameSummaryDto)
      const gameData = gameListItem.game || gameListItem;
      return new Game(gameData);
    }) : []
  }));
};

// Helper function to process single list
const processList = (list) => {
  if (!list) return list;
  
  return {
    ...list,
    games: list.games ? list.games.map(gameListItem => {
      // GameListItemDto has a nested Game property (GameSummaryDto)
      const gameData = gameListItem.game || gameListItem;
      return new Game(gameData);
    }) : []
  };
};

export const listsService = {
  // Get public lists
  async getPublicLists(page = 1, pageSize = 10) {
    try {
      const response = await apiClient.get(`/lists/public?page=${page}&pageSize=${pageSize}`);
      const result = response.data.data || {
        data: [],
        page: 1,
        pageSize: pageSize,
        totalCount: 0,
        totalPages: 0,
        hasNextPage: false,
        hasPreviousPage: false
      };

      // Process lists to convert game objects to Game instances
      if (result.data) {
        result.data = processLists(result.data);
      }

      return result;
    } catch (error) {
      console.error("Error fetching public lists:", error);
      throw new Error(
        error.response?.data?.message || "Failed to fetch public lists"
      );
    }
  },

  // Get lists by user
  async getUserLists(userId, page = 1, pageSize = 10) {
    try {
      if (!userId || (typeof userId !== "string" && typeof userId !== "number")) {
        throw new Error("User ID must be a non-empty string or number");
      }

      const response = await apiClient.get(
        `/lists/user/${userId}?page=${page}&pageSize=${pageSize}`
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

      // Process lists to convert game objects to Game instances
      if (result.data) {
        result.data = processLists(result.data);
      }

      return result;
    } catch (error) {
      console.error("Error fetching user lists:", error);
      throw new Error(
        error.response?.data?.message || "Failed to fetch user lists"
      );
    }
  },

  // Get single list by ID
  async getList(listId) {
    try {
      if (!listId || (typeof listId !== "string" && typeof listId !== "number")) {
        throw new Error("List ID must be a non-empty string or number");
      }

      const response = await apiClient.get(`/lists/${listId}`);
      return processList(response.data.data);
    } catch (error) {
      console.error("Error fetching list:", error);
      throw new Error(
        error.response?.data?.message || "Failed to fetch list"
      );
    }
  },

  // Create a new list
  async createList(listData) {
    try {
      if (!listData || typeof listData !== "object") {
        throw new Error("List data must be an object");
      }

      if (!listData.title || typeof listData.title !== "string" || listData.title.trim().length === 0) {
        throw new Error("List title is required");
      }

      const payload = {
        title: listData.title.trim(),
        description: listData.description ? listData.description.trim() : "",
        isPublic: Boolean(listData.isPublic)
      };

      // Add games array if provided
      if (listData.games && Array.isArray(listData.games) && listData.games.length > 0) {
        payload.gameIds = listData.games.map(game => game.id);
      }

      const response = await apiClient.post("/lists", payload);

      return processList(response.data.data);
    } catch (error) {
      console.error("Error creating list:", error);
      throw new Error(
        error.response?.data?.message || "Failed to create list"
      );
    }
  },

  // Update an existing list
  async updateList(listId, listData) {
    try {
      if (!listId || (typeof listId !== "string" && typeof listId !== "number")) {
        throw new Error("List ID must be a non-empty string or number");
      }

      if (!listData || typeof listData !== "object") {
        throw new Error("List data must be an object");
      }

      const updateData = {};
      if (listData.title) updateData.title = listData.title.trim();
      if (listData.description !== undefined) updateData.description = listData.description.trim();
      if (listData.isPublic !== undefined) updateData.isPublic = Boolean(listData.isPublic);

      const response = await apiClient.put(`/lists/${listId}`, updateData);
      return processList(response.data.data);
    } catch (error) {
      console.error("Error updating list:", error);
      throw new Error(
        error.response?.data?.message || "Failed to update list"
      );
    }
  },

  // Delete a list
  async deleteList(listId) {
    try {
      if (!listId || (typeof listId !== "string" && typeof listId !== "number")) {
        throw new Error("List ID must be a non-empty string or number");
      }

      const response = await apiClient.delete(`/lists/${listId}`);
      return response.data.data;
    } catch (error) {
      console.error("Error deleting list:", error);
      throw new Error(
        error.response?.data?.message || "Failed to delete list"
      );
    }
  },

  // Add game to list
  async addGameToList(listId, gameId) {
    try {
      if (!listId || (typeof listId !== "string" && typeof listId !== "number")) {
        throw new Error("List ID must be a non-empty string or number");
      }

      if (!gameId || (typeof gameId !== "string" && typeof gameId !== "number")) {
        throw new Error("Game ID must be a non-empty string or number");
      }

      const response = await apiClient.post(`/lists/${listId}/games/${gameId}`);
      return response.data.data;
    } catch (error) {
      console.error("Error adding game to list:", error);
      throw new Error(
        error.response?.data?.message || "Failed to add game to list"
      );
    }
  },

  // Remove game from list
  async removeGameFromList(listId, gameId) {
    try {
      if (!listId || (typeof listId !== "string" && typeof listId !== "number")) {
        throw new Error("List ID must be a non-empty string or number");
      }

      if (!gameId || (typeof gameId !== "string" && typeof gameId !== "number")) {
        throw new Error("Game ID must be a non-empty string or number");
      }

      const response = await apiClient.delete(`/lists/${listId}/games/${gameId}`);
      return response.data.data;
    } catch (error) {
      console.error("Error removing game from list:", error);
      throw new Error(
        error.response?.data?.message || "Failed to remove game from list"
      );
    }
  }
};