import apiClient from "./apiClient";
import { getStoredToken, getStoredUser } from "@/utils/authUtils";

export const authService = {
  // Login user
  async login(credentials) {
    try {
      const response = await apiClient.post("/auth/login", credentials);
      return response.data;
    } catch (error) {
      throw new Error(error.response?.data?.message || "Login failed");
    }
  },

  // Register user
  async register(userData) {
    try {
      const response = await apiClient.post("/auth/register", userData);
      return response.data;
    } catch (error) {
      throw new Error(error.response?.data?.message || "Registration failed");
    }
  },

  // Validate token
  async validateToken() {
    try {
      const response = await apiClient.post("/auth/validateToken", {});
      return response.data.isValid;
    } catch (error) {
      console.error("Token validation error:", error);
      return false;
    }
  },

  // Refresh token
  async refreshToken() {
    try {
      const response = await apiClient.post("/auth/refresh-token");
      return response.data;
    } catch (error) {
      throw new Error(error.response?.data?.message || "Token refresh failed");
    }
  },

  // Get current user profile
  async getCurrentUser() {
    try {
      const response = await apiClient.get("/auth/me");
      return response.data;
    } catch (error) {
      throw new Error(
        error.response?.data?.message || "Failed to get user profile"
      );
    }
  },

  // Update user profile
  async updateProfile(profileData) {
    try {
      const response = await apiClient.put("/profile/update", profileData);
      return response.data;
    } catch (error) {
      throw new Error(
        error.response?.data?.message || "Failed to update profile"
      );
    }
  },

  // Change password
  async changePassword(passwordData) {
    try {
      const response = await apiClient.post(
        "/auth/change-password",
        passwordData
      );
      return response.data;
    } catch (error) {
      throw new Error(
        error.response?.data?.message || "Failed to change password"
      );
    }
  },

  // Forgot password
  async forgotPassword(email) {
    try {
      const response = await apiClient.post("/auth/forgot-password", { email });
      return response.data;
    } catch (error) {
      throw new Error(
        error.response?.data?.message || "Failed to send reset email"
      );
    }
  },

  // Reset password
  async resetPassword(resetData) {
    try {
      const response = await apiClient.post("/auth/reset-password", resetData);
      return response.data;
    } catch (error) {
      throw new Error(
        error.response?.data?.message || "Failed to reset password"
      );
    }
  },

  // Logout (if you need to call backend)
  async logout() {
    try {
      await apiClient.post("/auth/logout");
    } catch (error) {
      // Don't throw error for logout - just log it
      console.error("Logout error:", error);
    }
  },

  // TODO: maybe remove this if not needed...already handled in store
  isAuthenticated() {
    const token = getStoredToken();
    return !!token;
  },

  getUser() {
    const userStr = getStoredUser();
    return !!userStr;
  },
};
