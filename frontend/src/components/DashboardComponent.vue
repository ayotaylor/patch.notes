<template>
  <div class="container-fluid py-4">
    <!-- Welcome Header -->
    <div class="row mb-4">
      <div class="col-12">
        <div class="card bg-primary text-white border-0 shadow-sm">
          <div class="card-body p-4">
            <div class="row align-items-center">
              <div class="col-md-8">
                <h1 class="h2 mb-2 fw-bold">
                  Welcome back, {{ authStore.userFullName || 'Gamer' }}! 🎮
                </h1>
                <p class="mb-0 opacity-75">
                  Discover new games, track your favorites, and connect with the gaming community.
                </p>
              </div>
              <div class="col-md-4 text-md-end">
                <div class="d-flex justify-content-md-end gap-3">
                  <div class="text-center">
                    <div class="h4 mb-0 fw-bold">{{ userStats.gamesCount || 0 }}</div>
                    <small class="opacity-75">Games</small>
                  </div>
                  <div class="text-center">
                    <div class="h4 mb-0 fw-bold">{{ userStats.achievementsCount || 0 }}</div>
                    <small class="opacity-75">Achievements</small>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Search Section -->
    <div class="row mb-4">
      <div class="col-12">
        <GameSearchComponent
          :show-card="true"
          :show-title="true"
          :show-results="true"
          :show-load-more="true"
          title="Search Games"
          placeholder="Search for games by title, genre, or developer..."
          results-mode="grid"
          pagination-mode="pages"
          :games-per-page="8"
          :auto-search="true"
          @select-game="handleGameSelect"
          @add-to-library="addToLibrary"
        />
      </div>
    </div>

    <!-- Main Content Grid -->
    <div class="row g-4">
      <!-- Popular Games -->
      <div class="col-lg-6">
        <div class="card shadow-sm border-0 h-100">
          <div class="card-header bg-white border-bottom">
            <div class="d-flex justify-content-between align-items-center">
              <h3 class="h5 mb-0 fw-bold">
                <i class="fas fa-fire text-danger me-2"></i>
                Popular Games
              </h3>
              <button
                @click="refreshPopularGames"
                :disabled="gamesStore.loading"
                class="btn btn-sm btn-outline-secondary"
              >
                <i class="fas fa-sync-alt" :class="{ 'fa-spin': gamesStore.loading }"></i>
              </button>
            </div>
          </div>
          <div class="card-body p-0">
            <!-- Loading State -->
            <div v-if="gamesStore.loading && popularGames.length === 0" class="text-center py-5">
              <div class="spinner-border text-primary mb-3"></div>
              <p class="text-muted">Loading popular games...</p>
            </div>

            <!-- Popular Games List -->
            <div v-else-if="popularGames.length > 0" class="list-group list-group-flush">
              <div
                v-for="(game, index) in popularGames.slice(0, 5)"
                :key="game.id"
                @click="() => viewGameDetails(game.id)"
                class="list-group-item list-group-item-action border-0 py-3 cursor-pointer"
              >
                <div class="d-flex align-items-center">
                  <!-- Rank -->
                  <div class="me-3">
                    <span class="badge bg-primary rounded-circle d-flex align-items-center justify-content-center" style="width: 30px; height: 30px;">
                      {{ index + 1 }}
                    </span>
                  </div>

                  <!-- Game Image -->
                  <div class="me-3">
                    <img
                      :src="getImageUrl(game.primaryImageUrl, FALLBACK_TYPES.GAME_ICON, IMAGE_CONTEXTS.GAME_CARD)"
                      :alt="game.name"
                      class="rounded"
                      style="width: 50px; height: 50px; object-fit: cover;"
                      @error="(e) => handleImageError(e, 'gameIcon')"
                    >
                  </div>

                  <!-- Game Info -->
                  <div class="flex-grow-1">
                    <h6 class="mb-1 fw-semibold">{{ game.name }}</h6>
                    <div class="d-flex align-items-center gap-3 small text-muted">
                      <span v-if="game.allGenres">
                        <i class="fas fa-tag me-1"></i>{{ game.primaryGenre }}
                      </span>
                      <span v-if="game.rating">
                        <i class="fas fa-star text-warning me-1"></i>{{ game.rating }}/5
                      </span>
                      <!-- <span v-if="game.players">
                        <i class="fas fa-users me-1"></i>{{ game.players }}
                      </span> -->
                    </div>
                  </div>

                  <!-- Action Button -->
                  <div class="ms-3">
                    <button
                      @click.stop="addToLibrary(game)"
                      class="btn btn-sm btn-outline-primary"
                      :disabled="isGameInLibrary(game.id)"
                    >
                      <i class="fas" :class="isGameInLibrary(game.id) ? 'fa-check' : 'fa-plus'"></i>
                    </button>
                  </div>
                </div>
              </div>
            </div>

            <!-- Empty State -->
            <div v-else class="text-center py-5">
              <i class="fas fa-gamepad text-muted mb-3" style="font-size: 2rem;"></i>
              <p class="text-muted">No popular games available right now.</p>
            </div>
          </div>
        </div>
      </div>

      <!-- New Games -->
      <div class="col-lg-6">
        <div class="card shadow-sm border-0 h-100">
          <div class="card-header bg-white border-bottom">
            <div class="d-flex justify-content-between align-items-center">
              <h3 class="h5 mb-0 fw-bold">
                <i class="fas fa-sparkles text-success me-2"></i>
                New Releases
              </h3>
              <button
                @click="refreshNewGames"
                :disabled="gamesStore.loading"
                class="btn btn-sm btn-outline-secondary"
              >
                <i class="fas fa-sync-alt" :class="{ 'fa-spin': gamesStore.loading }"></i>
              </button>
            </div>
          </div>
          <div class="card-body p-0">
            <!-- Loading State -->
            <div v-if="gamesStore.loading && newGames.length === 0" class="text-center py-5">
              <div class="spinner-border text-primary mb-3"></div>
              <p class="text-muted">Loading new games...</p>
            </div>

            <!-- New Games Grid -->
            <div v-else-if="newGames.length > 0" class="row g-2 p-3">
              <div
                v-for="game in newGames.slice(0, 4)"
                :key="game.id"
                class="col-6"
              >
                <div
                  @click="() => viewGameDetails(game.id)"
                  class="card border-0 bg-light cursor-pointer game-hover-card"
                >
                  <img
                    :src="getImageUrl(game.primaryImageUrl, FALLBACK_TYPES.GAME_SMALL, IMAGE_CONTEXTS.GAME_CARD)"
                    :alt="game.name"
                    class="card-img-top"
                    style="height: 120px; object-fit: cover;"
                    @error="(e) => handleImageError(e, 'gameSmall')"
                  >
                  <div class="card-body p-2">
                    <h6 class="card-title mb-1 fw-semibold small">{{ game.name }}</h6>
                    <p class="card-text small text-muted mb-2">{{ game.primaryGenre }}</p>
                    <div class="d-flex justify-content-between align-items-center">
                      <small class="text-muted">
                        <i class="fas fa-calendar me-1"></i>
                        {{ formatReleaseDate(game.firstReleaseDate) }}
                      </small>
                      <button
                        @click.stop="addToLibrary(game)"
                        class="btn btn-sm btn-outline-primary"
                        :disabled="isGameInLibrary(game.id)"
                      >
                        <i class="fas" :class="isGameInLibrary(game.id) ? 'fa-check' : 'fa-plus'"></i>
                      </button>
                    </div>
                  </div>
                </div>
              </div>
            </div>

            <!-- Empty State -->
            <div v-else class="text-center py-5">
              <i class="fas fa-calendar-plus text-muted mb-3" style="font-size: 2rem;"></i>
              <p class="text-muted">No new games available right now.</p>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Latest Reviews -->
    <div class="row mt-4">
      <div class="col-12">
        <div class="card shadow-sm border-0">
          <div class="card-header bg-white border-bottom">
            <div class="d-flex justify-content-between align-items-center">
              <h3 class="h5 mb-0 fw-bold">
                <i class="fas fa-comments text-primary me-2"></i>
                Latest Reviews
              </h3>
              <router-link to="/reviews" class="btn btn-sm btn-outline-primary">
                View All Reviews
                <i class="fas fa-arrow-right ms-2"></i>
              </router-link>
            </div>
          </div>
          <div class="card-body p-4">
            <!-- Loading State -->
            <div v-if="loadingReviews" class="text-center py-5">
              <div class="spinner-border text-primary mb-3"></div>
              <p class="text-muted">Loading latest reviews...</p>
            </div>

            <!-- Reviews List -->
            <div v-else-if="latestReviews.length > 0">
              <div class="row g-3">
                <div
                  v-for="review in latestReviews"
                  :key="review.id"
                  class="col-12"
                  :class="{ 'col-lg-6': latestReviews.length > 1 }"
                >
                  <ReviewCard
                    :review="review"
                    :show-game="true"
                    :truncated="true"
                    :max-length="120"
                    :is-liked="likedReviews.has(review.id)"
                    :is-processing-like="processingLikeReviews.has(review.id)"
                    @toggleLike="handleToggleLike"
                    @showComments="handleShowComments"
                  />
                </div>
              </div>
            </div>

            <!-- Empty State -->
            <div v-else class="text-center py-5">
              <i class="fas fa-star text-muted mb-3" style="font-size: 2rem;"></i>
              <p class="text-muted">No reviews available yet.</p>
              <p class="text-muted small">Be the first to share your gaming thoughts!</p>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Latest Lists -->
    <div class="row mt-4">
      <div class="col-12">
        <div class="card shadow-sm border-0">
          <div class="card-header bg-white border-bottom">
            <div class="d-flex justify-content-between align-items-center">
              <h3 class="h5 mb-0 fw-bold">
                <i class="fas fa-list text-success me-2"></i>
                Latest Lists
              </h3>
              <router-link to="/lists" class="btn btn-sm btn-outline-primary">
                View All Lists
                <i class="fas fa-arrow-right ms-2"></i>
              </router-link>
            </div>
          </div>
          <div class="card-body p-4">
            <!-- Loading State -->
            <div v-if="loadingLists" class="text-center py-5">
              <div class="spinner-border text-primary mb-3"></div>
              <p class="text-muted">Loading latest lists...</p>
            </div>

            <!-- Lists Grid -->
            <div v-else-if="latestLists.length > 0">
              <div class="row g-3">
                <div
                  v-for="list in latestLists.slice(0, 3)"
                  :key="list.id"
                  class="col-12"
                  :class="{ 'col-lg-6': latestLists.length > 1, 'col-xl-4': latestLists.length > 2 }"
                >
                  <ListCard
                    :list="list"
                    :truncated="true"
                    :max-length="150"
                    :max-games-to-show="4"
                    :show-like-button="false"
                    @edit="handleEditList"
                    @delete="handleDeleteList"
                  />
                </div>
              </div>
            </div>

            <!-- Empty State -->
            <div v-else class="text-center py-5">
              <i class="fas fa-list-ul text-muted mb-3" style="font-size: 2rem;"></i>
              <p class="text-muted">No public lists available yet.</p>
              <p class="text-muted small">Create the first list and share your favorite games!</p>
              <router-link to="/lists/create" class="btn btn-primary mt-2">
                <i class="fas fa-plus me-2"></i>
                Create List
              </router-link>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Quick Actions -->
    <div class="row mt-4">
      <div class="col-12">
        <div class="card shadow-sm border-0">
          <div class="card-body p-4">
            <h5 class="fw-bold mb-3">Quick Actions</h5>
            <div class="row g-3">
              <div class="col-md-3">
                <router-link to="/profile" class="btn btn-outline-primary w-100">
                  <i class="fas fa-user me-2"></i>
                  View Profile
                </router-link>
              </div>
              <div class="col-md-3">
                <button @click="viewLibrary" class="btn btn-outline-success w-100">
                  <i class="fas fa-book me-2"></i>
                  My Library
                </button>
              </div>
              <div class="col-md-3">
                <button @click="viewAchievements" class="btn btn-outline-warning w-100">
                  <i class="fas fa-trophy me-2"></i>
                  Achievements
                </button>
              </div>
              <div class="col-md-3">
                <button @click="viewFriends" class="btn btn-outline-info w-100">
                  <i class="fas fa-users me-2"></i>
                  Friends
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Error Alert -->
    <div v-if="error" class="alert alert-danger mt-3" role="alert">
      <i class="fas fa-exclamation-triangle me-2"></i>
      {{ error }}
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onBeforeUnmount } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/authStore'
import { useGamesStore } from '@/stores/gamesStore'
import { useProfileStore } from '@/stores/profileStore'
import { useToast } from 'vue-toastification'
import GameSearchComponent from '@/components/GameSearchComponent.vue'
import ReviewCard from '@/components/ReviewCard.vue'
import ListCard from '@/components/ListCard.vue'
import { useImageFallback, FALLBACK_TYPES } from '@/composables/useImageFallback'
import { reviewsService } from '@/services/reviewsService'
import { socialService } from '@/services/socialService'
import { commentsService } from '@/services/commentsService'
import { listsService } from '@/services/listsService'

