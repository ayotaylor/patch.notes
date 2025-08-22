<template>
  <div class="game-section">
    <!-- Section Header -->
    <div class="section-header d-flex justify-content-between align-items-center mb-3">
      <h3 class="section-title h5 mb-0 fw-bold d-flex align-items-center">
        <i v-if="icon" :class="['me-2', icon]"></i>
        {{ title }}
      </h3>
      
      <div class="section-actions d-flex gap-2">
        <button
          v-if="showRefresh"
          @click="$emit('refresh')"
          :disabled="loading"
          class="btn btn-sm btn-outline-secondary"
          :title="`Refresh ${title}`"
        >
          <i class="fas fa-sync-alt" :class="{ 'fa-spin': loading }"></i>
        </button>
        
        <button
          v-if="showViewAll"
          @click="$emit('view-all')"
          class="btn btn-sm btn-outline-primary"
        >
          View All
          <i class="fas fa-arrow-right ms-1"></i>
        </button>
      </div>
    </div>

    <!-- Content Area -->
    <div class="section-content">
      <!-- Loading State -->
      <div v-if="loading && games.length === 0" class="loading-state text-center py-5">
        <div class="spinner-border text-primary mb-3"></div>
        <p class="text-muted">{{ loadingMessage || `Loading ${title.toLowerCase()}...` }}</p>
      </div>

      <!-- Games Row -->
      <div v-else-if="games.length > 0" class="games-row">
        <div ref="scrollContainer" class="games-scroll-container">
          <div class="games-row-content d-flex gap-3">
            <GameRowCard
              v-for="game in displayedGames"
              :key="game.id"
              :game="game"
              @click="handleGameClick"
              @view-likes="handleViewLikes"
              @view-lists="handleViewLists"
              @view-reviews="handleViewReviews"
            />
          </div>
        </div>
        
        <!-- Navigation arrows for horizontal scroll -->
        <div v-if="showScrollArrows && hasScrollableContent" class="scroll-navigation">
          <button
            @click="scrollLeft"
            class="scroll-btn scroll-left"
            :disabled="!canScrollLeft"
          >
            <i class="fas fa-chevron-left"></i>
          </button>
          <button
            @click="scrollRight"
            class="scroll-btn scroll-right"
            :disabled="!canScrollRight"
          >
            <i class="fas fa-chevron-right"></i>
          </button>
        </div>
      </div>

      <!-- Empty State -->
      <div v-else class="empty-state text-center py-5">
        <i v-if="emptyIcon" :class="['text-muted mb-3', emptyIcon]" style="font-size: 2rem;"></i>
        <p class="text-muted">{{ emptyMessage || `No ${title.toLowerCase()} available right now.` }}</p>
        <p v-if="emptySubMessage" class="text-muted small">{{ emptySubMessage }}</p>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, nextTick, onMounted, onBeforeUnmount, defineProps, defineEmits } from 'vue'
import GameRowCard from './GameRowCard.vue'

// Props
const props = defineProps({
  title: {
    type: String,
    required: true
  },
  icon: {
    type: String,
    default: ''
  },
  games: {
    type: Array,
    default: () => []
  },
  loading: {
    type: Boolean,
    default: false
  },
  loadingMessage: {
    type: String,
    default: ''
  },
  emptyMessage: {
    type: String,
    default: ''
  },
  emptySubMessage: {
    type: String,
    default: ''
  },
  emptyIcon: {
    type: String,
    default: 'fas fa-gamepad'
  },
  showRefresh: {
    type: Boolean,
    default: true
  },
  showViewAll: {
    type: Boolean,
    default: true
  },
  showScrollArrows: {
    type: Boolean,
    default: true
  },
  maxGamesToShow: {
    type: Number,
    default: 10
  }
})


// Reactive refs
const scrollContainer = ref(null)
const canScrollLeft = ref(false)
const canScrollRight = ref(false)

// Computed properties
const displayedGames = computed(() => {
  return props.games.slice(0, props.maxGamesToShow)
})

