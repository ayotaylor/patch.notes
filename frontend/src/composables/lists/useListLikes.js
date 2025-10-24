import { ref } from 'vue'
import { useAuthStore } from '@/stores/authStore'
import { useToast } from 'vue-toastification'
import apiClient from '@/services/apiClient'

// Global state - shared across all components
const likedLists = ref(new Set())
const processingLikeLists = ref(new Set())

/**
 * Composable for managing list likes
 * STATEFUL - maintains global like state across all components
 *
 * @returns {Object} Like state and methods
 */
export function useListLikes() {
  const authStore = useAuthStore()
  const toast = useToast()

  /**
   * Load like status for a single list
   * @param {string} listId - The list ID to check
   */
  const loadLikeStatus = async (listId) => {
    if (!authStore.user || !listId) return

    try {
      const response = await apiClient.get(`/social/lists/${listId}/liked`)
      const isLiked = response.data.data

      if (isLiked) {
        likedLists.value.add(listId)
      } else {
        likedLists.value.delete(listId)
      }
    } catch (err) {
      console.error('Error loading like status for list:', listId, err)
    }
  }

  /**
   * Load like status for multiple lists
   * @param {Array} lists - Array of list objects with id property
   */
  const loadLikeStatusBatch = async (lists) => {
    if (!authStore.user || !lists || lists.length === 0) return

    try {
      const likeStatusPromises = lists.map(async (list) => {
        try {
          const response = await apiClient.get(`/social/lists/${list.id}/liked`)
          const isLiked = response.data.data

          if (isLiked) {
            likedLists.value.add(list.id)
          }
        } catch (error) {
          console.warn(`Failed to check like status for list ${list.id}:`, error)
        }
      })

      await Promise.all(likeStatusPromises)
    } catch (err) {
      console.error('Error loading like status batch:', err)
    }
  }

  /**
   * Toggle like status for a list
   * @param {Object} list - The list object to like/unlike
   * @param {Function} onSuccess - Optional callback on successful toggle
   * @returns {boolean} - Returns true if toggle was successful
   */
  const toggleLike = async (list, onSuccess) => {
    if (!authStore.user) {
      toast.info('Please sign in to like lists')
      return false
    }

    const listId = list.id
    const wasLiked = likedLists.value.has(listId)

    // Prevent duplicate requests
    if (processingLikeLists.value.has(listId)) return false

    try {
      processingLikeLists.value.add(listId)

      // Optimistic update
      if (wasLiked) {
        likedLists.value.delete(listId)
      } else {
        likedLists.value.add(listId)
      }

      // Make API call
      if (wasLiked) {
        await apiClient.delete(`/social/lists/${listId}/like`)
      } else {
        await apiClient.post(`/social/lists/${listId}/like`)
      }

      // Call success callback if provided
      if (onSuccess) {
        onSuccess(wasLiked)
      }

      return true
    } catch (err) {
      console.error('Error toggling list like:', err)
      toast.error('Failed to update like')

      // Revert optimistic update on error
      if (wasLiked) {
        likedLists.value.add(listId)
      } else {
        likedLists.value.delete(listId)
      }

      return false
    } finally {
      processingLikeLists.value.delete(listId)
    }
  }

  /**
   * Check if a list is liked by the current user
   * @param {string} listId - The list ID to check
   * @returns {boolean}
   */
  const isLiked = (listId) => {
    return likedLists.value.has(listId)
  }

  /**
   * Check if a list is currently being processed
   * @param {string} listId - The list ID to check
   * @returns {boolean}
   */
  const isProcessing = (listId) => {
    return processingLikeLists.value.has(listId)
  }

  /**
   * Clear all like state (useful for logout)
   */
  const clearLikes = () => {
    likedLists.value.clear()
    processingLikeLists.value.clear()
  }

  return {
    // State
    likedLists,
    processingLikeLists,

    // Methods
    loadLikeStatus,
    loadLikeStatusBatch,
    toggleLike,
    isLiked,
    isProcessing,
    clearLikes
  }
}