// Composables
const router = useRouter()
const authStore = useAuthStore()
const gamesStore = useGamesStore()
const profileStore = useProfileStore()
const toast = useToast()
const { handleImageError, getImageUrl, IMAGE_CONTEXTS } = useImageFallback()

// State
const error = ref('')
const userStats = ref({
  gamesCount: 0,
  achievementsCount: 0,
  totalPlayTime: '0h'
})

// User library for checking if games are already added
const userLibrary = ref(new Set())

// Reviews state
const latestReviews = ref([])
const loadingReviews = ref(false)
const likedReviews = ref(new Set())
const processingLikeReviews = ref(new Set())

// Lists state
const latestLists = ref([])
const loadingLists = ref(false)

// Computed
const popularGames = computed(() => gamesStore.popularGames)
const newGames = computed(() => gamesStore.newGames || [])

// Methods
const handleGameSelect = (game) => {
  viewGameDetails(game.id)
}

const viewGameDetails = (gameId) => {
  console.log('Navigating to game:', gameId)

  // Ensure we have a valid ID
  if (!gameId) {
    console.error('No game ID provided for navigation')
    return
  }

  // Navigate using router.push with proper identifier
  router.push({
    name: 'GameDetails',
    params: { identifier: String(gameId) }
  })
}

// TODO: Implement this function to add game to user's library
const addToLibrary = async (game) => {
  try {
    // In a real app, you'd call an API to add the game to user's library
    // await userLibraryService.addGame(game.id)

    userLibrary.value.add(game.id)
    toast.success(`Added "${game.name}" to your library!`)
  } catch (err) {
    toast.error('Failed to add game to library')
    console.error('Add to library error:', err)
  }
}

