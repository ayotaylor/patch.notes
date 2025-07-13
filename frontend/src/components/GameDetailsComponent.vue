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
    <div v-else-if="game" class="row">
      <!-- Breadcrumb -->
      <div class="col-12 mb-3">
        <nav aria-label="breadcrumb">
          <ol class="breadcrumb">
            <li class="breadcrumb-item">
              <router-link to="Dashboard" class="text-decoration-none">
                <i class="fas fa-home me-1"></i>Dashboard
              </router-link>
            </li>
            <li class="breadcrumb-item">
              <router-link to="/games" class="text-decoration-none">Games</router-link>
            </li>
            <li class="breadcrumb-item active" aria-current="page">{{ game.name }}</li>
          </ol>
        </nav>
      </div>

      <!-- Game Header -->
      <div class="col-12 mb-4">
        <div class="card shadow-lg border-0">
          <div class="row g-0">
            <!-- Game Image -->
            <div class="col-md-4">
              <img :src="game.covers[0].url || '/default-game.png'" :alt="game.name" class="img-fluid w-100 h-100"
                style="object-fit: cover; min-height: 300px; border-radius: 15px 0 0 15px;">
            </div>

            <!-- Game Info -->
            <div class="col-md-8">
              <div class="card-body p-4">
                <!-- Title and Rating -->
                <div class="d-flex justify-content-between align-items-start mb-3">
                  <div>
                    <h1 class="h2 fw-bold mb-2">{{ game.name }}</h1>
                    <p v-if="game.developers" class="text-muted mb-0">
                      {{ Developer }}:
                      <span v-for="developer in game.developers" :key="developer"
                        class="badge bg-light text-dark border">
                        {{ developer }}
                      </span>

                    </p>
                  </div>
                  <div v-if="game.rating" class="text-end">
                    <div class="h4 mb-0 fw-bold text-warning">
                      <i class="fas fa-star"></i>
                      {{ game.rating }}/5
                    </div>
                    <small class="text-muted">{{ game.reviewCount || 0 }} reviews</small>
                  </div>
                </div>

                <!-- Game Meta Info -->
                <div class="row g-3 mb-4">
                  <div class="col-sm-6">
                    <div class="d-flex align-items-center">
                      <i class="fas fa-tag text-primary me-2"></i>
                      <div>
                        <small class="text-muted d-block">Genre</small>
                        <span class="fw-semibold">{{ game.genres[0].name || 'Not specified' }}</span>
                      </div>
                    </div>
                  </div>
                  <div class="col-sm-6">
                    <div class="d-flex align-items-center">
                      <i class="fas fa-calendar text-primary me-2"></i>
                      <div>
                        <small class="text-muted d-block">Release Date</small>
                        <span class="fw-semibold">{{ formatDate(game.firstReleaseDate) }}</span>
                      </div>
                    </div>
                  </div>
                  <div class="col-sm-6">
                    <div class="d-flex align-items-center">
                      <i class="fas fa-users text-primary me-2"></i>
                      <div>
                        <small class="text-muted d-block">Players</small>
                        <span class="fw-semibold">{{ game.players || 'Single player' }}</span>
                      </div>
                    </div>
                  </div>
                  <div class="col-sm-6">
                    <div class="d-flex align-items-center">
                      <i class="fas fa-download text-primary me-2"></i>
                      <div>
                        <small class="text-muted d-block">Size</small>
                        <span class="fw-semibold">{{ game.size || 'Unknown' }}</span>
                      </div>
                    </div>
                  </div>
                </div>

                <!-- Price and Actions -->
                <div class="d-flex align-items-center justify-content-between mb-3">
                  <div class="h3 mb-0 fw-bold text-success">
                    {{ game.price === 0 ? 'Free' : game.price ? `${game.price}` : 'Price TBA' }}
                  </div>
                  <div class="d-flex gap-2">
                    <button @click="addToLibrary" :disabled="isInLibrary || isAddingToLibrary" class="btn btn-lg"
                      :class="isInLibrary ? 'btn-success' : 'btn-primary'">
                      <span v-if="isAddingToLibrary" class="spinner-border spinner-border-sm me-2"></span>
                      <i v-else class="fas" :class="isInLibrary ? 'fa-check' : 'fa-plus'"></i>
                      {{ isInLibrary ? 'In Library' : isAddingToLibrary ? 'Adding...' : 'Add to Library' }}
                    </button>
                    <button @click="toggleWishlist" class="btn btn-lg btn-outline-secondary"
                      :class="{ 'active': isInWishlist }">
                      <i class="fas fa-heart" :class="{ 'text-danger': isInWishlist }"></i>
                    </button>
                  </div>
                </div>

                <!-- Tags -->
                <div v-if="game.tags && game.tags.length > 0" class="mb-3">
                  <div class="d-flex flex-wrap gap-1">
                    <span v-for="tag in game.tags" :key="tag" class="badge bg-light text-dark border">
                      {{ tag }}
                    </span>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Game Description and Details -->
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
            <p class="mb-0" style="line-height: 1.6;">
              {{ game.description || 'No description available for this game.' }}
            </p>
          </div>
        </div>

        <!-- Screenshots -->
        <div v-if="game.screenshots && game.screenshots.length > 0" class="card shadow-sm border-0 mb-4">
          <div class="card-header bg-white border-bottom">
            <h3 class="h5 mb-0 fw-bold">
              <i class="fas fa-images text-primary me-2"></i>
              Screenshots
            </h3>
          </div>
          <div class="card-body p-4">
            <div class="row g-3">
              <div v-for="(screenshot, index) in game.screenshots" :key="index" class="col-md-6">
                <img :src="screenshot" :alt="`Screenshot ${index + 1}`" class="img-fluid rounded cursor-pointer"
                  style="width: 100%; height: 200px; object-fit: cover;" @click="openImageModal(screenshot)">
              </div>
            </div>
          </div>
        </div>

        <!-- System Requirements -->
        <div v-if="game.systemRequirements" class="card shadow-sm border-0 mb-4">
          <div class="card-header bg-white border-bottom">
            <h3 class="h5 mb-0 fw-bold">
              <i class="fas fa-desktop text-primary me-2"></i>
              System Requirements
            </h3>
          </div>
          <div class="card-body p-4">
            <div class="row">
              <div v-if="game.systemRequirements.minimum" class="col-md-6 mb-3">
                <h6 class="fw-bold text-success">Minimum</h6>
                <ul class="list-unstyled small">
                  <li v-if="game.systemRequirements.minimum.os">
                    <strong>OS:</strong> {{ game.systemRequirements.minimum.os }}
                  </li>
                  <li v-if="game.systemRequirements.minimum.processor">
                    <strong>Processor:</strong> {{ game.systemRequirements.minimum.processor }}
                  </li>
                  <li v-if="game.systemRequirements.minimum.memory">
                    <strong>Memory:</strong> {{ game.systemRequirements.minimum.memory }}
                  </li>
                  <li v-if="game.systemRequirements.minimum.graphics">
                    <strong>Graphics:</strong> {{ game.systemRequirements.minimum.graphics }}
                  </li>
                  <li v-if="game.systemRequirements.minimum.storage">
                    <strong>Storage:</strong> {{ game.systemRequirements.minimum.storage }}
                  </li>
                </ul>
              </div>
              <div v-if="game.systemRequirements.recommended" class="col-md-6">
                <h6 class="fw-bold text-primary">Recommended</h6>
                <ul class="list-unstyled small">
                  <li v-if="game.systemRequirements.recommended.os">
                    <strong>OS:</strong> {{ game.systemRequirements.recommended.os }}
                  </li>
                  <li v-if="game.systemRequirements.recommended.processor">
                    <strong>Processor:</strong> {{ game.systemRequirements.recommended.processor }}
                  </li>
                  <li v-if="game.systemRequirements.recommended.memory">
                    <strong>Memory:</strong> {{ game.systemRequirements.recommended.memory }}
                  </li>
                  <li v-if="game.systemRequirements.recommended.graphics">
                    <strong>Graphics:</strong> {{ game.systemRequirements.recommended.graphics }}
                  </li>
                  <li v-if="game.systemRequirements.recommended.storage">
                    <strong>Storage:</strong> {{ game.systemRequirements.recommended.storage }}
                  </li>
                </ul>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Sidebar -->
      <div class="col-lg-4">
        <!-- Quick Stats -->
        <div class="card shadow-sm border-0 mb-4">
          <div class="card-header bg-white border-bottom">
            <h3 class="h5 mb-0 fw-bold">
              <i class="fas fa-chart-bar text-primary me-2"></i>
              Game Stats
            </h3>
          </div>
          <div class="card-body p-4">
            <div class="d-flex justify-content-between align-items-center mb-3">
              <span class="text-muted">Players Online</span>
              <span class="fw-bold text-success">{{ game.playersOnline || 'N/A' }}</span>
            </div>
            <div class="d-flex justify-content-between align-items-center mb-3">
              <span class="text-muted">Average Playtime</span>
              <span class="fw-bold">{{ game.averagePlaytime || 'Unknown' }}</span>
            </div>
            <div class="d-flex justify-content-between align-items-center mb-3">
              <span class="text-muted">Achievements</span>
              <span class="fw-bold text-warning">{{ game.achievementsCount || 0 }}</span>
            </div>
            <div class="d-flex justify-content-between align-items-center">
              <span class="text-muted">Last Updated</span>
              <span class="fw-bold">{{ formatDate(game.lastUpdated) }}</span>
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
                  <img :src="similarGame.imageUrl || '/default-game.png'" :alt="similarGame.name" class="rounded me-3"
                    style="width: 50px; height: 50px; object-fit: cover;">
                  <div class="flex-grow-1">
                    <h6 class="mb-1 fw-semibold">{{ similarGame.name }}</h6>
                    <div class="d-flex align-items-center gap-2 small text-muted">
                      <span v-if="similarGame.rating">
                        <i class="fas fa-star text-warning me-1"></i>{{ similarGame.rating }}/5
                      </span>
                      <span v-if="similarGame.price !== undefined">
                        {{ similarGame.price === 0 ? 'Free' : `${similarGame.price}` }}
                      </span>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Reviews Summary -->
        <div v-if="game.reviewSummary" class="card shadow-sm border-0">
          <div class="card-header bg-white border-bottom">
            <h3 class="h5 mb-0 fw-bold">
              <i class="fas fa-star text-primary me-2"></i>
              Reviews Summary
            </h3>
          </div>
          <div class="card-body p-4">
            <div class="text-center mb-3">
              <div class="h2 fw-bold text-warning mb-1">{{ game.rating }}/5</div>
              <div class="text-muted">{{ game.reviewCount || 0 }} reviews</div>
            </div>

            <div v-if="game.reviewSummary.breakdown" class="mb-3">
              <div v-for="(count, stars) in game.reviewSummary.breakdown" :key="stars"
                class="d-flex align-items-center mb-2">
                <span class="me-2 small">{{ stars }}★</span>
                <div class="progress flex-grow-1 me-2" style="height: 8px;">
                  <div class="progress-bar bg-warning" :style="{ width: `${(count / game.reviewCount) * 100}%` }"></div>
                </div>
                <span class="small text-muted">{{ count }}</span>
              </div>
            </div>

            <div class="text-center">
              <button @click="viewAllReviews" class="btn btn-outline-primary btn-sm">
                <i class="fas fa-comments me-1"></i>
                View All Reviews
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Image Modal -->
    <div v-if="selectedImage" class="modal d-block" tabindex="-1" @click="closeImageModal">
      <div class="modal-dialog modal-lg modal-dialog-centered">
        <div class="modal-content bg-transparent border-0">
          <div class="modal-body p-0">
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

