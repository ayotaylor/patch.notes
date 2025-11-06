import { useToast } from 'vue-toastification'
import { commentsService } from '@/services/commentsService'

/**
 * Composable for comment operations
 * STATELESS - components manage their own comment data
 *
 * @returns {Object} Comment operation methods
 */
export function useComments() {
  const toast = useToast()

  /**
   * Load comments for a review
   * @param {string|number} reviewId - The review ID
   * @param {number} page - Page number (default: 1)
   * @param {number} pageSize - Page size (default: 10)
   * @returns {Object} { data, hasNextPage, hasPreviousPage, totalCount, page, totalPages }
   */
  const loadReviewComments = async (reviewId, page = 1, pageSize = 10) => {
    try {
      const response = await commentsService.getReviewComments(reviewId, page, pageSize)
      return {
        data: response.data || [],
        hasNextPage: response.hasNextPage || false,
        hasPreviousPage: response.hasPreviousPage || false,
        totalCount: response.totalCount || 0,
        page: response.page || page,
        totalPages: response.totalPages || 0
      }
    } catch (error) {
      console.error('Error loading review comments:', error)
      toast.error('Failed to load comments')
      throw error
    }
  }

  /**
   * Load comments for a list
   * @param {string|number} listId - The list ID
   * @param {number} page - Page number (default: 1)
   * @param {number} pageSize - Page size (default: 10)
   * @returns {Object} { data, hasNextPage, hasPreviousPage, totalCount, page, totalPages }
   */
  const loadListComments = async (listId, page = 1, pageSize = 10) => {
    try {
      const response = await commentsService.getListComments(listId, page, pageSize)
      return {
        data: response.data || [],
        hasNextPage: response.hasNextPage || false,
        hasPreviousPage: response.hasPreviousPage || false,
        totalCount: response.totalCount || 0,
        page: response.page || page,
        totalPages: response.totalPages || 0
      }
    } catch (error) {
      console.error('Error loading list comments:', error)
      toast.error('Failed to load comments')
      throw error
    }
  }

  /**
   * Add a comment to a review
   * @param {string|number} reviewId - The review ID
   * @param {string} content - The comment content
   * @param {Function} onSuccess - Optional callback on successful addition
   * @returns {Object|null} - Returns the created comment or null on failure
   */
  const addReviewComment = async (reviewId, content, onSuccess) => {
    try {
      const comment = await commentsService.addReviewComment(reviewId, { content })
      toast.success('Comment posted successfully!')

      // Call success callback if provided
      if (onSuccess) {
        onSuccess(comment)
      }

      return comment
    } catch (error) {
      console.error('Error adding review comment:', error)
      toast.error(error.message || 'Failed to post comment')
      return null
    }
  }

  /**
   * Add a comment to a list
   * @param {string|number} listId - The list ID
   * @param {string} content - The comment content
   * @param {Function} onSuccess - Optional callback on successful addition
   * @returns {Object|null} - Returns the created comment or null on failure
   */
  const addListComment = async (listId, content, onSuccess) => {
    try {
      const comment = await commentsService.addListComment(listId, { content })
      toast.success('Comment posted successfully!')

      // Call success callback if provided
      if (onSuccess) {
        onSuccess(comment)
      }

      return comment
    } catch (error) {
      console.error('Error adding list comment:', error)
      toast.error(error.message || 'Failed to post comment')
      return null
    }
  }

  /**
   * Update a comment
   * @param {string|number} commentId - The comment ID
   * @param {string} content - The updated comment content
   * @param {Function} onSuccess - Optional callback on successful update
   * @returns {Object|null} - Returns the updated comment or null on failure
   */
  const updateComment = async (commentId, content, onSuccess) => {
    try {
      const comment = await commentsService.updateComment(commentId, { content })
      toast.success('Comment updated successfully!')

      // Call success callback if provided
      if (onSuccess) {
        onSuccess(comment)
      }

      return comment
    } catch (error) {
      console.error('Error updating comment:', error)
      toast.error(error.message || 'Failed to update comment')
      return null
    }
  }

  /**
   * Delete a comment
   * @param {string|number} commentId - The comment ID
   * @param {Function} onSuccess - Optional callback on successful deletion
   * @returns {boolean} - Returns true if deletion was successful
   */
  const deleteComment = async (commentId, onSuccess) => {
    if (!confirm('Are you sure you want to delete this comment?')) {
      return false
    }

    try {
      await commentsService.deleteComment(commentId)
      toast.success('Comment deleted successfully!')

      // Call success callback if provided
      if (onSuccess) {
        onSuccess()
      }

      return true
    } catch (error) {
      console.error('Error deleting comment:', error)
      toast.error('Failed to delete comment')
      return false
    }
  }

  return {
    loadReviewComments,
    loadListComments,
    addReviewComment,
    addListComment,
    updateComment,
    deleteComment
  }
}
