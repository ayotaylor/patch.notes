<script setup>
import { ref, onMounted } from 'vue'
import ReviewCardBase from './ReviewCardBase.vue'
import { useReviews } from '@/composables/reviews/useReviews'
import { getRelativeTimeDetailed } from '@/utils/dateUtils'

// Composables
const { loadReviews: fetchReviews } = useReviews()

// State
const reviews = ref([])
const loading = ref(true)
const error = ref(null)
const pageSize = ref(20)

const loadReviews = async () => {
  loading.value = true
  error.value = null

  try {
    const response = await fetchReviews({ page: 1, pageSize: pageSize.value })

    // Add relative date to each review
    reviews.value = response.data.map(review => ({
      ...review,
      relativeDate: getRelativeTimeDetailed(review.createdAt)
    }))
  } catch (err) {
    error.value = 'Failed to load reviews'
    console.error('Error loading reviews:', err)
  } finally {
    loading.value = false
  }
}

const handleLikeReview = (review) => {
  // The like functionality is already handled by ReviewCardList via useReviewLikes
  // This handler is kept for potential future use
  console.log('Review liked:', review.id)
}

onMounted(() => {
  loadReviews()
})
</script>

<template>
  <div class="min-h-screen bg-theme-bg-primary dark:bg-theme-bg-primary-dark transition-colors duration-200">

    <!-- Reviews List Section -->
    <div class="flex justify-center px-4 md:px-8 lg:px-40 mt-8">
      <div class="w-full max-w-1280">
        <h2 class="font-newsreader text-3xl font-bold text-theme-text-primary dark:text-theme-text-primary-dark mb-6 border-b border-theme-border dark:border-theme-border-dark pb-4">
          All Reviews
        </h2>

        <!-- Loading State -->
        <div v-if="loading" class="text-center py-16">
          <div class="inline-block animate-spin rounded-full h-12 w-12 border-b-2 border-theme-text-primary dark:border-theme-text-primary-dark"></div>
          <p class="font-tinos text-base text-theme-text-secondary dark:text-theme-text-secondary-dark mt-4">Loading reviews...</p>
        </div>

        <!-- Error State -->
        <div v-else-if="error" class="text-center py-16">
          <p class="font-tinos text-lg text-red-500">{{ error }}</p>
        </div>

        <!-- Reviews List (75% width, left justified) -->
        <div v-else-if="reviews.length > 0" class="space-y-6 mb-16">
          <ReviewCardBase
            v-for="review in reviews"
            :key="review.id"
            :review="review"
            variant="list"
            :max-characters="460"
            @like-review="handleLikeReview"
          />
        </div>

        <!-- Empty State -->
        <div v-else class="text-center py-16">
          <p class="font-tinos text-lg text-theme-text-secondary dark:text-theme-text-secondary-dark">No reviews found</p>
        </div>
      </div>
    </div>
  </div>
</template>