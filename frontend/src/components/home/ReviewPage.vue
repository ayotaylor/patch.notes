<script setup>
import { ref, onMounted, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import ReviewCardBase from './ReviewCardBase.vue'
import ActionsPanel from './ActionsPanel.vue'
import GameImageComponent from './GameImageComponent.vue'
import CommentList from './CommentList.vue'
import { useReviewLikes } from '@/composables/reviews/useReviewLikes'
import { useAuthStore } from '@/stores/authStore'
import { reviewsService } from '@/services/reviewsService'

const { toggleLike } = useReviewLikes()
const authStore = useAuthStore()

const route = useRoute()
const router = useRouter()

// Props from route params
const username = ref(route.params.username)
const gameSlug = ref(route.params.gameSlug)

// State
const review = ref(null)
const loading = ref(true)
const error = ref(null)

// Computed properties for ActionsPanel
const canEdit = computed(() => {
  return authStore.user && review.value?.user?.id === authStore.user.id
})

const canDelete = computed(() => {
  return authStore.user && review.value?.user?.id === authStore.user.id
})

const isReviewLiked = computed(() => {
  return review.value?.isLikedByCurrentUser || false
})

const reviewLikeCount = computed(() => {
  return review.value?.likeCount || 0
})

// Load review from router state or fetch from API
const loadReview = async () => {
  loading.value = true
  error.value = null

  try {
    // Check if review was passed via router state
    const stateReview = history.state?.review || router.currentRoute.value.state?.review

    if (stateReview) {
      // Use review from state (navigation from review card)
      review.value = stateReview
      loading.value = false
      return
    }

    // If no state, fetch from API (direct URL access or page refresh)
    if (!username.value || !gameSlug.value) {
      error.value = 'Missing required parameters'
      loading.value = false
      return
    }

    const fetchedReview = await reviewsService.getUserGameReviewBySlug(username.value, gameSlug.value)

    if (!fetchedReview) {
      error.value = 'Review not found'
      loading.value = false
      return
    }

    review.value = fetchedReview
  } catch (err) {
    error.value = 'Failed to load review'
    console.error('Error loading review:', err)
  } finally {
    loading.value = false
  }
}

const handleLikeReview = async (reviewData) => {
  await toggleLike(reviewData, (wasLiked) => {
    // Update like count and status in local review data
    reviewData.isLikedByCurrentUser = !wasLiked
    reviewData.likeCount += wasLiked ? -1 : 1
  })
}

// ActionsPanel handlers
const handleLike = () => {
  if (review.value) {
    handleLikeReview(review.value)
  }
}

const handleEdit = () => {
  // TODO: Open edit review modal
  console.log('Edit review')
}

const handleDelete = () => {
  // TODO: Implement delete review functionality
  console.log('Delete review')
}

const handleRate = () => {
  // TODO: Open rate modal
  console.log('Rate game')
}

const handleReview = () => {
  // TODO: Open review modal
  console.log('Write review')
}

const handleAddToList = () => {
  // TODO: Open add to list modal
  console.log('Add to list')
}

onMounted(() => {
  loadReview()
})
</script>

<template>
  <div class="min-h-screen bg-theme-bg-primary dark:bg-theme-bg-primary-dark transition-colors duration-200">
    <!-- Review Details Section -->
    <div class="flex justify-center px-4 md:px-8 lg:px-40 mt-8 pb-16">
      <div class="w-full max-w-1280">
        <!-- Loading State -->
        <div v-if="loading" class="text-center py-16">
          <div class="inline-block animate-spin rounded-full h-12 w-12 border-b-2 border-theme-text-primary dark:border-theme-text-primary-dark"></div>
          <p class="font-tinos text-base text-theme-text-secondary dark:text-theme-text-secondary-dark mt-4">Loading review...</p>
        </div>

        <!-- Error State -->
        <div v-else-if="error" class="text-center py-16">
          <p class="font-tinos text-lg text-red-500">{{ error }}</p>
        </div>

        <!-- Review Content -->
        <div v-else-if="review" class="flex flex-col lg:flex-row gap-6">
          <!-- Left side: 75% - Game Image, Review Content, and Comments -->
          <div class="lg:w-3/4 flex flex-col gap-6">
            <!-- Row 1: Game Image and Review Content -->
            <div class="flex flex-col lg:flex-row gap-6">
              <!-- Column 1: 33.33% of 75% - Game Image -->
              <div class="lg:w-1/3">
                <div class="lg:sticky lg:top-4">
                  <GameImageComponent
                    v-if="review.game?.primaryImageUrl"
                    :image-url="review.game.primaryImageUrl"
                    :game-name="review.game.name"
                    size="large"
                    class="mb-4"
                  />
                  <div class="bg-theme-bg-secondary dark:bg-theme-bg-secondary-dark rounded-lg p-4">
                    <div class="text-center">
                      <div class="font-newsreader text-2xl font-bold text-theme-text-primary dark:text-theme-text-primary-dark">{{ review.rating.toFixed(1) }}</div>
                      <div class="font-tinos text-sm text-theme-text-secondary dark:text-theme-text-secondary-dark">Rating</div>
                    </div>
                  </div>
                </div>
              </div>

              <!-- Column 2: 66.66% of 75% - Main Content -->
              <div class="lg:w-2/3">
                <!-- <h2 class="font-newsreader text-3xl font-bold text-theme-text-primary dark:text-theme-text-primary-dark mb-6 border-b border-theme-border dark:border-theme-border-dark pb-4">
                  Review
                </h2> -->

                <ReviewCardBase
                  :review="review"
                  variant="detail"
                  @like-review="handleLikeReview"
                />
              </div>
            </div>

            <!-- Row 2: Comments Section (full width of 75%) -->
            <div class="w-full">
              <CommentList
                content-type="review"
                :content-id="review.id"
              />
            </div>
          </div>

          <!-- Right side: 25% - Actions Panel -->
          <div class="lg:w-1/4">
            <div class="lg:sticky lg:top-4">
              <ActionsPanel
                context="review"
                :can-edit="canEdit"
                :can-delete="canDelete"
                :is-liked="isReviewLiked"
                :like-count="reviewLikeCount"
                @like="handleLike"
                @edit="handleEdit"
                @delete="handleDelete"
                @rate="handleRate"
                @review="handleReview"
                @add-to-list="handleAddToList"
              />
            </div>
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