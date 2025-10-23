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
  showMoreLink,
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
  <div class="grid grid-cols-4 gap-4 w-3/4">
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

    <!-- Column 2: Review info (75% width) -->
    <div class="col-span-3 flex flex-col gap-3">
      <!-- Row 1: Game name and release year -->
      <div>
        <span class="font-newsreader text-xl font-bold text-cod-gray cursor-pointer hover:underline" @click="navigateToGame">
          {{ review.game.name }}
        </span>
        <span v-if="review.game.releaseYear" class="font-tinos text-base text-river-bed ml-2">
          ({{ review.game.releaseYear }})
        </span>
      </div>

      <!-- Row 2: User pic and username -->
      <div class="flex items-center gap-2">
        <img
          v-if="review.user.profileImageUrl"
          :src="review.user.profileImageUrl"
          :alt="review.user.displayName"
          class="w-6 h-6 rounded-full object-cover"
          @error="(e) => (e.target.style.display = 'none')"
        />
        <span class="font-tinos text-sm text-river-bed">{{ review.user.displayName }}</span>
      </div>

      <!-- Row 3: Review score, heart, comments, date -->
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
        <div class="flex items-center gap-1 text-river-bed">
          <div class="w-5 h-5 border border-river-bed rounded flex items-center justify-center">
            <i class="fas fa-comment text-xs"></i>
          </div>
          <span class="font-tinos text-sm">{{ review.commentCount || 0 }}</span>
        </div>

        <!-- Review date -->
        <span class="font-tinos text-sm text-river-bed">{{ review.relativeDate }}</span>
      </div>

      <!-- Row 4: Review text with "more" link -->
      <div>
        <p class="font-tinos text-base text-ebony-clay leading-6 inline">
          {{ displayedReviewText }}
        </p>
        <button
          v-if="showMoreLink"
          @click="navigateToReview"
          class="font-tinos text-base text-blue-500 hover:underline ml-1"
        >
          read more
        </button>
      </div>

      <!-- Row 5: Like option and count -->
      <div class="flex items-center gap-2 mt-2">
        <button
          v-if="isLoggedIn"
          @click="handleLikeReview"
          class="flex items-center gap-1 text-river-bed hover:text-cod-gray transition-colors"
        >
          <i
            :class="review.isLikedByCurrentUser ? 'fas fa-thumbs-up text-blue-500' : 'far fa-thumbs-up'"
            class="text-sm"
          ></i>
        </button>
        <span class="font-tinos text-sm text-river-bed">{{ review.likeCount || 0 }} likes</span>
      </div>
    </div>
  </div>
</template>
