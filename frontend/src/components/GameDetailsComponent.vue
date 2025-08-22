<template>
  <div class="container py-4">
    <!-- Loading State -->
    <div v-if="loading" class="text-center py-5">
      <div class="spinner-border text-primary mb-3" style="width: 3rem; height: 3rem;"></div>
      <h5 class="text-muted">Loading game details...</h5>
    </div>

    <!-- Error State -->
    <div v-else-if="error" class="alert alert-danger" role="alert">
      <i class="fas fa-exclamation-triangle me-2"></i>
      {{ error }}
      <div class="mt-3">
        <button @click="loadGameDetails" class="btn btn-outline-danger me-2">
          <i class="fas fa-retry me-1"></i>
          Try Again
        </button>
        <router-link to="/dashboard" class="btn btn-secondary">
          <i class="fas fa-arrow-left me-1"></i>
          Back to Dashboard
        </router-link>
      </div>
    </div>

    <!-- Game Details Content -->
    <div v-else-if="game && game.id" class="row">
      <!-- Breadcrumb -->
      <div class="col-12 mb-3">
        <nav aria-label="breadcrumb">
          <ol class="breadcrumb">
            <li class="breadcrumb-item">
              <router-link to="/dashboard" class="text-decoration-none">
                <i class="fas fa-home me-1"></i>Dashboard
              </router-link>
            </li>
            <li class="breadcrumb-item">
              <router-link to="/games" class="text-decoration-none">Games</router-link>
            </li>
            <li class="breadcrumb-item active" aria-current="page">{{ game.name || 'Loading...' }}</li>
          </ol>
        </nav>
      </div>

      <!-- Game Header -->
      <div class="col-12 mb-4">
        <div class="card shadow-lg border-0">
          <div class="row g-0">
            <!-- Game Image -->
            <div class="col-md-4">
              <img
                :src="gameImageUrl"
                :alt="game.name || 'Game image'"
                class="img-fluid w-100 h-100"
                style="object-fit: cover; min-height: 300px; border-radius: 15px 0 0 15px;"
                loading="eager"
                @error="(e) => handleImageError(e, 'game')"
              >
            </div>

            <!-- Game Info -->
            <div class="col-md-8">
              <div class="card-body p-4">
                <!-- Title and Rating -->
                <div class="d-flex justify-content-between align-items-start mb-3">
                  <div>
                    <h1 class="h2 fw-bold mb-2">{{ game.name || 'Unknown Game' }}</h1>
                    <p v-if="developersText && developersText !== 'Unknown'" class="text-muted mb-0">
                      Developer: {{ developersText }}
                    </p>
                  </div>
                  <div class="text-end">
                    <!-- User Reviews Rating -->
                    <div v-if="game.averageRating > 0" class="mb-2">
                      <div class="h4 mb-0 fw-bold text-primary">
                        <i class="fas fa-star"></i>
                        {{ game.averageRating.toFixed(1) }}/5
                      </div>
                      <small class="text-muted">{{ game.reviewsCount }} Review{{ game.reviewsCount !== 1 ? 's' : '' }}</small>
                    </div>

                    <!-- IGDB Rating -->
                    <div v-if="gameRating > 0" class="mt-2">
                      <div class="h5 mb-0 fw-bold text-warning">
                        <i class="fas fa-star"></i>
                        {{ gameRating }}/5
                      </div>
                      <small class="text-muted">IGDB Rating</small>
                    </div>
                  </div>
                </div>

                <!-- Game Meta Info -->
                <div class="row g-3 mb-4">
                  <div class="col-sm-6">
                    <div class="d-flex align-items-center">
                      <i class="fas fa-tag text-primary me-2"></i>
                      <div>
                        <small class="text-muted d-block">Genres</small>
                        <span class="fw-semibold">{{ genresText }}</span>
                      </div>
                    </div>
                  </div>
                  <div class="col-sm-6">
                    <div class="d-flex align-items-center">
                      <i class="fas fa-calendar text-primary me-2"></i>
                      <div>
                        <small class="text-muted d-block">Release Date</small>
                        <span class="fw-semibold">{{ releaseDateText }}</span>
                      </div>
                    </div>
                  </div>
                  <div class="col-sm-6" v-if="gameModeText">
                    <div class="d-flex align-items-center">
                      <i class="fas fa-users text-primary me-2"></i>
                      <div>
                        <small class="text-muted d-block">Game Modes</small>
                        <span class="fw-semibold">{{ gameModeText }}</span>
                      </div>
                    </div>
                  </div>
                  <div class="col-sm-6" v-if="platformText">
                    <div class="d-flex align-items-center">
                      <i class="fas fa-gamepad text-primary me-2"></i>
                      <div>
                        <small class="text-muted d-block">Platforms</small>
                        <span class="fw-semibold">{{ platformText }}</span>
                      </div>
                    </div>
                  </div>
                </div>

                <!-- Engagement Stats -->
                <div v-if="game" class="row g-3 mb-4">
                  <div :class="game.hypes > 0 ? 'col-4' : 'col-6'" v-if="game.hypes > 0">
                    <div class="text-center">
                      <div class="h5 mb-0 fw-bold text-danger">{{ formatNumber(game.hypes) }}</div>
                      <small class="text-muted">Hypes</small>
                    </div>
                  </div>
                  <div :class="game.hypes > 0 ? 'col-4' : 'col-6'">
                    <div class="text-center">
                      <div class="h5 mb-0 fw-bold text-primary">{{ formatNumber(game.likesCount || 0) }}</div>
                      <small class="text-muted">Likes</small>
                    </div>
                  </div>
                  <div :class="game.hypes > 0 ? 'col-4' : 'col-6'">
                    <div class="text-center">
                      <div class="h5 mb-0 fw-bold text-warning">{{ formatNumber(game.favoritesCount || 0) }}</div>
                      <small class="text-muted">Favorites</small>
                    </div>
                  </div>
                </div>

                <!-- Action Buttons -->
                <div class="d-flex gap-2 mb-3">
                  <button @click="toggleFavorites" :disabled="isProcessingFavorites"
                    class="btn btn-lg flex-grow-1" :class="isInFavorites ? 'btn-success' : 'btn-primary'">
                    <span v-if="isProcessingFavorites" class="spinner-border spinner-border-sm me-2"></span>
                    <i v-else class="fas" :class="isInFavorites ? 'fa-check' : 'fa-plus'"></i>
                    {{ isProcessingFavorites ? 'Processing...' : (isInFavorites ? 'Remove From Favorites' : 'Add to Favorites') }}
                  </button>

                  <button @click="toggleLike" :disabled="isProcessingLikes"
                    class="btn btn-lg flex-grow-1" :class="isInLikes ? 'btn-success' : 'btn-primary'">
                    <span v-if="isProcessingLikes" class="spinner-border spinner-border-sm me-2"></span>
                    <i v-else class="fas" :class="isInLikes ? 'fa-thumbs-up' : 'fa-thumbs-up'"></i>
                    {{ isProcessingLikes ? 'Processing...' : (isInLikes ? 'Liked' : 'Like') }}
                  </button>

                  <button @click="toggleWishlist" class="btn btn-lg btn-outline-secondary"
                    :class="{ 'active': isInWishlist }">
                    <i class="fas fa-heart" :class="{ 'text-danger': isInWishlist }"></i>
                    <span class="d-none d-md-inline ms-1">{{ isInWishlist ? 'Wishlisted' : 'Wishlist' }}</span>
                  </button>

                </div>

                <!-- New Release Badge -->
                <div v-if="isNewRelease" class="mb-3">
                  <span class="badge bg-warning text-dark fs-6">
                    <i class="fas fa-star me-1"></i>
                    New Release
                  </span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Content Grid -->
      <div class="col-lg-8">
        <!-- Description -->
        <div class="card shadow-sm border-0 mb-4">
          <div class="card-header bg-white border-bottom">
            <h3 class="h5 mb-0 fw-bold">
              <i class="fas fa-align-left text-primary me-2"></i>
              About This Game
            </h3>
          </div>
          <div class="card-body p-4">
            <div v-if="game.summary" class="mb-3">
              <p class="mb-0" style="line-height: 1.6;">{{ game.summary }}</p>
            </div>
            <div v-if="game.storyline && game.storyline !== game.summary">
              <h6 class="fw-bold mt-3 mb-2">Storyline</h6>
              <p class="mb-0" style="line-height: 1.6;">{{ game.storyline }}</p>
            </div>
            <div v-if="!game.summary && !game.storyline">
              <p class="text-muted mb-0">No description available for this game.</p>
            </div>
          </div>
        </div>

        <!-- Screenshots -->
        <div v-if="screenshotUrls.length > 0" class="card shadow-sm border-0 mb-4">
          <div class="card-header bg-white border-bottom">
            <h3 class="h5 mb-0 fw-bold">
              <i class="fas fa-images text-primary me-2"></i>
              Screenshots
            </h3>
          </div>
          <div class="card-body p-4">
            <div class="row g-3">
              <div v-for="(screenshot, index) in screenshotUrls" :key="index" class="col-md-6">
                <img :src="screenshot" :alt="`Screenshot ${index + 1}`"
                  class="img-fluid rounded cursor-pointer screenshot-img" @click="openImageModal(screenshot)"
                  loading="lazy">
              </div>
            </div>
          </div>
        </div>

        <!-- Franchises -->
        <div v-if="franchisesList.length > 0" class="card shadow-sm border-0 mb-4">
          <div class="card-header bg-white border-bottom">
            <h3 class="h5 mb-0 fw-bold">
              <i class="fas fa-layer-group text-primary me-2"></i>
              Franchises
            </h3>
          </div>
          <div class="card-body p-4">
            <div class="d-flex flex-wrap gap-2">
              <span v-for="franchise in franchisesList" :key="franchise.id"
                class="badge bg-light text-dark border fs-6">
                {{ franchise.name }}
              </span>
            </div>
          </div>
        </div>

        <!-- Reviews Section -->
        <div class="card shadow-sm border-0 mb-4">
          <div class="card-header bg-white border-bottom">
            <div class="d-flex justify-content-between align-items-center">
              <h3 class="h5 mb-0 fw-bold">
                <i class="fas fa-comments text-primary me-2"></i>
                Reviews
                <span v-if="game.reviewsCount > 0" class="text-muted fw-normal">({{ game.reviewsCount }})</span>
              </h3>
              <button
                v-if="authStore.isAuthenticated && !userReview"
                @click="showReviewForm = !showReviewForm"
                class="btn btn-primary btn-sm"
              >
                <i class="fas fa-plus me-2"></i>
                Write Review
              </button>
            </div>
          </div>
          <div class="card-body p-4">
            <!-- User's Review Form -->
            <div v-if="showReviewForm" class="mb-4">
              <ReviewForm
                :game="game"
                :existing-review="userReview"
                :is-submitting="isSubmittingReview"
                @submit="submitReview"
                @cancel="showReviewForm = false"
                @delete="deleteUserReview"
              />
            </div>

            <!-- Recent Reviews Section -->
            <div v-if="!showReviewForm">
              <!-- Recent Reviews Heading -->
              <div v-if="gameReviews.length > 0" class="mb-3">
                <h4 class="h6 fw-bold text-secondary mb-0">
                  <i class="fas fa-clock me-2"></i>
                  Recent Reviews
                </h4>
              </div>

              <!-- Reviews List -->
              <ReviewsList
                :reviews="gameReviews"
                :loading="loadingReviews"
                :loading-more="loadingMoreReviews"
                :has-more-reviews="hasMoreReviews"
                :total-count="game.reviewsCount || 0"
                :show-header="false"
                :show-game="false"
                :show-limited="true"
                :display-limit="5"
                :truncate-reviews="true"
                :empty-message="(game.reviewsCount === 0 && gameReviews.length === 0) ? 'No reviews yet' : 'Loading reviews...'"
                :empty-sub-message="(game.reviewsCount === 0 && gameReviews.length === 0) ? 'Be the first to share your thoughts about this game!' : ''"
                :liked-reviews="likedReviews"
                :processing-like-reviews="processingLikeReviews"
                @load-more="loadMoreReviews"
                @show-all="showAllReviews"
                @edit="editReview"
                @delete="deleteReview"
                @toggleLike="handleToggleLike"
                @showComments="handleShowComments"
              >
                <template #empty-actions>
                  <button
                    v-if="authStore.isAuthenticated"
                    @click="showReviewForm = true"
                    class="btn btn-primary"
                  >
                    <i class="fas fa-star me-2"></i>
                    Write the First Review
                  </button>
                </template>
              </ReviewsList>

              <!-- User's Review Section -->
              <div v-if="userReview && gameReviews.length > 0" class="mt-4">
                <div class="border-top pt-4">
                  <h4 class="h6 fw-bold text-secondary mb-3">
                    <i class="fas fa-user me-2"></i>
                    Your Review
                  </h4>
                  <!-- <div class="alert alert-info d-flex justify-content-between align-items-center mb-3">
                    <div>
                      <i class="fas fa-info-circle me-2"></i>
                      You have reviewed this game
                    </div>
                    <button @click="showReviewForm = true" class="btn btn-sm btn-outline-primary">
                      Edit Review
                    </button>
                  </div> -->
                  <ReviewCard
                    :review="userReview"
                    :highlighted="true"
                    :truncated="false"
                    :is-liked="likedReviews.has(userReview.id)"
                    :is-processing-like="processingLikeReviews.has(userReview.id)"
                    @edit="showReviewForm = true"
                    @delete="deleteUserReview"
                    @toggleLike="handleToggleLike"
                    @showComments="handleShowComments"
                  />
                </div>
              </div>

              <!-- User's Review Only (when no other reviews) -->
              <div v-else-if="userReview && gameReviews.length === 0" class="mb-4">
                <h4 class="h6 fw-bold text-secondary mb-3">
                  <i class="fas fa-user me-2"></i>
                  Your Review
                </h4>
                <div class="alert alert-info d-flex justify-content-between align-items-center mb-3">
                  <div>
                    <i class="fas fa-info-circle me-2"></i>
                    You have reviewed this game
                  </div>
                  <button @click="showReviewForm = true" class="btn btn-sm btn-outline-primary">
                    Edit Review
                  </button>
                </div>
                <ReviewCard
                  :review="userReview"
                  :highlighted="true"
                  :truncated="false"
                  :is-liked="likedReviews.has(userReview.id)"
                  :is-processing-like="processingLikeReviews.has(userReview.id)"
                  @edit="showReviewForm = true"
                  @delete="deleteUserReview"
                  @toggleLike="handleToggleLike"
                  @showComments="handleShowComments"
                />
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Sidebar -->
      <div class="col-lg-4">
        <!-- Game Details -->
        <div class="card shadow-sm border-0 mb-4">
          <div class="card-header bg-white border-bottom">
            <h3 class="h5 mb-0 fw-bold">
              <i class="fas fa-info-circle text-primary me-2"></i>
              Game Details
            </h3>
          </div>
          <div class="card-body p-4">
            <div v-if="platformsList.length > 0" class="mb-3">
              <h6 class="fw-bold text-muted small">PLATFORMS</h6>
              <div class="d-flex flex-wrap gap-1">
                <span v-for="platform in platformsList" :key="platform.id" class="badge bg-secondary">
                  {{ platform.abbreviation || platform.name }}
                </span>
              </div>
            </div>

            <div v-if="genresList.length > 0" class="mb-3">
              <h6 class="fw-bold text-muted small">GENRES</h6>
              <div class="d-flex flex-wrap gap-1">
                <span v-for="genre in genresList" :key="genre.id" class="badge bg-primary">
                  {{ genre.name }}
                </span>
              </div>
            </div>

            <div v-if="gameModesList.length > 0" class="mb-3">
              <h6 class="fw-bold text-muted small">GAME MODES</h6>
              <div class="d-flex flex-wrap gap-1">
                <span v-for="mode in gameModesList" :key="mode.id" class="badge bg-info">
                  {{ mode.name }}
                </span>
              </div>
            </div>

            <div v-if="ageRatingsList.length > 0" class="mb-3">
              <h6 class="fw-bold text-muted small">AGE RATINGS</h6>
              <div class="d-flex flex-wrap gap-1">
                <span v-for="rating in ageRatingsList" :key="rating.id" class="badge bg-warning text-dark">
                  {{ rating.organization }}: {{ rating.name }}
                </span>
              </div>
            </div>
          </div>
        </div>

        <!-- Similar Games -->
        <div v-if="similarGames.length > 0" class="card shadow-sm border-0 mb-4">
          <div class="card-header bg-white border-bottom">
            <h3 class="h5 mb-0 fw-bold">
              <i class="fas fa-gamepad text-primary me-2"></i>
              Similar Games
            </h3>
          </div>
          <div class="card-body p-0">
            <div class="list-group list-group-flush">
              <div v-for="similarGame in similarGames.slice(0, 4)" :key="similarGame.id"
                @click="viewGame(similarGame.id)"
                class="list-group-item list-group-item-action border-0 py-3 cursor-pointer">
                <div class="d-flex align-items-center">
                  <img :src="getSimilarGameImage(similarGame)" :alt="similarGame.name" class="rounded me-3"
                    style="width: 50px; height: 50px; object-fit: cover;" loading="lazy">
                  <div class="flex-grow-1">
                    <h6 class="mb-1 fw-semibold">{{ similarGame.name }}</h6>
                    <div class="d-flex align-items-center gap-2 small text-muted">
                      <span v-if="getSimilarGameRating(similarGame) > 0">
                        <i class="fas fa-star text-warning me-1"></i>{{ getSimilarGameRating(similarGame) }}/5
                      </span>
                      <span>{{ getSimilarGameGenre(similarGame) }}</span>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Image Modal -->
    <div v-if="selectedImage" class="modal d-block" tabindex="-1" @click="closeImageModal">
      <div class="modal-dialog modal-xl modal-dialog-centered">
        <div class="modal-content bg-transparent border-0">
          <div class="modal-body p-0 position-relative">
            <img :src="selectedImage" class="img-fluid w-100 rounded">
            <button @click="closeImageModal" class="btn btn-light position-absolute top-0 end-0 m-2">
              <i class="fas fa-times"></i>
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, watch, defineProps, nextTick } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useGamesStore } from '@/stores/gamesStore'
import { useToast } from 'vue-toastification'
import { useAuthStore } from '@/stores/authStore'
import { useAuthRedirect } from '@/utils/authRedirect'
import { useImageFallback, FALLBACK_TYPES } from '@/composables/useImageFallback'
import { reviewsService } from '@/services/reviewsService'
import ReviewCard from './ReviewCard.vue'
import ReviewForm from './ReviewForm.vue'
import ReviewsList from './ReviewsList.vue'
import { socialService } from '@/services/socialService'
import { commentsService } from '@/services/commentsService'

