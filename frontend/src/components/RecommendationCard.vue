<template>
  <div class="card recommendation-card border-0 shadow-sm mb-4">
    <div class="card-body p-4">
      <div class="row align-items-start">
        <!-- Game Cover Image -->
        <div class="col-md-3 col-lg-2 mb-3 mb-md-0">
          <div class="position-relative">
            <img
              :src="gameImageUrl || '/default-game-cover.png'"
              :alt="recommendation.name"
              class="img-fluid rounded game-cover"
              style="width: 100%; height: 200px; object-fit: cover;"
              loading="lazy"
              @error="handleImageError"
              @click="$emit('view-game', recommendation.gameId)"
            >
            
            <!-- Confidence Score Badge -->
            <div class="position-absolute top-0 end-0 m-2">
              <span 
                class="badge confidence-badge"
                :class="confidenceBadgeClass"
                :title="`Confidence Score: ${(recommendation.confidenceScore * 100).toFixed(1)}%`"
              >
                {{ confidenceLevel }}
              </span>
            </div>

            <!-- Rating Badge -->
            <div v-if="recommendation.rating && recommendation.rating > 0" class="position-absolute top-0 start-0 m-2">
              <span class="badge bg-dark bg-opacity-75 d-flex align-items-center">
                <i class="fas fa-star text-warning me-1"></i>
                {{ recommendation.rating.toFixed(1) }}/5
              </span>
            </div>
          </div>
        </div>

        <!-- Game Details -->
        <div class="col-md-9 col-lg-10">
          <!-- Game Name and Basic Info -->
          <div class="mb-3">
            <h5 class="card-title mb-2 fw-bold text-primary cursor-pointer" @click="$emit('view-game', recommendation.gameId)">
              {{ recommendation.name }}
              <i class="fas fa-external-link-alt ms-2 small text-muted"></i>
            </h5>
            
            <!-- Genres and Platforms -->
            <div class="mb-2">
              <div v-if="recommendation.genres && recommendation.genres.length > 0" class="mb-1">
                <small class="text-muted me-3">
                  <i class="fas fa-tags me-1"></i>
                  {{ recommendation.genres.slice(0, 3).join(', ') }}
                  <span v-if="recommendation.genres.length > 3"> +{{ recommendation.genres.length - 3 }} more</span>
                </small>
              </div>
              
              <div v-if="recommendation.platforms && recommendation.platforms.length > 0">
                <small class="text-muted">
                  <i class="fas fa-gamepad me-1"></i>
                  {{ recommendation.platforms.slice(0, 3).join(', ') }}
                  <span v-if="recommendation.platforms.length > 3"> +{{ recommendation.platforms.length - 3 }} more</span>
                </small>
              </div>
            </div>
          </div>

          <!-- Game Summary -->
          <div v-if="recommendation.summary" class="mb-3">
            <p class="text-muted small mb-2" style="line-height: 1.5;">
              {{ truncatedSummary }}
              <button 
                v-if="recommendation.summary.length > summaryLimit"
                @click="showFullSummary = !showFullSummary"
                class="btn btn-link p-0 ms-1 small"
              >
                {{ showFullSummary ? 'Show Less' : 'Show More' }}
              </button>
            </p>
          </div>

          <!-- AI Reasoning/Explanation -->
          <div class="mb-3">
            <div class="d-flex align-items-start">
              <i class="fas fa-robot text-info me-2 mt-1"></i>
              <div class="flex-grow-1">
                <h6 class="small fw-semibold text-info mb-1">Why this game matches your request:</h6>
                <p class="small mb-0" style="line-height: 1.6;">
                  {{ recommendation.reasoning || 'This game was recommended based on your preferences and query.' }}
                </p>
              </div>
            </div>
          </div>

          <!-- User Activity Match Info -->
          <div v-if="hasUserActivityMatch" class="mb-3">
            <div class="alert alert-success py-2 px-3 small mb-0">
              <div class="d-flex align-items-center mb-1">
                <i class="fas fa-user-check text-success me-2"></i>
                <span class="fw-semibold">Personal Connection</span>
              </div>
              
              <div class="ms-4">
                <div v-if="recommendation.userActivityMatch.isUserFavorite" class="mb-1">
                  <i class="fas fa-heart text-danger me-1"></i>
                  <span>This is one of your favorite games!</span>
                </div>
                
                <div v-if="recommendation.userActivityMatch.isUserLiked" class="mb-1">
                  <i class="fas fa-thumbs-up text-success me-1"></i>
                  <span>You've liked this game before</span>
                </div>
                
                <div v-if="recommendation.userActivityMatch.followedUsersWhoLiked?.length > 0" class="mb-1">
                  <i class="fas fa-users text-info me-1"></i>
                  <span>{{ recommendation.userActivityMatch.followedUsersWhoLiked.length }} people you follow like this game</span>
                </div>
                
                <div v-if="recommendation.userActivityMatch.similarToUserFavorites?.length > 0">
                  <i class="fas fa-star text-warning me-1"></i>
                  <span>Similar to your favorite: {{ recommendation.userActivityMatch.similarToUserFavorites[0] }}</span>
                </div>
              </div>
            </div>
          </div>

          <!-- Action Buttons -->
          <div class="d-flex gap-2 flex-wrap">
            <button
              @click="$emit('view-game', recommendation.gameId)"
              class="btn btn-primary btn-sm"
            >
              <i class="fas fa-eye me-1"></i>
              View Details
            </button>
            
            <button
              @click="$emit('add-to-library', recommendation)"
              class="btn btn-outline-success btn-sm"
            >
              <i class="fas fa-plus me-1"></i>
              Add to Library
            </button>
            
            <button
              @click="$emit('add-to-wishlist', recommendation)"
              class="btn btn-outline-secondary btn-sm"
            >
              <i class="fas fa-heart me-1"></i>
              Wishlist
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, defineProps, defineEmits } from 'vue'
import { useImageFallback, FALLBACK_TYPES } from '@/composables/useImageFallback'

