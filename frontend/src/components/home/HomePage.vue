<script setup>
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useGamesStore } from '@/stores/gamesStore'
import { useTheme } from '@/composables/useTheme'
import HeaderBar from './HeaderBar.vue'
import HomeNavigation from './NavigationBar.vue'
import GameCarousel from './GameCarousel.vue'
import ReviewCardBase from './ReviewCardBase.vue'
import ListCardPopular from './ListCardPopular.vue'
import { useReviews } from '@/composables/reviews/useReviews'
import { useReviewLikes } from '@/composables/reviews/useReviewLikes'
import { useLists } from '@/composables/lists/useLists'
import { useListLikes } from '@/composables/lists/useListLikes'
import { socialService } from '@/services/socialService'

const router = useRouter()
const gamesStore = useGamesStore()
useTheme() // Initialize theme
const { loadReviews } = useReviews()
const { toggleLike, loadLikeStatusBatch } = useReviewLikes()
const { loadLists } = useLists()
const { toggleLike: toggleListLike, loadLikeStatusBatch: loadListLikeStatusBatch } = useListLikes()

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

// Popular Lists State
const popularLists = ref([])
const loadingLists = ref(true)
const listsLoadError = ref(false)
const listsRetryAttempt = ref(0)
const MAX_LISTS_RETRIES = 2

// Popular Members State
const popularMembers = ref([])
const loadingMembers = ref(true)
const membersLoadError = ref(false)