// Props
const props = defineProps({
  gameId: {
    type: [Number, String],
    default: null
  },
  slug: {
    type: [String],
    default: null
  }
})

// Composables
const route = useRoute()
const router = useRouter()
const gamesStore = useGamesStore()
const authStore = useAuthStore()
const toast = useToast()
const { requireAuth } = useAuthRedirect()
const { handleImageError, createReactiveImageUrl, getImageUrl, IMAGE_CONTEXTS } = useImageFallback()

// State
const game = ref(null)
const loading = ref(true)
const error = ref('')
const isProcessingFavorites = ref(false)
const userFavorites = ref(new Set())
const isProcessingLikes = ref(false)
const userLikes = ref(new Set())
const userWishlist = ref(new Set())
const similarGames = ref([])
const selectedImage = ref(null)

// Review state
const gameReviews = ref([])
const userReview = ref(null)
const loadingReviews = ref(false)
const loadingMoreReviews = ref(false)
const hasMoreReviews = ref(false)
const showReviewForm = ref(false)
const isSubmittingReview = ref(false)
const reviewsPage = ref(1)
const likedReviews = ref(new Set())
const processingLikeReviews = ref(new Set())

// Computed properties with safe access
const gameIdentifier = computed(() => {
  const propId = props.gameId
  const propSlug = props.slug
  const routeIdentifier = route.params.identifier

  console.log('Computing identifier:', { propId, propSlug, routeIdentifier })
  return propId || propSlug || routeIdentifier || null
})

