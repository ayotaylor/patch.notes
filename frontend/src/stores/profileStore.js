import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { profileService } from '@/services/profileService'

export const useProfileStore = defineStore('profile', () => {
  // State
  const profiles = ref(new Map()) // Cache profiles by userId
  const currentProfile = ref(null)
  const loading = ref(false)
  const error = ref(null)

  // Getters
  const getProfileById = computed(() => {
    return (userId) => profiles.value.get(userId)
  })

  // Actions
  const fetchProfile = async (userId = null) => {
    try {
      loading.value = true
      error.value = null

      let profile
      if (userId) {
        profile = await profileService.getProfileById(userId)
      } else {
        profile = await profileService.getProfile()
      }

      // Cache the profile by email
      // TODO: use userId or a different property for caching that's better than email
      const profileId = profile.email
      profiles.value.set(profileId, profile)

      // Set as current if it's the user's own profile
      if (!userId) {
        currentProfile.value = profile
      }

      return profile
    } catch (err) {
      error.value = err.message
      throw err
    } finally {
      loading.value = false
    }
  }

  const updateProfile = async (profileData) => {
    try {
      loading.value = true
      error.value = null

      const updatedProfile = await profileService.updateProfile(profileData)
      
      // Update cache
      profiles.value.set(updatedProfile.userId, updatedProfile)
      currentProfile.value = updatedProfile

      return updatedProfile
    } catch (err) {
      error.value = err.message
      throw err
    } finally {
      loading.value = false
    }
  }

  const updateTopGames = async (gamesData) => {
    try {
      loading.value = true
      error.value = null

      const response = await profileService.updateTopGames(gamesData)
      
      // Update current profile with new games
      if (currentProfile.value) {
        currentProfile.value.topGames = response.topGames
        profiles.value.set(currentProfile.value.userId, currentProfile.value)
      }

      return response
    } catch (err) {
      error.value = err.message
      throw err
    } finally {
      loading.value = false
    }
  }

  const uploadProfileImage = async (file) => {
    try {
      loading.value = true
      error.value = null

      const response = await profileService.uploadProfileImage(file)
      
      // Update current profile with new image URL
      if (currentProfile.value) {
        currentProfile.value.profileImageUrl = response.imageUrl
        profiles.value.set(currentProfile.value.userId, currentProfile.value)
      }

      return response
    } catch (err) {
      error.value = err.message
      throw err
    } finally {
      loading.value = false
    }
  }

  const clearProfile = (userId) => {
    profiles.value.delete(userId)
    if (currentProfile.value?.userId === userId) {
      currentProfile.value = null
    }
  }

  const clearAllProfiles = () => {
    profiles.value.clear()
    currentProfile.value = null
  }

  return {
    // State
    profiles,
    currentProfile,
    loading,
    error,

    // Getters
    getProfileById,

    // Actions
    fetchProfile,
    updateProfile,
    updateTopGames,
    uploadProfileImage,
    clearProfile,
    clearAllProfiles
  }
})