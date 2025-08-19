<template>
  <div class="review-details-page">
    <div class="container py-4">
      <!-- Back Button -->
      <div class="row mb-4">
        <div class="col-12">
          <button @click="goBack" class="btn btn-outline-secondary">
            <i class="fas fa-arrow-left me-2"></i>
            Back
          </button>
        </div>
      </div>

      <!-- Loading State -->
      <div v-if="loading" class="text-center py-5">
        <div class="spinner-border text-primary" role="status">
          <span class="visually-hidden">Loading...</span>
        </div>
        <p class="mt-3 text-muted">Loading review...</p>
      </div>

      <!-- Error State -->
      <div v-else-if="error" class="alert alert-danger" role="alert">
        <h4 class="alert-heading">Error Loading Review</h4>
        <p>{{ error }}</p>
        <button @click="loadReview" class="btn btn-outline-danger">
          <i class="fas fa-redo me-2"></i>Try Again
        </button>
      </div>

      <!-- Review Content -->
      <div v-else-if="review" class="row">
        <div class="col-12">
          <!-- Review Card -->
          <div class="mb-4">
            <ReviewCard
              :review="review"
              :show-game="true"
              :show-date="true"
              :truncated="false"
              :is-liked="likedReviews.has(review.id)"
              :is-processing-like="processingLikeReviews.has(review.id)"
              @edit="handleEditReview"
              @delete="handleDeleteReview"
              @toggleLike="handleToggleLike"
              @showComments="scrollToComments"
            />
          </div>

          <!-- Comments Section -->
          <div class="card shadow-sm border-0">
            <div class="card-header bg-white border-bottom">
              <h4 class="mb-0 fw-bold">
                <i class="fas fa-comments text-primary me-2"></i>
                Comments
                <span v-if="commentCount > 0" class="text-muted fw-normal">({{ commentCount }})</span>
              </h4>
            </div>
            <div class="card-body p-4">
              <CommentSection
                item-type="review"
                :item-id="reviewId"
                :auto-load="true"
                @commentAdded="handleCommentAdded"
                @commentUpdated="handleCommentUpdated"
                @commentDeleted="handleCommentDeleted"
                @countChanged="handleCommentCountChanged"
                ref="commentSectionRef"
              />
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, defineProps } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/authStore'
import { useToast } from 'vue-toastification'
import { reviewsService } from '@/services/reviewsService'
import { socialService } from '@/services/socialService'
import { commentsService } from '@/services/commentsService'
import ReviewCard from './ReviewCard.vue'
import CommentSection from './CommentSection.vue'

// Props
const props = defineProps({
  reviewId: {
    type: [String, Number],
    required: true
  }
})

// Composables
const router = useRouter()
const authStore = useAuthStore()
const toast = useToast()

// State
const review = ref(null)
const loading = ref(true)
const error = ref('')
const commentCount = ref(0)
const likedReviews = ref(new Set())
const processingLikeReviews = ref(new Set())

// Refs
const commentSectionRef = ref(null)

// Methods
const loadReview = async () => {
  try {
    loading.value = true
    error.value = ''

    // Load the specific review
    review.value = await reviewsService.getReview(props.reviewId)
    
    // Load comment count and add it to the review object
    try {
      const count = await commentsService.getReviewCommentCount(props.reviewId)
      commentCount.value = count
      // Add comment count to review object so ReviewCard can display it
      if (review.value) {
        review.value.commentCount = count
      }
    } catch (commentErr) {
      console.error('Error loading comment count:', commentErr)
      commentCount.value = 0
      if (review.value) {
        review.value.commentCount = 0
      }
    }

    // Load like status if user is authenticated
    if (authStore.user) {
      await loadLikeStatus()
    }

  } catch (err) {
    console.error('Error loading review:', err)
    error.value = err.message || 'Failed to load review'
  } finally {
    loading.value = false
  }
}

const loadLikeStatus = async () => {
  if (!authStore.user || !review.value) return

  try {
    // Check if user has liked this review
    // This would need to be implemented in the API
    // For now, assume not liked
    // const isLiked = await socialService.isReviewLiked(review.value.id)
    // if (isLiked) {
    //   likedReviews.value.add(review.value.id)
    // }
  } catch (err) {
    console.error('Error loading like status:', err)
  }
}

const handleToggleLike = async (reviewObj) => {
  if (!authStore.user) {
    toast.info('Please sign in to like reviews')
    return
  }

  const reviewId = reviewObj.id
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
    review.value.likeCount = (review.value.likeCount || 0) + (wasLiked ? -1 : 1)

  } catch (err) {
    console.error('Error toggling review like:', err)
    toast.error('Failed to update like')
  } finally {
    processingLikeReviews.value.delete(reviewId)
  }
}

const handleEditReview = (reviewObj) => {
  if (reviewObj.user.id === authStore.user?.id) {
    // Navigate to game details page with edit mode
    router.push(`/games/${reviewObj.game.slug}?edit=true`)
  }
}

const handleDeleteReview = async (reviewObj) => {
  if (reviewObj.user.id !== authStore.user?.id) return

  if (confirm('Are you sure you want to delete this review?')) {
    try {
      await reviewsService.deleteReview(reviewObj.id)
      toast.success('Review deleted successfully!')
      // Navigate back to game details or previous page
      goBack()
    } catch (error) {
      console.error('Error deleting review:', error)
      toast.error('Failed to delete review')
    }
  }
}

const scrollToComments = () => {
  // Scroll to comments section since we're already on the review page
  const commentsElement = document.querySelector('.card-header')
  if (commentsElement) {
    commentsElement.scrollIntoView({ behavior: 'smooth', block: 'start' })
  }
}

const handleCommentAdded = () => {
  // Handle new comment added
}

const handleCommentUpdated = () => {
  // Handle comment updated
}

const handleCommentDeleted = () => {
  // Handle comment deleted
}

const handleCommentCountChanged = (count) => {
  commentCount.value = count
  // Also update the review object so ReviewCard shows the correct count
  if (review.value) {
    review.value.commentCount = count
  }
}

const goBack = () => {
  // Try to go back to the previous page, or navigate to the game details
  if (window.history.length > 1) {
    router.go(-1)
  } else if (review.value?.game) {
    router.push(`/games/${review.value.game.slug}`)
  } else {
    router.push('/dashboard')
  }
}

// Lifecycle
onMounted(() => {
  loadReview()
})
</script>

<style scoped>
.review-details-page {
  min-height: 100vh;
  background-color: #f8f9fa;
}

.card {
  border-radius: 12px;
}

@media (max-width: 768px) {
  .container {
    padding: 1rem;
  }
}
</style>