const isInFavorites = computed(() => {
  if (!game.value?.id) return false
  return userFavorites.value.has(String(game.value.id))
})

const isInLikes = computed(() => {
  if (!game.value?.id) return false;
  return userLikes.value.has(String(game.value.id));
})

const isInWishlist = computed(() => {
  const id = gameIdentifier.value
  return id && userWishlist.value.has(String(id))
})

// Safe property accessors with IGDB sizing
const gameImageUrl = createReactiveImageUrl(
  computed(() => game.value?.primaryImageUrl),
  FALLBACK_TYPES.GAME,
  IMAGE_CONTEXTS.GAME_DETAIL_MAIN
)

const gameRating = computed(() => {
  try {
    return game.value?.rating || 0
  } catch (error) {
    console.warn('Error getting game rating:', error)
    return 0
  }
})

const developersText = computed(() => {
  try {
    return game.value?.allDevelopers || 'Unknown'
  } catch (error) {
    console.warn('Error getting developers text:', error)
    return 'Unknown'
  }
})

const genresText = computed(() => {
  try {
    return game.value?.allGenres || 'Not specified'
  } catch (error) {
    console.warn('Error getting genres text:', error)
    return 'Not specified'
  }
})

const releaseDateText = computed(() => {
  try {
    return game.value?.formattedReleaseDate || 'TBA'
  } catch (error) {
    console.warn('Error getting release date text:', error)
    return 'TBA'
  }
})

