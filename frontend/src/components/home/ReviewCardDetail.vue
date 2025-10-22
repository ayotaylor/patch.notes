<script setup>
import GameImageComponent from './GameImageComponent.vue'
import { useReviewCard } from '@/composables/useReviewCard'

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
  stars,
  heartIcon,
  heartColor,
  navigateToGame
} = useReviewCard(props.review, Infinity) // No truncation for detail view

const handleLikeReview = () => {
  if (isLoggedIn.value) {
    emit('like-review', props.review)
  }
}
</script>

<template>
  <div class="grid grid-cols-4 gap-6">
    <!-- Column 1: Medium game image (25% width) -->
    <div class="col-span-1">
      <GameImageComponent
        :image-url="review.game.primaryImageUrl"
        :game-name="review.game.name"
        size="medium"
        class="cursor-pointer"
        @click="navigateToGame"
      />
    </div>

    <!-- Column 2: Review details (50% width) -->
    <div class="col-span-2 flex flex-col gap-4">
      <!-- Row 1: Small game image and "Review by {username}" -->
      <div class="flex items-center gap-3 pb-4 border-b border-gray-300">
        <img
          v-if="review.user.profileImageUrl"
          :src="review.user.profileImageUrl"
          :alt="review.user.displayName"
          class="w-8 h-8 rounded-full object-cover"
          @error="(e) => (e.target.style.display = 'none')"
        />
        <span class="font-tinos text-base text-river-bed">
          Review by <span class="text-cod-gray font-semibold">{{ review.user.displayName }}</span>
        </span>
      </div>

      <!-- Row 2: Game name and release year -->
      <div>
        <span class="font-newsreader text-2xl font-bold text-cod-gray cursor-pointer hover:underline" @click="navigateToGame">
          {{ review.game.name }}
        </span>
        <span v-if="review.game.releaseYear" class="font-tinos text-lg text-river-bed ml-3">
          ({{ review.game.releaseYear }})
        </span>
      </div>

      <!-- Row 3: Review score, heart, comments -->
      <div class="flex items-center gap-4">
        <!-- Stars -->
        <div class="flex items-center gap-1">
          <i
            v-for="n in stars.full"
            :key="'full-' + n"
            class="fas fa-star text-yellow-500 text-lg"
          ></i>
          <i
            v-if="stars.half"
            class="fas fa-star-half-alt text-yellow-500 text-lg"
          ></i>
          <i
            v-for="n in stars.empty"
            :key="'empty-' + n"
            class="far fa-star text-yellow-500 text-lg"
          ></i>
          <span class="font-tinos text-base text-river-bed ml-2">{{ review.rating }}/5</span>
        </div>

        <!-- Heart if liked by user -->
        <i v-if="review.game?.isLikedByUser" :class="[heartIcon, heartColor]"></i>

        <!-- Comments with speech bubble -->
        <div class="flex items-center gap-2 text-river-bed">
          <div class="w-6 h-6 border border-river-bed rounded flex items-center justify-center">
            <i class="fas fa-comment text-sm"></i>
          </div>
          <span class="font-tinos text-base">{{ review.commentCount || 0 }} comments</span>
        </div>
      </div>

      <!-- Row 4: Review text -->
      <div class="mt-2">
        <p class="font-tinos text-lg text-ebony-clay leading-7 whitespace-pre-wrap">
          {{ displayedReviewText }}
        </p>
      </div>

      <!-- Row 5: Like option and count -->
      <div class="flex items-center gap-3 mt-4">
        <button
          v-if="isLoggedIn"
          @click="handleLikeReview"
          class="flex items-center gap-2 px-4 py-2 border border-gray-300 rounded hover:bg-gray-50 transition-colors"
        >
          <i
            :class="review.isLikedByCurrentUser ? 'fas fa-thumbs-up text-blue-500' : 'far fa-thumbs-up text-river-bed'"
          ></i>
          <span class="font-tinos text-base text-cod-gray">
            {{ review.isLikedByCurrentUser ? 'Liked' : 'Like' }}
          </span>
        </button>
        <span class="font-tinos text-base text-river-bed">{{ review.likeCount || 0 }} likes</span>
      </div>
    </div>
  </div>
</template>