// Computed
// const gameId = computed(() => {
//   return props.gameId || route.params.gameId || route.params.identifier || null
// })
// const slug = computed(() => {
//   return props.slug || route.params.slug 
//     || (!props.gameId && route.params.identifier)
//     || null
// })

const gameIdentifier = computed(() => {
  // Priority: props first, then route params
  const propId = props.gameId
  const propSlug = props.slug
  const routeIdentifier = route.params.identifier
  
  console.log('Computing identifier:', { propId, propSlug, routeIdentifier })
  
  return propId || propSlug || routeIdentifier || null
})

const isInLibrary = computed(() => gameIdentifier.value && userLibrary.value.has(String(gameIdentifier.value)))
const isInWishlist = computed(() => gameIdentifier.value && userWishlist.value.has(String(gameIdentifier.value)))

// Methods
const loadGameDetails = async () => {
  const identifier = gameIdentifier.value

  if (!identifier) {
    loading.value = false;
    error.value = 'No game identifier provided'
    return
  }

  loading.value = true
  error.value = ''

  try {
    game.value = await gamesStore.fetchGameDetails(identifier)

    // TODO: get similar games, based on igdb api response or maybe by some other property
    // currntly loading similar games if genre is available
    if (game.value.genres && game.value.genres.length > 0) {
      await loadSimilarGames()
    }

    // Load user's library status (in real app, this would come from an API)
    await loadUserLibraryStatus()

  } catch (err) {
    error.value = err.message || 'Failed to load game details'
    game.value = null
    console.error('Error loading game details:', err)
  } finally {
    loading.value = false
  }
}

