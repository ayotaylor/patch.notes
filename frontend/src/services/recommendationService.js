import apiClient from "./apiClient";

export const recommendationService = {
  // Get general game recommendations
  async getRecommendations(query, maxResults = 10, includeFollowedUsersPreferences = true) {
    try {
      if (!query || typeof query !== "string" || query.trim() === "") {
        throw new Error("Query must be a non-empty string");
      }

      const requestBody = {
        query: query.trim(),
        maxResults,
        includeFollowedUsersPreferences
      };

      const response = await apiClient.post("/recommendation/search", requestBody);
      
      if (!response.data.success) {
        throw new Error(response.data.message || "Failed to get recommendations");
      }

      return response.data.data || {
        games: [],
        responseMessage: "",
        followUpQuestions: [],
        conversationId: null,
        requiresFollowUp: false
      };
    } catch (error) {
      console.error("Error getting recommendations:", error);
      throw new Error(
        error.response?.data?.message || error.message || "Failed to get recommendations"
      );
    }
  },

  // Get personalized recommendations (requires authentication)
  async getPersonalizedRecommendations(query, maxResults = 10, includeFollowedUsersPreferences = true) {
    try {
      if (!query || typeof query !== "string" || query.trim() === "") {
        throw new Error("Query must be a non-empty string");
      }

      const requestBody = {
        query: query.trim(),
        maxResults,
        includeFollowedUsersPreferences
      };

      const response = await apiClient.post("/recommendation/personalized", requestBody);
      
      if (!response.data.success) {
        throw new Error(response.data.message || "Failed to get personalized recommendations");
      }

      return response.data.data || {
        games: [],
        responseMessage: "",
        followUpQuestions: [],
        conversationId: null,
        requiresFollowUp: false
      };
    } catch (error) {
      console.error("Error getting personalized recommendations:", error);
      throw new Error(
        error.response?.data?.message || error.message || "Failed to get personalized recommendations"
      );
    }
  },

  // Continue a conversation with follow-up queries
  async continueConversation(conversationId, query, maxResults = 10, includeFollowedUsersPreferences = true) {
    try {
      if (!conversationId || typeof conversationId !== "string" || conversationId.trim() === "") {
        throw new Error("Conversation ID must be a non-empty string");
      }
      
      if (!query || typeof query !== "string" || query.trim() === "") {
        throw new Error("Query must be a non-empty string");
      }

      const requestBody = {
        query: query.trim(),
        maxResults,
        includeFollowedUsersPreferences,
        conversationId: conversationId.trim()
      };

      const response = await apiClient.post(`/recommendation/continue/${encodeURIComponent(conversationId.trim())}`, requestBody);
      
      if (!response.data.success) {
        throw new Error(response.data.message || "Failed to continue conversation");
      }

      return response.data.data || {
        games: [],
        responseMessage: "",
        followUpQuestions: [],
        conversationId: null,
        requiresFollowUp: false
      };
    } catch (error) {
      console.error("Error continuing conversation:", error);
      throw new Error(
        error.response?.data?.message || error.message || "Failed to continue conversation"
      );
    }
  },

  // Get example queries
  async getExampleQueries() {
    try {
      const response = await apiClient.get("/recommendation/examples");
      
      if (!response.data.success) {
        throw new Error(response.data.message || "Failed to get example queries");
      }

      return response.data.data || [];
    } catch (error) {
      console.error("Error getting example queries:", error);
      throw new Error(
        error.response?.data?.message || error.message || "Failed to get example queries"
      );
    }
  },

  // Check system health
  async checkHealth() {
    try {
      const response = await apiClient.get("/recommendation/health");
      
      if (!response.data.success) {
        throw new Error(response.data.message || "System health check failed");
      }

      return response.data.data || {
        VectorDatabase: "Unknown",
        Status: "Unknown",
        Timestamp: new Date().toISOString()
      };
    } catch (error) {
      console.error("Error checking system health:", error);
      throw new Error(
        error.response?.data?.message || error.message || "Health check failed"
      );
    }
  },

  // Helper method to determine which endpoint to use based on auth status
  async getRecommendationsAuto(query, maxResults = 10, isAuthenticated = false, includeFollowedUsersPreferences = true) {
    if (isAuthenticated) {
      return await this.getPersonalizedRecommendations(query, maxResults, includeFollowedUsersPreferences);
    } else {
      return await this.getRecommendations(query, maxResults, includeFollowedUsersPreferences);
    }
  }
};

export default recommendationService;