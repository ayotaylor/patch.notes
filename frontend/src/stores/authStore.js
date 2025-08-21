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
  AUTH_STORAGE_KEY,
  USER_STORAGE_KEY,
} from "@/utils/authUtils";

export const useAuthStore = defineStore("auth", () => {
  //state
  const user = ref(null); // TODO: define a more specific type for user
  // e.g. { id: string, email: string, firstName: string, lastName: string, profile: object }
  const token = ref(null);
  const loading = ref(false);
  const lastTokenValidation = ref(null);
  // Track if user was logged out via interceptor to prevent router conflicts
  const loggedOutViaInterceptor = ref(false);

  // getters
  // TODO: consider using a more robust authentication check
  const isAuthenticated = computed(() => {
    return !!(
      (user.value && user.value.id && token.value && token.value.length > 10) // Basic sanity check
    );
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

  const getUserProfile = computed(() => {
    if (!user.value) return null;
    return {
      id: user.value.id,
      email: user.value.email,
      firstName: user.value.firstName || "",
      lastName: user.value.lastName || "",
      dispayName: userFullName.value,
      profileImageUrl: user.value.profile?.imageUrl || null,
      bio: user.value.profile?.bio || "",
    }
  });

  // update user profile in state and localStorage
  const updateUserProfile = (profileData) => {
    if (user.value) {
      // update user object
      if (profileData.firstName) {
        user.value.firstName = profileData.firstName;
      }
      if (profileData.lastName) {
        user.value.lastName = profileData.lastName;
      }
      if (profileData.email) {
        user.value.email = profileData.email;
      }

      // update profile object
      if (!user.value.profile) {
        user.value.profile = {};
      }
      Object.assign(user.value.profile, profileData);

      // update localStorage
      setAuthData(token.value, user.value);
    }
  };

  const router = useRouter();
  const toast = useToast();

  async function register(userData) {
    loading.value = true;
    try {
      const response = await authService.register(userData);

      user.value = response.user;
      token.value = response.token;
      lastTokenValidation.value = Date.now();

      // Store in localStorage
      setAuthData(token.value, user.value);

      return { success: true, data: response };
    } catch (error) {
      console.error("Registration error:", error);
      throw error;
    } finally {
      loading.value = false;
    }
  }

  async function login(credentials) {
    loading.value = true;
    loggedOutViaInterceptor.value = false; // Reset flag
    try {
      const response = await authService.login(credentials);

      user.value = response.user;
      token.value = response.token;
      // Update last token validation time
      lastTokenValidation.value = Date.now();

      setAuthData(token.value, user.value);
      return { success: true, data: response };
    } catch (error) {
      console.error("Login error:", error);
      throw error;
    } finally {
      loading.value = false;
    }
  }

  async function updateProfile(profileData) {
    loading.value = true;
    try {
      const response = await authService.updateProfile(profileData);
      // Update user state
      // TODO: might not be necessary if response.profile is the same as user.value.profile
      updateUserProfileState(response.profile);

      // TODO: maybe update last token validation time if needed

      return { success: true, data: response };
    } catch (error) {
      console.error("Profile update error:", error);
      throw error;
    } finally {
      loading.value = false;
    }
  }

  const updateUserProfileState = (profileData) => {
    if (user.value) {
      user.value = {
        ...user.value,
        profile: {
          ...user.value.profile,
          ...profileData
        }
      }
    }
  }

  async function logout(redirectPath = '/dashboard') {
    loading.value = true;
    try {
      // Clear local storage
      clearAuthData();

      // Reset user state
      clearAuthState();

      // Redirect to appropriate page
      await router.push(redirectPath);

      toast.success("Logged out successfully");
    } catch (error) {
      console.error("Logout error:", error);
      toast.error("Error during logout");
    } finally {
      loading.value = false;
    }
  }

  async function validateToken() {
    // Skip if already being handled by interceptor
    if (loggedOutViaInterceptor.value) {
      return false;
    }
    const tokenToValidate = token.value || getStoredToken();
    if (!tokenToValidate) {
      await logout();
      return false;
    }

    try {
      const isValid = await authService.validateToken();
      if (!isValid) {
        await logout();
        return false;
      }
      setLastTokenValidation(Date.now());
      return true;
    } catch (error) {
      console.error("Token validation error:", error);
      await logout();
      return false;
    }
  }

  function loadUserFromStorage() {
    const storedUser = getStoredUser();
    const storedToken = getStoredToken();
    if (storedUser && token) {
      try {
        user.value = storedUser;
        token.value = storedToken;
        loggedOutViaInterceptor.value = false; // Reset flag
      } catch (error) {
        console.error("Error parsing user data:", error);
        clearAuthState();
      }
    }
  }

  function clearAuthState() {
    // Reset state
    user.value = null;
    token.value = null;
    lastTokenValidation.value = null;
  }

  function setLastTokenValidation(timestamp) {
    lastTokenValidation.value = timestamp;
  }

  // Flag for interceptor coordination
  function setLoggedOutViaInterceptor(value) {
    loggedOutViaInterceptor.value = value;
  }

  // Handle storage changes for multi-tab sync
  function handleStorageChange(event) {
    if (event.key === USER_STORAGE_KEY) {
      if (event.newValue) {
        try {
          user.value = JSON.parse(event.newValue);
          loggedOutViaInterceptor.value = false; // Reset flag
        } catch (error) {
          console.error("Error parsing user data from storage event:", error);
        }
      } else {
        // clear user and token from local storage
        clearAuthData();
      }
    } else if (event.key === AUTH_STORAGE_KEY) {
      if (event.newValue) {
        token.value = event.newValue;
        loggedOutViaInterceptor.value = false; // Reset flag
      } else {
        // clear user and token from local storage
        clearAuthData();
      }
    }
  }

  return {
    // state
    user,
    token,
    lastTokenValidation,
    loading,
    // getters
    isAuthenticated,
    userFullName,
    getUserProfile,
    // actions
    register,
    login,
    updateProfile,
    updateUserProfileState,
    updateUserProfile,  // TODO: consider removing this if not needed. logic might be redundant
    logout,
    validateToken,
    loadUserFromStorage,
    clearAuthState,
    setLastTokenValidation,
    setLoggedOutViaInterceptor,
    handleStorageChange,
  };
});
