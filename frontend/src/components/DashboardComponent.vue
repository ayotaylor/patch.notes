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
        <div class="card shadow-sm border-0">
          <div class="card-header bg-white border-bottom">
            <h3 class="h5 mb-0 fw-bold">
              <i class="fas fa-search text-primary me-2"></i>
              Search Games
            </h3>
          </div>
          <div class="card-body p-4">
            <GameSearchComponent :show-card="false" :show-title="false" :show-results="true" :show-load-more="false"
              placeholder="Search for games by title, genre, or developer..." results-mode="compact"
              pagination-mode="infinite-scroll" max-height="400px" :auto-search="true" :debounce-ms="500"
              @select-game="handleGameSelect" @add-to-library="addToLibrary" />
          </div>
        </div>
      </div>
    </div>

    <!-- Game Sections -->
    <div class="row g-4">
      <!-- Popular Games -->
      <div class="col-12">
        <GameSection title="Popular Games" icon="fas fa-fire text-danger" :games="popularGames"
          :loading="gamesStore.loading" loading-message="Loading popular games..."
          empty-message="No popular games available right now." empty-icon="fas fa-gamepad" :max-games-to-show="10"
          @refresh="refreshPopularGames" @view-all="viewAllPopularGames" @game-click="handleGameClick"
          @view-likes="handleViewLikes" @view-lists="handleViewLists" @view-reviews="handleViewReviews" />
      </div>

      <!-- New Releases -->
      <div class="col-12">
        <GameSection title="New Releases" icon="fas fa-sparkles text-success" :games="newGames"
          :loading="gamesStore.loading" loading-message="Loading new releases..."
          empty-message="No new releases available right now." empty-icon="fas fa-calendar-plus" :max-games-to-show="10"
          @refresh="refreshNewGames" @view-all="viewAllNewGames" @game-click="handleGameClick"
          @view-likes="handleViewLikes" @view-lists="handleViewLists" @view-reviews="handleViewReviews" />
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
                <div v-for="review in latestReviews" :key="review.id" class="col-12"
                  :class="{ 'col-lg-6': latestReviews.length > 1 }">
                  <ReviewCard :review="review" :show-game="true" :truncated="true" :max-length="120"
                    :is-liked="likedReviews.has(review.id)" :is-processing-like="processingLikeReviews.has(review.id)"
                    @toggleLike="handleToggleLike" @showComments="handleShowComments" />
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
                <div v-for="list in latestLists.slice(0, 3)" :key="list.id" class="col-12"
                  :class="{ 'col-lg-6': latestLists.length > 1, 'col-xl-4': latestLists.length > 2 }">
                  <ListCard :list="list" :truncated="true" :max-length="150" :max-games-to-show="4"
                    :show-like-button="false" :show-comment-button="true" @edit="handleEditList"
                    @delete="handleDeleteList" @showComments="handleShowListComments" />
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
import GameSection from '@/components/GameSection.vue'
import ReviewCard from '@/components/ReviewCard.vue'
import ListCard from '@/components/ListCard.vue'
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

// New game section event handlers
const handleGameClick = (game) => {
  viewGameDetails(game.id)
}

const viewAllPopularGames = () => {
  // Navigate to a dedicated popular games page
  // For now, we can navigate to a general games page with a popular filter
  router.push('/games?filter=popular')
}

const viewAllNewGames = () => {
  // Navigate to a dedicated new releases page
  // For now, we can navigate to a general games page with a new filter
  router.push('/games?filter=new')
}

// Stat navigation handlers
const handleViewLikes = (game) => {
  // Navigate to game details page with likes section
  router.push({
    name: 'GameDetails',
    params: { identifier: String(game.id) },
    hash: '#likes'
  })
}

const handleViewLists = (game) => {
  // Navigate to game details page with lists section or a dedicated lists page
  router.push({
    name: 'GameDetails',
    params: { identifier: String(game.id) },
    hash: '#lists'
  })
}

const handleViewReviews = (game) => {
  // Navigate to game details page with reviews section
  router.push({
    name: 'GameDetails',
    params: { identifier: String(game.id) },
    hash: '#reviews'
  })
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


const loadLatestReviews = async () => {
  try {
    loadingReviews.value = true
    const reviews = await reviewsService.getLatestReviews()
    const reviewsWithCommentCounts = await commentsService.loadCommentCountsForReviews(reviews)
    latestReviews.value = reviewsWithCommentCounts

    // Load like status for each review if user is authenticated
    if (authStore.user) {
      likedReviews.value.clear()
      const likeStatusPromises = latestReviews.value.map(async (review) => {
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

const handleShowListComments = (list) => {
  // Navigate to the list details page where comments can be viewed
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

}
</style>