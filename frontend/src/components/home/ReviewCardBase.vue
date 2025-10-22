<script setup>
import { computed } from 'vue'
import ReviewCardPopular from './ReviewCardPopular.vue'
import ReviewCardDetail from './ReviewCardDetail.vue'
import ReviewCardList from './ReviewCardList.vue'

const props = defineProps({
  review: {
    type: Object,
    required: true
  },
  variant: {
    type: String,
    default: 'popular',
    validator: (value) => ['popular', 'detail', 'list'].includes(value)
  },
  maxCharacters: {
    type: Number,
    default: 460
  }
})

const emit = defineEmits(['like-review'])

const currentComponent = computed(() => {
  const componentMap = {
    popular: ReviewCardPopular,
    detail: ReviewCardDetail,
    list: ReviewCardList
  }
  return componentMap[props.variant]
})

const handleLikeReview = (review) => {
  emit('like-review', review)
}
</script>

<template>
  <component
    :is="currentComponent"
    :review="review"
    :max-characters="maxCharacters"
    @like-review="handleLikeReview"
  />
</template>
