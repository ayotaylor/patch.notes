<script setup>
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useGamesStore } from '@/stores/gamesStore'
import HomeHeader from './HeaderBar.vue'
import HomeNavigation from './NavigationBar.vue'
import GameCarousel from './GameCarousel.vue'
import ReviewCardBase from './ReviewCardBase.vue'
import { useReviews } from '@/composables/reviews/useReviews'
import { useReviewLikes } from '@/composables/reviews/useReviewLikes'

const router = useRouter()
const gamesStore = useGamesStore()
const { loadReviews } = useReviews()
const { toggleLike, loadLikeStatusBatch } = useReviewLikes()

// Reviews State
const popularReviews = ref([])
const loadingReviews = ref(true)
const reviewsLoadError = ref(false)
const retryAttempt = ref(0)
const MAX_RETRIES = 2

// Recently Reviewed Games State
const recentlyReviewedGames = ref([])
const loadingRecentGames = ref(true)
const recentGamesError = ref(false)

const handleGameClick = (game) => {
  // Navigate to game details page using slug or id
  if (game.slug) {
    router.push({ name: 'game-details', params: { slug: game.slug } })
  } else if (game.id) {
    router.push({ name: 'game-details', params: { id: game.id } })
  }
}

// Load recently reviewed games from API using store
const fetchRecentlyReviewedGames = async () => {
  try {
    loadingRecentGames.value = true
    recentGamesError.value = false

    const games = await gamesStore.fetchLatestReviewedGames(12, false)
    recentlyReviewedGames.value = games
  } catch (error) {
    console.error('Error loading recently reviewed games:', error)
    recentGamesError.value = true

    // Retry with fewer games
    try {
      const games = await gamesStore.fetchLatestReviewedGames(6, false)
      recentlyReviewedGames.value = games
      recentGamesError.value = false
    } catch (retryError) {
      console.error('Retry failed for recently reviewed games:', retryError)
      // Keep error state
    }
  } finally {
    loadingRecentGames.value = false
  }
}

// Load popular reviews from API
const fetchPopularReviews = async () => {
  try {
    loadingReviews.value = true
    reviewsLoadError.value = false

    const result = await loadReviews({ page: 1, pageSize: 6 })
    popularReviews.value = result.data

    // Load like status for all reviews if user is authenticated
    if (result.data.length > 0) {
      await loadLikeStatusBatch(result.data)
    }
  } catch (error) {
    console.error('Error loading popular reviews:', error)
    reviewsLoadError.value = true

    // Retry with fewer reviews if first attempt failed
    if (retryAttempt.value < MAX_RETRIES) {
      retryAttempt.value++
      const retryPageSize = Math.max(3, 6 - retryAttempt.value * 2)
      console.log(`Retrying with pageSize: ${retryPageSize}`)

      try {
        const result = await loadReviews({ page: 1, pageSize: retryPageSize })
        popularReviews.value = result.data
        reviewsLoadError.value = false

        if (result.data.length > 0) {
          await loadLikeStatusBatch(result.data)
        }
      } catch (retryError) {
        console.error('Retry failed:', retryError)
        // Keep error state, don't show section
      }
    }
  } finally {
    loadingReviews.value = false
  }
}

const handleLikeReview = async (review) => {
  await toggleLike(review, (wasLiked) => {
    // Update like count in local review data
    review.likeCount = (review.likeCount || 0) + (wasLiked ? -1 : 1)
  })
}

// Load data on mount
onMounted(() => {
  fetchPopularReviews()
  fetchRecentlyReviewedGames()
})

const popularLists = [
  {
    title: 'The Greatest RPGs of All Time',
    author: 'GamerGirl92',
    likes: '1.2k',
    image: 'https://api.builder.io/api/v1/image/assets/TEMP/543add445417e596a718defbf54e4aa007a27560?width=800',
    avatar: 'https://api.builder.io/api/v1/image/assets/TEMP/0d9d0255c0c70c57840eac6244ec7935cc307d23?width=48',
  },
  {
    title: 'Indie Gems You Might Have Missed',
    author: 'RetroKing',
    likes: '876',
    image: 'https://api.builder.io/api/v1/image/assets/TEMP/3bbca8c9e6b6c5ae4b954e5e47ffd98702adce35?width=800',
    avatar: 'https://api.builder.io/api/v1/image/assets/TEMP/c42469ab39146cb23888fcc95433e76c06a8ec78?width=48',
  },
  {
    title: 'Hardest Games Ever Made',
    author: 'SoulslikeFan',
    likes: '2.5k',
    image: 'https://api.builder.io/api/v1/image/assets/TEMP/9d74285b6f34ccd2fdc9f81402f5ded8cc026dc5?width=800',
    avatar: 'https://api.builder.io/api/v1/image/assets/TEMP/001a48cccf735939c0b4babf3fb27bfcbd4f319d?width=48',
  },
]

