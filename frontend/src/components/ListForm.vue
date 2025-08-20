<template>
  <div class="modal fade" :id="modalId" tabindex="-1" aria-hidden="true">
    <div class="modal-dialog modal-lg">
      <div class="modal-content">
        <div class="modal-header">
          <h5 class="modal-title">{{ isEditing ? 'Edit List' : 'Create New List' }}</h5>
          <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
        </div>
        
        <form @submit.prevent="submitForm">
          <div class="modal-body">
            <!-- Title Field -->
            <div class="mb-3">
              <label for="listTitle" class="form-label">List Title <span class="text-danger">*</span></label>
              <input
                id="listTitle"
                v-model="formData.title"
                type="text"
                class="form-control"
                :class="{ 'is-invalid': errors.title }"
                placeholder="Enter list title..."
                maxlength="100"
                required
              >
              <div v-if="errors.title" class="invalid-feedback">
                {{ errors.title }}
              </div>
              <div class="form-text">{{ formData.title.length }}/100 characters</div>
            </div>

            <!-- Description Field -->
            <div class="mb-3">
              <label for="listDescription" class="form-label">Description</label>
              <textarea
                id="listDescription"
                v-model="formData.description"
                class="form-control"
                :class="{ 'is-invalid': errors.description }"
                rows="3"
                placeholder="Describe your list..."
                maxlength="500"
              ></textarea>
              <div v-if="errors.description" class="invalid-feedback">
                {{ errors.description }}
              </div>
              <div class="form-text">{{ formData.description.length }}/500 characters</div>
            </div>

            <!-- Privacy Setting -->
            <div class="mb-3">
              <div class="form-check form-switch">
                <input
                  id="listPublic"
                  v-model="formData.isPublic"
                  class="form-check-input"
                  type="checkbox"
                >
                <label class="form-check-label" for="listPublic">
                  <strong>{{ formData.isPublic ? 'Public List' : 'Private List' }}</strong>
                </label>
              </div>
              <div class="form-text">
                {{ formData.isPublic ? 'Anyone can view this list' : 'Only you can view this list' }}
              </div>
            </div>

            <!-- Game Search and Selection -->
            <div class="mb-3">
              <label class="form-label">Games in List</label>
              
              <!-- Search for games to add -->
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
                <div v-if="gameSearchResults.length > 0" class="mt-2 border rounded p-2" style="max-height: 200px; overflow-y: auto;" @scroll="handleSearchScroll" ref="searchResultsContainer">
                  <div 
                    v-for="game in gameSearchResults" 
                    :key="game.id"
                    class="d-flex align-items-center p-2 rounded cursor-pointer hover-bg-light"
                    @click="addGameToList(game)"
                    style="cursor: pointer;"
                  >
                    <img
                      :src="getGameImageUrl(game)"
                      :alt="game.name"
                      class="rounded me-3"
                      style="width: 40px; height: 40px; object-fit: cover;"
                      @error="handleGameImageError"
                    >
                    <div class="flex-grow-1">
                      <div class="fw-semibold">{{ game.name }}</div>
                      <small class="text-muted">{{ game.releaseDate ? new Date(game.releaseDate).getFullYear() : 'N/A' }}</small>
                    </div>
                    <button 
                      type="button" 
                      class="btn btn-sm btn-outline-primary"
                      :disabled="isGameInList(game.id)"
                    >
                      <i class="fas" :class="isGameInList(game.id) ? 'fa-check' : 'fa-plus'"></i>
                      {{ isGameInList(game.id) ? 'Added' : 'Add' }}
                    </button>
                  </div>
                  
                  <!-- Load more indicator -->
                  <div v-if="loadingMore" class="text-center p-2">
                    <div class="spinner-border spinner-border-sm" role="status">
                      <span class="visually-hidden">Loading more...</span>
                    </div>
                    <small class="ms-2">Loading more games...</small>
                  </div>
                  
                  <!-- End of results indicator -->
                  <div v-else-if="gameSearchResults.length > 0 && !gamesStore.canLoadMore" class="text-center text-muted p-2">
                    <small>No more results</small>
                  </div>
                </div>

                <!-- Loading state -->
                <div v-if="searchLoading" class="mt-2 text-center">
                  <div class="spinner-border spinner-border-sm" role="status">
                    <span class="visually-hidden">Loading...</span>
                  </div>
                  <small class="ms-2">Searching games...</small>
                </div>
              </div>

              <!-- Selected Games -->
              <div v-if="formData.games.length > 0" class="selected-games">
                <h6 class="mb-2">Selected Games ({{ formData.games.length }})</h6>
                <div class="row g-2">
                  <div 
                    v-for="game in formData.games" 
                    :key="game.id" 
                    class="col-auto position-relative"
                  >
                    <div class="game-item">
                      <img
                        :src="getGameImageUrl(game)"
                        :alt="game.name"
                        class="rounded"
                        style="width: 80px; height: 80px; object-fit: cover;"
                        @error="handleGameImageError"
                        :title="game.name"
                      >
                      <button
                        type="button"
                        class="btn btn-sm btn-danger position-absolute top-0 end-0"
                        style="margin: 4px;"
                        @click="removeGameFromList(game.id)"
                        :title="`Remove ${game.name}`"
                      >
                        <i class="fas fa-times" style="font-size: 0.75rem;"></i>
                      </button>
                    </div>
                  </div>
                </div>
              </div>

              <!-- Empty state -->
              <div v-else class="text-center text-muted py-4">
                <i class="fas fa-gamepad fa-2x mb-2 opacity-50"></i>
                <p class="mb-0">No games added yet. Search and add games to your list.</p>
              </div>

              <!-- Error message for games -->
              <div v-if="errors.games" class="invalid-feedback d-block mt-2">
                {{ errors.games }}
              </div>
            </div>
          </div>

          <div class="modal-footer">
            <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">
              Cancel
            </button>
            <button 
              type="submit" 
              class="btn btn-primary"
              :disabled="isSubmitting || !isFormValid"
            >
              <span v-if="isSubmitting" class="spinner-border spinner-border-sm me-2" role="status"></span>
              {{ isEditing ? 'Update List' : 'Create List' }}
            </button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, watch, defineProps, defineEmits, defineExpose } from 'vue'
