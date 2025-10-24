<script setup>
import { ref, computed, onMounted, watch } from 'vue'
import { useRoute } from 'vue-router'
import { useGamesStore } from '@/stores/gamesStore'
import { useAuthStore } from '@/stores/authStore'
import { useToast } from 'vue-toastification'
import { useImageFallback, FALLBACK_TYPES } from '@/composables/useImageFallback'
import { socialService } from '@/services/socialService'
import ActionsPanel from './ActionsPanel.vue'
import RatingsDistribution from './RatingsDistribution.vue'
import HomeHeader from './HeaderBar.vue'
import HomeNavigation from './NavigationBar.vue'

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
const gamesStore = useGamesStore()
const authStore = useAuthStore()
const toast = useToast()
const { handleImageError, createReactiveImageUrl, IMAGE_CONTEXTS } = useImageFallback()

// State
const game = ref(null)
const loading = ref(true)
const error = ref('')
const isProcessingLike = ref(false)
const isProcessingWishlist = ref(false)
const isLiked = ref(false)
const isInWishlist = ref(false)

// Placeholder state for features to be implemented
const popularReviews = ref([])
const recentReviews = ref([])
const listsWithGame = ref([])
const ratingsDistribution = ref([
  { stars: 5, count: 0 },
  { stars: 4.5, count: 0 },
  { stars: 4, count: 0 },
  { stars: 3.5, count: 0 },
  { stars: 3, count: 0 },
  { stars: 2.5, count: 0 },
  { stars: 2, count: 0 },
  { stars: 1.5, count: 0 },
  { stars: 1, count: 0 },
  { stars: 0.5, count: 0 }
])

// Computed properties
const gameIdentifier = computed(() => {
  return props.gameId || props.slug || route.params.identifier || null
})

const gameImageUrl = createReactiveImageUrl(
  computed(() => game.value?.primaryImageUrl),
  FALLBACK_TYPES.GAME,
  IMAGE_CONTEXTS.GAME_DETAIL_MAIN
)

const gameName = computed(() => game.value?.name || 'Unknown Game')
const gameYear = computed(() => game.value?.firstReleaseDate ? new Date(game.value.firstReleaseDate).getFullYear() : '')
const gameDeveloper = computed(() => game.value?.allDevelopers || 'Unknown')
const gameSummary = computed(() => game.value?.summary || 'No summary available')
const gameGenres = computed(() => {
  if (!game.value?.genres || !Array.isArray(game.value.genres)) return []
  return game.value.genres.map(g => g.name || g)
})
const gameReleaseDate = computed(() => game.value?.formattedReleaseDate || 'TBA')
const gameModesText = computed(() => {
  if (!game.value?.gameModes || !Array.isArray(game.value.gameModes)) return ''
  return game.value.gameModes.map(m => m.name || m).join(', ')
})
const gamePlatformsText = computed(() => {
  if (!game.value?.platforms || !Array.isArray(game.value.platforms)) return ''
  const platforms = game.value.platforms
  if (platforms.length <= 3) {
    return platforms.map(p => p.abbreviation || p.name || p).join(', ')
  }
  return `${platforms[0].abbreviation || platforms[0].name}, ${platforms[1].abbreviation || platforms[1].name} +${platforms.length - 2} more`
})
const isNewRelease = computed(() => game.value?.isNewRelease || false)
const likeCount = computed(() => game.value?.likesCount || 0)
const listCount = computed(() => 0) // Placeholder until API is ready
const averageRating = computed(() => game.value?.averageRating || 0)
const totalRatings = computed(() => game.value?.reviewsCount || 0)

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

    // Initialize engagement counts if not present
    if (typeof game.value.likesCount === 'undefined' || game.value.likesCount === null) {
      game.value.likesCount = 0
    }

    // Load user's like status if authenticated
    if (authStore.isAuthenticated && game.value.id) {
      await loadLikeStatus()
    }

    // Load wishlist status from localStorage
    loadWishlistStatus()

    // TODO: Load popular reviews, recent reviews, and lists when endpoints are ready
  } catch (err) {
    error.value = err.message || 'Failed to load game details'
    game.value = null
    console.error('Error loading game details:', err)
  } finally {
    loading.value = false
  }
}

const loadLikeStatus = async () => {
  if (!game.value?.id) return

  try {
    const liked = await socialService.isGameLiked(game.value.id)
    isLiked.value = liked
  } catch (err) {
    console.error('Error loading like status:', err)
  }
}

const loadWishlistStatus = () => {
  if (!gameIdentifier.value) return

  try {
    const wishlist = JSON.parse(localStorage.getItem('userWishlist') || '[]')
    isInWishlist.value = wishlist.includes(String(gameIdentifier.value))
  } catch (err) {
    console.error('Error loading wishlist status:', err)
  }
}