const isGameInLibrary = (gameId) => {
  return userLibrary.value.has(gameId)
}

const refreshPopularGames = async () => {
  try {
    await gamesStore.fetchPopularGames(10)
  } catch (err) {
    error.value = 'Failed to refresh popular games'
    console.error('Refresh popular games error:', err)
  }
}

const refreshNewGames = async () => {
  try {
    await gamesStore.fetchNewGames(8)
  } catch (err) {
    error.value = 'Failed to refresh new games'
    console.error('Refresh new games error:', err)
  }
}

const loadUserStats = async () => {
  try {
    if (authStore.user?.id) {
      const profile = await profileStore.fetchProfile()
      userStats.value = {
        gamesCount: profile.gamesCount || 0,
        achievementsCount: profile.achievementsCount || 0,
        totalPlayTime: profile.totalPlayTime || '0h'
      }
    }
  } catch (err) {
    console.error('Error loading user stats:', err)
  }
}

const formatReleaseDate = (dateString) => {
  if (!dateString) return 'TBA'
  const date = new Date(dateString)
  return date.toLocaleDateString('en-US', { month: 'short', year: 'numeric' })
}

const loadLatestReviews = async () => {
  try {
    loadingReviews.value = true
    const reviews = await reviewsService.getLatestReviews()
    const reviewsWithCommentCounts = await commentsService.loadCommentCountsForReviews(reviews)
    latestReviews.value = reviewsWithCommentCounts
  } catch (error) {
    console.error('Error loading latest reviews:', error)
    toast.error('Failed to load latest reviews')
  } finally {
    loadingReviews.value = false
  }
}