const hasScrollableContent = computed(() => {
  return props.games.length > 5 // Show arrows if more than 5 games
})

// Emits
const emit = defineEmits(['refresh', 'view-all', 'game-click', 'view-likes', 'view-lists', 'view-reviews'])

// Methods
const handleGameClick = (game) => {
  emit('game-click', game)
}

const handleViewLikes = (game) => {
  emit('view-likes', game)
}

const handleViewLists = (game) => {
  emit('view-lists', game)
}

const handleViewReviews = (game) => {
  emit('view-reviews', game)
}

const scrollLeft = () => {
  const container = scrollContainer.value
  if (container) {
    container.scrollBy({ left: -200, behavior: 'smooth' })
    nextTick(updateScrollButtons)
  }
}

const scrollRight = () => {
  const container = scrollContainer.value
  if (container) {
    container.scrollBy({ left: 200, behavior: 'smooth' })
    nextTick(updateScrollButtons)
  }
}

const updateScrollButtons = () => {
  const container = scrollContainer.value
  if (container) {
    canScrollLeft.value = container.scrollLeft > 0
    canScrollRight.value = container.scrollLeft < (container.scrollWidth - container.clientWidth)
  }
}

const handleScroll = () => {
  updateScrollButtons()
}

// Lifecycle
onMounted(() => {
  nextTick(() => {
    updateScrollButtons()
    const container = scrollContainer.value
    if (container) {
      container.addEventListener('scroll', handleScroll)
    }
  })
})

onBeforeUnmount(() => {
  const container = scrollContainer.value
  if (container) {
    container.removeEventListener('scroll', handleScroll)
  }
})
</script>

<style scoped>
.game-section {
  background: white;
  border-radius: 15px;
  padding: 1.5rem;
  box-shadow: 0 2px 10px rgba(0, 0, 0, 0.05);
  border: 1px solid #e9ecef;
}

.section-title {
  color: #333;
}

.games-row {
  position: relative;
}

.games-scroll-container {
  overflow-x: auto;
  scrollbar-width: none; /* Firefox */
  -ms-overflow-style: none; /* IE and Edge */
  padding-bottom: 0.5rem;
}

.games-scroll-container::-webkit-scrollbar {
  display: none; /* Chrome, Safari, Opera */
}

.games-row-content {
  min-width: max-content;
  padding: 0.5rem 0;
}

.scroll-navigation {
  position: absolute;
  top: 50%;
  left: 0;
  right: 0;
  transform: translateY(-50%);
  pointer-events: none;
  z-index: 2;
}

.scroll-btn {
  position: absolute;
  top: 0;
  background: white;
  border: 1px solid #dee2e6;
  border-radius: 50%;
  width: 36px;
  height: 36px;
  display: flex;
  align-items: center;
  justify-content: center;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
  pointer-events: all;
  transition: all 0.2s ease;
  color: #6c757d;
}

.scroll-btn:hover:not(:disabled) {
  background: #f8f9fa;
  color: #495057;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
}

.scroll-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.scroll-left {
  left: -18px;
}

.scroll-right {
  right: -18px;
}

.loading-state,
.empty-state {
  min-height: 200px;
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
}

/* Responsive adjustments */
@media (max-width: 768px) {
  .game-section {
    padding: 1rem;
    border-radius: 12px;
  }
  
  .section-header {
    flex-direction: column;
    align-items: flex-start;
    gap: 0.5rem;
  }
  
  .section-actions {
    align-self: flex-end;
  }
  
  .scroll-btn {
    width: 32px;
    height: 32px;
  }
  
  .scroll-left {
    left: -16px;
  }
  
  .scroll-right {
    right: -16px;
  }
}

@media (max-width: 576px) {
  .game-section {
    padding: 0.75rem;
  }
  
  .section-title {
    font-size: 1rem;
  }
  
  .scroll-navigation {
    display: none; /* Hide scroll arrows on mobile, rely on touch scroll */
  }
}
</style>