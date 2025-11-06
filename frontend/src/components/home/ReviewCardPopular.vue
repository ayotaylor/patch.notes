<script setup>
import GameImageComponent from './GameImageComponent.vue'
import StarRating from '@/components/StarRating.vue'
import { useReviewCard } from '@/composables/useReviewCard'
import { useReviewLikes } from '@/composables/reviews/useReviewLikes'

const props = defineProps({
  review: {
    type: Object,
    required: true
  },
  maxCharacters: {
    type: Number,
    default: 460
  }
})

const emit = defineEmits(['like-review'])

const {
  isLoggedIn,
  displayedReviewText,
  navigateToReview,
  navigateToGame
} = useReviewCard(props.review, props.maxCharacters)

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
  <div class="flex flex-col gap-4">
    <!-- Row 1: Game image and user/game info -->
    <div class="grid grid-cols-4 gap-4">
      <!-- Column 1: Small game image (25% width) -->
      <div class="col-span-1">
        <GameImageComponent
          :image-url="review.game.primaryImageUrl"
          :game-name="review.game.name"
          size="small"
          class="cursor-pointer"
          @click="navigateToGame"
        />
      </div>

      <!-- Column 2: User/game info (75% width) with padding-top for bottom 75% alignment -->
      <div class="col-span-3 flex flex-col justify-end pb-1">
        <!-- Line 1: User pic and name -->
        <div class="flex items-center gap-2 mb-2">
          <img
            v-if="review.user.profileImageUrl"
            :src="review.user.profileImageUrl"
            :alt="review.user.displayName"
            class="w-6 h-6 rounded-full object-cover"
            @error="(e) => (e.target.style.display = 'none')"
          />
          <span class="font-tinos text-sm text-theme-text-secondary dark:text-theme-text-secondary-dark">{{ review.user.displayName }}</span>
        </div>

        <!-- Line 2: Game name and release year -->
        <div class="mb-2">
          <span class="font-tinos text-base text-theme-text-primary dark:text-theme-text-primary-dark cursor-pointer hover:underline" @click="navigateToGame">
            {{ review.game.name }}
          </span>
          <span v-if="review.game.releaseYear" class="font-tinos text-sm text-theme-text-secondary dark:text-theme-text-secondary-dark ml-2">
            ({{ review.game.releaseYear }})
          </span>
        </div>

        <!-- Line 3: Review score, heart, comments -->
        <div class="flex items-center gap-3">
          <!-- Stars -->
          <StarRating :rating="review.rating" size="small" />

          <!-- Heart if liked by user -->
          <svg
            v-if="review.game?.isLikedByUser"
            class="w-4 h-4 text-red-500"
            viewBox="0 0 20 20"
            fill="currentColor"
          >
            <path fill-rule="evenodd" d="M3.172 5.172a4 4 0 015.656 0L10 6.343l1.172-1.171a4 4 0 115.656 5.656L10 17.657l-6.828-6.829a4 4 0 010-5.656z" clip-rule="evenodd" />
          </svg>

          <!-- Comments with speech bubble -->
          <div class="flex items-center gap-1 text-theme-text-secondary dark:text-theme-text-secondary-dark">
            <div class="w-5 h-5 border border-theme-text-secondary dark:border-theme-text-secondary-dark rounded flex items-center justify-center">
              <svg class="w-3 h-3" viewBox="0 0 20 20" fill="currentColor">
                <path fill-rule="evenodd" d="M18 10c0 3.866-3.582 7-8 7a8.841 8.841 0 01-4.083-.98L2 17l1.338-3.123C2.493 12.767 2 11.434 2 10c0-3.866 3.582-7 8-7s8 3.134 8 7zM7 9H5v2h2V9zm8 0h-2v2h2V9zM9 9h2v2H9V9z" clip-rule="evenodd" />
              </svg>
            </div>
            <span class="font-tinos text-sm">{{ review.commentCount || 0 }}</span>
          </div>
        </div>
      </div>
    </div>

    <!-- Row 2: Review content (full width) -->
    <div class="w-full">
      <p class="font-tinos text-base text-theme-text-primary dark:text-theme-text-primary-dark leading-6 cursor-pointer" @click="navigateToReview">
        {{ displayedReviewText }}
      </p>
    </div>

    <!-- Row 3: Like option and count -->
    <div class="flex items-center gap-2">
      <button
        v-if="isLoggedIn"
        @click="handleLikeReview"
        class="flex items-center gap-1 text-theme-text-secondary dark:text-theme-text-secondary-dark hover:text-theme-text-primary dark:hover:text-theme-text-primary-dark transition-colors"
      >
        <svg
          v-if="review.isLikedByCurrentUser"
          class="w-4 h-4 text-blue-500"
          viewBox="0 0 20 20"
          fill="currentColor"
        >
          <path d="M2 10.5a1.5 1.5 0 113 0v6a1.5 1.5 0 01-3 0v-6zM6 10.333v5.43a2 2 0 001.106 1.79l.05.025A4 4 0 008.943 18h5.416a2 2 0 001.962-1.608l1.2-6A2 2 0 0015.56 8H12V4a2 2 0 00-2-2 1 1 0 00-1 1v.667a4 4 0 01-.8 2.4L6.8 7.933a4 4 0 00-.8 2.4z" />
        </svg>
        <svg
          v-else
          class="w-4 h-4"
          viewBox="0 0 20 20"
          fill="none"
          stroke="currentColor"
        >
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M14 10h4.764a2 2 0 011.789 2.894l-3.5 7A2 2 0 0115.263 21h-4.017c-.163 0-.326-.02-.485-.06L7 20m7-10V5a2 2 0 00-2-2h-.095c-.5 0-.905.405-.905.905 0 .714-.211 1.412-.608 2.006L7 11v9m7-10h-2M7 20H5a2 2 0 01-2-2v-6a2 2 0 012-2h2.5" transform="scale(0.85) translate(1.5, 0)" />
        </svg>
      </button>
      <span class="font-tinos text-sm text-theme-text-secondary dark:text-theme-text-secondary-dark">{{ review.likeCount || 0 }} likes</span>
    </div>
  </div>
</template>
