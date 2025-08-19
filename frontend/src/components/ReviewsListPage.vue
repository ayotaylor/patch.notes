<template>
  <div class="container py-4">
    <!-- Page Header -->
    <div class="row mb-4">
      <div class="col-12">
        <div class="d-flex justify-content-between align-items-center mb-3">
          <div>
            <h1 class="h2 fw-bold mb-2">
              <i class="fas fa-comments text-primary me-3"></i>
              {{ pageTitle }}
            </h1>
            <p class="text-muted mb-0">
              {{ pageSubtitle }}
            </p>
          </div>

          <!-- Back Button -->
          <button @click="goBack" class="btn btn-outline-secondary">
            <i class="fas fa-arrow-left me-2"></i>
            Back
          </button>
        </div>

        <!-- Breadcrumb -->
        <nav aria-label="breadcrumb" v-if="showBreadcrumb">
          <ol class="breadcrumb">
            <li class="breadcrumb-item">
              <router-link to="/dashboard" class="text-decoration-none">
                <i class="fas fa-home me-1"></i>Dashboard
              </router-link>
            </li>
            <li v-if="gameId" class="breadcrumb-item">
              <router-link :to="`/games/${gameId}`" class="text-decoration-none">
                {{ gameName || 'Game' }}
              </router-link>
            </li>
            <li v-else-if="userId" class="breadcrumb-item">
              <router-link :to="`/profile/${userId}`" class="text-decoration-none">
                {{ userName || 'Profile' }}
              </router-link>
            </li>
            <li class="breadcrumb-item active" aria-current="page">Reviews</li>
          </ol>
        </nav>
      </div>
    </div>

    <!-- Filters and Search -->
    <div class="row mb-4">
      <div class="col-12">
        <div class="card shadow-sm border-0">
          <div class="card-body p-3">
            <div class="row g-3 align-items-end">
              <!-- Search -->
              <div class="col-md-4">
                <label class="form-label small fw-semibold">Search Reviews</label>
                <div class="input-group">
                  <span class="input-group-text">
                    <i class="fas fa-search"></i>
                  </span>
                  <input
                    v-model="searchQuery"
                    type="text"
                    class="form-control"
                    placeholder="Search by game title or review content..."
                    @keyup.enter="applyFilters"
                  >
                </div>
              </div>

              <!-- Rating Filter -->
              <div class="col-md-2" v-if="!gameId">
                <label class="form-label small fw-semibold">Rating</label>
                <select v-model="ratingFilter" class="form-select">
                  <option value="">All Ratings</option>
                  <option value="5">5 Stars</option>
                  <option value="4">4+ Stars</option>
                  <option value="3">3+ Stars</option>
                  <option value="2">2+ Stars</option>
                  <option value="1">1+ Stars</option>
                </select>
              </div>

              <!-- Sort -->
              <div class="col-md-2">
                <label class="form-label small fw-semibold">Sort By</label>
                <select v-model="sortBy" class="form-select">
                  <option value="newest">Newest First</option>
                  <option value="oldest">Oldest First</option>
                  <option value="highest">Highest Rated</option>
                  <option value="lowest">Lowest Rated</option>
                </select>
              </div>

              <!-- Action Buttons -->
              <div class="col-md-4">
                <div class="d-flex gap-2">
                  <button @click="applyFilters" class="btn btn-primary">
                    <i class="fas fa-filter me-2"></i>
                    Apply Filters
                  </button>
                  <button @click="resetFilters" class="btn btn-outline-secondary">
                    <i class="fas fa-times me-2"></i>
                    Reset
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Reviews List -->
    <div class="row">
      <div class="col-12">
        <ReviewsList
          :reviews="reviews"
          :loading="loading"
          :loading-more="loadingMore"
          :has-more-reviews="hasMore"
          :total-count="totalCount"
          :show-header="false"
          :show-game="!gameId"
          :show-sort-options="false"
          :grid-layout="false"
          :truncate-reviews="false"
          :empty-message="emptyMessage"
          :empty-sub-message="emptySubMessage"
          :liked-reviews="likedReviews"
          :processing-like-reviews="processingLikeReviews"
          @load-more="loadMore"
          @edit="handleEditReview"
          @delete="handleDeleteReview"
          @toggleLike="handleToggleLike"
          @showComments="handleShowComments"
        >
          <template #empty-actions>
            <div class="d-flex gap-2 justify-content-center">
              <router-link
                v-if="!gameId && !userId"
                to="/dashboard"
                class="btn btn-primary"
              >
                <i class="fas fa-search me-2"></i>
                Find Games to Review
              </router-link>
              <router-link
                v-else-if="gameId"
                :to="`/games/${gameId}`"
                class="btn btn-primary"
              >
                <i class="fas fa-star me-2"></i>
                Write a Review
              </router-link>
            </div>
          </template>
        </ReviewsList>
      </div>
    </div>

    <!-- Review Form Modal -->
    <div
      v-if="showEditModal"
      class="modal d-block"
      tabindex="-1"
      @click="closeEditModal"
    >
      <div class="modal-dialog modal-lg modal-dialog-centered" @click.stop>
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">Edit Review</h5>
            <button @click="closeEditModal" class="btn-close"></button>
          </div>
          <div class="modal-body p-0">
            <ReviewForm
              :game="editingReview?.game"
              :existing-review="editingReview"
              :is-submitting="isSubmittingReview"
              @submit="handleSubmitEdit"
              @cancel="closeEditModal"
              @delete="handleDeleteFromModal"
            />
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, watch, defineProps } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/authStore'
import { useToast } from 'vue-toastification'
import { reviewsService } from '@/services/reviewsService'
import ReviewsList from './ReviewsList.vue'
import ReviewForm from './ReviewForm.vue'
import { socialService } from '@/services/socialService'
import { commentsService } from '@/services/commentsService'

