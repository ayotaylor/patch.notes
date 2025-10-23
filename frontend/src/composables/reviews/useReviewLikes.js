import { ref } from 'vue'
import { useAuthStore } from '@/stores/authStore'
import { useToast } from 'vue-toastification'
import { socialService } from '@/services/socialService'

// Global state - shared across all components
const likedReviews = ref(new Set())
const processingLikeReviews = ref(new Set())

/**
 * Composable for managing review likes
 * STATEFUL - maintains global like state across all components
 *
 * @returns {Object} Like state and methods
 */
export function useReviewLikes() {
  const authStore = useAuthStore()
  const toast = useToast()

  /**
   * Load like status for a single review
   * @param {number|string} reviewId - The review ID to check
   */
  const loadLikeStatus = async (reviewId) => {
    if (!authStore.user || !reviewId) return

    try {
      const isLiked = await socialService.isReviewLiked(reviewId)
      if (isLiked) {
        likedReviews.value.add(reviewId)
      } else {
        likedReviews.value.delete(reviewId)
      }
    } catch (err) {
      console.error('Error loading like status for review:', reviewId, err)
    }
  }

  /**
   * Load like status for multiple reviews
   * @param {Array} reviews - Array of review objects with id property
   */
  const loadLikeStatusBatch = async (reviews) => {
    if (!authStore.user || !reviews || reviews.length === 0) return

    try {
      const likeStatusPromises = reviews.map(async (review) => {
        try {
          const isLiked = await socialService.isReviewLiked(review.id)
          if (isLiked) {
            likedReviews.value.add(review.id)
          }
        } catch (error) {
          console.warn(`Failed to check like status for review ${review.id}:`, error)
        }
      })

      await Promise.all(likeStatusPromises)
    } catch (err) {
      console.error('Error loading like status batch:', err)
    }
  }

  /**
   * Toggle like status for a review
   * @param {Object} review - The review object to like/unlike
   * @param {Function} onSuccess - Optional callback on successful toggle
   * @returns {boolean} - Returns true if toggle was successful
   */
  const toggleLike = async (review, onSuccess) => {
    if (!authStore.user) {
      toast.info('Please sign in to like reviews')
      return false
    }

    const reviewId = review.id
    const wasLiked = likedReviews.value.has(reviewId)

    // Prevent duplicate requests
    if (processingLikeReviews.value.has(reviewId)) return false

    try {
      processingLikeReviews.value.add(reviewId)

      // Optimistic update
      if (wasLiked) {
        likedReviews.value.delete(reviewId)
      } else {
        likedReviews.value.add(reviewId)
      }

      // Make API call
      if (wasLiked) {
        await socialService.unlikeReview(reviewId)
      } else {
        await socialService.likeReview(reviewId)
      }

      // Call success callback if provided
      if (onSuccess) {
        onSuccess(wasLiked)
      }

      return true
    } catch (err) {
      console.error('Error toggling review like:', err)
      toast.error('Failed to update like')

      // Revert optimistic update on error
      if (wasLiked) {
        likedReviews.value.add(reviewId)
      } else {
        likedReviews.value.delete(reviewId)
      }

      return false
    } finally {
      processingLikeReviews.value.delete(reviewId)
    }
  }

  /**
   * Check if a review is liked by the current user
   * @param {number|string} reviewId - The review ID to check
   * @returns {boolean}
   */
  const isLiked = (reviewId) => {
    return likedReviews.value.has(reviewId)
  }

  /**
   * Check if a review is currently being processed
   * @param {number|string} reviewId - The review ID to check
   * @returns {boolean}
   */
  const isProcessing = (reviewId) => {
    return processingLikeReviews.value.has(reviewId)
  }

  /**
   * Clear all like state (useful for logout)
   */
  const clearLikes = () => {
    likedReviews.value.clear()
    processingLikeReviews.value.clear()
  }

  return {
    // State
    likedReviews,
    processingLikeReviews,

    // Methods
    loadLikeStatus,
    loadLikeStatusBatch,
    toggleLike,
    isLiked,
    isProcessing,
    clearLikes
  }
}
