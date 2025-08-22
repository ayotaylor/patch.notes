<template>
  <div class="game-search-component">
    <!-- Search Input -->
    <div class="search-input-section">
      <div class="card shadow-sm border-0" v-if="showCard">
        <div class="card-header bg-white border-bottom" v-if="showTitle">
          <h3 class="h5 mb-0 fw-bold">
            <i class="fas fa-search text-primary me-2"></i>
            {{ title }}
          </h3>
        </div>
        <div class="card-body p-4">
          <div class="row">
            <div class="col-12 col-sm-11 col-md-10 col-lg-8 mx-auto">
              <div class="position-relative">
                <input
                  v-model="searchQuery"
                  type="text"
                  class="form-control form-control-lg pe-5"
                  :placeholder="placeholder"
                  :disabled="gamesStore.searchLoading"
                >
                <!-- Embedded clear button -->
                <button
                  v-if="searchQuery.trim() || hasResults"
                  @click="clearSearch"
                  class="btn-clear-embedded"
                  type="button"
                  title="Clear search"
                >
                  <i class="fas fa-times"></i>
                </button>
                <!-- Loading indicator -->
                <div
                  v-if="gamesStore.searchLoading"
                  class="search-loading-indicator"
                >
                  <span class="spinner-border spinner-border-sm text-primary"></span>
                </div>
              </div>
            </div>
          </div>

          <!-- Search Results -->
          <div v-if="showResults && hasResults" class="mt-4">
            <h6 class="fw-bold mb-3">
              {{ resultsTitle || `Search Results (${gamesStore.searchResults.length} of ${gamesStore.searchPagination.totalCount})` }}
            </h6>

            <!-- Compact Results (for ProfileComponent) -->
            <div v-if="resultsMode === 'compact'"
                 ref="scrollContainer"
                 class="list-group"
                 :style="{ maxHeight: maxHeight, overflowY: 'auto' }">
              <button
                v-for="game in displayedResults"
                :key="game.id"
                @click="selectGame(game)"
                :disabled="isGameDisabled(game)"
                class="list-group-item list-group-item-action d-flex align-items-center"
                :class="{ 'disabled': isGameDisabled(game) }"
              >
                <img
                  :src="getImageUrl(game.primaryImageUrl, 'gameIcon')"
                  :alt="game.name"
                  class="rounded me-3"
                  style="width: 40px; height: 40px; object-fit: cover;"
                  @error="(e) => handleImageError(e, 'gameIcon')"
                >
                <div class="flex-grow-1">
                  <h6 class="mb-0">{{ game.name }}</h6>
                  <small class="text-muted">{{ game.primaryGenre || 'Unknown Genre' }}</small>
                </div>
                <i v-if="isGameSelected && isGameSelected(game)" class="fas fa-check text-success ms-auto"></i>
              </button>

              <!-- Infinite scroll loading indicator -->
              <div v-if="paginationMode === 'infinite-scroll' && isLoadingMore" class="infinite-scroll-loading text-center py-2">
                <div class="spinner-border spinner-border-sm text-primary"></div>
                <small class="text-muted ms-2">Loading more...</small>
              </div>
            </div>

            <!-- Grid Results (for DashboardComponent) -->
            <div v-else class="row g-3">
              <div
                v-for="game in displayedResults"
                :key="game.id"
                class="col-md-6 col-lg-3"
              >
                <GameCard
                  :game="game"
                  @click="selectGame(game)"
                  @add-to-library="(game) => $emit('add-to-library', game)"
                />
              </div>
            </div>

            <!-- Pagination Controls -->
            <div v-if="showLoadMore" class="pagination-controls mt-3">
              <!-- Page-based Pagination -->
              <div v-if="paginationMode === 'pages'" class="d-flex justify-content-between align-items-center">
                <div class="d-flex gap-2">
                  <button
                    @click="goToPrevPage"
                    :disabled="!canGoPrev"
                    class="btn btn-outline-primary"
                  >
                    <i class="fas fa-chevron-left me-1"></i>
                    Previous
                  </button>
                  <button
                    @click="goToNextPage"
                    :disabled="!canGoNext || gamesStore.searchLoading"
                    class="btn btn-primary"
                  >
                    <span v-if="gamesStore.searchLoading" class="spinner-border spinner-border-sm me-2"></span>
                    <span v-else>Next</span>
                    <i v-if="!gamesStore.searchLoading" class="fas fa-chevron-right ms-1"></i>
                  </button>
                </div>
                <div class="text-muted">
                  Page {{ currentPage }} of {{ totalPages }}
                  ({{ gamesStore.searchPagination.totalCount }} total results)
                </div>
              </div>

              <!-- Load More Button (original behavior) -->
              <div v-else-if="paginationMode === 'load-more' && gamesStore.canLoadMore" class="text-center">
                <button
                  @click="loadMoreResults"
                  :disabled="gamesStore.searchLoading"
                  class="btn btn-primary"
                >
                  <span v-if="gamesStore.searchLoading" class="spinner-border spinner-border-sm me-2"></span>
                  <i v-else class="fas fa-plus me-2"></i>
                  {{ gamesStore.searchLoading ? 'Loading...' : 'Load More Results' }}
                </button>
              </div>

              <!-- Infinite Scroll (no controls needed, handled by scroll event) -->
            </div>
          </div>

          <!-- No Search Results -->
          <div v-else-if="showResults && hasSearched && !gamesStore.searchLoading && !hasResults" class="text-center py-4">
            <i class="fas fa-search text-muted mb-3" style="font-size: 2rem;"></i>
            <p class="text-muted">No games found for "{{ searchQuery }}". Try a different search term.</p>
          </div>
        </div>
      </div>

      <!-- Inline Search (no card wrapper) -->
      <div v-else>
        <div class="mb-3" v-if="showTitle">
          <label class="form-label fw-bold">{{ title }}</label>
        </div>
        <div class="position-relative">
          <input
            v-model="searchQuery"
            @input="handleSearchInput"
            type="text"
            class="form-control pe-4"
            :placeholder="placeholder"
          >
          <!-- Embedded clear button for inline version -->
          <button
            v-if="searchQuery.trim() || hasResults"
            @click="clearSearch"
            class="btn-clear-embedded-inline"
            type="button"
            title="Clear search"
          >
            <i class="fas fa-times"></i>
          </button>
          <!-- Loading indicator for inline version -->
          <div
            v-if="gamesStore.searchLoading"
            class="search-loading-indicator-inline"
          >
            <span class="spinner-border spinner-border-sm text-primary"></span>
          </div>
        </div>

        <!-- Inline Results -->
        <div v-if="showResults && hasResults" class="mt-3">
          <h6 class="fw-bold mb-2">{{ resultsTitle || 'Search Results' }}</h6>
          <div ref="scrollContainer" class="list-group" :style="{ maxHeight: maxHeight, overflowY: 'auto' }">
            <button
              v-for="game in displayedResults"
              :key="game.id"
              @click="selectGame(game)"
              :disabled="isGameDisabled(game)"
              class="list-group-item list-group-item-action d-flex align-items-center"
              :class="{ 'disabled': isGameDisabled(game) }"
            >
              <img
                :src="getImageUrl(game.primaryImageUrl, 'gameIcon')"
                :alt="game.name"
                class="rounded me-3"
                style="width: 40px; height: 40px; object-fit: cover;"
                @error="(e) => handleImageError(e, 'gameIcon')"
              >
              <div>
                <h6 class="mb-0">{{ game.name }}</h6>
                <small class="text-muted">{{ game.primaryGenre || 'Unknown Genre' }}</small>
              </div>
              <i v-if="isGameSelected && isGameSelected(game)" class="fas fa-check text-success ms-auto"></i>
            </button>

            <!-- Infinite scroll loading indicator for inline mode -->
            <div v-if="paginationMode === 'infinite-scroll' && isLoadingMore" class="infinite-scroll-loading text-center py-2">
              <div class="spinner-border spinner-border-sm text-primary"></div>
              <small class="text-muted ms-2">Loading more...</small>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted, onUnmounted, nextTick, defineProps, defineEmits, defineExpose } from 'vue'
