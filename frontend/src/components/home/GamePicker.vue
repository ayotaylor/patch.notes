<template>
  <div class="game-picker">
    <!-- Search Input -->
    <div class="mb-4">
      <div class="relative">
        <div class="flex items-stretch h-10 rounded-md overflow-hidden bg-theme-bg-primary dark:bg-theme-bg-primary-dark border border-theme-border dark:border-theme-border-dark">
          <div class="flex items-center justify-center pl-3">
            <svg class="w-5 h-5 text-theme-text-secondary dark:text-theme-text-secondary-dark" viewBox="0 0 25 29" fill="currentColor" xmlns="http://www.w3.org/2000/svg">
              <path d="M20.0868 23.5807L13.9618 17.4557C13.4757 17.8446 12.9166 18.1524 12.2847 18.3793C11.6527 18.6062 10.9803 18.7196 10.2673 18.7196C8.50112 18.7196 7.00633 18.1079 5.78295 16.8845C4.55957 15.6611 3.94788 14.1663 3.94788 12.4001C3.94788 10.6339 4.55957 9.13914 5.78295 7.91576C7.00633 6.69238 8.50112 6.08069 10.2673 6.08069C12.0335 6.08069 13.5283 6.69238 14.7517 7.91576C15.9751 9.13914 16.5868 10.6339 16.5868 12.4001C16.5868 13.1131 16.4733 13.7855 16.2465 14.4175C16.0196 15.0494 15.7118 15.6085 15.3229 16.0946L21.4479 22.2196L20.0868 23.5807ZM10.2673 16.7751C11.4826 16.7751 12.5156 16.3498 13.3663 15.4991C14.217 14.6484 14.6423 13.6154 14.6423 12.4001C14.6423 11.1849 14.217 10.1519 13.3663 9.30117C12.5156 8.45048 11.4826 8.02513 10.2673 8.02513C9.05204 8.02513 8.01906 8.45048 7.16836 9.30117C6.31767 10.1519 5.89232 11.1849 5.89232 12.4001C5.89232 13.6154 6.31767 14.6484 7.16836 15.4991C8.01906 16.3498 9.05204 16.7751 10.2673 16.7751Z"/>
            </svg>
          </div>
          <input
            v-model="searchQuery"
            type="text"
            placeholder="Search for games..."
            @input="handleSearchInput"
            class="flex-1 px-3 bg-transparent text-theme-text-primary dark:text-theme-text-primary-dark font-tinos text-sm outline-none"
          />
          <!-- Clear Button -->
          <button
            v-if="searchQuery.trim()"
            @click="clearSearch"
            type="button"
            class="flex items-center justify-center pr-3 text-theme-text-secondary dark:text-theme-text-secondary-dark hover:text-theme-text-primary dark:hover:text-theme-text-primary-dark transition-colors"
            title="Clear search"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
          <!-- Loading Spinner -->
          <div v-if="gamesStore.searchLoading" class="flex items-center justify-center pr-3">
            <svg class="animate-spin h-4 w-4 text-theme-btn-primary dark:text-theme-btn-primary-dark" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
              <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
              <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
            </svg>
          </div>
        </div>
      </div>
    </div>

    <!-- Results Container -->
    <div class="min-h-[300px] max-h-[400px] overflow-y-auto bg-theme-bg-primary dark:bg-theme-bg-primary-dark border border-theme-border dark:border-theme-border-dark rounded-lg p-2">
      <!-- Loading State -->
      <div v-if="gamesStore.searchLoading && !hasResults" class="flex flex-col items-center justify-center py-16">
        <svg class="animate-spin h-8 w-8 text-theme-btn-primary dark:text-theme-btn-primary-dark mb-3" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
          <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
        </svg>
        <span class="font-tinos text-sm text-theme-text-secondary dark:text-theme-text-secondary-dark">Searching for games...</span>
      </div>

      <!-- Results List -->
      <div v-else-if="filteredResults.length > 0" class="space-y-2">
        <button
          v-for="game in filteredResults"
          :key="game.id"
          type="button"
          @click="selectGame(game)"
          class="w-full flex items-center gap-4 p-4 hover:bg-theme-bg-secondary dark:hover:bg-theme-bg-secondary-dark transition-colors duration-200 border border-theme-border dark:border-theme-border-dark rounded-lg text-left cursor-pointer"
        >
          <img
            :src="getGameImageUrl(game)"
            :alt="game.name"
            class="w-16 h-16 object-cover rounded-lg"
            @error="handleImageError"
          />
          <div class="flex-1 min-w-0">
            <h6 class="font-tinos text-base font-semibold text-theme-text-primary dark:text-theme-text-primary-dark truncate">{{ game.name }}</h6>
            <p class="font-tinos text-sm text-theme-text-secondary dark:text-theme-text-secondary-dark truncate">{{ game.primaryGenre || 'Unknown Genre' }}</p>
          </div>
          <svg class="w-6 h-6 text-theme-btn-primary dark:text-theme-btn-primary-dark flex-shrink-0" fill="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
            <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-2 15l-5-5 1.41-1.41L10 14.17l7.59-7.59L19 8l-9 9z"/>
          </svg>
        </button>
      </div>

      <!-- Empty State - No Results -->
      <div v-else-if="hasSearched && !gamesStore.searchLoading" class="flex flex-col items-center justify-center py-16 px-4">
        <svg class="w-16 h-16 text-theme-text-secondary dark:text-theme-text-secondary-dark mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
        </svg>
        <p class="font-tinos text-base text-theme-text-secondary dark:text-theme-text-secondary-dark text-center mb-2">No games found</p>
        <p class="font-tinos text-sm text-theme-text-secondary dark:text-theme-text-secondary-dark text-center">Try a different search term</p>
      </div>

      <!-- Empty State - No Search -->
      <div v-else class="flex flex-col items-center justify-center py-16 px-4">
        <svg class="w-16 h-16 text-theme-text-secondary dark:text-theme-text-secondary-dark mb-4" fill="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
          <path d="M15.5 14h-.79l-.28-.27C15.41 12.59 16 11.11 16 9.5 16 5.91 13.09 3 9.5 3S3 5.91 3 9.5 5.91 16 9.5 16c1.61 0 3.09-.59 4.23-1.57l.27.28v.79l5 4.99L20.49 19l-4.99-5zm-6 0C7.01 14 5 11.99 5 9.5S7.01 5 9.5 5 14 7.01 14 9.5 11.99 14 9.5 14z"/>
        </svg>
        <p class="font-tinos text-base text-theme-text-secondary dark:text-theme-text-secondary-dark text-center">Start typing to search for games</p>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import { useGamesStore } from '@/stores/gamesStore'

