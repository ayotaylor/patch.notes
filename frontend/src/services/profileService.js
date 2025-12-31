import apiClient from "./apiClient";

export const profileService = {
  // Get current user's profile
  async getProfile() {
    try {
    const response = await apiClient.get('/profile');
    return response.data
    } catch (error) {
      throw new Error(
        error.response?.data?.message || "Failed to get user profile"
      )
    }
    //return await get('/api/userprofile')
  },

  // Get specific user's profile by ID
  async getProfileById(userId) {
    try {
        if (!userId) {
        throw new Error("User ID is required to fetch profile")
        }
        // Use the API client to fetch the profile
        const response = await apiClient.get(`/profile/${userId}`);
        return response.data
    } catch (error) {
      throw new Error(
        error.response?.data?.message || "Failed to get user profile by ID"
      )
    }
    // const { get } = useApi()
    // return await get(`/api/userprofile/${userId}`)
  },

  // Update profile
  async updateProfile(profileData) {
    try {
      if (!profileData || typeof profileData !== 'object') {
        throw new Error("Profile data must be an object")
      }
      const response = await apiClient.put('/profile/update', profileData);
      return response.data
    } catch (error) {
      throw new Error(
        error.response?.data?.message || "Failed to update user profile"
      )
    }
    // const { put } = useApi()
    // return await put('/api/userprofile', profileData)
  },

  // Update top games
  async updateTopGames(gamesData) {
    try {
      if (!gamesData || !Array.isArray(gamesData)) {
        throw new Error("Games data must be an array")
      }
      const response = await apiClient.put('/profile/games', gamesData);
      return response.data
    } catch (error) {
      throw new Error(
        error.response?.data?.message || "Failed to update top games"
      )
    }
  },

  // Upload profile image
  async uploadProfileImage(file) {
    try {
        if (!file || !(file instanceof File)) {
            throw new Error("A valid file must be provided for upload")
        }
        if (file.size > 5 * 1024 * 1024) { // 5MB limit
            throw new Error("File size exceeds the 5MB limit")
        }
        if (!file.type.startsWith('image/')) {
            throw new Error("Only image files are allowed") 
        }

        const formData = new FormData();
        formData.append('image', file);
        const response = await apiClient.post('/profile/upload-image', formData, {
            headers: {
                'Content-Type': 'multipart/form-data'
            }
        });
        return response.data;
    } catch (error) {
      throw new Error(
        error.response?.data?.message || "Failed to upload profile image"
      )
    }
  }
}