import { useGamesStore } from '@/stores/gamesStore'
import { useImageFallback } from '@/composables/useImageFallback'
import GameCard from '@/components/GameCard.vue'

// Props
const props = defineProps({
  // Display options
  showCard: {
    type: Boolean,
    default: true
  },
  showTitle: {
    type: Boolean,
    default: true
  },
  showResults: {
    type: Boolean,
    default: true
  },
  showLoadMore: {
    type: Boolean,
    default: true
  },

  // Content customization
  title: {
    type: String,
    default: 'Search Games'
  },
  placeholder: {
    type: String,
    default: 'Search for games by title, genre, or developer...'
  },
  resultsTitle: {
    type: String,
    default: ''
  },

  // Results display
  resultsMode: {
    type: String,
    default: 'grid', // 'grid' or 'compact'
    validator: (value) => ['grid', 'compact'].includes(value)
  },
  maxResults: {
    type: Number,
    default: null // No limit by default
  },
  maxHeight: {
    type: String,
    default: '400px'
  },

  // Behavior
  autoSearch: {
    type: Boolean,
    default: true // Auto search on input change
  },
  debounceMs: {
    type: Number,
    default: 300
  },

  // Pagination mode
  paginationMode: {
    type: String,
    default: 'load-more', // 'load-more', 'pages', 'infinite-scroll'
    validator: (value) => ['load-more', 'pages', 'infinite-scroll'].includes(value)
  },
  gamesPerPage: {
    type: Number,
    default: 8 // Games to show per page in pagination mode
  },

  // Selection callbacks
  isGameDisabled: {
    type: Function,
    default: () => false
  },
  isGameSelected: {
    type: Function,
    default: null
  }
})

