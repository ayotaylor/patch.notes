<template>
  <div class="bg-cod-gray text-white rounded-lg overflow-hidden">
    <!-- Header with Ratings and Fans -->
    <div class="flex justify-between items-center px-4 py-3 border-b border-gray-600">
      <h3 class="font-tinos font-bold text-sm uppercase tracking-wider">Ratings</h3>
      <span class="font-tinos text-sm text-gray-300">{{ formatFans(totalRatings) }} Fans</span>
    </div>

    <!-- Bar Chart and Rating Display -->
    <div class="px-4 py-4">
      <div class="flex items-end gap-3">
        <!-- Star Icon (Left) -->
        <div class="flex-shrink-0 pb-1">
          <svg class="w-6 h-6 text-green-500" fill="currentColor" viewBox="0 0 20 20">
            <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z" />
          </svg>
        </div>

        <!-- Bar Chart -->
        <div class="flex-1 flex items-end gap-0.5 h-20">
          <div
            v-for="rating in ratingsDistribution"
            :key="rating.stars"
            class="flex-1 bg-gray-600 rounded-t transition-all duration-300 hover:bg-gray-500 cursor-pointer"
            :style="{ height: `${(rating.count / maxRatingCount) * 100}%`, minHeight: rating.count > 0 ? '8px' : '2px' }"
            :title="`${rating.stars} stars: ${rating.count} ratings`"
          ></div>
        </div>

        <!-- Rating Number and Stars (Right) -->
        <div class="flex-shrink-0 flex flex-col items-end gap-1 pb-1">
          <!-- Rating Number -->
          <div class="font-newsreader text-3xl font-bold text-white leading-none">
            {{ averageRating.toFixed(1) }}
          </div>
          <!-- Star Icons -->
          <div class="flex gap-0.5">
            <svg
              v-for="n in 5"
              :key="n"
              class="w-4 h-4 text-green-500"
              fill="currentColor"
              viewBox="0 0 20 20"
            >
              <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z" />
            </svg>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({
  averageRating: {
    type: Number,
    default: 0
  },
  totalRatings: {
    type: Number,
    default: 0
  },
  ratingsDistribution: {
    type: Array,
    default: () => [
      { stars: 5, count: 0 },
      { stars: 4.5, count: 0 },
      { stars: 4, count: 0 },
      { stars: 3.5, count: 0 },
      { stars: 3, count: 0 },
      { stars: 2.5, count: 0 },
      { stars: 2, count: 0 },
      { stars: 1.5, count: 0 },
      { stars: 1, count: 0 },
      { stars: 0.5, count: 0 }
    ]
  }
})

// Maximum count for bar chart scaling
const maxRatingCount = computed(() => {
  return Math.max(...props.ratingsDistribution.map(r => r.count), 1)
})

// Format fan count
const formatFans = (count) => {
  if (count >= 1000000) {
    return `${(count / 1000000).toFixed(1)}M`
  } else if (count >= 1000) {
    return `${(count / 1000).toFixed(1)}K`
  }
  return count.toString()
}
</script>