const popularMembers = [
  {
    name: 'Jane Doe',
    avatar: 'https://api.builder.io/api/v1/image/assets/TEMP/55268c3c8278136914ad3fc2c8674959eedf6bde?width=192',
  },
  {
    name: 'John Smith',
    avatar: 'https://api.builder.io/api/v1/image/assets/TEMP/e91e04677d2fb3dd66f0821d210c18bb5acf9dfc?width=192',
  },
  {
    name: 'Alex Ray',
    avatar: 'https://api.builder.io/api/v1/image/assets/TEMP/36f7fe8b6319dee36537570df19423e5a9839fc0?width=192',
  },
  {
    name: 'Sarah Chen',
    avatar: 'https://api.builder.io/api/v1/image/assets/TEMP/4168d38c2da8f88eb2ceeb275fc9b4fe28bd94df?width=192',
  },
  {
    name: 'Emily Carter',
    avatar: 'https://api.builder.io/api/v1/image/assets/TEMP/22a1fed35e0669138b94ae7aaf974e82e9c56bcc?width=192',
  },
]
</script>

<template>
  <div class="min-h-screen bg-[#F6F7F7]">
    <!-- Header Component -->
    <HomeHeader />

    <!-- Navigation Component -->
    <HomeNavigation />

    <!-- Hero Section -->
    <div class="flex justify-center px-4 md:px-8 lg:px-40 mt-8">
      <div class="w-full max-w-1280">
        <div class="relative h-[520px] rounded-lg flex items-end justify-center bg-cover bg-center" style="
            background-image: linear-gradient(180deg, rgba(0, 0, 0, 0.5) 0%, rgba(0, 0, 0, 0.7) 100%),
              url('https://api.builder.io/api/v1/image/assets/TEMP/709559efbb027911fac46667344019496767a351?width=2560');
          ">
          <div class="text-center px-4 max-w-3xl pb-16">
            <h2 class="text-white font-newsreader text-2xl md:text-4xl font-light leading-tight tracking-tight mb-6">
              Track the games you've played.<br />
              Save the ones you want to play.<br />
              Tell your friends what's good.
            </h2>
            <button
              class="h-12 min-w-21 px-8 bg-white text-cod-gray border border-cod-gray font-roboto font-bold text-lg tracking-wide rounded">
              Sign Up
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Popular Games Section -->
    <div class="flex justify-center px-4 md:px-8 lg:px-40 mt-8">
      <div class="w-full max-w-1280">
        <GameCarousel
          title="Popular Games"
          :show-border="true"
          :show-view-all="true"
          view-all-link="/games/popular"
          image-size="default"
          @game-click="handleGameClick"
        />
      </div>
    </div>

    <!-- Recently Reviewed Games Section -->
    <div v-if="!recentGamesError || loadingRecentGames || recentlyReviewedGames.length > 0" class="flex justify-center px-4 md:px-8 lg:px-40 mt-8">
      <div class="w-full max-w-1280">
        <GameCarousel
          title="Recently Reviewed"
          :show-border="true"
          :show-view-all="true"
          view-all-link="/games/recent"
          :games="recentlyReviewedGames"
          :loading="loadingRecentGames"
          image-size="default"
          @game-click="handleGameClick"
        />
      </div>
    </div>

    <!-- Popular Reviews Section -->
    <div v-if="!reviewsLoadError || loadingReviews || popularReviews.length > 0" class="flex justify-center px-4 md:px-8 lg:px-40 mt-8">
      <div class="w-full max-w-1280">
        <!-- Section Header with View All Link -->
        <div class="flex justify-between items-center mb-4 border-b border-gray-300 pb-2">
          <h3 class="font-newsreader text-2xl font-bold text-cod-gray">Popular Reviews</h3>
          <router-link
            v-if="!loadingReviews && popularReviews.length > 0"
            to="/reviews"
            class="font-tinos text-base text-blue-600 hover:text-blue-800 hover:underline"
          >
            View All →
          </router-link>
        </div>

        <!-- Loading Skeleton -->
        <div v-if="loadingReviews" class="grid grid-cols-1 md:grid-cols-2 gap-6">
          <div v-for="n in 6" :key="`skeleton-${n}`" class="animate-pulse">
            <div class="flex flex-col gap-4">
              <!-- Row 1: Game image and user/game info -->
              <div class="grid grid-cols-4 gap-4">
                <!-- Skeleton image -->
                <div class="col-span-1">
                  <div class="bg-gray-300 rounded-lg w-full aspect-[3/4]"></div>
                </div>
                <!-- Skeleton text -->
                <div class="col-span-3 flex flex-col justify-end pb-1 gap-2">
                  <div class="h-4 bg-gray-300 rounded w-3/4"></div>
                  <div class="h-4 bg-gray-300 rounded w-1/2"></div>
                  <div class="flex gap-2">
                    <div class="h-3 bg-gray-300 rounded w-20"></div>
                    <div class="h-3 bg-gray-300 rounded w-20"></div>
                  </div>
                </div>
              </div>
              <!-- Row 2: Review text skeleton -->
              <div class="space-y-2">
                <div class="h-3 bg-gray-300 rounded"></div>
                <div class="h-3 bg-gray-300 rounded"></div>
                <div class="h-3 bg-gray-300 rounded w-5/6"></div>
              </div>
              <!-- Row 3: Like button skeleton -->
              <div class="h-3 bg-gray-300 rounded w-24"></div>
            </div>
          </div>
        </div>

        <!-- Reviews Grid -->
        <div v-else-if="popularReviews.length > 0" class="grid grid-cols-1 md:grid-cols-2 gap-6">
          <ReviewCardBase
            v-for="review in popularReviews"
            :key="review.id"
            :review="review"
            variant="popular"
            @like-review="handleLikeReview"
          />
        </div>

        <!-- Empty State (only if not loading and no error) -->
        <div v-else-if="!loadingReviews && !reviewsLoadError" class="text-center py-12">
          <p class="font-tinos text-lg text-river-bed">No reviews available yet.</p>
        </div>
      </div>
    </div>

    <!-- Popular Lists Section -->
    <div class="flex justify-center px-4 md:px-8 lg:px-40 mt-8">
      <div class="w-full max-w-1280">
        <h3 class="font-newsreader text-2xl font-bold text-cod-gray mb-4 border-b border-gray-300">Popular Lists</h3>
        <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          <div v-for="(list, index) in popularLists" :key="index" class="flex flex-col">
            <img :src="list.image" :alt="list.title" class="w-full aspect-video object-cover rounded-lg mb-4" />
            <h4 class="font-newsreader text-lg font-bold text-cod-gray mb-2">{{ list.title }}</h4>
            <div class="flex items-center gap-2">
              <img :src="list.avatar" :alt="list.author" class="w-6 h-6 rounded-full" />
              <span class="font-tinos text-sm text-river-bed">{{ list.author }}</span>
              <svg class="w-4 h-5" viewBox="0 0 17 21" fill="none" xmlns="http://www.w3.org/2000/svg">
                <path
                  d="M12.6879 16.8906H5.35454V8.22396L10.0212 3.55729L10.8545 4.39062C10.9323 4.4684 10.9962 4.57396 11.0462 4.70729C11.0962 4.84062 11.1212 4.9684 11.1212 5.09062V5.32396L10.3879 8.22396H14.6879C15.0434 8.22396 15.3545 8.35729 15.6212 8.62396C15.8879 8.89062 16.0212 9.20174 16.0212 9.55729V10.8906C16.0212 10.9684 16.0101 11.0517 15.9879 11.1406C15.9657 11.2295 15.9434 11.3128 15.9212 11.3906L13.9212 16.0906C13.8212 16.3128 13.6545 16.5017 13.4212 16.6573C13.1879 16.8128 12.9434 16.8906 12.6879 16.8906ZM6.68788 15.5573H12.6879L14.6879 10.8906V9.55729H8.68788L9.58788 5.89062L6.68788 8.79062V15.5573ZM6.68788 8.79062V9.55729V10.8906V15.5573V8.79062ZM5.35454 8.22396V9.55729H3.35454V15.5573H5.35454V16.8906H2.02121V8.22396H5.35454Z"
                  fill="#4B5563" />
              </svg>
              <span class="font-tinos text-sm text-river-bed">{{ list.likes }}</span>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Popular Members Section -->
    <div class="flex justify-center px-4 md:px-8 lg:px-40 mt-8 pb-16">
      <div class="w-full max-w-1280">
        <h3 class="font-newsreader text-2xl font-bold text-cod-gray mb-4 border-b border-gray-300">Popular Members</h3>
        <div class="flex flex-wrap justify-center gap-8">
          <div v-for="(member, index) in popularMembers" :key="index" class="flex flex-col items-center">
            <img :src="member.avatar" :alt="member.name" class="w-24 h-24 rounded-full mb-2" />
            <p class="font-tinos text-base text-cod-gray text-center">{{ member.name }}</p>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