const handleLike = async () => {
  if (!authStore.isAuthenticated) {
    toast.warning('Please sign in to like games')
    return
  }

  if (!game.value?.id || isProcessingLike.value) return

  isProcessingLike.value = true
  const wasLiked = isLiked.value

  try {
    if (wasLiked) {
      await socialService.removeGameLike(game.value.id)
      isLiked.value = false
      if (game.value) {
        game.value.likesCount = Math.max(0, (game.value.likesCount || 0) - 1)
      }
      toast.success('Removed from likes')
    } else {
      await socialService.likeGame(game.value.id)
      isLiked.value = true
      if (game.value) {
        game.value.likesCount = (game.value.likesCount || 0) + 1
      }
      toast.success('Added to likes')
    }
  } catch (err) {
    console.error('Error toggling like:', err)
    toast.error('Failed to update like status')
  } finally {
    isProcessingLike.value = false
  }
}

const handleWishlist = async () => {
  if (!gameIdentifier.value || isProcessingWishlist.value) return

  isProcessingWishlist.value = true

  try {
    const wishlist = JSON.parse(localStorage.getItem('userWishlist') || '[]')
    const identifier = String(gameIdentifier.value)

    if (isInWishlist.value) {
      const index = wishlist.indexOf(identifier)
      if (index > -1) {
        wishlist.splice(index, 1)
      }
      isInWishlist.value = false
      toast.success('Removed from wishlist')
    } else {
      wishlist.push(identifier)
      isInWishlist.value = true
      toast.success('Added to wishlist')
    }

    localStorage.setItem('userWishlist', JSON.stringify(wishlist))
  } catch (err) {
    console.error('Error toggling wishlist:', err)
    toast.error('Failed to update wishlist')
  } finally {
    isProcessingWishlist.value = false
  }
}

const handleRate = () => {
  // TODO: Open rate modal
  toast.info('Rate feature coming soon')
}

const handleReview = () => {
  // TODO: Open review modal
  toast.info('Review feature coming soon')
}

const handleAddToList = () => {
  // TODO: Open add to list modal
  toast.info('Add to list feature coming soon')
}

// Watchers
watch(() => route.params.identifier, async (newIdentifier, oldIdentifier) => {
  if (newIdentifier && newIdentifier !== oldIdentifier) {
    game.value = null
    error.value = ''
    await loadGameDetails()
  }
}, { immediate: false })

// Lifecycle
onMounted(async () => {
  const identifier = gameIdentifier.value

  if (identifier) {
    await loadGameDetails()
  } else {
    loading.value = false
    error.value = 'No game identifier found'
  }
})
</script>

