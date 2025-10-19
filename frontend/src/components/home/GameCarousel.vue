<script setup>
import { ref, computed, onMounted, onBeforeUnmount } from 'vue'
import { useGamesStore } from '@/stores/gamesStore'
import GameCard from './GameCard.vue'

const gamesStore = useGamesStore()

// Props
defineProps({
  title: {
    type: String,
    default: 'Popular Games'
  },
  showBorder: {
    type: Boolean,
    default: true
  },
  imageSize: {
    type: String,
    default: 'default'
  }
})

// Emits
const emit = defineEmits(['game-click'])

// State
const loading = ref(false)
const error = ref(null)
const popularGames = ref([])
const currentPage = ref(0)
const gamesPerPage = ref(6)
const slideDirection = ref('right')

// Computed
const totalPages = computed(() => {
  return Math.ceil(popularGames.value.length / gamesPerPage.value)
})

const displayedGames = computed(() => {
  const start = currentPage.value * gamesPerPage.value
  const end = start + gamesPerPage.value
  return popularGames.value.slice(start, end)
})

const canScrollLeft = computed(() => {
  return currentPage.value > 0
})

const canScrollRight = computed(() => {
  return currentPage.value < totalPages.value - 1
})

const showArrows = computed(() => {
  return popularGames.value.length > gamesPerPage.value
})

const transitionName = computed(() => {
  return slideDirection.value === 'right' ? 'slide-left' : 'slide-right'
})

// Methods
const fetchGames = async () => {
  try {
    loading.value = true
    error.value = null
    const games = await gamesStore.fetchPopularGames(24, false)
    popularGames.value = games
  } catch (err) {
    console.error('Error fetching popular games:', err)
    error.value = 'Failed to load popular games'
  } finally {
    loading.value = false
  }
}

const scrollLeft = () => {
  if (canScrollLeft.value) {
    slideDirection.value = 'left'
    currentPage.value--
  }
}

const scrollRight = () => {
  if (canScrollRight.value) {
    slideDirection.value = 'right'
    currentPage.value++
  }
}

const handleGameClick = (game) => {
  emit('game-click', game)
}

// Update games per page based on screen size
const updateGamesPerPage = () => {
  const width = window.innerWidth
  if (width < 560) {
    gamesPerPage.value = 2
  } else if (width < 768) {
    gamesPerPage.value = 3
  } else if (width < 1024) {
    gamesPerPage.value = 4
  } else {
    gamesPerPage.value = 6
  }
  currentPage.value = 0
}

// Lifecycle
onMounted(async () => {
  await fetchGames()
  updateGamesPerPage()
  window.addEventListener('resize', updateGamesPerPage)
})

onBeforeUnmount(() => {
  window.removeEventListener('resize', updateGamesPerPage)
})
</script>

<template>
  <div class="game-carousel">
    <!-- Section Title -->
    <h3
      class="font-newsreader text-2xl font-bold text-cod-gray mb-4"
      :class="{ 'border-b border-gray-300': showBorder }"
    >
      {{ title }}
    </h3>

    <!-- Loading State -->
    <div v-if="loading && popularGames.length === 0" class="loading-state">
      <div class="spinner"></div>
    </div>

    <!-- Error State -->
    <div v-else-if="error" class="error-state">
      <p class="text-red-500">{{ error }}</p>
      <button @click="fetchGames" class="retry-btn">Retry</button>
    </div>

    <!-- Carousel Content -->
    <div v-else-if="popularGames.length > 0" class="carousel-content">
      <!-- Games Container -->
      <div class="games-container">
        <div class="games-wrapper">
          <Transition :name="transitionName">
            <div :key="currentPage" class="games-grid-wrapper">
              <div class="games-grid">
                <GameCard
                  v-for="game in displayedGames"
                  :key="game.id"
                  :game="game"
                  :image-size="imageSize"
                  @click="handleGameClick"
                />
              </div>
            </div>
          </Transition>
        </div>
      </div>

      <!-- Navigation Arrows -->
      <div v-if="showArrows" class="nav-arrows">
        <button
          @click="scrollLeft"
          :disabled="!canScrollLeft"
          :class="['arrow', 'arrow-left', { disabled: !canScrollLeft }]"
          aria-label="Previous games"
        >
          <svg class="w-8 h-8" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
          </svg>
        </button>

        <button
          @click="scrollRight"
          :disabled="!canScrollRight"
          :class="['arrow', 'arrow-right', { disabled: !canScrollRight }]"
          aria-label="Next games"
        >
          <svg class="w-8 h-8" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
          </svg>
        </button>
      </div>
    </div>

    <!-- Empty State -->
    <div v-else class="empty-state">
      <p class="text-gray-500">No popular games available at the moment.</p>
    </div>
  </div>
