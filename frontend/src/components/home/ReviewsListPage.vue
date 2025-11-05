<script setup>
import { ref, onMounted } from 'vue'
import HomeHeader from './HeaderBar.vue'
import HomeNavigation from './NavigationBar.vue'
import ReviewCardBase from './ReviewCardBase.vue'

// State
const reviews = ref([])
const loading = ref(true)
const error = ref(null)
// eslint-disable-next-line no-unused-vars
const displayCount = ref(15)

// Generate placeholder review data
const generatePlaceholderReviews = () => {
  const games = [
    { name: 'Elden Ring', slug: 'elden-ring', year: 2022, image: 'https://images.igdb.com/igdb/image/upload/t_cover_big/co4jni.jpg' },
    { name: 'Baldur\'s Gate 3', slug: 'baldurs-gate-3', year: 2023, image: 'https://images.igdb.com/igdb/image/upload/t_cover_big/co5w2t.jpg' },
    { name: 'The Legend of Zelda: Tears of the Kingdom', slug: 'zelda-tears-of-kingdom', year: 2023, image: 'https://images.igdb.com/igdb/image/upload/t_cover_big/co5vmg.jpg' },
    { name: 'Starfield', slug: 'starfield', year: 2023, image: 'https://images.igdb.com/igdb/image/upload/t_cover_big/co6w2u.jpg' },
    { name: 'Hogwarts Legacy', slug: 'hogwarts-legacy', year: 2023, image: 'https://images.igdb.com/igdb/image/upload/t_cover_big/co5h87.jpg' }
  ]

  const users = [
    { id: 1, username: 'gamer123', displayName: 'Alex Johnson', image: 'https://via.placeholder.com/50' },
    { id: 2, username: 'proreview', displayName: 'Sarah Chen', image: 'https://via.placeholder.com/50' },
    { id: 3, username: 'rpgfanatic', displayName: 'Mike Roberts', image: 'https://via.placeholder.com/50' },
    { id: 4, username: 'casualplayer', displayName: 'Emma Davis', image: 'https://via.placeholder.com/50' },
    { id: 5, username: 'hardcoregamer', displayName: 'Chris Martinez', image: 'https://via.placeholder.com/50' }
  ]

  const reviewTexts = [
    'An absolute masterpiece that redefines the genre. The attention to detail is incredible, and every moment feels crafted with care. The gameplay loop is addictive, and I found myself playing for hours on end without realizing how much time had passed.',
    'While the game has its strong points, I found some aspects to be lacking. The story could have been better paced, and certain mechanics felt repetitive after a while. Still worth playing if you\'re a fan of the series.',
    'This game exceeded all my expectations. The world is vast and full of interesting content to discover. Combat feels satisfying and strategic, requiring you to think about your approach to each encounter.',
    'A solid entry in the franchise that brings fresh ideas while respecting what came before. The graphics are stunning, and the soundtrack is phenomenal. Some technical issues at launch, but overall a great experience.',
    'One of the best games I\'ve played in years. The narrative is compelling, characters are well-developed, and the gameplay mechanics are polished to perfection. Highly recommend to anyone who enjoys this type of game.'
  ]

  const placeholderReviews = []

  for (let i = 0; i < 15; i++) {
    const game = games[i % games.length]
    const user = users[i % users.length]
    const reviewText = reviewTexts[i % reviewTexts.length]
    const rating = 3 + Math.random() * 2 // Random rating between 3 and 5

    placeholderReviews.push({
      id: i + 1,
      reviewText,
      rating: Math.round(rating * 2) / 2, // Round to nearest 0.5
      isLikedByCurrentUser: Math.random() > 0.7,
      user: {
        id: user.id,
        username: user.username,
        displayName: user.displayName,
        profileImageUrl: user.image
      },
      game: {
        id: i + 1,
        name: game.name,
        slug: game.slug,
        releaseYear: game.year,
        primaryImageUrl: game.image,
        isLikedByUser: Math.random() > 0.5
      },
      likeCount: Math.floor(Math.random() * 200) + 10,
      commentCount: Math.floor(Math.random() * 50),
      relativeDate: `${Math.floor(Math.random() * 30) + 1} days ago`,
      createdAt: new Date(Date.now() - Math.random() * 30 * 24 * 60 * 60 * 1000)
    })
  }

  return placeholderReviews
}

const loadReviews = async () => {
  loading.value = true
  error.value = null

  try {
    // TODO: Replace with actual API call
    // const response = await reviewService.getAllReviews({ limit: displayCount.value })
    // reviews.value = Review.fromJSONArray(response.data)

    // Placeholder data for now
    reviews.value = generatePlaceholderReviews()
  } catch (err) {
    error.value = 'Failed to load reviews'
    console.error('Error loading reviews:', err)
  } finally {
    loading.value = false
  }
}

const handleLikeReview = (review) => {
  // TODO: Implement like functionality
  console.log('Liking review:', review)
  review.isLikedByCurrentUser = !review.isLikedByCurrentUser
  review.likeCount += review.isLikedByCurrentUser ? 1 : -1
}

onMounted(() => {
  loadReviews()
})
</script>

<template>
  <div class="min-h-screen bg-theme-bg-primary dark:bg-theme-bg-primary-dark transition-colors duration-200">
    <!-- Header Component -->
    <HomeHeader />

    <!-- Navigation Component -->
    <HomeNavigation />

    <!-- Reviews List Section -->
    <div class="flex justify-center px-4 md:px-8 lg:px-40 mt-8">
      <div class="w-full max-w-1280">
        <h2 class="font-newsreader text-3xl font-bold text-theme-text-primary dark:text-theme-text-primary-dark mb-6 border-b border-theme-border dark:border-theme-border-dark pb-4">
          All Reviews
        </h2>

        <!-- Loading State -->
        <div v-if="loading" class="text-center py-16">
          <div class="inline-block animate-spin rounded-full h-12 w-12 border-b-2 border-theme-text-primary dark:border-theme-text-primary-dark"></div>
          <p class="font-tinos text-base text-theme-text-secondary dark:text-theme-text-secondary-dark mt-4">Loading reviews...</p>
        </div>

        <!-- Error State -->
        <div v-else-if="error" class="text-center py-16">
          <p class="font-tinos text-lg text-red-500">{{ error }}</p>
        </div>

        <!-- Reviews List (75% width, left justified) -->
        <div v-else-if="reviews.length > 0" class="space-y-6 mb-16">
          <ReviewCardBase
            v-for="review in reviews"
            :key="review.id"
            :review="review"
            variant="list"
            :max-characters="460"
            @like-review="handleLikeReview"
          />
        </div>

        <!-- Empty State -->
        <div v-else class="text-center py-16">
          <p class="font-tinos text-lg text-theme-text-secondary dark:text-theme-text-secondary-dark">No reviews found</p>
        </div>
      </div>
    </div>
  </div>
</template>