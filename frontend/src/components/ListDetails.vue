<template>
  <div v-if="loading" class="d-flex justify-content-center align-items-center" style="min-height: 400px;">
    <div class="text-center">
      <div class="spinner-border text-primary" role="status">
        <span class="visually-hidden">Loading...</span>
      </div>
      <p class="mt-2 text-muted">Loading list details...</p>
    </div>
  </div>

  <div v-else-if="error" class="alert alert-danger" role="alert">
    <h4 class="alert-heading">Error Loading List</h4>
    <p>{{ error }}</p>
    <button @click="$emit('retry')" class="btn btn-outline-danger">
      <i class="fas fa-redo me-2"></i>Try Again
    </button>
  </div>

  <div v-else-if="list" class="list-details">
    <!-- List Header -->
    <div class="row mb-4">
      <div class="col">
        <!-- Breadcrumb -->
        <nav aria-label="breadcrumb" class="mb-3">
          <ol class="breadcrumb">
            <li class="breadcrumb-item">
              <router-link to="/lists" class="text-decoration-none">Lists</router-link>
            </li>
            <li class="breadcrumb-item active">{{ list.title }}</li>
          </ol>
        </nav>

        <!-- List Title and Actions -->
        <div class="d-flex justify-content-between align-items-start mb-3">
          <div class="flex-grow-1">
            <h1 class="mb-2">{{ list.title }}</h1>
            
            <!-- List Metadata -->
            <div class="d-flex align-items-center flex-wrap text-muted mb-2">
              <span class="me-3">
                <i class="fas fa-gamepad me-1"></i>
                {{ list.gameCount || (list.games ? list.games.length : 0) }} games
              </span>
              <span class="me-3">
                <i class="fas fa-calendar me-1"></i>
                {{ formattedDate }}
              </span>
              <span v-if="list.isPublic === false" class="me-3">
                <i class="fas fa-lock me-1"></i>
                Private
              </span>
            </div>

            <!-- User Info -->
            <div class="d-flex align-items-center mb-3">
              <router-link :to="`/profile/${list.user.id}`" class="text-decoration-none d-flex align-items-center">
                <img
                  :src="userImageUrl"
                  :alt="`${list.user.displayName || list.user.username}'s profile`"
                  class="rounded-circle me-2"
                  style="width: 32px; height: 32px; object-fit: cover;"
                  @error="handleImageError"
                >
                <span class="text-dark fw-semibold">{{ list.user.displayName || list.user.username }}</span>
              </router-link>
            </div>
          </div>

          <!-- Action Buttons -->
          <div class="d-flex align-items-start gap-2">
            <!-- Like Button -->
            <button 
              v-if="showSocialActions"
              @click="toggleLike" 
              class="btn btn-outline-primary"
              :class="{ 'btn-primary text-white': isLiked }"
              :disabled="isProcessingLike"
            >
              <span v-if="isProcessingLike" class="spinner-border spinner-border-sm me-2" role="status"></span>
              <i class="fas fa-heart me-1"></i>
              {{ list.likeCount || 0 }}
            </button>

            <!-- Owner Actions -->
            <div v-if="canEdit" class="dropdown">
              <button class="btn btn-outline-secondary dropdown-toggle" data-bs-toggle="dropdown">
                <i class="fas fa-cog me-1"></i>Manage
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
        </div>

        <!-- List Description -->
        <div v-if="list.description" class="mb-4">
          <p class="lead">{{ list.description }}</p>
        </div>
      </div>
    </div>

    <!-- Games Grid -->
    <div class="row mb-4">
      <div class="col">
        <div class="d-flex justify-content-between align-items-center mb-3">
          <h3>Games ({{ list.games ? list.games.length : 0 }})</h3>
          
          <!-- Add Game Button (for owners) -->
          <button v-if="canEdit" @click="$emit('addGames')" class="btn btn-outline-primary">
            <i class="fas fa-plus me-2"></i>Add Games
          </button>
        </div>

        <!-- Games Grid -->
        <div v-if="list.games && list.games.length > 0" class="row g-3">
          <div v-for="game in list.games" :key="game.id" class="col-6 col-md-4 col-lg-3 col-xl-2">
            <div class="game-card h-100">
              <router-link :to="`/games/${game.slug}`" class="text-decoration-none">
                <div class="card h-100 border-0 shadow-sm">
                  <img
                    :src="getGameImageUrl(game)"
                    :alt="game.name"
                    class="card-img-top"
                    style="height: 200px; object-fit: cover;"
                    @error="handleGameImageError"
                  >
                  
                  <!-- Remove button for owner -->
                  <button
                    v-if="canEdit"
                    @click.prevent="removeGame(game)"
                    class="btn btn-sm btn-danger position-absolute top-0 end-0 m-2"
                    style="opacity: 0.8;"
                    :title="`Remove ${game.name} from list`"
                  >
                    <i class="fas fa-times"></i>
                  </button>
                  
                  <div class="card-body p-2">
                    <h6 class="card-title mb-1 text-dark" style="font-size: 0.875rem; line-height: 1.2;">
                      {{ game.name }}
                    </h6>
                    <small class="text-muted">
                      {{ game.releaseDate ? new Date(game.releaseDate).getFullYear() : 'N/A' }}
                    </small>
                  </div>
                </div>
              </router-link>
            </div>
          </div>
        </div>

        <!-- Empty State -->
        <div v-else class="text-center text-muted py-5">
          <i class="fas fa-gamepad fa-3x mb-3 opacity-50"></i>
          <h4>No Games in This List</h4>
          <p class="mb-3">This list doesn't have any games yet.</p>
          <button v-if="canEdit" @click="$emit('addGames')" class="btn btn-primary">
            <i class="fas fa-plus me-2"></i>Add Your First Game
          </button>
        </div>
      </div>
    </div>

    <!-- Comments Section -->
    <div class="row">
      <div class="col">
        <div class="card border-0 shadow-sm">
          <div class="card-header bg-white">
            <h4 class="mb-0">
              <i class="fas fa-comments me-2"></i>
              Comments ({{ commentCount }})
            </h4>
          </div>
          <div class="card-body">
            <!-- Comments will be handled by a separate CommentSection component -->
            <slot name="comments">
              <p class="text-muted text-center py-3">
                Comments component will be implemented here
              </p>
            </slot>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed, defineProps, defineEmits } from 'vue'