// Emits
const emit = defineEmits(['search', 'select-game', 'add-to-library'])

// Composables
const gamesStore = useGamesStore()
const { handleImageError, getImageUrl } = useImageFallback()

// State
const searchQuery = ref('')
const hasSearched = ref(false)
const currentPage = ref(1)
const scrollContainer = ref(null)
const isLoadingMore = ref(false)

// Computed
const hasResults = computed(() => gamesStore.allSearchResults.length > 0)

const displayedResults = computed(() => {
  let results = []

  if (props.paginationMode === 'pages') {
    // For page-based pagination, show only current page from all loaded results
    const startIndex = (currentPage.value - 1) * props.gamesPerPage
    const endIndex = startIndex + props.gamesPerPage
    results = gamesStore.allSearchResults.slice(startIndex, endIndex)
  } else if (props.paginationMode === 'infinite-scroll') {
    // For infinite scroll, show all loaded results (accumulative)
    results = gamesStore.allSearchResults
  } else {
    // For load-more, show current displayed results from store
    results = gamesStore.searchResults
  }

  return props.maxResults ? results.slice(0, props.maxResults) : results
})

const totalPages = computed(() => {
  if (props.paginationMode !== 'pages') return 1
  return Math.ceil(gamesStore.searchPagination.totalCount / props.gamesPerPage)
})

const canGoNext = computed(() => {
  if (props.paginationMode === 'pages') {
    return currentPage.value < totalPages.value
  }
  return gamesStore.canLoadMore
})

const canGoPrev = computed(() => {
  return props.paginationMode === 'pages' && currentPage.value > 1
})

// Methods
const performSearch = async () => {
  if (!searchQuery.value.trim()) return

  try {
    hasSearched.value = true
    currentPage.value = 1
    await gamesStore.searchGames(searchQuery.value)

    // For pages mode, ensure we have enough data for proper pagination
    if (props.paginationMode === 'pages') {
      // Load additional pages if needed to support navigation
      while (gamesStore.allSearchResults.length < props.gamesPerPage * 2 && gamesStore.searchPagination.hasNextPage) {
        await loadMoreResults()
      }
    }

    emit('search', { query: searchQuery.value, results: displayedResults.value })
  } catch (err) {
    console.error('Search error:', err)
  }
}

