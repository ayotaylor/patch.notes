<template>
  <div class="bg-theme-bg-secondary dark:bg-theme-bg-secondary-dark rounded-xl shadow-sm border border-theme-border dark:border-theme-border-dark transition-colors duration-200">
    <div class="p-6 border-b border-theme-border dark:border-theme-border-dark">
      <div class="flex justify-between items-center">
        <h3 class="font-newsreader text-xl font-bold text-theme-text-primary dark:text-theme-text-primary-dark flex items-center">
          <svg class="w-6 h-6 text-yellow-600 dark:text-yellow-400 mr-2" fill="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
            <path d="M12 17.27L18.18 21l-1.64-7.03L22 9.24l-7.19-.61L12 2 9.19 8.63 2 9.24l5.46 4.73L5.82 21z"/>
          </svg>
          {{ isOwnProfile ? 'My ' : '' }}Reviews
          <span v-if="totalReviews > 0" class="font-tinos text-base font-normal text-theme-text-secondary dark:text-theme-text-secondary-dark ml-2">({{ totalReviews }})</span>
        </h3>
      </div>
    </div>

    <div class="p-6">
      <!-- Loading State -->
      <div v-if="loading" class="flex flex-col items-center justify-center py-16">
        <div class="w-10 h-10 border-4 border-theme-btn-primary dark:border-theme-btn-primary-dark border-t-transparent rounded-full animate-spin mb-3"></div>
        <p class="font-tinos text-sm text-theme-text-secondary dark:text-theme-text-secondary-dark">Loading reviews...</p>
      </div>

      <!-- Reviews List -->
      <div v-else-if="reviews.length > 0">
        <div class="space-y-4">
          <ReviewCardBase
            v-for="review in displayedReviews"
            :key="review.id"
            :review="review"
            variant="list"
            :max-characters="200"
            @like-review="handleLikeReview"
          />
        </div>

        <!-- View All Reviews Link -->
        <div v-if="hasMoreReviews" class="text-center mt-6">
          <router-link
            :to="`/profile/${userId}/reviews`"
            class="px-6 py-3 bg-theme-bg-primary dark:bg-theme-bg-primary-dark text-theme-text-primary dark:text-theme-text-primary-dark border border-theme-border dark:border-theme-border-dark rounded-lg font-tinos text-base hover:bg-theme-border dark:hover:bg-theme-border-dark transition-all duration-200 inline-flex items-center"
          >
            View All {{ totalReviews }} Reviews
            <svg class="w-4 h-4 ml-2" fill="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
              <path d="M10 6L8.59 7.41 13.17 12l-4.58 4.59L10 18l6-6z"/>
            </svg>
          </router-link>
        </div>
      </div>

      <!-- Empty State -->
      <div v-else class="text-center py-16">
        <svg class="w-16 h-16 text-theme-text-secondary dark:text-theme-text-secondary-dark mx-auto mb-4" fill="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
          <path d="M12 17.27L18.18 21l-1.64-7.03L22 9.24l-7.19-.61L12 2 9.19 8.63 2 9.24l5.46 4.73L5.82 21z"/>
        </svg>
        <h6 class="font-newsreader text-lg font-bold text-theme-text-secondary dark:text-theme-text-secondary-dark mb-2">No Reviews Yet</h6>
        <p class="font-tinos text-sm text-theme-text-secondary dark:text-theme-text-secondary-dark mb-4">
          {{ isOwnProfile ? "You haven't written any reviews yet." : `They haven't written any reviews yet.` }}
        </p>
        <router-link
          v-if="isOwnProfile"
          to="/home-page"
          class="px-6 py-3 bg-theme-btn-primary dark:bg-theme-btn-primary-dark text-white rounded-lg font-tinos text-base hover:opacity-90 transition-all duration-200 inline-flex items-center"
        >
          <svg class="w-5 h-5 mr-2" fill="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
            <path d="M15.5 14h-.79l-.28-.27C15.41 12.59 16 11.11 16 9.5 16 5.91 13.09 3 9.5 3S3 5.91 3 9.5 5.91 16 9.5 16c1.61 0 3.09-.59 4.23-1.57l.27.28v.79l5 4.99L20.49 19l-4.99-5zm-6 0C7.01 14 5 11.99 5 9.5S7.01 5 9.5 5 14 7.01 14 9.5 11.99 14 9.5 14z"/>
          </svg>
          Find Games to Review
        </router-link>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'
import ReviewCardBase from './ReviewCardBase.vue'

const props = defineProps({
  reviews: {
    type: Array,
    default: () => []
  },
  loading: {
    type: Boolean,
    default: false
  },
  totalReviews: {
    type: Number,
    default: 0
  },
  userId: {
    type: String,
    required: true
  },
  isOwnProfile: {
    type: Boolean,
    default: false
  },
  maxDisplayed: {
    type: Number,
    default: 4
  }
})

const emit = defineEmits(['like-review'])

const displayedReviews = computed(() => {
  return props.reviews.slice(0, props.maxDisplayed)
})

const hasMoreReviews = computed(() => {
  return props.totalReviews > props.maxDisplayed
})

const handleLikeReview = (review) => {
  emit('like-review', review)
}
</script>