const isNewRelease = computed(() => {
  try {
    return game.value?.isNewRelease || false
  } catch (error) {
    console.warn('Error getting new release status:', error)
    return false
  }
})

const screenshotUrls = computed(() => {
  try {
    if (!game.value?.screenshots) return []
    const screenshots = game.value.screenshots
    if (!Array.isArray(screenshots)) return []
    return screenshots.map(screenshot => {
      const url = screenshot.imageUrl || screenshot.url
      return url ? getImageUrl(url, FALLBACK_TYPES.GAME, IMAGE_CONTEXTS.SCREENSHOT_THUMBNAIL) : null
    }).filter(Boolean)
  } catch (error) {
    console.warn('Error getting screenshot URLs:', error)
    return []
  }
})

const platformText = computed(() => {
  try {
    const platforms = game.value?.platforms
    if (!Array.isArray(platforms) || platforms.length === 0) return ''

    if (platforms.length <= 2) {
      return platforms.map(p => p.name).join(', ')
    }

    return `${platforms[0].name} (+${platforms.length - 1} more)`
  } catch (error) {
    console.warn('Error getting platform text:', error)
    return ''
  }
})

const gameModeText = computed(() => {
  try {
    const modes = game.value?.gameModes
    if (!Array.isArray(modes) || modes.length === 0) return ''
    return modes.map(m => m.name).join(', ')
  } catch (error) {
    console.warn('Error getting game mode text:', error)
    return ''
  }
})