const loadMoreResults = async () => {
  try {
    if (isLoadingMore.value) return
    isLoadingMore.value = true
    await gamesStore.loadMoreSearchResults()
  } catch (err) {
    console.error('Load more error:', err)
  } finally {
    isLoadingMore.value = false
  }
}

const goToNextPage = async () => {
  if (props.paginationMode === 'pages') {
    if (currentPage.value < totalPages.value) {
      const nextPage = currentPage.value + 1
      // Check if we need to load more data from API before changing page
      const requiredData = nextPage * props.gamesPerPage
      if (gamesStore.allSearchResults.length < requiredData && gamesStore.searchPagination.hasNextPage) {
        await loadMoreResults()
      }
      currentPage.value = nextPage
    }
  } else {
    await loadMoreResults()
  }
}

const goToPrevPage = () => {
  if (props.paginationMode === 'pages' && currentPage.value > 1) {
    currentPage.value--
  }
}

const goToPage = async (page) => {
  if (props.paginationMode === 'pages' && page >= 1 && page <= totalPages.value) {
    currentPage.value = page
    // Check if we need to load more data from API
    const requiredData = page * props.gamesPerPage
    if (gamesStore.allSearchResults.length < requiredData && gamesStore.searchPagination.hasNextPage) {
      await loadMoreResults()
    }
  }
}

const selectGame = (game) => {
  emit('select-game', game)
}

const handleSearchInput = () => {
  if (props.autoSearch && searchQuery.value.trim()) {
    clearTimeout(searchTimeout)
    searchTimeout = setTimeout(performSearch, props.debounceMs)
  } else if (!searchQuery.value.trim()) {
    clearSearchResults()
  }
}

const clearSearchResults = () => {
  gamesStore.clearSearchResults()
  hasSearched.value = false
  currentPage.value = 1
}

const clearSearch = () => {
  searchQuery.value = ''
  clearSearchResults()
}

// Infinite scroll functionality
const handleScroll = async () => {
  if (props.paginationMode !== 'infinite-scroll' || isLoadingMore.value || !gamesStore.canLoadMore) return

  const container = scrollContainer.value
  if (!container) return

  const { scrollTop, scrollHeight, clientHeight } = container
  const threshold = 50 // Load more when 50px from bottom

  if (scrollTop + clientHeight >= scrollHeight - threshold) {
    await loadMoreResults()
  }
}

const setupInfiniteScroll = () => {
  if (props.paginationMode === 'infinite-scroll') {
    nextTick(() => {
      const container = scrollContainer.value
      if (container) {
        container.addEventListener('scroll', handleScroll, { passive: true })
      }
    })
  }
}

const cleanupInfiniteScroll = () => {
  const container = scrollContainer.value
  if (container) {
    container.removeEventListener('scroll', handleScroll)
  }
}

// Watchers
let searchTimeout
watch(searchQuery, (newValue) => {
  // Clear results if search query is empty
  if (!newValue.trim()) {
    clearSearchResults()
  } else {
    // Always auto-search since there's no manual search button
    handleSearchInput()
  }
})

// Lifecycle
onMounted(() => {
  setupInfiniteScroll()
})

onUnmounted(() => {
  cleanupInfiniteScroll()
  // Clear search results when component is unmounted (page navigation)
  clearSearchResults()
})

watch(() => props.paginationMode, () => {
  cleanupInfiniteScroll()
  setupInfiniteScroll()
})

// Setup infinite scroll when results become available
watch(() => displayedResults.value.length, () => {
  if (props.paginationMode === 'infinite-scroll' && displayedResults.value.length > 0) {
    // Re-setup infinite scroll after results are rendered
    nextTick(() => {
      setupInfiniteScroll()
    })
  }
}, { flush: 'post' })

