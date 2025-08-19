<template>
  <div class="card mb-3 shadow-sm border-0" :class="{ 'border-primary': highlighted }">
    <div class="card-body p-4">
      <!-- Header with user info -->
      <div class="d-flex justify-content-between align-items-start mb-3">
        <div class="d-flex align-items-center">
          <!-- User Profile Picture -->
          <router-link :to="`/profile/${list.user.id}`" class="text-decoration-none">
            <img
              :src="userImageUrl"
              :alt="`${list.user.displayName || list.user.username}'s profile`"
              class="rounded-circle me-3"
              style="width: 50px; height: 50px; object-fit: cover;"
              @error="handleImageError"
            >
          </router-link>

          <div>
            <!-- User Name -->
            <router-link :to="`/profile/${list.user.id}`" class="text-decoration-none">
              <h6 class="mb-0 fw-semibold text-dark">
                {{ list.user.displayName || list.user.username }}
              </h6>
            </router-link>

            <!-- Creation Date -->
            <small class="text-muted">{{ formattedDate }}</small>
          </div>
        </div>

        <!-- Actions Menu (only for owner) -->
        <div v-if="canEdit" class="dropdown">
          <button class="btn btn-sm btn-outline-secondary" data-bs-toggle="dropdown">
            <i class="fas fa-ellipsis-h"></i>
          </button>
          <ul class="dropdown-menu">
            <li>
              <button @click="$emit('edit', list)" class="dropdown-item">
                <i class="fas fa-edit me-2"></i>Edit List
              </button>
            </li>
            <li><hr class="dropdown-divider"></li>
            <li>
              <button @click="$emit('delete', list)" class="dropdown-item text-danger">
                <i class="fas fa-trash me-2"></i>Delete List
              </button>
            </li>
          </ul>
        </div>
      </div>

      <!-- List Title -->
      <div class="mb-3">
        <router-link :to="`/lists/${list.id}`" class="text-decoration-none">
          <h5 class="mb-1 fw-bold text-dark">{{ list.title }}</h5>
        </router-link>
      </div>

      <!-- List Description -->
      <div v-if="list.description" class="mb-3">
        <p class="mb-0 text-muted" style="line-height: 1.6;" :class="{ 'text-truncate-multiline': truncated && !expanded }">
          {{ list.description }}
        </p>

        <!-- Show More/Less Button -->
        <button v-if="truncated && list.description && list.description.length > maxLength"
                @click="expanded = !expanded"
                class="btn btn-link p-0 mt-2 text-primary">
          <small>{{ expanded ? 'Show Less' : 'Show More' }}</small>
        </button>
      </div>

      <!-- Game Images Grid -->
      <div v-if="displayGames.length > 0" class="mb-3">
        <div class="row g-2">
          <div v-for="game in displayGames" :key="game.id" class="col-auto">
            <router-link :to="`/games/${game.slug}`" class="text-decoration-none">
              <img
                :src="getGameImageUrl(game)"
                :alt="game.name"
                class="rounded game-thumbnail"
                style="width: 60px; height: 60px; object-fit: cover; cursor: pointer;"
                @error="handleGameImageError"
                :title="game.name"
              >
            </router-link>
          </div>
          
          <!-- Show count if more games -->
          <div v-if="list.games && list.games.length > maxGamesToShow" class="col-auto d-flex align-items-center">
            <div class="rounded bg-light d-flex align-items-center justify-content-center" 
                 style="width: 60px; height: 60px;">
              <small class="text-muted fw-semibold">+{{ list.games.length - maxGamesToShow }}</small>
            </div>
          </div>
        </div>
      </div>

      <!-- List Footer with stats and actions -->
      <div class="d-flex justify-content-between align-items-center mt-3 pt-3 border-top">
        <div class="d-flex align-items-center text-muted small">
          <span class="me-3">
            <i class="fas fa-gamepad me-1"></i>
            {{ list.gameCount || (list.games ? list.games.length : 0) }} games
          </span>
          <span v-if="list.isPublic === false" class="me-3">
            <i class="fas fa-lock me-1"></i>
            Private
          </span>
        </div>

        <div class="d-flex align-items-center">
          <!-- Like Button -->
          <button 
            v-if="showLikeButton"
            @click="$emit('toggleLike', list)" 
            class="btn btn-sm btn-outline-primary me-2"
            :class="{ 'btn-primary text-white': isLiked }"
            :disabled="isProcessingLike"
          >
            <i class="fas fa-heart me-1"></i>
            {{ list.likeCount || 0 }}
          </button>

          <!-- Comment Button -->
          <button 
            v-if="showCommentButton"
            @click="$emit('showComments', list)" 
            class="btn btn-sm btn-outline-secondary"
          >
            <i class="fas fa-comment me-1"></i>
            {{ list.commentCount || 0 }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, defineProps, defineEmits } from 'vue'