// Safe list accessors for template loops
const genresList = computed(() => {
  try {
    const genres = game.value?.genres
    return Array.isArray(genres) ? genres : []
  } catch (error) {
    console.warn('Error getting genres list:', error)
    return []
  }
})

const platformsList = computed(() => {
  try {
    const platforms = game.value?.platforms
    return Array.isArray(platforms) ? platforms : []
  } catch (error) {
    console.warn('Error getting platforms list:', error)
    return []
  }
})

const gameModesList = computed(() => {
  try {
    const modes = game.value?.gameModes
    return Array.isArray(modes) ? modes : []
  } catch (error) {
    console.warn('Error getting game modes list:', error)
    return []
  }
})

const ageRatingsList = computed(() => {
  try {
    const ratings = game.value?.ageRatings
    return Array.isArray(ratings) ? ratings : []
  } catch (error) {
    console.warn('Error getting age ratings list:', error)
    return []
  }
})

const franchisesList = computed(() => {
  try {
    const franchises = game.value?.franchises
    return Array.isArray(franchises) ? franchises : []
  } catch (error) {
    console.warn('Error getting franchises list:', error)
    return []
  }
})

// const hasEngagementStats = computed(() => {
//   try {
//     return !!game.value
//   } catch (error) {
//     console.warn('Error checking engagement stats:', error)
//     return false
//   }
// })

// Helper methods
const formatNumber = (num) => {
  if (num >= 1000000) return (num / 1000000).toFixed(1) + 'M'
  if (num >= 1000) return (num / 1000).toFixed(1) + 'K'
  return num.toString()
}

