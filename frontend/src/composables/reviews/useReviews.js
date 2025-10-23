import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/authStore'
import { useToast } from 'vue-toastification'
import { reviewsService } from '@/services/reviewsService'
import { commentsService } from '@/services/commentsService'

/**
 * Composable for review CRUD operations
 * STATELESS - components manage their own review data
 *
 * @returns {Object} Review operation methods
 */
export function useReviews() {
  const router = useRouter()
  const authStore = useAuthStore()
  const toast = useToast()

  /**
   * Load reviews with optional filtering
   * @param {Object} options - Load options
   * @param {string|number} options.gameId - Optional game ID to filter by
   * @param {string|number} options.userId - Optional user ID to filter by
   * @param {number} options.page - Page number (default: 1)
   * @param {number} options.pageSize - Page size (default: 15)
   * @returns {Object} { data, hasNextPage, totalCount }
   */
  const loadReviews = async ({ gameId, userId, page = 1, pageSize = 15 } = {}) => {
    try {
      let response

      if (gameId) {
        response = await reviewsService.getGameReviews(gameId, page, pageSize)
      } else if (userId) {
        response = await reviewsService.getUserReviews(userId, page, pageSize)
      } else {
        response = await reviewsService.getLatestReviews(page, pageSize)
        // For latest reviews, we need to simulate pagination if response is an array
        if (Array.isArray(response)) {
          response = {
            data: response.slice((page - 1) * pageSize, page * pageSize),
            hasNextPage: response.length > page * pageSize,
            totalCount: response.length
          }
        }
      }

      // Load comment counts for reviews
      const reviewsWithCommentCounts = await commentsService.loadCommentCountsForReviews(
        response.data || []
      )

      return {
        data: reviewsWithCommentCounts,
        hasNextPage: response.hasNextPage || false,
        totalCount: response.totalCount || 0
      }
    } catch (error) {
      console.error('Error loading reviews:', error)
      toast.error('Failed to load reviews')
      throw error
    }
  }

  /**
   * Load a single review by ID
   * @param {string|number} reviewId - The review ID
   * @returns {Object} The review object
   */
  const loadReview = async (reviewId) => {
    try {
      const review = await reviewsService.getReview(reviewId)

      // Load comment count and add it to the review object
      try {
        const count = await commentsService.getReviewCommentCount(reviewId)
        review.commentCount = count
      } catch (commentErr) {
        console.error('Error loading comment count:', commentErr)
        review.commentCount = 0
      }

      return review
    } catch (error) {
      console.error('Error loading review:', error)
      toast.error(error.message || 'Failed to load review')
      throw error
    }
  }

  /**
   * Delete a review
   * @param {Object} review - The review object to delete
   * @param {Function} onSuccess - Optional callback on successful delete
   * @returns {boolean} - Returns true if delete was successful
   */
  const deleteReview = async (review, onSuccess) => {
    if (review.user.id !== authStore.user?.id) {
      toast.error('You can only delete your own reviews')
      return false
    }

    if (!confirm('Are you sure you want to delete this review?')) {
      return false
    }

    try {
      await reviewsService.deleteReview(review.id)
      toast.success('Review deleted successfully!')

      // Call success callback if provided
      if (onSuccess) {
        onSuccess()
      }

      return true
    } catch (error) {
      console.error('Error deleting review:', error)
      toast.error('Failed to delete review')
      return false
    }
  }

  /**
   * Update a review
   * @param {string|number} reviewId - The review ID
   * @param {Object} reviewData - The updated review data
   * @param {Function} onSuccess - Optional callback on successful update
   * @returns {Object} - Returns the updated review or null on failure
   */
  const updateReview = async (reviewId, reviewData, onSuccess) => {
    try {
      const updatedReview = await reviewsService.updateReview(reviewId, reviewData)
      toast.success('Review updated successfully!')

      // Call success callback if provided
      if (onSuccess) {
        onSuccess(updatedReview)
      }

      return updatedReview
    } catch (error) {
      console.error('Error updating review:', error)
      toast.error(error.message || 'Failed to update review')
      return null
    }
  }

  /**
   * Navigate to review edit page
   * @param {Object} review - The review object to edit
   */
  const navigateToEdit = (review) => {
    if (review.user.id === authStore.user?.id) {
      router.push(`/games/${review.game.slug}?edit=true`)
    }
  }

  /**
   * Navigate to review details page
   * @param {Object} review - The review object
   */
  const navigateToDetails = (review) => {
    router.push(`/reviews/${review.id}`)
  }

  /**
   * Navigate back based on context
   * @param {Object} options - Navigation options
   * @param {string|number} options.gameId - Optional game ID to navigate to
   * @param {string|number} options.userId - Optional user ID to navigate to
   * @param {Object} options.review - Optional review object for fallback navigation
   * @param {string} options.fallback - Fallback route (default: '/dashboard')
   */
  const navigateBack = ({ gameId, userId, review, fallback = '/dashboard' } = {}) => {
    if (gameId) {
      router.push(`/games/${gameId}`)
    } else if (userId) {
      router.push(`/profile/${userId}`)
    } else if (window.history.length > 1) {
      router.go(-1)
    } else if (review?.game) {
      router.push(`/games/${review.game.slug}`)
    } else {
      router.push(fallback)
    }
  }

  return {
    // Data operations
    loadReviews,
    loadReview,
    deleteReview,
    updateReview,

    // Navigation
    navigateToEdit,
    navigateToDetails,
    navigateBack
  }
}