// Expose methods for parent components
defineExpose({
  performSearch,
  clearSearchResults,
  clearSearch,
  searchQuery,
  goToNextPage,
  goToPrevPage,
  goToPage
})
</script>

<style scoped>
.game-search-component .card {
  border-radius: 15px;
}

.game-search-component .input-group-lg .form-control {
  font-size: 1rem;
}

.game-search-component .list-group-item {
  transition: all 0.2s ease;
}

.game-search-component .list-group-item:hover:not(.disabled) {
  background-color: #f8f9fa;
}

.game-search-component .list-group-item.disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.game-search-component .pagination-controls {
  border-top: 1px solid #dee2e6;
  padding-top: 1rem;
}

.game-search-component .infinite-scroll-loading {
  border-top: 1px solid #f8f9fa;
  background-color: #fafafa;
}

/* Responsive card body padding */
@media (max-width: 576px) {
  .game-search-component .card-body {
    padding: 1.5rem !important;
  }
}

@media (max-width: 480px) {
  .game-search-component .card-body {
    padding: 1rem !important;
  }
}

@media (max-width: 360px) {
  .game-search-component .card-body {
    padding: 0.75rem !important;
  }
}

/* Embedded clear button styling */
.game-search-component .btn-clear-embedded,
.game-search-component .btn-clear-embedded-inline {
  position: absolute;
  top: 50%;
  right: 0.75rem;
  transform: translateY(-50%);
  background: none;
  border: none;
  color: #6c757d;
  padding: 0.25rem;
  border-radius: 50%;
  width: 1.5rem;
  height: 1.5rem;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.2s ease;
  z-index: 10;
}

.game-search-component .btn-clear-embedded:hover,
.game-search-component .btn-clear-embedded-inline:hover {
  color: #dc3545;
  background-color: rgba(220, 53, 69, 0.1);
  transform: translateY(-50%) scale(1.1);
}

.game-search-component .btn-clear-embedded:focus,
.game-search-component .btn-clear-embedded-inline:focus {
  outline: none;
  box-shadow: 0 0 0 2px rgba(220, 53, 69, 0.2);
}

/* Adjust for card version (larger input) */
.game-search-component .btn-clear-embedded {
  right: 1rem;
  width: 1.75rem;
  height: 1.75rem;
}

.game-search-component .btn-clear-embedded i,
.game-search-component .btn-clear-embedded-inline i {
  font-size: 0.75rem;
}

/* Responsive adjustments for embedded clear button */
@media (max-width: 576px) {
  .game-search-component .btn-clear-embedded,
  .game-search-component .btn-clear-embedded-inline {
    right: 0.5rem;
    width: 1.25rem;
    height: 1.25rem;
  }
  
  .game-search-component .btn-clear-embedded i,
  .game-search-component .btn-clear-embedded-inline i {
    font-size: 0.7rem;
  }
}

/* Loading indicator styling */
.game-search-component .search-loading-indicator {
  position: absolute;
  top: 50%;
  right: 2.5rem;
  transform: translateY(-50%);
  z-index: 15;
}

.game-search-component .search-loading-indicator-inline {
  position: absolute;
  top: 50%;
  right: 2rem;
  transform: translateY(-50%);
  z-index: 15;
}

/* Adjust clear button position when loading */
.game-search-component .btn-clear-embedded {
  z-index: 20;
}

.game-search-component .btn-clear-embedded-inline {
  z-index: 20;
}

/* Responsive adjustments for loading indicator */
@media (max-width: 576px) {
  .game-search-component .search-loading-indicator {
    right: 2rem;
  }
  
  .game-search-component .search-loading-indicator-inline {
    right: 1.5rem;
  }
}

/* Results container improvements */
.game-search-component .list-group {
  border-radius: 0.375rem;
}

.game-search-component .list-group .list-group-item:first-child {
  border-top-left-radius: 0.375rem;
  border-top-right-radius: 0.375rem;
}

.game-search-component .list-group .list-group-item:last-child {
  border-bottom-left-radius: 0.375rem;
  border-bottom-right-radius: 0.375rem;
}
</style>