// Image error handler is now provided by the composable

const getSimilarGameImage = (similarGame) => {
  try {
    return getImageUrl(similarGame.primaryImageUrl, FALLBACK_TYPES.GAME_ICON, IMAGE_CONTEXTS.SIMILAR_GAME)
  } catch (error) {
    return getImageUrl(null, FALLBACK_TYPES.GAME_ICON, IMAGE_CONTEXTS.SIMILAR_GAME)
  }
}

const getSimilarGameRating = (similarGame) => {
  try {
    return similarGame.rating || 0
  } catch (error) {
    return 0
  }
}

const getSimilarGameGenre = (similarGame) => {
  try {
    return similarGame.primaryGenre || 'Unknown'
  } catch (error) {
    return 'Unknown'
  }
}

// Methods
const loadGameDetails = async () => {
  const identifier = gameIdentifier.value

  if (!identifier) {
    loading.value = false
    error.value = 'No game identifier provided'
    return
  }

  loading.value = true
  error.value = ''

  try {
    console.log('Loading game details for:', identifier)
    const gameData = await gamesStore.fetchGameDetails(identifier)
    console.log('Game data received:', gameData)

    if (!gameData) {
      throw new Error('Game not found')
    }

    game.value = gameData

    // Initialize engagement counts if not present (handle undefined and null)
    if (typeof game.value.likesCount === 'undefined' || game.value.likesCount === null) {
      game.value.likesCount = 0
    }
    if (typeof game.value.favoritesCount === 'undefined' || game.value.favoritesCount === null) {
      game.value.favoritesCount = 0
    }

    // Load similar games if available
    if (game.value.id && game.value.similarGames && game.value.similarGames.length > 0) {
      await loadSimilarGames()
    }

    // Load user's favorites and likes status
    //TODO: create functions to check if game is liked and favorited by user to replace the two below
    // TODO: maybe use these in a different context like in the user's profile page
    await loadUserFavoritesStatus()
    await loadUserLikesStatus()

    // Load reviews and user's review
    await Promise.all([
      loadGameReviews(1),
      loadUserReview()
    ])

  } catch (err) {
    error.value = err.message || 'Failed to load game details'
    game.value = null
    console.error('Error loading game details:', err)
  } finally {
    loading.value = false
  }
}

const loadSimilarGames = async () => {
  try {
    if (!game.value || !gameIdentifier.value) return
    if (!game.value?.id) return

    const searchResults = await gamesStore.fetchSimilarGames(game.value.id)
    similarGames.value = searchResults.filter(g => g.id !== gameIdentifier.value)
  } catch (err) {
    console.error('Error loading similar games:', err)
  }
}

const loadUserFavoritesStatus = async () => {
  try {
    const userId = authStore.user.id;
    if (!userId) return;

    const userFavoritesResult = await gamesStore.getUserFavorites(userId);
    userFavorites.value = new Set(userFavoritesResult.map(game=>String(game.igdbId)));
  } catch (err) {
    console.error('Error loading user favorites status:', err)
  }
}

const loadUserLikesStatus = async () => {
  try {
    const userId = authStore.user.id;
    if (!userId || !game.value?.id) return;

    // Check if the current game is liked by the user
    const isLiked = await socialService.isGameLiked(game.value.id);
    if (isLiked) {
      userLikes.value.add(String(game.value.id));
    } else {
      userLikes.value.delete(String(game.value.id));
    }
  } catch (err) {
    console.error('Error loading user likes status:', err)
  }
}

const toggleFavorites = async () => {
  if (!game.value?.id) return
  if (requireAuth(authStore.isAuthenticated, 'Please sign in to add games to your favorites')) {
    return
  }
  if (isProcessingFavorites.value) return  // prevent multiple clicks

  //const userId = authStore.user.id
  const gameId = String(game.value.id)
  const wasInFavorites = isInFavorites.value // Capture current state before API call

  try {
    isProcessingFavorites.value = true
    let result

    if (wasInFavorites) {
      // remove from favorites
      result = await gamesStore.removeFromFavorites(game.value.id);
      // Only update state after successful API call
      userFavorites.value.delete(gameId);
      // Update favorites count in game object
      if (game.value) {
        game.value.favoritesCount = Math.max(0, (game.value.favoritesCount || 0) - 1)
      }
      toast.success(`Removed "${game.value.name}" from your favorites!`)
      console.log("The game removed from favorites: ", result);
    } else {
      // add to favorites
      result = await gamesStore.addToFavorites(game.value.id);
      // Only update state after successful API call
      userFavorites.value.add(gameId);
      // Update favorites count in game object
      if (game.value) {
        game.value.favoritesCount = (game.value.favoritesCount || 0) + 1
      }
      toast.success(`Added "${game.value.name}" to your favorites!`)
      console.log("The game added to favorites: ", result)
    }
  } catch (err) {
    // State remains unchanged on error, so button shows correct state
    toast.error(`Failed to ${wasInFavorites ? 'remove from' : 'add to'} favorites`)
    console.error('Toggle favorites error:', err)
  } finally {
    isProcessingFavorites.value = false
  }
}