</template>

<style scoped>
/* Carousel-specific styles only - shared utilities are in base.css @layer components */

/* Carousel Structure */
.carousel-content {
  position: relative;
  display: flex;
  justify-content: center;
}

.games-container {
  position: relative;
  width: fit-content;
  max-width: 100%;
}

.games-wrapper {
  position: relative;
  overflow: hidden;
  isolation: isolate;
  display: grid;
  grid-template-columns: 1fr;
  grid-template-rows: 1fr;
}

.games-wrapper > * {
  grid-column: 1;
  grid-row: 1;
}

/* Responsive Heights */
/* Use min-height to prevent cutoff while allowing growth for longer titles */
@media (max-width: 640px) {
  .games-wrapper {
    min-height: calc(63vw + 60px);
  }
}

@media (min-width: 640px) and (max-width: 768px) {
  .games-wrapper {
    min-height: calc(41vw + 60px);
  }
}

@media (min-width: 768px) and (max-width: 1024px) {
  .games-wrapper {
    min-height: calc(30vw + 60px);
  }
}

@media (min-width: 1024px) {
  .games-wrapper {
    min-height: calc(18vw + 60px);
  }
}

.games-grid-wrapper {
  width: 100%;
  height: 100%;
}

/* Games Grid */
.games-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 24px;
  width: 100%;
}

@media (min-width: 640px) {
  .games-grid {
    grid-template-columns: repeat(3, 1fr);
  }
}

@media (min-width: 768px) {
  .games-grid {
    grid-template-columns: repeat(4, 1fr);
  }
}

@media (min-width: 1024px) {
  .games-grid {
    grid-template-columns: repeat(6, 1fr);
  }
}

/* Navigation Arrows */
.nav-arrows {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  height: 100%;
  pointer-events: none;
  z-index: 10;
}

.arrow {
  position: absolute;
  display: flex;
  align-items: center;
  justify-content: center;
  pointer-events: all;
  color: #333;
  background: transparent;
  border: none;
  transition: all 0.2s;
  cursor: pointer;
}

.arrow:hover:not(.disabled) {
  transform: scale(1.25);
}

.arrow.disabled {
  opacity: 0.3;
  cursor: not-allowed;
}

.arrow-left {
  left: -40px;
}

.arrow-right {
  right: -40px;
}

@media (max-width: 640px) {
  .arrow {
    top: 120px;
  }
  .arrow-left {
    left: -32px;
  }
  .arrow-right {
    right: -32px;
  }
}

@media (min-width: 640px) and (max-width: 768px) {
  .arrow {
    top: 100px;
  }
}

@media (min-width: 768px) and (max-width: 1024px) {
  .arrow {
    top: 90px;
  }
}

@media (min-width: 1024px) {
  .arrow {
    top: 110px;
  }
}

/* Carousel Slide Transitions */
.slide-left-enter-active,
.slide-left-leave-active,
.slide-right-enter-active,
.slide-right-leave-active {
  transition: transform 0.6s ease-out;
}

.slide-left-enter-active,
.slide-right-enter-active {
  z-index: 2;
}

.slide-left-leave-active,
.slide-right-leave-active {
  z-index: 1;
}

.slide-left-enter-from {
  transform: translateX(100%);
}

.slide-left-enter-to,
.slide-left-leave-from,
.slide-right-enter-to,
.slide-right-leave-from {
  transform: translateX(0);
}

.slide-left-leave-to {
  transform: translateX(-100%);
}

.slide-right-enter-from {
  transform: translateX(-100%);
}

.slide-right-leave-to {
  transform: translateX(100%);
}
</style>
