<template>
  <div
    class="game-card cursor-pointer"
    :class="{ 'game-card-compact': hideTitle }"
    @click="$emit('click', game)"
  >
    <img
      :src="optimizedImageUrl"
      :alt="game.name || game.title"
      :class="imageClass"
      class="w-full object-cover rounded-lg shadow-md"
      loading="lazy"
    />
    <p
      v-if="!hideTitle"
      class="font-tinos text-base text-theme-text-primary dark:text-theme-text-primary-dark mt-3 line-clamp-2 min-h-[3rem]"
    >
      {{ game.name || game.title }}
    </p>
  </div>
</template>

<script setup>
import { computed } from 'vue'
import { getContextualIgdbImage, IMAGE_CONTEXTS, IGDB_IMAGE_SIZES } from '@/utils/igdbImageSizing'

const props = defineProps({
  game: {
    type: Object,
    required: true
  },
  imageSize: {
    type: String,
    default: 'default', // 'default', 'small', 'large'
    validator: (value) => ['default', 'small', 'large'].includes(value)
  },
  hideTitle: {
    type: Boolean,
    default: false
  }
})

defineEmits(['click'])

const imageClass = computed(() => {
  const sizeClasses = {
    small: 'aspect-[3/4] max-h-48',
    default: 'aspect-[3/4]',
    large: 'aspect-[3/4] max-h-96'
  }
  return sizeClasses[props.imageSize] || sizeClasses.default
})

const optimizedImageUrl = computed(() => {
  // Check multiple possible image properties
  const rawUrl = props.game.coverImageUrl ||
                 props.game.primaryImageUrl ||
                 props.game.cover?.imageUrl ||
                 props.game.image ||
                 '/placeholder-game.jpg'

  // Map imageSize prop to IGDB size
  const igdbSizeMap = {
    small: IGDB_IMAGE_SIZES.COVER_SMALL,
    default: IGDB_IMAGE_SIZES.COVER_BIG,
    large: IGDB_IMAGE_SIZES.HD_720P
  }

  const targetSize = igdbSizeMap[props.imageSize] || IGDB_IMAGE_SIZES.COVER_BIG

  // Use contextual image with custom size
  return getContextualIgdbImage(rawUrl, IMAGE_CONTEXTS.GAME_CARD, targetSize)
})
</script>

<style scoped>
/* Component-specific styles only - .line-clamp-2 is now in base.css @layer components */
.game-card {
  display: flex;
  flex-direction: column;
}

/* Compact mode: card height matches image height exactly */
.game-card-compact {
  height: fit-content;
}

.game-card-compact img {
  margin-bottom: 0;
}
</style>