import { useAuthStore } from '@/stores/authStore'
import { useImageFallback, FALLBACK_TYPES } from '@/composables/useImageFallback'

// Props
const props = defineProps({
  list: {
    type: Object,
    required: true
  },
  highlighted: {
    type: Boolean,
    default: false
  },
  truncated: {
    type: Boolean,
    default: true
  },
  maxLength: {
    type: Number,
    default: 200
  },
  maxGamesToShow: {
    type: Number,
    default: 6
  },
  showLikeButton: {
    type: Boolean,
    default: true
  },
  showCommentButton: {
    type: Boolean,
    default: true
  },
  isLiked: {
    type: Boolean,
    default: false
  },
  isProcessingLike: {
    type: Boolean,
    default: false
  }
})

// Emits
defineEmits(['edit', 'delete', 'toggleLike', 'showComments'])

// Composables
const authStore = useAuthStore()
const { handleImageError: handleImgError, createReactiveImageUrl } = useImageFallback()

// State
const expanded = ref(false)

// Computed
const canEdit = computed(() => {
  return authStore.user && authStore.user.id === props.list.user.id
})

const userImageUrl = createReactiveImageUrl(
  computed(() => props.list.user?.profileImageUrl),
  FALLBACK_TYPES.PROFILE
)

const displayGames = computed(() => {
  if (!props.list.games) return []
  return props.list.games.slice(0, props.maxGamesToShow)
})

const formattedDate = computed(() => {
  if (!props.list.createdAt) return ''

  try {
    const date = new Date(props.list.createdAt)
    return date.toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric'
    })
  } catch (error) {
    console.warn('Error formatting date:', error)
    return ''
  }
})

// Methods
const handleImageError = (e) => {
  handleImgError(e, 'profile')
}

const getGameImageUrl = (game) => {
  return game?.primaryImageUrl || game?.cover?.imageUrl || '/placeholder-game.png'
}

const handleGameImageError = (e) => {
  e.target.src = '/placeholder-game.png'
}
</script>

<style scoped>
.card {
  border-radius: 12px;
  transition: all 0.3s ease;
}

.card:hover {
  transform: translateY(-2px);
  box-shadow: 0 8px 25px rgba(0,0,0,0.1) !important;
}

.text-truncate-multiline {
  display: -webkit-box;
  -webkit-line-clamp: 3;
  -webkit-box-orient: vertical;
  overflow: hidden;
}

.game-thumbnail {
  transition: all 0.2s ease;
}

.game-thumbnail:hover {
  transform: scale(1.05);
  box-shadow: 0 4px 12px rgba(0,0,0,0.15);
}

.btn-link:hover {
  text-decoration: underline !important;
}

@media (max-width: 768px) {
  .card-body {
    padding: 1rem !important;
  }

  .d-flex.align-items-center img {
    width: 40px !important;
    height: 40px !important;
  }

  .game-thumbnail {
    width: 50px !important;
    height: 50px !important;
  }
}
</style>