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
  if (width < 640) {
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
/* Main Container */
.game-carousel {
  width: 100%;
}

/* Loading State */
.loading-state {
  display: flex;
  justify-content: center;
  align-items: center;
  padding: 64px 0; /* py-16 = 4rem = 64px */
}

.spinner {
  width: 48px;
  height: 48px;
  border: 2px solid #e5e7eb;
  border-top-color: #333;
  border-radius: 50%;
  animation: spin 1s linear infinite;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

/* Error State */
.error-state {
  text-align: center;
  padding: 64px 0;
}

.retry-btn {
  margin-top: 16px; /* mt-4 */
  padding: 8px 16px; /* py-2 px-4 */
  background: #333;
  color: white;
  border-radius: 4px;
  transition: opacity 0.2s;
}

.retry-btn:hover {
  opacity: 0.8;
}

/* Empty State */
.empty-state {
  text-align: center;
  padding: 64px 0;
}

/* Carousel Content */
.carousel-content {
  position: relative;
  display: flex;
  justify-content: center;
}

/* Games Container - holds the transition */
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

/* Responsive Heights - 4pt grid (multiples of 4px) */
@media (max-width: 640px) {
  .games-wrapper {
    height: calc(63vw + 48px); /* Image height + title (3rem = 48px) */
  }
}

@media (min-width: 640px) and (max-width: 768px) {
  .games-wrapper {
    height: calc(41vw + 48px);
  }
}

@media (min-width: 768px) and (max-width: 1024px) {
  .games-wrapper {
    height: calc(30vw + 48px);
  }
}

@media (min-width: 1024px) {
  .games-wrapper {
    height: 268px; /* 220px image + 48px title */
  }
}

/* Grid Wrapper - the element that transitions */
.games-grid-wrapper {
  width: 100%;
  height: 100%;
}

/* Games Grid */
.games-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 24px; /* gap-6 = 24px = 6×4pt */
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

/* Navigation Arrows Container */
.nav-arrows {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  height: 100%;
  pointer-events: none;
  z-index: 10;
}

/* Arrow Buttons */
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

/* Arrow Positioning - Vertically centered on images (4pt grid) */
.arrow-left {
  left: -40px; /* -10×4pt */
}

.arrow-right {
  right: -40px;
}

/* Responsive Arrow Positioning */
@media (max-width: 640px) {
  .arrow {
    top: 120px; /* 30×4pt */
  }
  .arrow-left {
    left: -32px; /* -8×4pt */
  }
  .arrow-right {
    right: -32px;
  }
}

@media (min-width: 640px) and (max-width: 768px) {
  .arrow {
    top: 100px; /* 25×4pt */
  }
}

@media (min-width: 768px) and (max-width: 1024px) {
  .arrow {
    top: 90px; /* 22.5×4pt ≈ 23×4pt = 92px, using 90px */
  }
}

@media (min-width: 1024px) {
  .arrow {
    top: 110px; /* 27.5×4pt ≈ 28×4pt = 112px, using 110px */
  }
}

/* Slide Transitions */
/* The wrapper is what gets transitioned */

/* Slide Left - Next page (right arrow clicked) */
.slide-left-enter-active {
  z-index: 2;
  transition: transform 0.6s ease-out;
}

.slide-left-leave-active {
  z-index: 1;
  transition: transform 0.6s ease-out;
}

.slide-left-enter-from {
  transform: translateX(100%);
}

.slide-left-enter-to {
  transform: translateX(0);
}

.slide-left-leave-from {
  transform: translateX(0);
}

.slide-left-leave-to {
  transform: translateX(-100%);
}

/* Slide Right - Previous page (left arrow clicked) */
.slide-right-enter-active {
  z-index: 2;
  transition: transform 0.6s ease-out;
}

.slide-right-leave-active {
  z-index: 1;
  transition: transform 0.6s ease-out;
}

.slide-right-enter-from {
  transform: translateX(-100%);
}

.slide-right-enter-to {
  transform: translateX(0);
}

.slide-right-leave-from {
  transform: translateX(0);
}

.slide-right-leave-to {
  transform: translateX(100%);
}
</style>