<template>
  <div class="min-h-screen bg-[#F6F7F7]">
    <!-- Header Component -->
    <HomeHeader />

    <!-- Navigation Component -->
    <HomeNavigation />

    <!-- Loading State -->
    <div v-if="loading" class="flex justify-center px-4 md:px-8 lg:px-40 mt-8">
      <div class="w-full max-w-1280">
        <div class="animate-pulse">
          <div class="bg-gray-300 rounded-lg h-96 mb-4"></div>
          <div class="h-8 bg-gray-300 rounded w-3/4 mb-2"></div>
          <div class="h-6 bg-gray-300 rounded w-1/2"></div>
        </div>
      </div>
    </div>

    <!-- Error State -->
    <div v-else-if="error" class="flex justify-center px-4 md:px-8 lg:px-40 mt-8">
      <div class="w-full max-w-1280">
        <div class="bg-white rounded-lg p-8 text-center">
          <p class="text-red-600 font-tinos text-lg mb-4">{{ error }}</p>
          <button
            @click="loadGameDetails"
            class="px-6 py-2 bg-cod-gray text-white font-tinos rounded hover:bg-opacity-90"
          >
            Try Again
          </button>
        </div>
      </div>
    </div>

    <!-- Game Details Content -->
    <div v-else-if="game" class="flex justify-center px-4 md:px-8 lg:px-40 mt-8 pb-16">
      <div class="w-full max-w-1280">
        <div class="flex flex-col lg:flex-row gap-6">
          <!-- Column 1: 25% - Game Image and Stats (sticky) -->
          <div class="lg:w-1/4">
            <div class="lg:sticky lg:top-4">
              <!-- Large Game Image -->
              <img
                :src="gameImageUrl"
                :alt="gameName"
                class="w-full rounded-lg mb-4"
                @error="(e) => handleImageError(e, 'game')"
              />

              <!-- Engagement Stats -->
              <div class="bg-white rounded-lg p-4 mb-4">
                <div class="flex justify-around text-center">
                  <div>
                    <div class="font-newsreader text-2xl font-bold text-cod-gray">{{ likeCount }}</div>
                    <div class="font-tinos text-sm text-gray-500">Likes</div>
                  </div>
                  <div>
                    <div class="font-newsreader text-2xl font-bold text-cod-gray">{{ listCount }}</div>
                    <div class="font-tinos text-sm text-gray-500">Lists</div>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <!-- Column 2: 50% - Main Content -->
          <div class="lg:w-1/2">
            <!-- Row 1: Game Title and Developer -->
            <div class="mb-4">
              <h1 class="font-newsreader text-4xl font-bold text-cod-gray mb-2">
                {{ gameName }} <span v-if="gameYear" class="text-gray-500">({{ gameYear }})</span>
              </h1>
              <p class="font-tinos text-lg text-gray-600">{{ gameDeveloper }}</p>
            </div>

            <!-- Row 2: Game Summary -->
            <div class="bg-white rounded-lg p-6 mb-6">
              <p class="font-tinos text-base text-cod-gray leading-relaxed">{{ gameSummary }}</p>
            </div>

            <!-- Row 3: Game Details -->
            <div class="bg-white rounded-lg p-6 mb-6">
              <div class="grid grid-cols-2 gap-4">
                <!-- Genres -->
                <div v-if="gameGenres.length > 0">
                  <h3 class="font-tinos font-bold text-sm text-gray-500 mb-2">GENRES</h3>
                  <div class="flex flex-wrap gap-2">
                    <span
                      v-for="(genre, index) in gameGenres"
                      :key="index"
                      class="px-3 py-1 bg-gray-100 text-cod-gray font-tinos text-sm rounded"
                    >
                      {{ genre }}
                    </span>
                  </div>
                </div>

                <!-- Release Date -->
                <div>
                  <h3 class="font-tinos font-bold text-sm text-gray-500 mb-2">RELEASE DATE</h3>
                  <p class="font-tinos text-base text-cod-gray">{{ gameReleaseDate }}</p>
                </div>

                <!-- Game Modes -->
                <div v-if="gameModesText">
                  <h3 class="font-tinos font-bold text-sm text-gray-500 mb-2">GAME MODES</h3>
                  <p class="font-tinos text-base text-cod-gray">{{ gameModesText }}</p>
                </div>

                <!-- Platforms -->
                <div v-if="gamePlatformsText">
                  <h3 class="font-tinos font-bold text-sm text-gray-500 mb-2">PLATFORMS</h3>
                  <p class="font-tinos text-base text-cod-gray">{{ gamePlatformsText }}</p>
                </div>
              </div>

              <!-- New Release Badge -->
              <div v-if="isNewRelease" class="mt-4">
                <span class="px-3 py-1 bg-yellow-400 text-cod-gray font-tinos text-sm font-bold rounded">
                  NEW RELEASE
                </span>
              </div>
            </div>

            <!-- Popular Reviews Section -->
            <div class="mb-6">
              <h2 class="font-newsreader text-2xl font-bold text-cod-gray mb-4 border-b border-gray-300 pb-2">
                Popular Reviews
              </h2>
              <div v-if="popularReviews.length === 0" class="bg-white rounded-lg p-6 text-center">
                <p class="font-tinos text-base text-gray-500">No reviews yet. Be the first to review!</p>
              </div>
              <!-- TODO: Add ReviewCardBase components here when popular reviews are loaded -->
            </div>

            <!-- Recent Reviews Section -->
            <div class="mb-6">
              <h2 class="font-newsreader text-2xl font-bold text-cod-gray mb-4 border-b border-gray-300 pb-2">
                Recent Reviews
              </h2>
              <div v-if="recentReviews.length === 0" class="bg-white rounded-lg p-6 text-center">
                <p class="font-tinos text-base text-gray-500">No recent reviews</p>
              </div>
              <!-- TODO: Add ReviewCardBase components here when recent reviews are loaded -->
            </div>

            <!-- Popular Lists Section -->
            <div class="mb-6">
              <h2 class="font-newsreader text-2xl font-bold text-cod-gray mb-4 border-b border-gray-300 pb-2">
                Popular Lists
              </h2>
              <div v-if="listsWithGame.length === 0" class="bg-white rounded-lg p-6 text-center">
                <p class="font-tinos text-base text-gray-500">No lists yet</p>
              </div>
              <!-- TODO: Add list cards here when lists with game are loaded -->
            </div>

            <!-- Placeholder Comments Section -->
            <div class="mb-6">
              <h2 class="font-newsreader text-2xl font-bold text-cod-gray mb-4 border-b border-gray-300 pb-2">
                Comments
              </h2>
              <div class="bg-white rounded-lg p-6 text-center">
                <p class="font-tinos text-base text-gray-500">Comments coming soon</p>
              </div>
            </div>
          </div>

          <!-- Column 3: 25% - Actions Panel and Ratings -->
          <div class="lg:w-1/4">
            <!-- Actions Panel -->
            <div class="mb-6">
              <ActionsPanel
                context="game"
                :is-liked="isLiked"
                :like-count="likeCount"
                :is-in-wishlist="isInWishlist"
                @like="handleLike"
                @wishlist="handleWishlist"
                @rate="handleRate"
                @review="handleReview"
                @add-to-list="handleAddToList"
              />
            </div>

            <!-- Ratings Distribution -->
            <RatingsDistribution
              :average-rating="averageRating"
              :total-ratings="totalRatings"
              :ratings-distribution="ratingsDistribution"
            />
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.max-w-1280 {
  max-width: 1280px;
}
</style>