const toggleLike = async () => {
  if (!game.value?.id) return
  if (requireAuth(authStore.isAuthenticated, 'Please sign in to like games')) {
    return
  }
  if (isProcessingLikes.value) return  // prevent multiple clicks

  // const userId = authStore.user.id
  const gameId = String(game.value.id)
  const wasInLikes = isInLikes.value // Capture current state before API call

  try {
    isProcessingLikes.value = true
    let result

    if (wasInLikes) {
      // remove from likes
      result = await gamesStore.removeFromLikes(game.value.id)
      // Only update state after successful API call
      userLikes.value.delete(gameId)
      // Update like count in game object
      if (game.value) {
        game.value.likesCount = Math.max(0, (game.value.likesCount || 0) - 1)
      }
      toast.success(`Removed "${game.value.name}" from your likes!`)
      console.log("The game removed from likes: ", result)
    } else {
      // add to likes
      result = await gamesStore.addToLikes(game.value.id)
      // Only update state after successful API call
      userLikes.value.add(gameId)
      // Update like count in game object
      if (game.value) {
        game.value.likesCount = (game.value.likesCount || 0) + 1
      }
      toast.success(`Added "${game.value.name}" to your likes!`)
      console.log("The game added to likes: ", result)
    }
  } catch (err) {
    // State remains unchanged on error, so button shows correct state
    toast.error(`Failed to ${wasInLikes ? 'remove from' : 'add to'} likes`)
    console.error('Toggle likes error:', err)
  } finally {
    isProcessingLikes.value = false
  }
}

const toggleWishlist = async () => {
  if (!gameIdentifier.value) return
  try {
    const identifier = String(gameIdentifier.value)
    if (isInWishlist.value) {
      userWishlist.value.delete(identifier)
      toast.success(`Removed "${game.value.name}" from wishlist`)
    } else {
      userWishlist.value.add(identifier)
      toast.success(`Added "${game.value.name}" to wishlist`)
    }

    localStorage.setItem('userWishlist', JSON.stringify([...userWishlist.value]))
  } catch (err) {
    toast.error('Failed to update wishlist')
    console.error('Wishlist error:', err)
  }
}

// Review Methods
const loadGameReviews = async (page = 1, append = false) => {
  if (!game.value?.id) return

  try {
    if (page === 1) {
      loadingReviews.value = true
      gameReviews.value = []
    } else {
      loadingMoreReviews.value = true
    }

    const response = await reviewsService.getGameReviews(game.value.id, page, 5)
    const reviewsWithCommentCounts = await commentsService.loadCommentCountsForReviews(response.data)

    if (append && page > 1) {
      gameReviews.value.push(...reviewsWithCommentCounts)
    } else {
      gameReviews.value = reviewsWithCommentCounts
    }

    hasMoreReviews.value = response.hasNextPage
    reviewsPage.value = page

    // Load like status for each review if user is authenticated
    if (authStore.user) {
      if (page === 1) {
        likedReviews.value.clear()
      }
      const likeStatusPromises = reviewsWithCommentCounts.map(async (review) => {
        try {
          const isLiked = await socialService.isReviewLiked(review.id)
          if (isLiked) {
            likedReviews.value.add(review.id)
          }
        } catch (error) {
          console.warn(`Failed to check like status for review ${review.id}:`, error)
        }
      })
      await Promise.all(likeStatusPromises)
    }
  } catch (error) {
    console.error('Error loading game reviews:', error)
    toast.error('Failed to load reviews')
  } finally {
    loadingReviews.value = false
    loadingMoreReviews.value = false
  }
}

const loadUserReview = async () => {
  if (!game.value?.id || !authStore.user?.id) return

  try {
    const review = await reviewsService.getUserGameReview(authStore.user.id, game.value.id)
    userReview.value = review

    // Load like status for user's review if it exists
    if (review) {
      try {
        const isLiked = await socialService.isReviewLiked(review.id)
        if (isLiked) {
          likedReviews.value.add(review.id)
        }
      } catch (error) {
        console.warn(`Failed to check like status for user review ${review.id}:`, error)
      }
    }
  } catch (error) {
    // User hasn't reviewed this game, which is fine
    userReview.value = null
  }
}

const submitReview = async (reviewData) => {
  if (!game.value?.id || !authStore.user?.id) return

  try {
    isSubmittingReview.value = true

    let result
    if (userReview.value) {
      // Update existing review
      result = await reviewsService.updateReview(userReview.value.id, reviewData)
      toast.success('Review updated successfully!')
    } else {
      // Add new review
      reviewData.gameId = game.value.id
      result = await reviewsService.addReview(reviewData)
      toast.success('Review added successfully!')
    }

    // Update local state
    userReview.value = result
    showReviewForm.value = false

    // Reload reviews and game stats
    await Promise.all([
      loadGameReviews(1),
      reloadGameDetails()
    ])
  } catch (error) {
    console.error('Error submitting review:', error)
    toast.error(error.message || 'Failed to submit review')
  } finally {
    isSubmittingReview.value = false
  }
}