import { useGamesStore } from '@/stores/gamesStore'

// Props
const props = defineProps({
  modalId: {
    type: String,
    default: 'listFormModal'
  },
  list: {
    type: Object,
    default: null
  }
})

// Emits
const emit = defineEmits(['submit', 'close'])

// Stores
const gamesStore = useGamesStore()

// State
const formData = ref({
  title: '',
  description: '',
  isPublic: true,
  games: []
})

const errors = ref({})
const isSubmitting = ref(false)
const gameSearchQuery = ref('')
const gameSearchResults = ref([])
const searchLoading = ref(false)
const searchTimeout = ref(null)
const loadingMore = ref(false)
const searchResultsContainer = ref(null)

// Computed
const isEditing = computed(() => !!props.list)

const isFormValid = computed(() => {
  return formData.value.title.trim().length > 0 && 
         formData.value.title.length <= 100 &&
         formData.value.description.length <= 500 &&
         formData.value.games.length > 0
})

// Methods
const resetForm = () => {
  formData.value = {
    title: '',
    description: '',
    isPublic: true,
    games: []
  }
  errors.value = {}
  gameSearchQuery.value = ''
  gameSearchResults.value = []
}

// Watch for prop changes
watch(() => props.list, (newList) => {
  if (newList) {
    formData.value = {
      title: newList.title || '',
      description: newList.description || '',
      isPublic: newList.isPublic !== false,
      games: [...(newList.games || [])]
    }
  } else {
    resetForm()
  }
}, { immediate: true })

const validateForm = () => {
  errors.value = {}

  if (!formData.value.title.trim()) {
    errors.value.title = 'Title is required'
  } else if (formData.value.title.length > 100) {
    errors.value.title = 'Title must be 100 characters or less'
  }

  if (formData.value.description.length > 500) {
    errors.value.description = 'Description must be 500 characters or less'
  }

  if (!formData.value.games || formData.value.games.length === 0) {
    errors.value.games = 'Please add at least one game to your list'
  }

  return Object.keys(errors.value).length === 0
}

const submitForm = async () => {
  if (!validateForm()) return

  try {
    isSubmitting.value = true
    
    const listData = {
      title: formData.value.title.trim(),
      description: formData.value.description.trim(),
      isPublic: formData.value.isPublic,
      games: formData.value.games
    }

    emit('submit', listData)
    
  } catch (error) {
    console.error('Error submitting form:', error)
  } finally {
    isSubmitting.value = false
  }
}

const searchGames = async () => {
  if (searchTimeout.value) {
    clearTimeout(searchTimeout.value)
  }

  searchTimeout.value = setTimeout(async () => {
    if (!gameSearchQuery.value.trim()) {
      gameSearchResults.value = []
      gamesStore.clearSearchResults()
      return
    }

    try {
      searchLoading.value = true
      await gamesStore.searchGames(gameSearchQuery.value.trim(), 1)
      gameSearchResults.value = gamesStore.allSearchResults
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
  gamesStore.clearSearchResults()
}

const handleSearchScroll = async (event) => {
  const container = event.target
  const scrollTop = container.scrollTop
  const scrollHeight = container.scrollHeight
  const clientHeight = container.clientHeight
  
  // Check if scrolled near bottom (within 50px)
  if (scrollTop + clientHeight >= scrollHeight - 50) {
    if (gamesStore.canLoadMore && !loadingMore.value && !searchLoading.value) {
      try {
        loadingMore.value = true
        await gamesStore.loadMoreSearchResults()
        gameSearchResults.value = gamesStore.allSearchResults
      } catch (error) {
        console.error('Error loading more search results:', error)
      } finally {
        loadingMore.value = false
      }
    }
  }
}

const addGameToList = (game) => {
  if (!isGameInList(game.id)) {
    formData.value.games.push(game)
  }
  // Clear search after adding
  clearGameSearch()
}

const removeGameFromList = (gameId) => {
  formData.value.games = formData.value.games.filter(game => game.id !== gameId)
}

const isGameInList = (gameId) => {
  return formData.value.games.some(game => game.id === gameId)
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

// Expose methods for parent component
defineExpose({
  resetForm
})
</script>

<style scoped>
.hover-bg-light:hover {
  background-color: rgba(0, 0, 0, 0.05);
}

.cursor-pointer {
  cursor: pointer;
}

.game-item {
  position: relative;
  display: inline-block;
}

.game-item:hover img {
  opacity: 0.8;
}

.selected-games {
  border: 1px solid #dee2e6;
  border-radius: 0.375rem;
  padding: 1rem;
  background-color: #f8f9fa;
}

@media (max-width: 768px) {
  .modal-dialog {
    margin: 0.5rem;
  }
  
  .game-item img {
    width: 60px !important;
    height: 60px !important;
  }
}
</style>