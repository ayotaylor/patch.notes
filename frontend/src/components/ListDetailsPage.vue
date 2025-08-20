<template>
  <div class="list-details-page">
    <div class="container-fluid py-4">
      <ListDetails
        :list="list"
        :loading="loading"
        :error="error"
        :is-liked="isLiked"
        :is-processing-like="isProcessingLike"
        :comment-count="commentCount"
        @edit="handleEditList"
        @delete="handleDeleteList" 
        @addGames="handleAddGames"
        @removeGame="handleRemoveGame"
        @toggleLike="handleToggleLike"
        @retry="loadList"
      >
        <!-- Comments Section Slot -->
        <template #comments>
          <CommentSection
            item-type="list"
            :item-id="listId"
            :auto-load="!!list"
            @countChanged="handleCommentCountChanged"
            ref="commentSectionRef"
          />
        </template>
      </ListDetails>
    </div>

    <!-- Edit List Modal -->
    <ListForm
      modal-id="editListModal"
      :list="list"
      @submit="handleSubmitListEdit"
      @close="handleCloseEditModal"
      ref="listFormRef"
    />

    <!-- Add Games Modal -->
    <div class="modal fade" id="addGamesModal" tabindex="-1" aria-hidden="true">
      <div class="modal-dialog modal-lg">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">Add Games to List</h5>
            <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
          </div>
          <div class="modal-body">
            <!-- Game Search -->
            <div class="mb-3">
              <div class="input-group">
                <input
                  v-model="gameSearchQuery"
                  type="text"
                  class="form-control"
                  placeholder="Search for games to add..."
                  @input="searchGames"
                >
                <button 
                  type="button" 
                  class="btn btn-outline-secondary"
                  @click="clearGameSearch"
                  :disabled="!gameSearchQuery"
                >
                  <i class="fas fa-times"></i>
                </button>
              </div>

              <!-- Search Results -->
              <div v-if="gameSearchResults.length > 0" class="mt-2 border rounded" style="max-height: 300px; overflow-y: auto;">
                <div 
                  v-for="game in gameSearchResults" 
                  :key="game.id"
                  class="d-flex align-items-center p-3 border-bottom cursor-pointer hover-bg-light"
                  style="cursor: pointer;"
                >
                  <img
                    :src="getGameImageUrl(game)"
                    :alt="game.name"
                    class="rounded me-3"
                    style="width: 50px; height: 50px; object-fit: cover;"
                    @error="handleGameImageError"
                  >
                  <div class="flex-grow-1">
                    <div class="fw-semibold">{{ game.name }}</div>
                    <small class="text-muted">{{ game.releaseDate ? new Date(game.releaseDate).getFullYear() : 'N/A' }}</small>
                  </div>
                  <button 
                    type="button" 
                    class="btn btn-sm btn-primary"
                    @click="addGameToList(game)"
                    :disabled="isGameInList(game.id) || isAddingGame"
                  >
                    <span v-if="isAddingGame && addingGameId === game.id" class="spinner-border spinner-border-sm me-1" role="status"></span>
                    <i class="fas" :class="isGameInList(game.id) ? 'fa-check' : 'fa-plus'"></i>
                    {{ isGameInList(game.id) ? 'Added' : 'Add' }}
                  </button>
                </div>
              </div>

              <!-- Search Loading -->
              <div v-if="searchLoading" class="mt-2 text-center py-3">
                <div class="spinner-border spinner-border-sm" role="status">
                  <span class="visually-hidden">Loading...</span>
                </div>
                <small class="ms-2">Searching games...</small>
              </div>

              <!-- No Results -->
              <div v-if="gameSearchQuery && !searchLoading && gameSearchResults.length === 0" class="mt-2 text-center text-muted py-3">
                <i class="fas fa-search mb-2"></i>
                <p class="mb-0">No games found for "{{ gameSearchQuery }}"</p>
              </div>
            </div>
          </div>
          <div class="modal-footer">
            <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">
              Done
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Delete Confirmation Modal -->
    <div class="modal fade" id="deleteListModal" tabindex="-1" aria-hidden="true">
      <div class="modal-dialog">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">Delete List</h5>
            <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
          </div>
          <div class="modal-body">
            <p>Are you sure you want to delete <strong>"{{ list?.title }}"</strong>?</p>
            <p class="text-muted mb-0">This action cannot be undone.</p>
          </div>
          <div class="modal-footer">
            <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">
              Cancel
            </button>
            <button 
              @click="confirmDeleteList"
              class="btn btn-danger"
              :disabled="isDeletingList"
            >
              <span v-if="isDeletingList" class="spinner-border spinner-border-sm me-2" role="status"></span>
              Delete List
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, nextTick } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/authStore'
import { useGamesStore } from '@/stores/gamesStore'
import { listsService } from '@/services/listsService'
import { socialService } from '@/services/socialService'
import ListDetails from './ListDetails.vue'
import ListForm from './ListForm.vue'
import CommentSection from './CommentSection.vue'