// Props
const props = defineProps({
  gameId: {
    type: [String, Number],
    default: null
  },
  userId: {
    type: [String, Number],
    default: null
  },
  gameName: {
    type: String,
    default: ''
  },
  userName: {
    type: String,
    default: ''
  }
})

// Composables
const router = useRouter()
const authStore = useAuthStore()
const toast = useToast()

// State
const reviews = ref([])
const loading = ref(false)
const loadingMore = ref(false)
const hasMore = ref(false)
const totalCount = ref(0)
const page = ref(1)
const pageSize = 15
const likedReviews = ref(new Set())
const processingLikeReviews = ref(new Set())

// Filters
const searchQuery = ref('')
const ratingFilter = ref('')
const sortBy = ref('newest')

// Edit modal
const showEditModal = ref(false)
const editingReview = ref(null)
const isSubmittingReview = ref(false)

// Computed
const pageTitle = computed(() => {
  if (props.gameId) {
    return `Reviews for ${props.gameName || 'Game'}`
  } else if (props.userId) {
    const userName = props.userName || 'User'
    return `Reviews by ${userName}`
  }
  return 'All Reviews'
})

const pageSubtitle = computed(() => {
  if (props.gameId) {
    return `See what players think about ${props.gameName || 'this game'}`
  } else if (props.userId) {
    const userName = props.userName || 'this user'
    return `All reviews written by ${userName}`
  }
  return 'Discover what the community thinks about their favorite games'
})

const showBreadcrumb = computed(() => {
  return props.gameId || props.userId
})

const emptyMessage = computed(() => {
  if (searchQuery.value) {
    return 'No reviews found'
  } else if (props.gameId) {
    return 'No reviews yet for this game'
  } else if (props.userId) {
    return 'No reviews written yet'
  }
  return 'No reviews available'
})

const emptySubMessage = computed(() => {
  if (searchQuery.value) {
    return 'Try adjusting your search terms or filters'
  } else if (props.gameId) {
    return 'Be the first to share your thoughts about this game!'
  } else if (props.userId) {
    return 'Start exploring games and write your first review'
  }
  return 'Check back later as more reviews are added'
})