import { useAuthStore } from '@/stores/authStore'
import { useImageFallback, FALLBACK_TYPES } from '@/composables/useImageFallback'

// Props
const props = defineProps({
  list: {
    type: Object,
    default: null
  },
  loading: {
    type: Boolean,
    default: false
  },
  error: {
    type: String,
    default: null
  },
  isLiked: {
    type: Boolean,
    default: false
  },
  isProcessingLike: {
    type: Boolean,
    default: false
  },
  commentCount: {
    type: Number,
    default: 0
  },
  showSocialActions: {
    type: Boolean,
    default: true
  }
})

// Emits
const emit = defineEmits(['edit', 'delete', 'addGames', 'removeGame', 'toggleLike', 'retry'])

// Composables
const authStore = useAuthStore()
const { handleImageError: handleImgError, createReactiveImageUrl } = useImageFallback()

// Computed
const canEdit = computed(() => {
  return authStore.user && props.list && authStore.user.id === props.list.user.id
})

const userImageUrl = createReactiveImageUrl(
  computed(() => props.list?.user?.profileImageUrl),
  FALLBACK_TYPES.PROFILE
)

const formattedDate = computed(() => {
  if (!props.list?.createdAt) return ''

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

const toggleLike = () => {
  if (!authStore.user) {
    // Redirect to login or show message
    return
  }
  
  emit('toggleLike', props.list)
}

const removeGame = (game) => {
  if (confirm(`Remove "${game.name}" from this list?`)) {
    emit('removeGame', game)
  }
}
</script>

<style scoped>
.list-details {
  max-width: 1200px;
  margin: 0 auto;
}

.game-card {
  transition: all 0.2s ease;
}

.game-card:hover {
  transform: translateY(-2px);
}

.game-card .card {
  transition: all 0.2s ease;
}

.game-card:hover .card {
  box-shadow: 0 8px 25px rgba(0,0,0,0.15) !important;
}

.game-card .position-absolute {
  transition: opacity 0.2s ease;
}

.game-card:not(:hover) .position-absolute {
  opacity: 0 !important;
}

.breadcrumb-item + .breadcrumb-item::before {
  content: "›";
}

@media (max-width: 768px) {
  .list-details {
    padding: 0 1rem;
  }
  
  .col-6 {
    flex: 0 0 50%;
    max-width: 50%;
  }
  
  .card-img-top {
    height: 150px !important;
  }
}

@media (max-width: 576px) {
  .d-flex.justify-content-between {
    flex-direction: column;
    align-items: flex-start !important;
  }
  
  .d-flex.align-items-start.gap-2 {
    margin-top: 1rem;
    align-self: stretch;
  }
}
</style>