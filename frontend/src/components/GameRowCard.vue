<template>
  <div
    @click="$emit('click', game)"
    class="game-row-card d-flex flex-column align-items-center text-center cursor-pointer"
  >
    <!-- Game Image -->
    <div class="game-image-container position-relative mb-2">
      <img
        :src="gameImageUrl"
        :alt="game.name"
        class="game-image rounded"
        @error="(e) => handleImageError(e, 'gameSmall')"
      >

      <!-- Rating Badge -->
      <div v-if="game.averageRating > 0 || game.rating > 0" class="position-absolute top-0 start-0">
        <span class="badge bg-dark bg-opacity-75 small">
          <i class="fas fa-star text-warning me-1"></i>
          <span v-if="game.averageRating > 0">{{ game.averageRating.toFixed(1) }}</span>
          <span v-else>{{ game.rating.toFixed(1) }}</span>
        </span>
      </div>
    </div>

    <!-- Game Name -->
    <h6 class="game-name fw-semibold mb-2 text-truncate w-100" :title="game.name">
      {{ game.name }}
    </h6>

    <!-- Stats Row -->
    <div class="stats-row d-flex justify-content-center align-items-center gap-3 small text-muted">
      <!-- Rating -->
      <div v-if="game.averageRating > 0 || game.rating > 0" class="stat-item">
        <i class="fas fa-star text-warning"></i>
        <span v-if="game.averageRating > 0">{{ game.averageRating.toFixed(1) }}</span>
        <span v-else>{{ game.rating.toFixed(1) }}</span>
      </div>

      <!-- Likes -->
      <div 
        v-if="game.likesCount > 0" 
        class="stat-item stat-clickable"
        @click.stop="$emit('view-likes', game)"
        :title="`View ${game.likesCount} likes`"
      >
        <i class="fas fa-heart text-danger"></i>
        {{ formatCount(game.likesCount) }}
      </div>

      <!-- Lists -->
      <div 
        v-if="game.favoritesCount > 0" 
        class="stat-item stat-clickable"
        @click.stop="$emit('view-lists', game)"
        :title="`View ${game.favoritesCount} lists`"
      >
        <i class="fas fa-list text-primary"></i>
        {{ formatCount(game.favoritesCount) }}
      </div>

      <!-- Reviews -->
      <div 
        v-if="game.reviewsCount > 0" 
        class="stat-item stat-clickable"
        @click.stop="$emit('view-reviews', game)"
        :title="`View ${game.reviewsCount} reviews`"
      >
        <i class="fas fa-comment text-info"></i>
        {{ formatCount(game.reviewsCount) }}
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed, defineProps, defineEmits } from 'vue'
import { Game } from '@/models/Game'
import { useImageFallback, FALLBACK_TYPES } from '@/composables/useImageFallback'

// Props
const props = defineProps({
  game: {
    type: Game,
    required: true
  }
})

// Emits
defineEmits(['click', 'view-likes', 'view-lists', 'view-reviews'])

// Image fallback composable
const { handleImageError, createReactiveImageUrl, IMAGE_CONTEXTS } = useImageFallback()

// Computed properties
const gameImageUrl = createReactiveImageUrl(
  computed(() => props.game.primaryImageUrl),
  FALLBACK_TYPES.GAME,
  IMAGE_CONTEXTS.GAME_CARD
)

// Format large numbers (e.g., 1500 -> 1.5k)
const formatCount = (count) => {
  if (count < 1000) return count.toString()
  if (count < 1000000) return (count / 1000).toFixed(1) + 'k'
  return (count / 1000000).toFixed(1) + 'M'
}
</script>

<style scoped>
.game-row-card {
  width: 180px;
  transition: transform 0.2s ease, box-shadow 0.2s ease;
  padding: 0.5rem;
  border-radius: 12px;
}

.game-row-card:hover {
  transform: translateY(-3px);
  box-shadow: 0 4px 15px rgba(0, 0, 0, 0.1);
  background-color: #f8f9fa;
}

.cursor-pointer {
  cursor: pointer;
}

.game-image-container {
  width: 132px;
  height: 187px;
}

.game-image {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.game-name {
  font-size: 0.875rem;
  line-height: 1.2;
  height: 2.4em;
  overflow: hidden;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  line-clamp: 2;
  -webkit-box-orient: vertical;
}

.stats-row {
  min-height: 1.5rem;
}

.stat-item {
  display: flex;
  align-items: center;
  gap: 0.25rem;
  white-space: nowrap;
}

.stat-clickable {
  cursor: pointer;
  transition: color 0.2s ease, transform 0.1s ease;
  border-radius: 4px;
  padding: 0.2rem 0.4rem;
}

.stat-clickable:hover {
  background-color: rgba(0, 0, 0, 0.05);
  transform: translateY(-1px);
  color: #495057 !important;
}

.badge {
  font-size: 0.7rem;
  padding: 0.2rem 0.4rem;
}

/* Responsive adjustments */
@media (max-width: 768px) {
  .game-row-card {
    width: 140px;
  }

  .game-image-container {
    width: 110px;
    height: 156px;
  }

  .game-name {
    font-size: 0.8rem;
  }

  .stats-row {
    font-size: 0.75rem;
  }
}

@media (max-width: 576px) {
  .game-row-card {
    width: 120px;
  }

  .game-image-container {
    width: 90px;
    height: 128px;
  }

  .stats-row {
    flex-direction: column;
    gap: 0.2rem;
  }

  .stat-clickable {
    padding: 0.1rem 0.2rem;
  }
}
</style>