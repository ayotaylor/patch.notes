<script setup>
import { computed } from 'vue'
import { getContextualIgdbImage, IGDB_IMAGE_SIZES, IMAGE_CONTEXTS } from '@/utils/igdbImageSizing'

const props = defineProps({
  imageUrl: {
    type: String,
    required: true
  },
  gameName: {
    type: String,
    required: true
  },
  size: {
    type: String,
    default: 'medium',
    validator: (value) => ['small', 'medium', 'large'].includes(value)
  },
  customClass: {
    type: String,
    default: ''
  },
  rounded: {
    type: Boolean,
    default: true
  }
})

// Size configurations based on requirements
const sizeConfig = {
  small: {
    width: 'w-[75px]',
    height: 'h-[115px]',
    igdbSize: IGDB_IMAGE_SIZES.COVER_SMALL,
    context: IMAGE_CONTEXTS.GAME_CARD_SMALL
  },
  medium: {
    width: 'w-[150px]',
    height: 'h-[227px]',
    igdbSize: IGDB_IMAGE_SIZES.COVER_BIG,
    context: IMAGE_CONTEXTS.GAME_CARD
  },
  large: {
    width: 'w-full',
    height: 'aspect-[3/4]',
    igdbSize: IGDB_IMAGE_SIZES.COVER_BIG,
    context: IMAGE_CONTEXTS.GAME_CARD
  }
}

const config = computed(() => sizeConfig[props.size])

const optimizedImageUrl = computed(() => {
  return getContextualIgdbImage(
    props.imageUrl,
    config.value.context,
    config.value.igdbSize
  )
})

const imageClasses = computed(() => {
  const classes = [
    'object-cover',
    config.value.width,
    config.value.height,
    props.rounded ? 'rounded-lg' : '',
    props.customClass
  ]
  return classes.filter(Boolean).join(' ')
})

const handleImageError = (event) => {
  event.target.src = 'https://via.placeholder.com/264x374?text=No+Image'
}
</script>

<template>
  <img
    :src="optimizedImageUrl"
    :alt="gameName"
    :class="imageClasses"
    loading="lazy"
    @error="handleImageError"
  />
</template>