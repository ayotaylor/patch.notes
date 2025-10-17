<template>
  <div class="relative" :class="containerClass">
    <div ref="searchInputRef" class="flex items-stretch h-9 rounded-md overflow-hidden bg-gray-100">
      <div class="flex items-center justify-center pl-3 bg-gray-100">
        <svg class="w-6 h-7" viewBox="0 0 25 29" fill="none" xmlns="http://www.w3.org/2000/svg">
          <path
            d="M20.0868 23.5807L13.9618 17.4557C13.4757 17.8446 12.9166 18.1524 12.2847 18.3793C11.6527 18.6062 10.9803 18.7196 10.2673 18.7196C8.50112 18.7196 7.00633 18.1079 5.78295 16.8845C4.55957 15.6611 3.94788 14.1663 3.94788 12.4001C3.94788 10.6339 4.55957 9.13914 5.78295 7.91576C7.00633 6.69238 8.50112 6.08069 10.2673 6.08069C12.0335 6.08069 13.5283 6.69238 14.7517 7.91576C15.9751 9.13914 16.5868 10.6339 16.5868 12.4001C16.5868 13.1131 16.4733 13.7855 16.2465 14.4175C16.0196 15.0494 15.7118 15.6085 15.3229 16.0946L21.4479 22.2196L20.0868 23.5807ZM10.2673 16.7751C11.4826 16.7751 12.5156 16.3498 13.3663 15.4991C14.217 14.6484 14.6423 13.6154 14.6423 12.4001C14.6423 11.1849 14.217 10.1519 13.3663 9.30117C12.5156 8.45048 11.4826 8.02513 10.2673 8.02513C9.05204 8.02513 8.01906 8.45048 7.16836 9.30117C6.31767 10.1519 5.89232 11.1849 5.89232 12.4001C5.89232 13.6154 6.31767 14.6484 7.16836 15.4991C8.01906 16.3498 9.05204 16.7751 10.2673 16.7751Z"
            fill="#6B7280"
          />
        </svg>
      </div>
      <input
        v-model="searchQuery"
        type="text"
        :placeholder="placeholder"
        @focus="handleFocus"
        @blur="handleBlur"
        class="flex-1 px-2 bg-gray-100 text-sm font-tinos text-pale-sky outline-none"
        :class="{ 'opacity-75': gamesStore.searchLoading }"
      />
      <!-- Clear Button -->
      <button
        v-if="searchQuery.trim()"
        @click="clearSearch"
        class="flex items-center justify-center pr-3 bg-gray-100 text-gray-500 hover:text-gray-700 transition-colors"
        type="button"
        title="Clear search"
      >
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
        </svg>
      </button>
      <!-- Loading Spinner -->
      <div v-if="gamesStore.searchLoading" class="flex items-center justify-center pr-3 bg-gray-100">
        <svg class="animate-spin h-4 w-4 text-gray-500" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
          <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
        </svg>
      </div>
    </div>

    <!-- Search Results Dropdown -->
    <div
      v-if="showDropdown && searchQuery.trim() && (hasResults || gamesStore.searchLoading || (!hasResults && hasSearched))"
      ref="dropdownRef"
      @mousedown.prevent
      class="absolute top-full left-0 right-0 mt-1 bg-white border border-gray-300 rounded-md shadow-lg max-h-96 overflow-y-auto z-[100]"
    >
      <!-- Loading State -->
      <div v-if="gamesStore.searchLoading && !hasResults" class="flex items-center justify-center py-8">
        <svg class="animate-spin h-6 w-6 text-gray-500 mr-2" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
          <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
        </svg>
        <span class="text-sm text-gray-500 font-tinos">Searching...</span>
      </div>

      <!-- Results List -->
      <div v-else-if="hasResults">
        <button
          v-for="game in gamesStore.searchResults"
          :key="game.id"
          @click="selectGame(game)"
          class="w-full flex items-center gap-3 p-3 hover:bg-gray-100 transition-colors border-b border-gray-200 last:border-b-0 text-left cursor-pointer"
        >
          <img
            :src="getImageUrl(game.primaryImageUrl, 'gameIcon')"
            :alt="game.name"
            class="w-10 h-10 object-cover rounded"
            @error="(e) => handleImageError(e, 'gameIcon')"
          />
          <div class="flex-1 min-w-0">
            <h6 class="text-sm font-tinos font-semibold text-cod-gray truncate">{{ game.name }}</h6>
            <p class="text-xs font-tinos text-gray-500 truncate">{{ game.primaryGenre || 'Unknown Genre' }}</p>
          </div>
        </button>
      </div>

      <!-- Empty State -->
      <div v-else-if="hasSearched && !hasResults && !gamesStore.searchLoading" class="flex flex-col items-center justify-center py-8 px-4">
        <svg class="w-12 h-12 text-gray-400 mb-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
        </svg>
        <p class="text-sm text-gray-500 font-tinos text-center">No games found for "{{ searchQuery }}"</p>
        <p class="text-xs text-gray-400 font-tinos text-center mt-1">Try a different search term</p>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted, onUnmounted } from 'vue'
