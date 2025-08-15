<template>
  <div class="card mb-3 shadow-sm border-0" :class="{ 'border-primary': highlighted }">
    <div class="card-body p-4">
      <!-- Header with user info and game info (if showGame is true) -->
      <div class="d-flex justify-content-between align-items-start mb-3">
        <div class="d-flex align-items-center">
          <!-- User Profile Picture -->
          <router-link :to="`/profile/${review.user.id}`" class="text-decoration-none">
            <img
              :src="userImageUrl"
              :alt="`${review.user.displayName || review.user.username}'s profile`"
              class="rounded-circle me-3"
              style="width: 50px; height: 50px; object-fit: cover;"
              @error="handleImageError"
            >
          </router-link>

          <div>
            <!-- User Name -->
            <router-link :to="`/profile/${review.user.id}`" class="text-decoration-none">
              <h6 class="mb-0 fw-semibold text-dark">
                {{ review.user.displayName || review.user.username }}
              </h6>
            </router-link>

            <!-- Game Name (if showing) -->
            <div v-if="showGame && review.game" class="mb-1">
              <router-link :to="`/games/${review.game.slug}`" class="text-decoration-none">
                <small class="text-primary fw-medium">{{ review.game.name }}</small>
              </router-link>
            </div>

            <!-- Review Date -->
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
              <button @click="$emit('edit', review)" class="dropdown-item">
                <i class="fas fa-edit me-2"></i>Edit Review
              </button>
            </li>
            <li><hr class="dropdown-divider"></li>
            <li>
              <button @click="$emit('delete', review)" class="dropdown-item text-danger">
                <i class="fas fa-trash me-2"></i>Delete Review
              </button>
            </li>
          </ul>
        </div>
      </div>

      <!-- Rating Stars -->
      <div class="mb-3">
        <div class="d-flex align-items-center">
          <div class="text-warning me-2" style="font-size: 1.1em;">
            <i v-for="n in 5" :key="n"
               :class="n <= review.rating ? 'fas fa-star' : 'far fa-star'"></i>
          </div>
          <span class="fw-semibold text-muted">{{ review.rating }}/5</span>
        </div>
      </div>

      <!-- Game Image (if showing game) -->
      <div v-if="showGame && review.game" class="d-flex mb-3">
        <router-link :to="`/games/${review.game.igdbId}`" class="text-decoration-none">
          <img
            :src="gameImageUrl"
            :alt="review.game.name"
            class="rounded me-3"
            style="width: 80px; height: 80px; object-fit: cover;"
            @error="handleGameImageError"
          >
        </router-link>
      </div>

      <!-- Review Text -->
      <div class="review-text">
        <p class="mb-0" style="line-height: 1.6;" :class="{ 'text-truncate-multiline': truncated && !expanded }">
          {{ review.reviewText }}
        </p>

        <!-- Show More/Less Button -->
        <button v-if="truncated && review.reviewText.length > maxLength"
                @click="expanded = !expanded"
                class="btn btn-link p-0 mt-2 text-primary">
          <small>{{ expanded ? 'Show Less' : 'Show More' }}</small>
        </button>
      </div>

      <!-- Review Footer -->
      <div v-if="showDate || showGameName" class="mt-3 pt-3 border-top">
        <div class="d-flex justify-content-between align-items-center text-muted small">
          <div v-if="showGameName && review.game">
            <i class="fas fa-gamepad me-1"></i>
            {{ review.game.name }}
          </div>
          <div v-if="showDate">
            {{ relativeDate }}
          </div>
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
  review: {
    type: Object,
    required: true
  },
  showGame: {
    type: Boolean,
    default: false
  },
  showDate: {
    type: Boolean,
    default: true
  },
  showGameName: {
    type: Boolean,
    default: false
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
  }
})

// Emits
defineEmits(['edit', 'delete'])

// Composables
const authStore = useAuthStore()
const { handleImageError: handleImgError, createReactiveImageUrl } = useImageFallback()

// State
const expanded = ref(false)

// Computed
const canEdit = computed(() => {
  return authStore.user && authStore.user.id === props.review.user.id
})

const userImageUrl = createReactiveImageUrl(
  computed(() => props.review.user?.profileImageUrl),
  FALLBACK_TYPES.PROFILE
)

const gameImageUrl = createReactiveImageUrl(
  computed(() => props.review.game?.primaryImageUrl),
  FALLBACK_TYPES.GAME_ICON
)

const formattedDate = computed(() => {
  if (!props.review.createdAt) return ''

  try {
    const date = new Date(props.review.createdAt)
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

const relativeDate = computed(() => {
  if (!props.review.createdAt) return ''

  try {
    const date = new Date(props.review.createdAt)
    const now = new Date()
    const diffTime = Math.abs(now - date)
    const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24))

    if (diffDays === 1) return 'Yesterday'
    if (diffDays < 7) return `${diffDays} days ago`
    if (diffDays < 30) return `${Math.ceil(diffDays / 7)} weeks ago`
    if (diffDays < 365) return `${Math.ceil(diffDays / 30)} months ago`
    return `${Math.ceil(diffDays / 365)} years ago`
  } catch (error) {
    console.warn('Error calculating relative date:', error)
    return ''
  }
})

// Methods
const handleImageError = (e) => {
  handleImgError(e, 'profile')
}

const handleGameImageError = (e) => {
  handleImgError(e, 'game')
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

.review-text p {
  word-wrap: break-word;
  white-space: pre-wrap;
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
}
</style>