// Methods
const loadReviews = async (pageNum = 1, append = false) => {
  try {
    if (pageNum === 1) {
      loading.value = true
      reviews.value = []
    } else {
      loadingMore.value = true
    }

    let response
    // TODO: Pass filters to API when backend supports them
    // const filters = { search: searchQuery.value, rating: ratingFilter.value, sort: sortBy.value }

    if (props.gameId) {
      response = await reviewsService.getGameReviews(props.gameId, pageNum, pageSize)
    } else if (props.userId) {
      response = await reviewsService.getUserReviews(props.userId, pageNum, pageSize)
    } else {
      response = await reviewsService.getLatestReviews(pageNum, pageSize)
      // For latest reviews, we need to simulate pagination
      response = {
        data: response.slice((pageNum - 1) * pageSize, pageNum * pageSize),
        hasNextPage: response.length > pageNum * pageSize,
        totalCount: response.length
      }
    }

    // Load comment counts for reviews
    const reviewsWithCommentCounts = await commentsService.loadCommentCountsForReviews(response.data || [])

    if (append && pageNum > 1) {
      reviews.value.push(...reviewsWithCommentCounts)
    } else {
      reviews.value = reviewsWithCommentCounts
    }

    hasMore.value = response.hasNextPage || false
    totalCount.value = response.totalCount || 0
    page.value = pageNum
  } catch (error) {
    console.error('Error loading reviews:', error)
    toast.error('Failed to load reviews')
    reviews.value = []
    hasMore.value = false
    totalCount.value = 0
  } finally {
    loading.value = false
    loadingMore.value = false
  }
}

const loadMore = async () => {
  await loadReviews(page.value + 1, true)
}

const applyFilters = async () => {
  await loadReviews(1)
}

const resetFilters = async () => {
  searchQuery.value = ''
  ratingFilter.value = ''
  sortBy.value = 'newest'
  await loadReviews(1)
}

const goBack = () => {
  if (props.gameId) {
    router.push(`/games/${props.gameId}`)
  } else if (props.userId) {
    router.push(`/profile/${props.userId}`)
  } else {
    router.push('/dashboard')
  }
}

const handleEditReview = (review) => {
  if (review.user.id === authStore.user?.id) {
    editingReview.value = review
    showEditModal.value = true
  }
}

const handleDeleteReview = async (review) => {
  if (review.user.id !== authStore.user?.id) return

  if (confirm('Are you sure you want to delete this review?')) {
    try {
      await reviewsService.deleteReview(review.id)
      toast.success('Review deleted successfully!')
      await loadReviews(1)
    } catch (error) {
      console.error('Error deleting review:', error)
      toast.error('Failed to delete review')
    }
  }
}

const handleSubmitEdit = async (reviewData) => {
  if (!editingReview.value?.id) return

  try {
    isSubmittingReview.value = true
    await reviewsService.updateReview(editingReview.value.id, reviewData)
    toast.success('Review updated successfully!')
    closeEditModal()
    await loadReviews(1)
  } catch (error) {
    console.error('Error updating review:', error)
    toast.error(error.message || 'Failed to update review')
  } finally {
    isSubmittingReview.value = false
  }
}

const handleDeleteFromModal = async () => {
  if (!editingReview.value?.id) return

  if (confirm('Are you sure you want to delete this review?')) {
    try {
      await reviewsService.deleteReview(editingReview.value.id)
      toast.success('Review deleted successfully!')
      closeEditModal()
      await loadReviews(1)
    } catch (error) {
      console.error('Error deleting review:', error)
      toast.error('Failed to delete review')
    }
  }
}

const closeEditModal = () => {
  showEditModal.value = false
  editingReview.value = null
  isSubmittingReview.value = false
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
    const targetReview = reviews.value.find(r => r.id === reviewId)
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

// Watchers
watch([searchQuery, ratingFilter, sortBy], () => {
  // Debounce the filter application
  clearTimeout(window.reviewFilterTimeout)
  window.reviewFilterTimeout = setTimeout(() => {
    applyFilters()
  }, 500)
})

// Lifecycle
onMounted(() => {
  loadReviews(1)
})
</script>

<style scoped>
.modal {
  background-color: rgba(0, 0, 0, 0.5);
}

.card {
  border-radius: 12px;
}

.input-group-text {
  background-color: #f8f9fa;
  border-color: #dee2e6;
}

.breadcrumb {
  background: none;
  padding: 0;
  margin: 0;
}

.breadcrumb-item + .breadcrumb-item::before {
  content: ">";
  font-weight: bold;
  color: #6c757d;
}

@media (max-width: 768px) {
  .container {
    padding: 1rem;
  }

  .d-flex.justify-content-between {
    flex-direction: column;
    align-items: flex-start !important;
    gap: 1rem;
  }

  .row.g-3.align-items-end {
    flex-direction: column;
  }

  .row.g-3.align-items-end > .col-md-4,
  .row.g-3.align-items-end > .col-md-2 {
    width: 100%;
    flex: none;
  }
}
</style>