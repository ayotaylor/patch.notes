<template>
  <div class="reviews-list">
    <!-- Header -->
    <div v-if="showHeader" class="d-flex justify-content-between align-items-center mb-4">
      <h3 class="h5 mb-0 fw-bold">
        <i class="fas fa-star text-primary me-2"></i>
        {{ title }}
        <span v-if="totalCount > 0" class="text-muted fw-normal">({{ totalCount }})</span>
      </h3>

      <!-- Sort Options -->
      <div v-if="showSortOptions" class="dropdown">
        <button
          class="btn btn-outline-secondary btn-sm dropdown-toggle"
          type="button"
          data-bs-toggle="dropdown"
        >
          <i class="fas fa-sort me-1"></i>
          {{ getSortLabel(sortBy) }}
        </button>
        <ul class="dropdown-menu">
          <li>
            <button
              @click="updateSort('newest')"
              class="dropdown-item"
              :class="{ active: sortBy === 'newest' }"
            >
              <i class="fas fa-calendar me-2"></i>Newest First
            </button>
          </li>
          <li>
            <button
              @click="updateSort('oldest')"
              class="dropdown-item"
              :class="{ active: sortBy === 'oldest' }"
            >
              <i class="fas fa-calendar-alt me-2"></i>Oldest First
            </button>
          </li>
          <li>
            <button
              @click="updateSort('highest')"
              class="dropdown-item"
              :class="{ active: sortBy === 'highest' }"
            >
              <i class="fas fa-star me-2"></i>Highest Rated
            </button>
          </li>
          <li>
            <button
              @click="updateSort('lowest')"
              class="dropdown-item"
              :class="{ active: sortBy === 'lowest' }"
            >
              <i class="fas fa-star-half-alt me-2"></i>Lowest Rated
            </button>
          </li>
        </ul>
      </div>
    </div>

    <!-- Loading State -->
    <div v-if="loading" class="text-center py-5">
      <div class="spinner-border text-primary mb-3"></div>
      <p class="text-muted">Loading reviews...</p>
    </div>

    <!-- Empty State -->
    <div v-else-if="reviews.length === 0" class="text-center py-5">
      <div class="mb-3">
        <i class="fas fa-star-o text-muted" style="font-size: 3rem;"></i>
      </div>
      <h5 class="text-muted mb-2">{{ emptyMessage }}</h5>
      <p class="text-muted">
        {{ emptySubMessage }}
      </p>
      <slot name="empty-actions"></slot>
    </div>

    <!-- Reviews Grid -->
    <div v-else>
      <div class="row g-3">
        <div
          v-for="review in displayedReviews"
          :key="review.id"
          class="col-12"
          :class="{ 'col-lg-6': gridLayout && reviews.length > 1 }"
        >
          <ReviewCard
            :review="review"
            :show-game="showGame"
            :show-date="showDate"
            :show-game-name="showGameName"
            :highlighted="review.id === highlightedReviewId"
            :truncated="truncateReviews"
            :max-length="maxReviewLength"
            :is-liked="props.likedReviews?.has(review.id) || false"
            :is-processing-like="props.processingLikeReviews?.has(review.id) || false"
            @edit="$emit('edit', review)"
            @delete="$emit('delete', review)"
            @toggleLike="$emit('toggleLike', review)"
            @showComments="$emit('showComments', review)"
          />
        </div>
      </div>

      <!-- Load More / Show All Button -->
      <div v-if="hasMoreReviews || (showLimited && reviews.length > displayLimit)" class="text-center mt-4">
        <button
          v-if="showLimited && reviews.length > displayLimit"
          @click="showAllReviews"
          class="btn btn-outline-primary"
          :disabled="loadingMore"
        >
          <span v-if="loadingMore" class="spinner-border spinner-border-sm me-2"></span>
          <i v-else class="fas fa-plus me-2"></i>
          Show All {{ totalCount }} Reviews
        </button>

        <button
          v-else-if="hasMoreReviews && !showLimited"
          @click="loadMore"
          class="btn btn-outline-primary"
          :disabled="loadingMore"
        >
          <span v-if="loadingMore" class="spinner-border spinner-border-sm me-2"></span>
          <i v-else class="fas fa-plus me-2"></i>
          Load More Reviews
        </button>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed, defineProps, defineEmits } from 'vue'
import ReviewCard from './ReviewCard.vue'

// Props
const props = defineProps({
  reviews: {
    type: Array,
    default: () => []
  },
  loading: {
    type: Boolean,
    default: false
  },
  loadingMore: {
    type: Boolean,
    default: false
  },
  hasMoreReviews: {
    type: Boolean,
    default: false
  },
  showHeader: {
    type: Boolean,
    default: true
  },
  title: {
    type: String,
    default: 'Reviews'
  },
  totalCount: {
    type: Number,
    default: 0
  },
  showGame: {
    type: Boolean,
    default: false
  },
  showDate: {
    type: Boolean,
    default: true
  },
  showGameName: {
    type: Boolean,
    default: false
  },
  showSortOptions: {
    type: Boolean,
    default: true
  },
  gridLayout: {
    type: Boolean,
    default: false
  },
  truncateReviews: {
    type: Boolean,
    default: true
  },
  maxReviewLength: {
    type: Number,
    default: 200
  },
  highlightedReviewId: {
    type: [String, Number],
    default: null
  },
  emptyMessage: {
    type: String,
    default: 'No reviews yet'
  },
  emptySubMessage: {
    type: String,
    default: 'Be the first to share your thoughts!'
  },
  showLimited: {
    type: Boolean,
    default: false
  },
  displayLimit: {
    type: Number,
    default: 5
  },
  sortBy: {
    type: String,
    default: 'newest'
  },
  likedReviews: {
    type: Set,
    default: () => new Set()
  },
  processingLikeReviews: {
    type: Set,
    default: () => new Set()
  }
})

// Emits
const emits = defineEmits(['load-more', 'show-all', 'sort-change', 'edit', 'delete', 'toggleLike', 'showComments'])

// Computed
const displayedReviews = computed(() => {
  if (props.showLimited) {
    return props.reviews.slice(0, props.displayLimit)
  }
  return props.reviews
})

// Methods
const loadMore = () => {
  emits('load-more')
}

const showAllReviews = () => {
  emits('show-all')
}

const updateSort = (newSort) => {
  emits('sort-change', newSort)
}

const getSortLabel = (sort) => {
  const labels = {
    newest: 'Newest',
    oldest: 'Oldest',
    highest: 'Highest Rated',
    lowest: 'Lowest Rated'
  }
  return labels[sort] || 'Sort'
}
</script>

<style scoped>
.reviews-list {
  width: 100%;
}

.dropdown-item.active {
  background-color: var(--bs-primary);
  color: white;
}

.dropdown-item:hover {
  background-color: var(--bs-light);
}

.dropdown-item.active:hover {
  background-color: var(--bs-primary);
  opacity: 0.9;
}

@media (max-width: 768px) {
  .d-flex.justify-content-between {
    flex-direction: column;
    align-items: flex-start !important;
    gap: 1rem;
  }

  .dropdown {
    align-self: flex-end;
  }
}
</style>