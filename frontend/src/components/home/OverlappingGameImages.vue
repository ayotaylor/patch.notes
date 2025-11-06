<template>
  <div ref="containerRef" class="relative h-48 bg-theme-bg-secondary dark:bg-theme-bg-secondary-dark rounded-lg overflow-hidden transition-colors duration-200">
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
      <svg class="w-16 h-16 text-gray-400" fill="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
        <path d="M21.5 8h-3.502c-.338-1.187-1.237-2.15-2.354-2.658L14.126 2.88c-.378-.17-.818.054-.938.454l-.91 3.024c-.073.242.089.494.337.525 1.809.225 3.167 1.632 3.345 3.395.025.25.238.423.487.423h4.553c.83 0 1.5.67 1.5 1.5v5.6c0 .83-.67 1.5-1.5 1.5-.83 0-1.5-.67-1.5-1.5V15.5c0-.276-.224-.5-.5-.5s-.5.224-.5.5v1.3c0 1.38 1.12 2.5 2.5 2.5s2.5-1.12 2.5-2.5v-5.6c0-1.38-1.12-2.5-2.5-2.5zM6.356 5.342C5.239 5.85 4.34 6.813 4.002 8H.5C-.62 8-1.5 8.88-1.5 10v5.6c0 1.38 1.12 2.5 2.5 2.5s2.5-1.12 2.5-2.5V14.3c0-.276.224-.5.5-.5s.5.224.5.5v1.5c0 .83-.67 1.5-1.5 1.5s-1.5-.67-1.5-1.5v-5.6c0-.83.67-1.5 1.5-1.5h4.553c.25 0 .462-.173.487-.423.178-1.763 1.536-3.17 3.345-3.395.248-.03.41-.283.337-.525l-.91-3.024c-.12-.4-.56-.624-.938-.454l-1.518.663z"/>
      </svg>
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
