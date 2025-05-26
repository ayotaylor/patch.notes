import { ref, computed } from "vue";
import { defineStore } from "pinia";
import { authService } from "@/services/authService";
import { useRouter } from "vue-router";
import { useToast } from "vue-toastification";
import {
  getStoredToken,
  getStoredUser,
  clearAuthData,
  setAuthData,
} from "@/utils/authUtils";

export const useAuthStore = defineStore("auth", () => {
  //state
  const user = ref(null);
  const token = ref(null);
  const loading = ref(false);

  // getters
  const isAuthenticated = computed(() => {
    return !!user.value && !!token.value;
  });

  const userFullName = computed(() => {
    if (!user.value) return "";
    const { firstName, lastName } = user.value;
    // TODO: maybe come back to this and use a more robust name formatting
    return (
      `${firstName || ""} ${lastName || ""}`.trim() ||
      user.value.email ||
      "User"
    );
  });

  const router = useRouter();
  const toast = useToast();

  async function register(userData) {
    this.loading = true;
    try {
      const response = await authService.register(userData);

      user.value = response.user;
      token.value = response.token;

      // Store in localStorage
      setAuthData(token.value, user.value);

      toast.success("Registration successful");

      return { success: true, data: response };
    } catch (error) {
      console.error("Registration error:", error);
      toast.error(error.message || "Registration failed");
      throw error;
    } finally {
      this.loading = false;
    }
  }

  async function login(credentials) {
    loading.value = true;
    try {
      const response = await authService.login(credentials);
      user.value = response.user;
      token.value = response.token;
      setAuthData(token.value, user.value);
      toast.success("Login successful");
      return { success: true, data: response };
    } catch (error) {
      console.error("Login error:", error);
      toast.error(error.message || "Login failed");
      throw error;
    } finally {
      loading.value = false;
    }
  }

  async function logout() {
    this.loading = true;
    try {
      // Clear local storage
      clearAuthData();

      // Reset user state
      user.value = null;
      token.value = null;

      // Redirect to login
      await router.push("/login");

      toast.success("Logged out successfully");
    } catch (error) {
      console.error("Logout error:", error);
      toast.error("Error during logout");
    } finally {
      this.loading = false;
    }
  }

  async function validateToken() {
    const token = getStoredToken();
    if (!token) {
      await logout();
      return false;
    }

    try {
      const isValid = await authService.validateToken(token);
      if (!isValid) {
        await logout();
        return false;
      }
      return true;
    } catch (error) {
      console.error("Token validation error:", error);
      await logout();
      return false;
    }
  }

  function loadUserFromStorage() {
    const userStr = getStoredUser();
    const token = getStoredToken();
    if (userStr && token) {
      try {
        this.user = JSON.parse(userStr);
        this.token = token;
      } catch (error) {
        console.error("Error parsing user data:", error);
        this.clearStorage();
      }
    }
  }

  function clearAuthState() {
    clearAuthData();
    // Reset state
    this.user = null;
    this.token = null;
  }

  // Handle storage changes for multi-tab sync
  function handleStorageChange(event) {
    if (event.key === "user") {
      if (event.newValue) {
        try {
          this.user = JSON.parse(event.newValue);
        } catch (error) {
          console.error("Error parsing user data from storage event:", error);
        }
      } else {
        this.user = null;
      }
    } else if (event.key === "authToken") {
      if (event.newValue) {
        this.token = event.newValue;
      } else {
        // If token is removed, clear user as well
        this.token = null;
        this.user = null;
      }
    }
  }

  return {
    // state
    user,
    token,
    loading,
    // getters
    isAuthenticated,
    userFullName,
    // actions
    register,
    login,
    logout,
    validateToken,
    loadUserFromStorage,
    clearAuthState,
    handleStorageChange,
  };
});
