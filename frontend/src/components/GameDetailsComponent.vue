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
                @error="handleImageError"
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
                  <div v-if="gameRating > 0" class="text-end">
                    <div class="h4 mb-0 fw-bold text-warning">
                      <i class="fas fa-star"></i>
                      {{ gameRating }}/5
                    </div>
                    <small class="text-muted">IGDB Rating</small>
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
                <div v-if="hasEngagementStats" class="row g-3 mb-4">
                  <div class="col-4" v-if="game.hypes > 0">
                    <div class="text-center">
                      <div class="h5 mb-0 fw-bold text-danger">{{ formatNumber(game.hypes) }}</div>
                      <small class="text-muted">Hypes</small>
                    </div>
                  </div>
                  <div class="col-4" v-if="game.likes > 0">
                    <div class="text-center">
                      <div class="h5 mb-0 fw-bold text-primary">{{ formatNumber(game.likes) }}</div>
                      <small class="text-muted">Likes</small>
                    </div>
                  </div>
                  <div class="col-4" v-if="game.favorites > 0">
                    <div class="text-center">
                      <div class="h5 mb-0 fw-bold text-warning">{{ formatNumber(game.favorites) }}</div>
                      <small class="text-muted">Favorites</small>
                    </div>
                  </div>
                </div>

                <!-- Action Buttons -->
                <div class="d-flex gap-2 mb-3">
                  <button @click="addToLibrary" :disabled="isInLibrary || isAddingToLibrary"
                    class="btn btn-lg flex-grow-1" :class="isInLibrary ? 'btn-success' : 'btn-primary'">
                    <span v-if="isAddingToLibrary" class="spinner-border spinner-border-sm me-2"></span>
                    <i v-else class="fas" :class="isInLibrary ? 'fa-check' : 'fa-plus'"></i>
                    {{ isInLibrary ? 'In Library' : isAddingToLibrary ? 'Adding...' : 'Add to Library' }}
                  </button>

                  <button @click="toggleWishlist" class="btn btn-lg btn-outline-secondary"
                    :class="{ 'active': isInWishlist }">
                    <i class="fas fa-heart" :class="{ 'text-danger': isInWishlist }"></i>
                    <span class="d-none d-md-inline ms-1">{{ isInWishlist ? 'Wishlisted' : 'Wishlist' }}</span>
                  </button>

                  <button @click="toggleLike" class="btn btn-lg btn-outline-primary"
                    :class="{ 'active': game.isLikedByUser }">
                    <i class="fas fa-thumbs-up" :class="{ 'text-primary': game.isLikedByUser }"></i>
                    <span class="d-none d-md-inline ms-1">{{ game.isLikedByUser ? 'Liked' : 'Like' }}</span>
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
const toast = useToast()

// State
const game = ref(null)
const loading = ref(true)
const error = ref('')
const isAddingToLibrary = ref(false)
const selectedImage = ref(null)
const userLibrary = ref(new Set())
const userWishlist = ref(new Set())
const similarGames = ref([])

// Computed properties with safe access
const gameIdentifier = computed(() => {
  const propId = props.gameId
  const propSlug = props.slug
  const routeIdentifier = route.params.identifier

  console.log('Computing identifier:', { propId, propSlug, routeIdentifier })
  return propId || propSlug || routeIdentifier || null
})

const isInLibrary = computed(() => {
  const id = gameIdentifier.value
  return id && userLibrary.value.has(String(id))
})

const isInWishlist = computed(() => {
  const id = gameIdentifier.value
  return id && userWishlist.value.has(String(id))
})

// Safe property accessors
const gameImageUrl = computed(() => {
  try {
    if (!game.value) return '/default-game.png'
    return game.value.primaryImageUrl || '/default-game.png'
  } catch (error) {
    console.warn('Error getting game image URL:', error)
    return '/default-game.png'
  }
})

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
    return screenshots.map(screenshot => screenshot.imageUrl || screenshot.url).filter(Boolean)
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

const hasEngagementStats = computed(() => {
  try {
    return game.value && (game.value.hypes > 0 || game.value.likes > 0 || game.value.favorites > 0)
  } catch (error) {
    console.warn('Error checking engagement stats:', error)
    return false
  }
})

// Helper methods
const formatNumber = (num) => {
  if (num >= 1000000) return (num / 1000000).toFixed(1) + 'M'
  if (num >= 1000) return (num / 1000).toFixed(1) + 'K'
  return num.toString()
}

const handleImageError = (event) => {
  event.target.src = '/default-game.png'
}

const getSimilarGameImage = (similarGame) => {
  try {
    return similarGame.primaryImageUrl || '/default-game.png'
  } catch (error) {
    return '/default-game.png'
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

    // Load similar games if available
    if (game.value.id && game.value.similarGames && game.value.similarGames.length > 0) {
      await loadSimilarGames()
    }

    // Load user's library status
    await loadUserLibraryStatus()

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

const loadUserLibraryStatus = async () => {
  try {
    const libraryData = localStorage.getItem('userLibrary')
    if (libraryData) {
      const library = JSON.parse(libraryData)
      userLibrary.value = new Set(library)
    }

    const wishlistData = localStorage.getItem('userWishlist')
    if (wishlistData) {
      const wishlist = JSON.parse(wishlistData)
      userWishlist.value = new Set(wishlist)
    }
  } catch (err) {
    console.error('Error loading user library status:', err)
  }
}

const addToLibrary = async () => {
  if (!gameIdentifier.value) return

  try {
    isAddingToLibrary.value = true
    userLibrary.value.add(gameIdentifier.value)
    localStorage.setItem('userLibrary', JSON.stringify([...userLibrary.value]))
    toast.success(`Added "${game.value.name}" to your library!`)
  } catch (err) {
    toast.error('Failed to add game to library')
    console.error('Add to library error:', err)
  } finally {
    isAddingToLibrary.value = false
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

const toggleLike = async () => {
  // TODO: Implement like functionality
  console.log('Toggle like for game:', game.value.id)
}

const viewGame = (id) => {
  router.push(`/games/${id}`)
}

const openImageModal = (imageUrl) => {
  selectedImage.value = imageUrl
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

.progress {
  border-radius: 10px;
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