const deleteUserReview = async () => {
  if (!userReview.value?.id) return

  try {
    await reviewsService.deleteReview(userReview.value.id)
    toast.success('Review deleted successfully!')

    // Update local state
    userReview.value = null
    showReviewForm.value = false

    // Reload reviews and game stats
    await Promise.all([
      loadGameReviews(1),
      reloadGameDetails()
    ])
  } catch (error) {
    console.error('Error deleting review:', error)
    toast.error('Failed to delete review')
  }
}

const handleToggleLike = async (review) => {
  if (requireAuth(authStore.isAuthenticated, 'Please sign in to like reviews')) {
    return
  }

  const reviewId = review.id
  const wasLiked = likedReviews.value.has(reviewId)

  if (processingLikeReviews.value.has(reviewId)) return

  try {
    processingLikeReviews.value.add(reviewId)

    if (wasLiked) {
      await socialService.unlikeReview(reviewId)
      likedReviews.value.delete(reviewId)
    } else {
      await socialService.likeReview(reviewId)
      likedReviews.value.add(reviewId)
    }

    // Update like count in review (both userReview and gameReviews)
    if (userReview.value?.id === reviewId) {
      userReview.value.likeCount = (userReview.value.likeCount || 0) + (wasLiked ? -1 : 1)
    }
    const targetReview = gameReviews.value.find(r => r.id === reviewId)
    if (targetReview) {
      targetReview.likeCount = (targetReview.likeCount || 0) + (wasLiked ? -1 : 1)
    }

  } catch (err) {
    console.error('Error toggling review like:', err)
    toast.error('Failed to update like')
  } finally {
    processingLikeReviews.value.delete(reviewId)
  }
}

const handleShowComments = (review) => {
  // Navigate to dedicated review details page
  router.push(`/reviews/${review.id}`)
}

const loadMoreReviews = async () => {
  await loadGameReviews(reviewsPage.value + 1, true)
}

const showAllReviews = () => {
  // Navigate to dedicated reviews page
  router.push(`/games/${game.value.id}/reviews`)
}

const editReview = (review) => {
  if (review.user.id === authStore.user?.id) {
    showReviewForm.value = true
  }
}

const deleteReview = (review) => {
  if (review.user.id === authStore.user?.id) {
    deleteUserReview()
  }
}

const reloadGameDetails = async () => {
  // Reload game details to get updated review stats
  try {
    const gameData = await gamesStore.fetchGameDetails(gameIdentifier.value)
    if (gameData) {
      game.value.reviewsCount = gameData.reviewsCount
      game.value.averageRating = gameData.averageRating
    }
  } catch (error) {
    console.error('Error reloading game details:', error)
  }
}

const viewGame = (id) => {
  router.push(`/games/${id}`)
}

const openImageModal = (imageUrl) => {
  // Use high-resolution version for modal
  selectedImage.value = getImageUrl(imageUrl, FALLBACK_TYPES.GAME, IMAGE_CONTEXTS.SCREENSHOT_MODAL)
}

const closeImageModal = () => {
  selectedImage.value = null
}

// Watchers
watch(() => route.params.identifier, async (newIdentifier, oldIdentifier) => {
  console.log('Route identifier changed:', { old: oldIdentifier, new: newIdentifier })

  if (newIdentifier && newIdentifier !== oldIdentifier) {
    game.value = null
    error.value = ''
    similarGames.value = []
    await loadGameDetails()
  }
}, { immediate: false })

// Lifecycle
onMounted(async () => {
  console.log('Component mounted')
  console.log('Route:', route)
  console.log('Route params:', route.params)
  console.log('Props:', { gameId: props.gameId, slug: props.slug })

  await nextTick()

  const identifier = gameIdentifier.value
  console.log('Final identifier on mount:', identifier)

  if (identifier) {
    await loadGameDetails()
  } else {
    setTimeout(async () => {
      const delayedIdentifier = gameIdentifier.value
      console.log('Delayed identifier check:', delayedIdentifier)

      if (delayedIdentifier) {
        await loadGameDetails()
      } else {
        loading.value = false
        error.value = 'No game identifier found'
      }
    }, 200)
  }
})
</script>

<style scoped>
.cursor-pointer {
  cursor: pointer;
}

.card {
  border-radius: 15px;
}

.card-header {
  border-radius: 15px 15px 0 0 !important;
}


.modal {
  background-color: rgba(0, 0, 0, 0.8);
}

.list-group-item:hover {
  background-color: #f8f9fa;
}

@media (max-width: 768px) {
  .container {
    padding: 1rem;
  }

  .card-body {
    padding: 1.5rem !important;
  }

  .d-flex.gap-2 {
    flex-direction: column;
  }

  .d-flex.gap-2 .btn {
    margin-bottom: 0.5rem;
  }

  .row.g-0 {
    flex-direction: column;
  }

  .col-md-4 img {
    border-radius: 15px 15px 0 0 !important;
    min-height: 200px !important;
  }
}
</style>