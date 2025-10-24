import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/authStore'
import { useToast } from 'vue-toastification'
import { listsService } from '@/services/listsService'

/**
 * Composable for list CRUD operations
 * STATELESS - components manage their own list data
 *
 * @returns {Object} List operation methods
 */
export function useLists() {
  const router = useRouter()
  const authStore = useAuthStore()
  const toast = useToast()

  /**
   * Load lists with optional filtering
   * @param {Object} options - Load options
   * @param {string} options.userId - Optional user ID to filter by
   * @param {number} options.page - Page number (default: 1)
   * @param {number} options.pageSize - Page size (default: 10)
   * @returns {Object} { data, hasNextPage, totalCount, page, totalPages }
   */
  const loadLists = async ({ userId, page = 1, pageSize = 10 } = {}) => {
    try {
      let response

      if (userId) {
        response = await listsService.getUserLists(userId, page, pageSize)
      } else {
        response = await listsService.getPublicLists(page, pageSize)
      }

      return {
        data: response.data || [],
        hasNextPage: response.hasNextPage || false,
        hasPreviousPage: response.hasPreviousPage || false,
        totalCount: response.totalCount || 0,
        page: response.page || page,
        totalPages: response.totalPages || 0
      }
    } catch (error) {
      console.error('Error loading lists:', error)
      toast.error('Failed to load lists')
      throw error
    }
  }

  /**
   * Load a single list by ID
   * @param {string} listId - The list ID
   * @returns {Object} The list object
   */
  const loadList = async (listId) => {
    try {
      const list = await listsService.getList(listId)
      return list
    } catch (error) {
      console.error('Error loading list:', error)
      toast.error(error.message || 'Failed to load list')
      throw error
    }
  }

  /**
   * Create a new list
   * @param {Object} listData - The list data
   * @param {Function} onSuccess - Optional callback on successful creation
   * @returns {Object} - Returns the created list or null on failure
   */
  const createList = async (listData, onSuccess) => {
    try {
      const createdList = await listsService.createList(listData)
      toast.success('List created successfully!')

      // Call success callback if provided
      if (onSuccess) {
        onSuccess(createdList)
      }

      return createdList
    } catch (error) {
      console.error('Error creating list:', error)
      toast.error(error.message || 'Failed to create list')
      return null
    }
  }

  /**
   * Update a list
   * @param {string} listId - The list ID
   * @param {Object} listData - The updated list data
   * @param {Function} onSuccess - Optional callback on successful update
   * @returns {Object} - Returns the updated list or null on failure
   */
  const updateList = async (listId, listData, onSuccess) => {
    try {
      const updatedList = await listsService.updateList(listId, listData)
      toast.success('List updated successfully!')

      // Call success callback if provided
      if (onSuccess) {
        onSuccess(updatedList)
      }

      return updatedList
    } catch (error) {
      console.error('Error updating list:', error)
      toast.error(error.message || 'Failed to update list')
      return null
    }
  }

  /**
   * Delete a list
   * @param {Object} list - The list object to delete
   * @param {Function} onSuccess - Optional callback on successful delete
   * @returns {boolean} - Returns true if delete was successful
   */
  const deleteList = async (list, onSuccess) => {
    if (list.userId !== authStore.user?.id) {
      toast.error('You can only delete your own lists')
      return false
    }

    if (!confirm('Are you sure you want to delete this list?')) {
      return false
    }

    try {
      await listsService.deleteList(list.id)
      toast.success('List deleted successfully!')

      // Call success callback if provided
      if (onSuccess) {
        onSuccess()
      }

      return true
    } catch (error) {
      console.error('Error deleting list:', error)
      toast.error('Failed to delete list')
      return false
    }
  }

  /**
   * Add game to list
   * @param {string} listId - The list ID
   * @param {string} gameId - The game ID to add
   * @param {Function} onSuccess - Optional callback on successful addition
   * @returns {boolean} - Returns true if addition was successful
   */
  const addGameToList = async (listId, gameId, onSuccess) => {
    try {
      await listsService.addGameToList(listId, gameId)
      toast.success('Game added to list!')

      // Call success callback if provided
      if (onSuccess) {
        onSuccess()
      }

      return true
    } catch (error) {
      console.error('Error adding game to list:', error)
      toast.error(error.message || 'Failed to add game to list')
      return false
    }
  }

  /**
   * Remove game from list
   * @param {string} listId - The list ID
   * @param {string} gameId - The game ID to remove
   * @param {Function} onSuccess - Optional callback on successful removal
   * @returns {boolean} - Returns true if removal was successful
   */
  const removeGameFromList = async (listId, gameId, onSuccess) => {
    if (!confirm('Are you sure you want to remove this game from the list?')) {
      return false
    }

    try {
      await listsService.removeGameFromList(listId, gameId)
      toast.success('Game removed from list!')

      // Call success callback if provided
      if (onSuccess) {
        onSuccess()
      }

      return true
    } catch (error) {
      console.error('Error removing game from list:', error)
      toast.error('Failed to remove game from list')
      return false
    }
  }

  /**
   * Navigate to list details page
   * @param {Object} list - The list object
   */
  const navigateToDetails = (list) => {
    router.push(`/lists/${list.id}`)
  }

  /**
   * Navigate to lists page
   */
  const navigateToLists = () => {
    router.push('/lists')
  }

  /**
   * Navigate back based on context
   * @param {Object} options - Navigation options
   * @param {string} options.userId - Optional user ID to navigate to
   * @param {string} options.fallback - Fallback route (default: '/lists')
   */
  const navigateBack = ({ userId, fallback = '/lists' } = {}) => {
    if (userId) {
      router.push(`/profile/${userId}`)
    } else if (window.history.length > 1) {
      router.go(-1)
    } else {
      router.push(fallback)
    }
  }

  return {
    // Data operations
    loadLists,
    loadList,
    createList,
    updateList,
    deleteList,
    addGameToList,
    removeGameFromList,

    // Navigation
    navigateToDetails,
    navigateToLists,
    navigateBack
  }
}
