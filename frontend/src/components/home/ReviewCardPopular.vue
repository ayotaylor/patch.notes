<script setup>
import GameImageComponent from './GameImageComponent.vue'
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
  stars,
  heartIcon,
  heartColor,
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
          <div class="flex items-center gap-1">
            <i
              v-for="n in stars.full"
              :key="'full-' + n"
              class="fas fa-star text-yellow-500 text-sm"
            ></i>
            <i
              v-if="stars.half"
              class="fas fa-star-half-alt text-yellow-500 text-sm"
            ></i>
            <i
              v-for="n in stars.empty"
              :key="'empty-' + n"
              class="far fa-star text-yellow-500 text-sm"
            ></i>
          </div>

          <!-- Heart if liked by user -->
          <i v-if="review.game?.isLikedByUser" :class="[heartIcon, heartColor, 'text-sm']"></i>

          <!-- Comments with speech bubble -->
          <div class="flex items-center gap-1 text-theme-text-secondary dark:text-theme-text-secondary-dark">
            <div class="w-5 h-5 border border-theme-text-secondary dark:border-theme-text-secondary-dark rounded flex items-center justify-center">
              <i class="fas fa-comment text-xs"></i>
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
        <i
          :class="review.isLikedByCurrentUser ? 'fas fa-thumbs-up text-blue-500' : 'far fa-thumbs-up'"
          class="text-sm"
        ></i>
      </button>
      <span class="font-tinos text-sm text-theme-text-secondary dark:text-theme-text-secondary-dark">{{ review.likeCount || 0 }} likes</span>
    </div>
  </div>
</template>
