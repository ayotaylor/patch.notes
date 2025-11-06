<script setup>
// import GameImageComponent from './GameImageComponent.vue'
import StarRating from '@/components/StarRating.vue'
import { useReviewCard } from '@/composables/useReviewCard'
import { useReviewLikes } from '@/composables/reviews/useReviewLikes'

const props = defineProps({
  review: {
    type: Object,
    required: true
  }
})

const emit = defineEmits(['like-review'])

const {
  isLoggedIn,
  displayedReviewText,
  navigateToGame
} = useReviewCard(props.review, Infinity) // No truncation for detail view

const { toggleLike } = useReviewLikes()

const handleLikeReview = async () => {
  if (isLoggedIn.value) {
    await toggleLike(props.review, () => {
      // Emit event to parent for potential UI updates
      emit('like-review', props.review)
    })
  }
}
</script>

<template>
  <div class="grid gap-6">
    <!-- Column 1: Medium game image (25% width) -->
    <!-- <div class="col-span-1">
      <GameImageComponent
        :image-url="review.game.primaryImageUrl"
        :game-name="review.game.name"
        size="medium"
        class="cursor-pointer"
        @click="navigateToGame"
      />
    </div> -->

    <!-- Column 2: Review details (50% width) -->
    <div class="flex flex-col gap-4">
      <!-- Row 1: Small user profile and "Review by {username}" -->
      <div class="flex items-center gap-3 pb-4 border-b border-theme-border dark:border-theme-border-dark">
        <img
          v-if="review.user.profileImageUrl"
          :src="review.user.profileImageUrl"
          :alt="review.user.displayName"
          class="w-8 h-8 rounded-full object-cover"
          @error="(e) => (e.target.style.display = 'none')"
        />
        <span class="font-tinos text-base text-theme-text-secondary dark:text-theme-text-secondary-dark">
          Review by <span class="text-theme-text-primary dark:text-theme-text-primary-dark font-semibold">{{ review.user.displayName }}</span>
        </span>
      </div>

      <!-- Row 2: Game name and release year -->
      <div>
        <span class="font-newsreader text-2xl font-bold text-theme-text-primary dark:text-theme-text-primary-dark cursor-pointer hover:underline" @click="navigateToGame">
          {{ review.game.name }}
        </span>
        <span v-if="review.game.releaseYear" class="font-tinos text-lg text-theme-text-secondary dark:text-theme-text-secondary-dark ml-3">
          ({{ review.game.releaseYear }})
        </span>
      </div>

      <!-- Row 3: Review score, heart, comments -->
      <div class="flex items-center gap-4">
        <!-- Stars -->
        <StarRating :rating="review.rating" size="medium" show-rating />

        <!-- Heart if liked by user -->
        <svg
          v-if="review.game?.isLikedByUser"
          class="w-5 h-5 text-red-500"
          viewBox="0 0 20 20"
          fill="currentColor"
        >
          <path fill-rule="evenodd" d="M3.172 5.172a4 4 0 015.656 0L10 6.343l1.172-1.171a4 4 0 115.656 5.656L10 17.657l-6.828-6.829a4 4 0 010-5.656z" clip-rule="evenodd" />
        </svg>

        <!-- Comments with speech bubble -->
        <div class="flex items-center gap-2 text-theme-text-secondary dark:text-theme-text-secondary-dark">
          <div class="w-6 h-6 border border-theme-text-secondary dark:border-theme-text-secondary-dark rounded flex items-center justify-center">
            <svg class="w-4 h-4" viewBox="0 0 20 20" fill="currentColor">
              <path fill-rule="evenodd" d="M18 10c0 3.866-3.582 7-8 7a8.841 8.841 0 01-4.083-.98L2 17l1.338-3.123C2.493 12.767 2 11.434 2 10c0-3.866 3.582-7 8-7s8 3.134 8 7zM7 9H5v2h2V9zm8 0h-2v2h2V9zM9 9h2v2H9V9z" clip-rule="evenodd" />
            </svg>
          </div>
          <span class="font-tinos text-base">{{ review.commentCount || 0 }} comments</span>
        </div>
      </div>

      <!-- Row 4: Review text -->
      <div class="mt-2">
        <p class="font-tinos text-lg text-theme-text-primary dark:text-theme-text-primary-dark leading-7 whitespace-pre-wrap">
          {{ displayedReviewText }}
        </p>
      </div>

      <!-- Row 5: Like option and count -->
      <div class="flex items-center gap-3 mt-4">
        <button
          v-if="isLoggedIn"
          @click="handleLikeReview"
          class="flex items-center gap-2 px-4 py-2 border border-theme-border dark:border-theme-border-dark rounded hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors"
        >
          <svg
            v-if="review.isLikedByCurrentUser"
            class="w-5 h-5 text-blue-500"
            viewBox="0 0 20 20"
            fill="currentColor"
          >
            <path d="M2 10.5a1.5 1.5 0 113 0v6a1.5 1.5 0 01-3 0v-6zM6 10.333v5.43a2 2 0 001.106 1.79l.05.025A4 4 0 008.943 18h5.416a2 2 0 001.962-1.608l1.2-6A2 2 0 0015.56 8H12V4a2 2 0 00-2-2 1 1 0 00-1 1v.667a4 4 0 01-.8 2.4L6.8 7.933a4 4 0 00-.8 2.4z" />
          </svg>
          <svg
            v-else
            class="w-5 h-5 text-theme-text-secondary dark:text-theme-text-secondary-dark"
            viewBox="0 0 20 20"
            fill="none"
            stroke="currentColor"
          >
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M14 10h4.764a2 2 0 011.789 2.894l-3.5 7A2 2 0 0115.263 21h-4.017c-.163 0-.326-.02-.485-.06L7 20m7-10V5a2 2 0 00-2-2h-.095c-.5 0-.905.405-.905.905 0 .714-.211 1.412-.608 2.006L7 11v9m7-10h-2M7 20H5a2 2 0 01-2-2v-6a2 2 0 012-2h2.5" transform="scale(0.85) translate(1.5, 0)" />
          </svg>
          <span class="font-tinos text-base text-theme-text-primary dark:text-theme-text-primary-dark">
            {{ review.isLikedByCurrentUser ? 'Liked' : 'Like' }}
          </span>
        </button>
        <span class="font-tinos text-base text-theme-text-secondary dark:text-theme-text-secondary-dark">{{ review.likeCount || 0 }} likes</span>
      </div>
    </div>
  </div>
</template>