// TODO: implement this. get result from patchnotes api
const loadSimilarGames = async () => {
  try {
    // In a real app, you'd have an API endpoint for similar games
    const searchResults = await gamesStore.searchGames(game.value.genre)
    similarGames.value = searchResults.filter(g => g.id !== gameIdentifier.value)
  } catch (err) {
    console.error('Error loading similar games:', err)
  }
}

// TODO: check if this is needed
const loadUserLibraryStatus = async () => {
  try {
    // In a real app, you'd load this from your user library API
    // For demo purposes, we'll use local storage or mock data
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

// TODO: implement adding to liking, add to list, reviews, etc.
const addToLibrary = async () => {
  if (!gameIdentifier.value) return

  try {
    isAddingToLibrary.value = true

    // In a real app, you'd call your API here
    // await userLibraryService.addGame(gameId.value)

    userLibrary.value.add(gameIdentifier.value)

    // Save to localStorage for demo
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
      const identifier = String(gameIdentifier.value)
      userWishlist.value.delete(identifier)
      toast.success(`Removed "${game.value.name}" from wishlist`)
    } else {
      userWishlist.value.add(identifier)
      toast.success(`Added "${game.value.name}" to wishlist`)
    }

    // Save to localStorage for demo
    localStorage.setItem('userWishlist', JSON.stringify([...userWishlist.value]))
  } catch (err) {
    toast.error('Failed to update wishlist')
    console.error('Wishlist error:', err)
  }
}

const viewGame = (id) => {
  router.push(`/games/${id}`)
}

const viewAllReviews = () => {
  router.push(`/games/${gameIdentifier.value}/reviews`)
}

const openImageModal = (imageUrl) => {
  selectedImage.value = imageUrl
}

const closeImageModal = () => {
  selectedImage.value = null
}

const formatDate = (dateString) => {
  if (!dateString) return 'Unknown'
  const date = new Date(dateString)
  return date.toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'long',
    day: 'numeric'
  })
}

