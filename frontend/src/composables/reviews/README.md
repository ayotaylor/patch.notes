# Review Composables

This directory contains reusable composables for managing review functionality across the application.

## Composables Overview

### 1. useReviewLikes.js (Stateful)
**Purpose**: Manages review like/unlike functionality with global state

**Type**: Stateful - maintains shared state across all components

**State**:
- `likedReviews` - Set of review IDs liked by current user
- `processingLikeReviews` - Set of review IDs currently being processed

**Methods**:
- `loadLikeStatus(reviewId)` - Load like status for a single review
- `loadLikeStatusBatch(reviews)` - Load like status for multiple reviews
- `toggleLike(review, onSuccess)` - Toggle like/unlike on a review
- `isLiked(reviewId)` - Check if a review is liked
- `isProcessing(reviewId)` - Check if a review is being processed
- `clearLikes()` - Clear all like state (useful for logout)

**Usage Example**:
```javascript
import { useReviewLikes } from '@/composables/reviews/useReviewLikes'

const { likedReviews, processingLikeReviews, toggleLike, loadLikeStatus } = useReviewLikes()

// Toggle like with callback
await toggleLike(review, (wasLiked) => {
  // Update local state
  review.likeCount += wasLiked ? -1 : 1
})

// Check if liked
const isReviewLiked = likedReviews.value.has(review.id)

// Load like status for a review
await loadLikeStatus(reviewId)

// Load like status for multiple reviews
await loadLikeStatusBatch(reviewsArray)
```

### 2. useReviews.js (Stateless)
**Purpose**: Provides CRUD operations for reviews

**Type**: Stateless - components manage their own review data

**Methods**:
- `loadReviews(options)` - Load reviews with optional filtering
  - Options: `{ gameId, userId, page, pageSize }`
  - Returns: `{ data, hasNextPage, totalCount }`
- `loadReview(reviewId)` - Load a single review by ID
- `deleteReview(review, onSuccess)` - Delete a review
- `updateReview(reviewId, reviewData, onSuccess)` - Update a review
- `navigateToEdit(review)` - Navigate to review edit page
- `navigateToDetails(review)` - Navigate to review details page
- `navigateBack(options)` - Navigate back based on context

**Usage Example**:
```javascript
import { useReviews } from '@/composables/reviews/useReviews'
import { ref } from 'vue'

const { loadReviews, deleteReview, updateReview } = useReviews()

// Component manages its own state
const reviews = ref([])
const loading = ref(false)

// Load reviews for a game
const fetchGameReviews = async () => {
  loading.value = true
  try {
    const result = await loadReviews({ gameId: 123, page: 1, pageSize: 15 })
    reviews.value = result.data
  } catch (error) {
    console.error(error)
  } finally {
    loading.value = false
  }
}

// Delete a review
await deleteReview(review, () => {
  // Refresh list after delete
  fetchGameReviews()
})

// Update a review
await updateReview(reviewId, { rating: 5, reviewText: 'Amazing!' }, (updatedReview) => {
  // Update local state
  const index = reviews.value.findIndex(r => r.id === reviewId)
  if (index !== -1) {
    reviews.value[index] = updatedReview
  }
})
```

## Complete Example: Reviews List Page

```javascript
<script setup>
import { ref, onMounted } from 'vue'
import { useReviews } from '@/composables/reviews/useReviews'
import { useReviewLikes } from '@/composables/reviews/useReviewLikes'

// Props
const props = defineProps({
  gameId: String
})

// Composables
const { loadReviews, deleteReview } = useReviews()
const { likedReviews, processingLikeReviews, toggleLike, loadLikeStatusBatch } = useReviewLikes()

// Component state
const reviews = ref([])
const loading = ref(false)
const page = ref(1)

// Load reviews
const fetchReviews = async () => {
  loading.value = true
  try {
    const result = await loadReviews({
      gameId: props.gameId,
      page: page.value,
      pageSize: 15
    })

    reviews.value = result.data

    // Load like status for all reviews
    await loadLikeStatusBatch(result.data)
  } catch (error) {
    console.error('Failed to load reviews:', error)
  } finally {
    loading.value = false
  }
}

// Handle like toggle
const handleToggleLike = async (review) => {
  await toggleLike(review, (wasLiked) => {
    // Update like count in local state
    const targetReview = reviews.value.find(r => r.id === review.id)
    if (targetReview) {
      targetReview.likeCount += wasLiked ? -1 : 1
    }
  })
}

// Handle delete
const handleDelete = async (review) => {
  await deleteReview(review, () => {
    // Refresh list
    fetchReviews()
  })
}

onMounted(() => {
  fetchReviews()
})
</script>

<template>
  <div>
    <div v-if="loading">Loading...</div>
    <div v-else>
      <div v-for="review in reviews" :key="review.id">
        <ReviewCard
          :review="review"
          :is-liked="likedReviews.has(review.id)"
          :is-processing="processingLikeReviews.has(review.id)"
          @toggleLike="handleToggleLike"
          @delete="handleDelete"
        />
      </div>
    </div>
  </div>
</template>
```

## State Management Pattern

### Why Hybrid Approach?

**Stateful for Likes** (useReviewLikes):
- Like status is user-centric, not page-centric
- If a user likes a review on one page, it should show as liked on all pages
- One global state shared across the entire app

**Stateless for CRUD** (useReviews):
- Each page shows different review data (popular, latest, game-specific)
- Components manage their own `reviews` array, `loading`, `pagination`
- Composable just provides methods

### Multiple Pages with Same Likes

```javascript
// PopularReviewsPage.vue
const reviews = ref([])  // Different data
const { toggleLike, likedReviews } = useReviewLikes()  // SAME like state

// LatestReviewsPage.vue
const reviews = ref([])  // Different data
const { toggleLike, likedReviews } = useReviewLikes()  // SAME like state

// When user likes review #123 on Popular page,
// it automatically shows as liked on Latest page too!
```

## Future Enhancements

This pattern can be extended to other entities:

```
composables/
├── reviews/
│   ├── useReviews.js
│   └── useReviewLikes.js
├── comments/
│   ├── useComments.js
│   └── useCommentLikes.js
├── games/
│   ├── useGames.js
│   └── useGameLikes.js
└── lists/
    ├── useLists.js
    └── useListLikes.js
```

## Best Practices

1. **Always use composables for review operations** - Don't duplicate logic in components
2. **Let components manage their own data arrays** - Use `useReviews()` for methods only
3. **Trust the global like state** - Use `useReviewLikes()` for all like operations
4. **Use callbacks** - Pass `onSuccess` callbacks to handle local state updates
5. **Load like status after fetching reviews** - Call `loadLikeStatusBatch()` after loading reviews

## Migration Guide

### Before (Old Pattern):
```javascript
// Duplicated in every component
const likedReviews = ref(new Set())
const processingLikes = ref(new Set())

const handleToggleLike = async (review) => {
  // ... 40 lines of duplicated logic
}
```

### After (New Pattern):
```javascript
// One line import
const { likedReviews, toggleLike } = useReviewLikes()

// One line usage
await toggleLike(review, (wasLiked) => { /* update local state */ })
```