const loadLatestLists = async () => {
  try {
    loadingLists.value = true
    const response = await listsService.getPublicLists(1, 6) // Get 6 latest public lists
    latestLists.value = response.data || []
  } catch (error) {
    console.error('Error loading latest lists:', error)
    toast.error('Failed to load latest lists')
  } finally {
    loadingLists.value = false
  }
}

const handleToggleLike = async (review) => {
  if (!authStore.user) {
    toast.info('Please sign in to like reviews')
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

    // Update like count in review
    const targetReview = latestReviews.value.find(r => r.id === reviewId)
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

// List handlers
const handleEditList = (list) => {
  router.push(`/lists/${list.id}/edit`)
}

const handleDeleteList = (list) => {
  // This would typically show a confirmation dialog
  // For now, just navigate to the list page
  router.push(`/lists/${list.id}`)
}

// Quick action methods
const viewLibrary = () => {
  router.push('/library')
}

const viewAchievements = () => {
  router.push('/achievements')
}

const viewFriends = () => {
  router.push('/friends')
}

// Watchers
// Search is now handled by GameSearchComponent

// Lifecycle
onMounted(async () => {
  try {
    // Load initial data
    await Promise.all([
      gamesStore.fetchPopularGames(10),
      gamesStore.fetchNewGames(8),
      loadUserStats(),
      loadLatestReviews(),
      loadLatestLists()
    ])
  } catch (err) {
    error.value = 'Failed to load dashboard data'
    console.error('Dashboard loading error:', err)
  }
})

// Clear search results when leaving the dashboard page
onBeforeUnmount(() => {
  gamesStore.clearSearchResults()
})
</script>

<style scoped>
.cursor-pointer {
  cursor: pointer;
}

.game-hover-card {
  transition: transform 0.2s ease, box-shadow 0.2s ease;
}

.game-hover-card:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
}

.list-group-item:hover {
  background-color: #f8f9fa;
}

.input-group-lg .form-control {
  font-size: 1rem;
}

.card {
  border-radius: 15px;
}

.card-header {
  border-radius: 15px 15px 0 0 !important;
}

@media (max-width: 768px) {
  .container-fluid {
    padding: 1rem;
  }

  .card-body {
    padding: 1.5rem !important;
  }

  .input-group-lg {
    flex-direction: column;
  }

  .input-group-lg .btn {
    border-radius: 0.375rem !important;
    margin-top: 0.5rem;
  }
}
</style>