// Watchers
// watcher for route changes
watch(() => route.params, async (newParams, oldParams) => {
  console.log('Route params changed:', { old: oldParams, new: newParams })
  
  // Only reload if the identifier actually changed
  const oldIdentifier = oldParams?.identifier
  const newIdentifier = newParams?.identifier
  
  if (newIdentifier && newIdentifier !== oldIdentifier) {
    await nextTick()
    await loadGameDetails()
  }
}, { immediate: false, deep: true })

// Watcher for computed identifier changes
watch(() => gameIdentifier.value, async (newIdentifier, oldIdentifier) => {
  console.log('Game identifier changed:', { old: oldIdentifier, new: newIdentifier })
  
  if (newIdentifier && newIdentifier !== oldIdentifier) {
    await nextTick()
    await loadGameDetails()
  }
}, { immediate: false })

// Lifecycle
onMounted(async () => {
  console.log('Component mounted')
  console.log('Route:', route)
  console.log('Route params:', route.params)
  console.log('Props:', { gameId: props.gameId, slug: props.slug })

  // Wait for next tick to ensure route is fully loaded
  await nextTick()

  const identifier = gameIdentifier.value
  console.log('Final identifier on mount:', identifier)


  if (identifier) {
    await loadGameDetails()
  } else {
    // Give route time to load if needed
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