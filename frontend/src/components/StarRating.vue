<script setup>
import { computed } from 'vue'

const props = defineProps({
  rating: {
    type: Number,
    required: true,
    validator: (value) => value >= 0 && value <= 5
  },
  size: {
    type: String,
    default: 'medium',
    validator: (value) => ['small', 'medium', 'large'].includes(value)
  },
  showRating: {
    type: Boolean,
    default: false
  }
})

// Calculate star distribution
const stars = computed(() => {
  const fullStars = Math.floor(props.rating)
  const hasHalfStar = (props.rating % 1) >= 0.5
  const emptyStars = 5 - fullStars - (hasHalfStar ? 1 : 0)

  return {
    full: fullStars,
    half: hasHalfStar ? 1 : 0,
    empty: emptyStars
  }
})

// Size classes
const sizeClasses = computed(() => {
  const sizes = {
    small: 'w-4 h-4',
    medium: 'w-5 h-5',
    large: 'w-6 h-6'
  }
  return sizes[props.size]
})

const textSizeClasses = computed(() => {
  const sizes = {
    small: 'text-sm',
    medium: 'text-base',
    large: 'text-lg'
  }
  return sizes[props.size]
})
</script>

<template>
  <div class="flex items-center gap-1">
    <!-- Full Stars -->
    <svg
      v-for="n in stars.full"
      :key="'full-' + n"
      :class="sizeClasses"
      viewBox="0 0 20 20"
      fill="currentColor"
      class="text-yellow-500"
    >
      <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z" />
    </svg>

    <!-- Half Star -->
    <svg
      v-if="stars.half"
      :class="sizeClasses"
      viewBox="0 0 20 20"
      fill="none"
      class="text-yellow-500"
    >
      <defs>
        <linearGradient :id="'half-star-gradient-' + rating">
          <stop offset="50%" stop-color="currentColor" />
          <stop offset="50%" stop-color="transparent" />
        </linearGradient>
      </defs>
      <!-- Outline -->
      <path
        d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z"
        stroke="currentColor"
        stroke-width="1"
      />
      <!-- Half fill -->
      <path
        d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z"
        :fill="`url(#half-star-gradient-${rating})`"
      />
    </svg>

    <!-- Empty Stars -->
    <svg
      v-for="n in stars.empty"
      :key="'empty-' + n"
      :class="sizeClasses"
      viewBox="0 0 20 20"
      fill="none"
      stroke="currentColor"
      class="text-yellow-500"
    >
      <path
        d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z"
        stroke-width="1.5"
      />
    </svg>

    <!-- Rating text (optional) -->
    <span
      v-if="showRating"
      :class="textSizeClasses"
      class="font-tinos text-theme-text-secondary dark:text-theme-text-secondary-dark ml-1"
    >
      {{ rating }}/5
    </span>
  </div>
</template>