const handleGameClick = (game) => {
  // Navigate to game details page using slug or id
  if (game.slug) {
    router.push({ name: 'GameDetails', params: { identifier: game.slug } })
  } else if (game.id) {
    router.push({ name: 'GameDetails', params: { identifier: game.id } })
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

// Load popular lists from API
const fetchPopularLists = async () => {
  try {
    loadingLists.value = true
    listsLoadError.value = false

    const result = await loadLists({ page: 1, pageSize: 6 })
    popularLists.value = result.data

    // Load like status for all lists if user is authenticated
    if (result.data.length > 0) {
      await loadListLikeStatusBatch(result.data)
    }
  } catch (error) {
    console.error('Error loading popular lists:', error)
    listsLoadError.value = true

    // Retry with fewer lists if first attempt failed
    if (listsRetryAttempt.value < MAX_LISTS_RETRIES) {
      listsRetryAttempt.value++
      const retryPageSize = Math.max(3, 6 - listsRetryAttempt.value * 2)
      console.log(`Retrying lists with pageSize: ${retryPageSize}`)

      try {
        const result = await loadLists({ page: 1, pageSize: retryPageSize })
        popularLists.value = result.data
        listsLoadError.value = false

        if (result.data.length > 0) {
          await loadListLikeStatusBatch(result.data)
        }
      } catch (retryError) {
        console.error('Lists retry failed:', retryError)
        // Keep error state, don't show section
      }
    }
  } finally {
    loadingLists.value = false
  }
}

// eslint-disable-next-line no-unused-vars
const handleLikeList = async (list) => {
  await toggleListLike(list, (wasLiked) => {
    // Update like count in local list data
    list.likeCount = (list.likeCount || 0) + (wasLiked ? -1 : 1)
  })
}

// Load popular members from API
const fetchPopularMembers = async () => {
  try {
    loadingMembers.value = true
    membersLoadError.value = false

    const result = await socialService.getAllUsers(1, 6)
    popularMembers.value = result.data || []
  } catch (error) {
    console.error('Error loading popular members:', error)
    membersLoadError.value = true
    popularMembers.value = []
  } finally {
    loadingMembers.value = false
  }
}

// Navigate to user profile
const handleMemberClick = (member) => {
  if (member.id) {
    router.push({ name: 'UserProfile', params: { userId: member.id } })
  }
}

// Load data on mount
onMounted(() => {
  fetchPopularReviews()
  fetchRecentlyReviewedGames()
  fetchPopularLists()
  fetchPopularMembers()
})
</script>

<template>
  <div class="min-h-screen bg-theme-bg-primary dark:bg-theme-bg-primary-dark transition-colors duration-200">
    <!-- Header Component -->
    <HeaderBar />

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
          view-all-link="/games/recently-reviewed"
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
        <div class="flex justify-between items-center mb-4 border-b border-theme-border dark:border-theme-border-dark pb-2">
          <h3 class="font-newsreader text-2xl font-bold text-theme-text-primary dark:text-theme-text-primary-dark">Popular Reviews</h3>
          <router-link
            v-if="!loadingReviews && popularReviews.length > 0"
            to="/reviews"
            class="font-tinos text-base text-blue-600 dark:text-blue-400 hover:text-blue-800 dark:hover:text-blue-300 hover:underline"
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
          <p class="font-tinos text-lg text-theme-text-secondary dark:text-theme-text-secondary-dark">No reviews available yet.</p>
        </div>
      </div>
    </div>

    <!-- Popular Lists Section -->
    <div v-if="!listsLoadError || loadingLists || popularLists.length > 0" class="flex justify-center px-4 md:px-8 lg:px-40 mt-8">
      <div class="w-full max-w-1280">
        <!-- Section Header with View All Link -->
        <div class="flex justify-between items-center mb-4 border-b border-theme-border dark:border-theme-border-dark pb-2">
          <h3 class="font-newsreader text-2xl font-bold text-theme-text-primary dark:text-theme-text-primary-dark">Popular Lists</h3>
          <router-link
            v-if="!loadingLists && popularLists.length > 0"
            to="/lists"
            class="font-tinos text-base text-blue-600 dark:text-blue-400 hover:text-blue-800 dark:hover:text-blue-300 hover:underline"
          >
            View All →
          </router-link>
        </div>

        <!-- Loading Skeleton -->
        <div v-if="loadingLists" class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          <div v-for="n in 6" :key="`skeleton-${n}`" class="animate-pulse">
            <div class="flex flex-col gap-3">
              <!-- Image skeleton -->
              <div class="bg-gray-300 rounded-lg w-full h-48"></div>
              <!-- Title skeleton -->
              <div class="h-5 bg-gray-300 rounded w-3/4"></div>
              <!-- User/likes skeleton -->
              <div class="flex items-center gap-2">
                <div class="w-6 h-6 bg-gray-300 rounded-full"></div>
                <div class="h-4 bg-gray-300 rounded w-24"></div>
                <div class="h-4 bg-gray-300 rounded w-16"></div>
              </div>
            </div>
          </div>
        </div>

        <!-- Lists Grid -->
        <div v-else-if="popularLists.length > 0" class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          <ListCardPopular
            v-for="list in popularLists"
            :key="list.id"
            :list="list"
          />
        </div>

        <!-- Empty State (only if not loading and no error) -->
        <div v-else-if="!loadingLists && !listsLoadError" class="text-center py-12">
          <p class="font-tinos text-lg text-theme-text-secondary dark:text-theme-text-secondary-dark">No lists available yet.</p>
        </div>
      </div>
    </div>

    <!-- Popular Members Section -->
    <div v-if="!membersLoadError || loadingMembers || popularMembers.length > 0" class="flex justify-center px-4 md:px-8 lg:px-40 mt-8 pb-16">
      <div class="w-full max-w-1280">
        <div class="flex justify-between items-center mb-4 border-b border-theme-border dark:border-theme-border-dark pb-2">
          <h3 class="font-newsreader text-2xl font-bold text-theme-text-primary dark:text-theme-text-primary-dark">Popular Members</h3>
          <router-link
            v-if="!loadingMembers && popularMembers.length > 0"
            to="/members"
            class="font-tinos text-base text-blue-600 dark:text-blue-400 hover:text-blue-800 dark:hover:text-blue-300 hover:underline"
          >
            View All →
          </router-link>
        </div>

        <!-- Loading Skeleton -->
        <div v-if="loadingMembers" class="flex flex-wrap justify-center gap-8">
          <div v-for="n in 5" :key="`skeleton-${n}`" class="flex flex-col items-center animate-pulse">
            <div class="w-24 h-24 bg-gray-300 rounded-full mb-2"></div>
            <div class="h-4 bg-gray-300 rounded w-20"></div>
          </div>
        </div>

        <!-- Members Grid -->
        <div v-else-if="popularMembers.length > 0" class="flex flex-wrap justify-center gap-8">
          <button
            v-for="member in popularMembers"
            :key="member.id"
            @click="handleMemberClick(member)"
            class="flex flex-col items-center group cursor-pointer transition-transform hover:scale-105"
          >
            <img
              :src="member.profileImageUrl || `https://ui-avatars.com/api/?name=${encodeURIComponent(member.displayName || member.username)}&size=96&background=6c757d&color=ffffff`"
              :alt="member.displayName || member.username"
              class="w-24 h-24 rounded-full mb-2 border-4 border-theme-border dark:border-theme-border-dark group-hover:border-theme-btn-primary dark:group-hover:border-theme-btn-primary-dark transition-colors"
            />
            <p class="font-tinos text-base text-theme-text-primary dark:text-theme-text-primary-dark text-center group-hover:text-theme-btn-primary dark:group-hover:text-theme-btn-primary-dark transition-colors">
              {{ member.displayName || member.username }}
            </p>
          </button>
        </div>

        <!-- Empty State -->
        <div v-else-if="!loadingMembers && !membersLoadError" class="text-center py-12">
          <p class="font-tinos text-lg text-theme-text-secondary dark:text-theme-text-secondary-dark">No members available yet.</p>
        </div>
      </div>
    </div>
  </div>
</template>
