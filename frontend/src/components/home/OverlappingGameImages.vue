<template>
  <div ref="containerRef" class="relative h-48 bg-gray-200 rounded-lg overflow-hidden">
    <!-- Horizontal overlapping layout -->
    <div v-if="displayGames.length > 0" class="relative h-full flex items-center">
      <div
        v-for="(game, index) in displayGames"
        :key="index"
        class="absolute h-full"
        :style="getImageStyle(index)"
      >
        <img
          :src="getOptimizedImageUrl(game)"
          :alt="game.name"
          class="h-full w-full object-cover rounded"
          @error="handleImageError"
        />
      </div>
    </div>

    <!-- Empty state if no games -->
    <div v-else class="flex items-center justify-center h-full">
      <i class="fas fa-gamepad text-gray-400 text-4xl"></i>
    </div>

    <!-- Optional slot for overlay content (badges, etc.) -->
    <slot name="overlay"></slot>
  </div>
</template>

<script setup>
import { computed, ref, onMounted, onUnmounted } from 'vue'
import { getContextualIgdbImage, IGDB_IMAGE_SIZES } from '@/utils/igdbImageSizing'

const props = defineProps({
  games: {
    type: Array,
    default: () => []
  },
  maxDisplay: {
    type: Number,
    default: 5
  }
})

// Refs
const containerRef = ref(null)
const containerWidth = ref(1000) // Default fallback

// Computed
const displayGames = computed(() => {
  return props.games.slice(0, props.maxDisplay)
})

// Watch container width changes
let resizeObserver = null

onMounted(() => {
  if (containerRef.value) {
    // Get initial width
    containerWidth.value = containerRef.value.offsetWidth

    // Create ResizeObserver to watch for size changes
    resizeObserver = new ResizeObserver((entries) => {
      for (const entry of entries) {
        containerWidth.value = entry.contentRect.width
      }
    })

    resizeObserver.observe(containerRef.value)
  }
})

onUnmounted(() => {
  if (resizeObserver) {
    resizeObserver.disconnect()
  }
})

// Methods
const getOptimizedImageUrl = (game) => {
  const url = game.coverImageUrl || game.primaryImageUrl
  if (!url) return 'https://via.placeholder.com/90x128?text=No+Image'

  // Use IGDB image sizing utility to get optimized cover image
  return getContextualIgdbImage(url, null, IGDB_IMAGE_SIZES.COVER_BIG)
}

const getImageStyle = (index) => {
  // COVER_BIG dimensions: 264 x 374 pixels
  // Container height: h-48 = 192px
  const coverBigAspectRatio = 264 / 374 // ~0.706
  const containerHeight = 192 // h-48 in pixels
  const baseImageWidth = containerHeight * coverBigAspectRatio // ~135.6px

  const visiblePortion = 0.75 // 75% of each image is visible (25% overlap)
  const gameCount = displayGames.value.length

  // Calculate required total width: first image (100%) + remaining images (75% each)
  // Total width needed = imageWidth * (1 + (gameCount - 1) * 0.75)
  // We want this to fit within the container, so scale down if needed
  const totalWidthNeeded = baseImageWidth * (1 + (gameCount - 1) * visiblePortion)

  // Scale down image width if total width exceeds container
  const scaleFactor = totalWidthNeeded > containerWidth.value ? containerWidth.value / totalWidthNeeded : 1
  const imageWidth = baseImageWidth * scaleFactor

  // Calculate offset for this image
  const offset = imageWidth * visiblePortion * index

  return {
    left: `${offset}px`,
    zIndex: displayGames.value.length - index,
    width: `${imageWidth}px`
  }
}

const handleImageError = (e) => {
  e.target.src = 'https://via.placeholder.com/90x128?text=No+Image'
}
</script>

<style scoped>
/* No additional styles needed - using inline styles for dynamic positioning */
</style>
