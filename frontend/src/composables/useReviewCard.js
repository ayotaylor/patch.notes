import { computed } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/authStore'

export function useReviewCard(review, maxCharacters = 460) {
  const router = useRouter()
  const authStore = useAuthStore()

  const isLoggedIn = computed(() => authStore.isAuthenticated)

  // Truncate review text based on max characters
  const displayedReviewText = computed(() => {
    const text = review.value?.reviewText || review.reviewText || ''
    const limit = maxCharacters.value || maxCharacters

    if (text.length <= limit) {
      return text
    }

    return text.substring(0, limit).trim() + '...'
  })

  // Check if we should show "read more" link
  const showMoreLink = computed(() => {
    const text = review.value?.reviewText || review.reviewText || ''
    const limit = maxCharacters.value || maxCharacters
    return text.length > limit
  })

  // Calculate star rating
  const stars = computed(() => {
    const rating = review.value?.rating || review.rating || 0
    const fullStars = Math.floor(rating)
    const hasHalfStar = (rating % 1) >= 0.5
    const emptyStars = 5 - fullStars - (hasHalfStar ? 1 : 0)

    return {
      full: fullStars,
      half: hasHalfStar ? 1 : 0,
      empty: emptyStars
    }
  })

  // Heart icon for liked games
  const heartIcon = computed(() => {
    const game = review.value?.game || review.game
    return game?.isLikedByUser ? 'fas fa-heart' : 'far fa-heart'
  })

  const heartColor = computed(() => {
    const game = review.value?.game || review.game
    return game?.isLikedByUser ? 'text-red-500' : 'text-gray-400'
  })

  // Navigation methods
  const navigateToReview = () => {
    const reviewData = review.value || review
    const route = `/${reviewData.user.displayName}/game/${reviewData.game.slug}`
    router.push({
      path: route,
      state: { review: reviewData }
    })
  }

  const navigateToGame = () => {
    const reviewData = review.value || review
    router.push(`/games/${reviewData.game.slug}`)
  }

  const navigateToUser = () => {
    const reviewData = review.value || review
    router.push(`/profile/${reviewData.user.id}`)
  }

  return {
    isLoggedIn,
    displayedReviewText,
    showMoreLink,
    stars,
    heartIcon,
    heartColor,
    navigateToReview,
    navigateToGame,
    navigateToUser
  }
}
