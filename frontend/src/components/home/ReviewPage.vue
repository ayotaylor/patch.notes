<script setup>
import { ref, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import HomeHeader from './HeaderBar.vue'
import HomeNavigation from './NavigationBar.vue'
import ReviewCardBase from './ReviewCardBase.vue'
import { useReviewLikes } from '@/composables/reviews/useReviewLikes'

const { toggleLike } = useReviewLikes()

const route = useRoute()

// Props from route params
const username = ref(route.params.username)
const gameSlug = ref(route.params.gameSlug)

// State
const review = ref(null)
const loading = ref(true)
const error = ref(null)

// Placeholder comments data
const placeholderComments = ref([
  {
    id: 1,
    user: {
      username: 'GamerDude123',
      profileImageUrl: 'https://via.placeholder.com/40'
    },
    text: 'Great review! I completely agree with your assessment of the combat system.'
  },
  {
    id: 2,
    user: {
      username: 'JaneTheGamer',
      profileImageUrl: 'https://via.placeholder.com/40'
    },
    text: 'This game has been on my wishlist for a while. Your review convinced me to finally pick it up!'
  },
  {
    id: 3,
    user: {
      username: 'RetroFan88',
      profileImageUrl: 'https://via.placeholder.com/40'
    },
    text: 'I had a different experience with the story, but I respect your perspective. Solid review overall.'
  }
])

// Placeholder review data (will be replaced with API call)
const loadReview = async () => {
  loading.value = true
  error.value = null

  try {
    // TODO: Replace with actual API call
    // const response = await reviewService.getReview(username.value, gameSlug.value)
    // review.value = new Review(response.data)

    // Placeholder data for now
    review.value = {
      id: 1,
      reviewText: 'This is an absolutely incredible game that redefines the open-world genre. From the moment you step into the world, you\'re immediately immersed in a rich, vibrant environment that feels alive and dynamic. The attention to detail is astounding, from the weather systems to the NPC interactions.\n\nThe combat system is fluid and responsive, offering a perfect balance between challenge and accessibility. Boss fights are epic and memorable, each requiring a unique strategy to overcome. The variety of weapons and abilities keeps the gameplay fresh throughout the entire experience.\n\nThe story is compelling and emotionally resonant, with characters that feel real and relatable. The voice acting is top-notch, and the soundtrack perfectly complements every moment of the journey.\n\nIf you\'re a fan of open-world games, this is an absolute must-play. It sets a new standard for what the genre can achieve.',
      rating: 4.5,
      isLikedByCurrentUser: false,
      user: {
        id: 1,
        username: username.value,
        displayName: 'John Doe',
        profileImageUrl: 'https://via.placeholder.com/50'
      },
      game: {
        id: 1,
        name: 'Elden Ring',
        slug: gameSlug.value,
        releaseYear: 2022,
        primaryImageUrl: 'https://images.igdb.com/igdb/image/upload/t_cover_big/co4jni.jpg',
        isLikedByUser: true
      },
      likeCount: 142,
      commentCount: 23,
      relativeDate: '2 days ago',
      createdAt: new Date('2024-01-15')
    }
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

onMounted(() => {
  loadReview()
})
</script>

<template>
  <div class="min-h-screen bg-[#F6F7F7]">
    <!-- Header Component -->
    <HomeHeader />

    <!-- Navigation Component -->
    <HomeNavigation />

    <!-- Review Details Section -->
    <div class="flex justify-center px-4 md:px-8 lg:px-40 mt-8">
      <div class="w-full max-w-1280">
        <!-- Loading State -->
        <div v-if="loading" class="text-center py-16">
          <div class="inline-block animate-spin rounded-full h-12 w-12 border-b-2 border-cod-gray"></div>
          <p class="font-tinos text-base text-river-bed mt-4">Loading review...</p>
        </div>

        <!-- Error State -->
        <div v-else-if="error" class="text-center py-16">
          <p class="font-tinos text-lg text-red-500">{{ error }}</p>
        </div>

        <!-- Review Content -->
        <div v-else-if="review">
          <h2 class="font-newsreader text-3xl font-bold text-cod-gray mb-6 border-b border-gray-300 pb-4">
            Review
          </h2>

          <ReviewCardBase
            :review="review"
            variant="detail"
            @like-review="handleLikeReview"
          />
        </div>
      </div>
    </div>

    <!-- Comments Section (Placeholder) -->
    <div v-if="review" class="flex justify-center px-4 md:px-8 lg:px-40 mt-12 mb-16">
      <div class="w-full max-w-1280">
        <div class="border-t border-gray-300 pt-8">
          <!-- Comments Header -->
          <h3 class="font-newsreader text-2xl font-bold text-cod-gray mb-6">
            Comments ({{ placeholderComments.length }})
          </h3>

          <!-- Comments List -->
          <div class="space-y-6">
            <div
              v-for="comment in placeholderComments"
              :key="comment.id"
              class="border-b border-gray-200 pb-6 last:border-b-0"
            >
              <div class="grid grid-cols-4 gap-4">
                <!-- Column 1: User info (25% width) -->
                <div class="col-span-1">
                  <div class="flex items-center gap-2">
                    <img
                      :src="comment.user.profileImageUrl"
                      :alt="comment.user.username"
                      class="w-8 h-8 rounded-full object-cover"
                      @error="(e) => (e.target.style.display = 'none')"
                    />
                    <span class="font-tinos text-sm text-cod-gray font-semibold">
                      {{ comment.user.username }}
                    </span>
                  </div>
                </div>

                <!-- Column 2: Comment text (75% width) -->
                <div class="col-span-3">
                  <p class="font-tinos text-base text-ebony-clay leading-6">
                    {{ comment.text }}
                  </p>
                </div>
              </div>
            </div>
          </div>

          <!-- Placeholder message -->
          <div class="mt-8 p-4 bg-gray-100 rounded-lg">
            <p class="font-tinos text-sm text-river-bed italic text-center">
              Comment functionality will be implemented in a future update.
            </p>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>