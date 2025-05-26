import axios from "axios";
import { getStoredToken, clearAuthData } from "@/utils/authUtils";
import { useAuthStore } from "@/stores/authStore";

// Create axios instance with base configuration
const apiClient = axios.create({
  baseURL: process.env.VUE_APP_API_BASE_URL || "http://localhost:5174/api",
  timeout: 10000,
  headers: {
    "Content-Type": "application/json",
  },
});

// Request interceptor to add auth token
apiClient.interceptors.request.use(
  (config) => {
    const token = getStoredToken();
    // If token exists, add it to the Authorization header
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Response interceptor to handle common errors
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      // Token expired or invalid
      console.log("Token expired/invalid - clearing auth data");
      clearAuthData();
      const authStore = useAuthStore();
      authStore.clearAuthState(); // Cl ear store state
      //console.error("Unauthorized access - redirecting to login");
      // Redirect to login page
      // Note: This will not work in a non-browser environment like Node.js
      // You may need to handle this differently in a server-side context
      // Only redirect if we're not already on login page
      if (window.location.pathname !== "/login") {
        window.location.href = "/login";
      }
    }
    return Promise.reject(error);
  }
);

export default apiClient;