// Props
const props = defineProps({
  recommendation: {
    type: Object,
    required: true
  }
})

// Emits
defineEmits(['view-game', 'add-to-library', 'add-to-wishlist'])

// State
const showFullSummary = ref(false)
const summaryLimit = 150

// Image fallback composable
const { handleImageError: handleImgError, createReactiveImageUrl } = useImageFallback()

// Computed properties
const gameImageUrl = createReactiveImageUrl(
  computed(() => props.recommendation.coverUrl),
  FALLBACK_TYPES.GAME_SMALL
)

const truncatedSummary = computed(() => {
  if (!props.recommendation.summary) return ''
  
  const summary = props.recommendation.summary
  if (showFullSummary.value || summary.length <= summaryLimit) {
    return summary
  }
  
  return summary.substring(0, summaryLimit).trim() + '...'
})

const confidenceLevel = computed(() => {
  const score = props.recommendation.confidenceScore || 0
  if (score >= 0.8) return 'High'
  if (score >= 0.6) return 'Medium'
  if (score >= 0.4) return 'Fair'
  return 'Low'
})

const confidenceBadgeClass = computed(() => {
  const score = props.recommendation.confidenceScore || 0
  if (score >= 0.8) return 'bg-success'
  if (score >= 0.6) return 'bg-warning'
  if (score >= 0.4) return 'bg-info'
  return 'bg-secondary'
})

const hasUserActivityMatch = computed(() => {
  const match = props.recommendation.userActivityMatch
  if (!match) return false
  
  return match.isUserFavorite || 
         match.isUserLiked || 
         match.isFromFollowedUsers ||
         (match.followedUsersWhoLiked && match.followedUsersWhoLiked.length > 0) ||
         (match.similarToUserFavorites && match.similarToUserFavorites.length > 0) ||
         (match.similarToUserLikedGames && match.similarToUserLikedGames.length > 0)
})

// Methods
const handleImageError = (e) => {
  handleImgError(e, 'gameSmall')
}
</script>

<style scoped>
.recommendation-card {
  border-radius: 15px;
  transition: all 0.3s ease;
  border: 1px solid #f0f0f0;
}

.recommendation-card:hover {
  transform: translateY(-2px);
  box-shadow: 0 8px 25px rgba(0,0,0,0.15) !important;
}

.game-cover {
  border-radius: 10px;
  cursor: pointer;
  transition: transform 0.2s ease;
}

.game-cover:hover {
  transform: scale(1.02);
}

.cursor-pointer {
  cursor: pointer;
}

.cursor-pointer:hover {
  text-decoration: underline;
}

.confidence-badge {
  font-size: 0.7rem;
  padding: 0.25rem 0.5rem;
}

.alert-success {
  background-color: #f8f9fa;
  border-color: #d1ecf1;
  color: #155724;
}

@media (max-width: 768px) {
  .game-cover {
    height: 150px !important;
  }
  
  .card-body {
    padding: 1.5rem !important;
  }
  
  .d-flex.gap-2 {
    flex-direction: column;
  }
  
  .btn-sm {
    width: 100%;
    margin-bottom: 0.5rem;
  }
}
</style>