import { useRouter } from 'vue-router'
import { useGamesStore } from '@/stores/gamesStore'
import { useImageFallback } from '@/composables/useImageFallback'

// Props
// eslint-disable-next-line no-unused-vars
const props = defineProps({
  placeholder: {
    type: String,
    default: 'Search'
  },
  containerClass: {
    type: String,
    default: 'min-w-40 max-w-64'
  }
})

// Emits
const emit = defineEmits(['select-game', 'search'])

// Router and Store
const router = useRouter()
const gamesStore = useGamesStore()
const { handleImageError, getImageUrl } = useImageFallback()

// State
const searchQuery = ref('')
const showDropdown = ref(false)
const isFocused = ref(false)
const dropdownRef = ref(null)
const searchInputRef = ref(null)
let searchTimeout = null

// Computed
const hasResults = computed(() => gamesStore.searchResults.length > 0)
const hasSearched = computed(() => searchQuery.value.trim().length > 0)

// Methods
const performSearch = async () => {
  if (!searchQuery.value.trim()) {
    clearSearchResults()
    return
  }

  try {
    await gamesStore.searchGames(searchQuery.value)
    showDropdown.value = true
    emit('search', { query: searchQuery.value, results: gamesStore.searchResults })
  } catch (err) {
    console.error('Search error:', err)
  }
}

const handleSearchInput = () => {
  clearTimeout(searchTimeout)
  if (searchQuery.value.trim()) {
    searchTimeout = setTimeout(performSearch, 300)
  } else {
    clearSearchResults()
  }
}

const clearSearchResults = () => {
  gamesStore.clearSearchResults()
  showDropdown.value = false
  isFocused.value = false
}

const clearSearch = () => {
  searchQuery.value = ''
  clearSearchResults()
}

const selectGame = (game) => {
  emit('select-game', game)
  router.push(`/games/${game.id}`)
  showDropdown.value = false
  searchQuery.value = ''
  clearSearchResults()
}

const handleFocus = () => {
  isFocused.value = true
  // Don't auto-show dropdown on focus
  // It will show when user types and gets results
}

const handleBlur = () => {
  isFocused.value = false
  // Delay to allow click events on dropdown/clear button
  setTimeout(() => {
    if (!isFocused.value) {
      clearSearch()
    }
  }, 200)
}

const handleClickOutside = (event) => {
  if (
    dropdownRef.value &&
    !dropdownRef.value.contains(event.target) &&
    searchInputRef.value &&
    !searchInputRef.value.contains(event.target)
  ) {
    showDropdown.value = false
    isFocused.value = false
    clearSearch()
  }
}

// Watchers
watch(searchQuery, (newValue) => {
  handleSearchInput()
  // Hide dropdown if search query is empty
  if (!newValue.trim()) {
    showDropdown.value = false
  }
})

// Lifecycle
onMounted(() => {
  document.addEventListener('click', handleClickOutside)
})

onUnmounted(() => {
  document.removeEventListener('click', handleClickOutside)
  clearSearchResults()
})

// Expose methods for parent components
defineExpose({
  clearSearch
})
</script>
