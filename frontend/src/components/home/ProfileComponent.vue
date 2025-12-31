<template>
  <div class="min-h-screen bg-theme-bg-primary dark:bg-theme-bg-primary-dark transition-colors duration-200 py-8 px-4">
    <!-- Loading State -->
    <div v-if="loading" class="flex flex-col items-center justify-center py-20">
      <div class="w-12 h-12 border-4 border-theme-btn-primary dark:border-theme-btn-primary-dark border-t-transparent rounded-full animate-spin"></div>
      <p class="mt-4 font-tinos text-base text-theme-text-secondary dark:text-theme-text-secondary-dark">Loading profile...</p>
    </div>

    <!-- Error State -->
    <div v-else-if="error" class="max-w-2xl mx-auto">
      <div class="p-4 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg">
        <div class="flex items-center">
          <svg class="w-5 h-5 text-red-600 dark:text-red-400 mr-2" fill="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
            <path d="M12 2L1 21h22L12 2zm0 3.83L19.53 19H4.47L12 5.83zM11 16h2v2h-2v-2zm0-6h2v4h-2v-4z"/>
          </svg>
          <span class="font-tinos text-sm text-red-800 dark:text-red-300">{{ error }}</span>
        </div>
      </div>
    </div>

    <!-- Profile Content -->
    <div v-else-if="profile" class="max-w-6xl mx-auto space-y-6">
      <!-- User Info Component -->
      <UserInfoComponent
        :profile-image-url="profile.profileImageUrl"
        :display-name="profile.displayName || fullName || 'User'"
        :first-name="profile.firstName"
        :last-name="profile.lastName"
        :email="profile.email"
        :bio="profile.bio"
        :games-count="profile.gamesCount || 0"
        :achievements-count="profile.achievementsCount || 0"
        :total-play-time="profile.totalPlayTime || '0h'"
        :is-own-profile="isOwnProfile"
        :is-following="isFollowed"
        :following-in-progress="followingInProgress"
        @toggle-follow="toggleFollow"
      />

      <!-- Favorite Games Component -->
      <FavoriteGamesComponent
        :games="topGames"
        :is-own-profile="isOwnProfile"
      />

      <!-- Recent Reviews Component -->
      <RecentReviewsComponent
        :reviews="userReviews"
        :loading="loadingReviews"
        :total-reviews="totalReviews"
        :user-id="profileUserId"
        :is-own-profile="isOwnProfile"
        :max-displayed="4"
        @like-review="handleToggleLike"
      />
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue'
import { defineProps } from 'vue'
import { useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/authStore'
import { useProfileStore } from '@/stores/profileStore'
import { useGamesStore } from '@/stores/gamesStore'
import { useToast } from 'vue-toastification'
import { useAuthRedirect } from '@/utils/authRedirect'
import UserInfoComponent from './UserInfoComponent.vue'
import FavoriteGamesComponent from './FavoriteGamesComponent.vue'
import RecentReviewsComponent from './RecentReviewsComponent.vue'
import { reviewsService } from '@/services/reviewsService'
import { socialService } from '@/services/socialService'
import { commentsService } from '@/services/commentsService'

// Props for viewing other users' profiles
const props = defineProps({
  userId: {
    type: String,
    default: null
  }
})

// Composables
const route = useRoute()
const authStore = useAuthStore()
const profileStore = useProfileStore()
const gamesStore = useGamesStore()
const toast = useToast()
const { redirectToLoginWithReturn } = useAuthRedirect()

// State
const profile = ref(null)
const loading = ref(true)
const error = ref('')

// Reviews state
const userReviews = ref([])
const loadingReviews = ref(false)
const totalReviews = ref(0)
const likedReviews = ref(new Set())
const processingLikeReviews = ref(new Set())

// Games state
const topGames = ref([])

// Follow state
const isFollowed = ref(false)
const followingInProgress = ref(false)

// Computed properties
const profileUserId = computed(() => props.userId || route.params.userId || authStore.user?.id)
const isOwnProfile = computed(() => profileUserId.value === authStore.user?.id)

const fullName = computed(() => {
  if (!profile.value) return ''
  const { firstName, lastName } = profile.value
  return `${firstName || ''} ${lastName || ''}`.trim()
})

// Methods
const fetchProfile = async () => {
  try {
    loading.value = true
    error.value = ''

    const response = await profileStore.fetchProfile(
      isOwnProfile.value ? null : profileUserId.value
    )

    profile.value = response

    // Load user's favorites to populate top games
    if (profileUserId.value) {
      await loadUserFavorites()
    }

    // Load user's reviews
    await loadUserReviews()

    // Check follow status if viewing another user's profile
    if (!isOwnProfile.value && authStore.user) {
      await checkFollowStatus()
    }
  } catch (err) {
    error.value = err.message || 'Failed to load profile'
    console.error('Error fetching profile:', err)
  } finally {
    loading.value = false
  }
}

const loadUserFavorites = async () => {
  try {
    const favorites = await gamesStore.getUserFavorites(profileUserId.value)
    topGames.value = favorites ? favorites.slice(0, 5) : []
  } catch (err) {
    console.error('Error loading user favorites:', err)
    topGames.value = []
  }
}

const loadUserReviews = async () => {
  if (!profileUserId.value) return

  try {
    loadingReviews.value = true
    const response = await reviewsService.getUserReviews(profileUserId.value, 1, 8) // Load more than 4 to check if there are more
    const reviewsWithCommentCounts = await commentsService.loadCommentCountsForReviews(response.data || [])
    userReviews.value = reviewsWithCommentCounts
    totalReviews.value = response.totalCount || 0

    // Load like status for each review if current user is authenticated
    if (authStore.user) {
      likedReviews.value.clear()
      const likeStatusPromises = userReviews.value.map(async (review) => {
        try {
          const isLiked = await socialService.isReviewLiked(review.id)
          if (isLiked) {
            likedReviews.value.add(review.id)
          }
        } catch (error) {
          console.warn(`Failed to check like status for review ${review.id}:`, error)
        }
      })
      await Promise.all(likeStatusPromises)
    }
  } catch (error) {
    console.error('Error loading user reviews:', error)
    userReviews.value = []
    totalReviews.value = 0
  } finally {
    loadingReviews.value = false
  }
}

const handleToggleLike = async (review) => {
  if (!authStore.user) {
    toast.info('Please sign in to like reviews')
    return
  }

  const reviewId = review.id
  const wasLiked = likedReviews.value.has(reviewId)

  if (processingLikeReviews.value.has(reviewId)) return

  try {
    processingLikeReviews.value.add(reviewId)

    if (wasLiked) {
      await socialService.unlikeReview(reviewId)
      likedReviews.value.delete(reviewId)
    } else {
      await socialService.likeReview(reviewId)
      likedReviews.value.add(reviewId)
    }

    // Update like count in review
    const targetReview = userReviews.value.find(r => r.id === reviewId)
    if (targetReview) {
      targetReview.likeCount = (targetReview.likeCount || 0) + (wasLiked ? -1 : 1)
    }

  } catch (err) {
    console.error('Error toggling review like:', err)
    toast.error('Failed to update like')
  } finally {
    processingLikeReviews.value.delete(reviewId)
  }
}

// Follow methods
const checkFollowStatus = async () => {
  if (!profileUserId.value || isOwnProfile.value || !authStore.user) return

  try {
    const response = await socialService.isUserFollowed(profileUserId.value)
    isFollowed.value = response
  } catch (error) {
    console.error('Error checking follow status:', error)
    isFollowed.value = false
  }
}

const toggleFollow = async () => {
  if (!authStore.user) {
    redirectToLoginWithReturn('Please login to follow users')
    return
  }

  followingInProgress.value = true

  try {
    if (isFollowed.value) {
      await socialService.unfollowUser(profileUserId.value)
      isFollowed.value = false
      toast.success(`Unfollowed ${profile.value.displayName}`)
    } else {
      await socialService.followUser(profileUserId.value)
      isFollowed.value = true
      toast.success(`Now following ${profile.value.displayName}`)
    }
  } catch (error) {
    console.error('Error toggling follow:', error)
    toast.error(error.message || 'Failed to update follow status')
  } finally {
    followingInProgress.value = false
  }
}

// Lifecycle
onMounted(() => {
  if (profileUserId.value) {
    fetchProfile()
  }
})

// Watch for route changes to refetch profile data
watch(() => route.fullPath, () => {
  if (profileUserId.value) {
    fetchProfile()
  }
})
</script>