const props = defineProps({
  excludedGameIds: {
    type: Array,
    default: () => []
  }
})

const emit = defineEmits(['select-game'])

const gamesStore = useGamesStore()
const searchQuery = ref('')
let searchTimeout = null

// Computed
const hasResults = computed(() => gamesStore.searchResults.length > 0)
const hasSearched = computed(() => searchQuery.value.trim().length > 0)

const filteredResults = computed(() => {
  if (!gamesStore.searchResults) return []
  return gamesStore.searchResults.filter(game => !props.excludedGameIds.includes(game.id))
})

// Methods
const handleSearchInput = () => {
  // Debounce search
  if (searchTimeout) {
    clearTimeout(searchTimeout)
  }

  if (!searchQuery.value.trim()) {
    gamesStore.clearSearchResults()
    return
  }

  searchTimeout = setTimeout(async () => {
    try {
      await gamesStore.searchGames(searchQuery.value)
    } catch (err) {
      console.error('Search error:', err)
    }
  }, 300)
}

const selectGame = (game) => {
  emit('select-game', game)
  clearSearch()
}

const clearSearch = () => {
  searchQuery.value = ''
  gamesStore.clearSearchResults()
}

const getGameImageUrl = (game) => {
  return game.primaryImageUrl || game.coverImageUrl || 'https://via.placeholder.com/150x227?text=No+Image'
}

const handleImageError = (e) => {
  e.target.src = 'https://via.placeholder.com/150x227?text=No+Image'
}
</script>

<style scoped>
.game-picker {
  width: 100%;
}
</style>