// Composables
const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()
const gamesStore = useGamesStore()

// State
const list = ref(null)
const loading = ref(false)
const error = ref(null)
const isLiked = ref(false)
const isProcessingLike = ref(false)
const commentCount = ref(0)

// Game search for adding games
const gameSearchQuery = ref('')
const gameSearchResults = ref([])
const searchLoading = ref(false)
const searchTimeout = ref(null)
const isAddingGame = ref(false)
const addingGameId = ref(null)

const isDeletingList = ref(false)

const listFormRef = ref(null)
const commentSectionRef = ref(null)

// Computed
const listId = computed(() => route.params.listId)

const canEdit = computed(() => {
  return authStore.user && list.value && authStore.user.id === list.value.user.id
})

// Methods
const loadList = async () => {
  if (!listId.value) return

  try {
    loading.value = true
    error.value = null

    list.value = await listsService.getList(listId.value)
    
    // Initialize comment count from API response
    commentCount.value = list.value?.commentCount || 0
    
    // Load like status if user is authenticated
    if (authStore.user) {
      await loadLikeStatus()
    }

  } catch (err) {
    console.error('Error loading list:', err)
    error.value = err.message || 'Failed to load list'
  } finally {
    loading.value = false
  }
}

const loadLikeStatus = async () => {
  if (!authStore.user || !list.value) return

  try {
    isLiked.value = await socialService.isListLiked(list.value.id)
  } catch (err) {
    console.error('Error loading like status:', err)
    isLiked.value = false
  }
}

const handleEditList = () => {
  nextTick(() => {
    const modal = new window.bootstrap.Modal(document.getElementById('editListModal'))
    modal.show()
  })
}

const handleSubmitListEdit = async (listData) => {
  try {
    const updatedList = await listsService.updateList(list.value.id, listData)
    list.value = updatedList

    // Hide modal
    const modal = window.bootstrap.Modal.getInstance(document.getElementById('editListModal'))
    modal.hide()

  } catch (err) {
    console.error('Error updating list:', err)
  }
}

const handleCloseEditModal = () => {
  // Modal cleanup if needed
}

const handleDeleteList = () => {
  const modal = new window.bootstrap.Modal(document.getElementById('deleteListModal'))
  modal.show()
}

const confirmDeleteList = async () => {
  if (!list.value) return

  try {
    isDeletingList.value = true
    await listsService.deleteList(list.value.id)

    // Navigate back to lists page
    router.push('/lists')

  } catch (err) {
    console.error('Error deleting list:', err)
  } finally {
    isDeletingList.value = false
  }
}

const handleAddGames = () => {
  gameSearchQuery.value = ''
  gameSearchResults.value = []
  const modal = new window.bootstrap.Modal(document.getElementById('addGamesModal'))
  modal.show()
}

const handleRemoveGame = async (game) => {
  if (!canEdit.value) return

  try {
    await listsService.removeGameFromList(list.value.id, game.id)
    
    // Remove game from list
    if (list.value.games) {
      const gameIndex = list.value.games.findIndex(g => g.id === game.id)
      if (gameIndex !== -1) {
        list.value.games.splice(gameIndex, 1)
      }
    }

  } catch (err) {
    console.error('Error removing game from list:', err)
  }
}

const handleToggleLike = async () => {
  if (!authStore.user || !list.value) return

  const wasLiked = isLiked.value

  try {
    isProcessingLike.value = true

    if (wasLiked) {
      await socialService.unlikeList(list.value.id)
      isLiked.value = false
    } else {
      await socialService.likeList(list.value.id)
      isLiked.value = true
    }

    // Update like count
    list.value.likeCount = (list.value.likeCount || 0) + (wasLiked ? -1 : 1)

  } catch (err) {
    console.error('Error toggling list like:', err)
  } finally {
    isProcessingLike.value = false
  }
}

const handleCommentCountChanged = (count) => {
  commentCount.value = count
}

// Game search methods
const searchGames = async () => {
  if (searchTimeout.value) {
    clearTimeout(searchTimeout.value)
  }

  searchTimeout.value = setTimeout(async () => {
    if (!gameSearchQuery.value.trim()) {
      gameSearchResults.value = []
      return
    }

    try {
      searchLoading.value = true
      const results = await gamesStore.searchGames(gameSearchQuery.value.trim(), 1)
      gameSearchResults.value = results.slice(0, 10) // Limit to 10 results
    } catch (error) {
      console.error('Error searching games:', error)
      gameSearchResults.value = []
    } finally {
      searchLoading.value = false
    }
  }, 300) // Debounce for 300ms
}

const clearGameSearch = () => {
  gameSearchQuery.value = ''
  gameSearchResults.value = []
}

const addGameToList = async (game) => {
  if (!canEdit.value || isGameInList(game.id)) return

  try {
    isAddingGame.value = true
    addingGameId.value = game.id

    await listsService.addGameToList(list.value.id, game.id)
    
    // Add game to list
    if (!list.value.games) {
      list.value.games = []
    }
    list.value.games.push(game)

    // Clear search
    clearGameSearch()

  } catch (err) {
    console.error('Error adding game to list:', err)
  } finally {
    isAddingGame.value = false
    addingGameId.value = null
  }
}

const isGameInList = (gameId) => {
  return list.value?.games?.some(game => game.id === gameId) || false
}

const getGameImageUrl = (game) => {
  return game?.coverUrl || game?.primaryImageUrl || game?.cover?.imageUrl || 'data:image/svg+xml;charset=UTF-8,<svg xmlns="http://www.w3.org/2000/svg" width="200" height="200" viewBox="0 0 200 200"><rect width="200" height="200" fill="%23f8f9fa"/><text x="50%" y="50%" text-anchor="middle" dy=".3em" fill="%236c757d" font-family="Arial" font-size="14">Game Cover</text></svg>'
}

const handleGameImageError = (e) => {
  // Prevent infinite loading loop
  if (!e.target.dataset.errorHandled) {
    e.target.dataset.errorHandled = 'true'
    e.target.src = 'data:image/svg+xml;charset=UTF-8,<svg xmlns="http://www.w3.org/2000/svg" width="200" height="200" viewBox="0 0 200 200"><rect width="200" height="200" fill="%23f8f9fa"/><text x="50%" y="50%" text-anchor="middle" dy=".3em" fill="%236c757d" font-family="Arial" font-size="14">Game Cover</text></svg>'
  }
}

// Lifecycle
onMounted(() => {
  if (listId.value) {
    loadList()
  }
})
</script>

<style scoped>
.list-details-page {
  min-height: 100vh;
  background-color: #f8f9fa;
}

.hover-bg-light:hover {
  background-color: rgba(0, 0, 0, 0.05);
}

.cursor-pointer {
  cursor: pointer;
}

@media (max-width: 768px) {
  .container-fluid {
    padding-left: 1rem;
    padding-right: 1rem;
  }
}
</style>