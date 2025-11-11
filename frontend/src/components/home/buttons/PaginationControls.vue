<template>
  <div v-if="totalPages > 1" class="flex justify-center gap-4 mt-8 pb-8">
    <ButtonComponent
      title="Previous"
      :variant="variant"
      :size="size"
      :disabled="currentPage === 1"
      @click="handlePrevious"
    />
    <span class="flex items-center font-tinos text-base text-theme-text-secondary dark:text-theme-text-secondary-dark">
      Page {{ currentPage }} of {{ totalPages }}
    </span>
    <ButtonComponent
      title="Next"
      :variant="variant"
      :size="size"
      :disabled="currentPage === totalPages"
      @click="handleNext"
    />
  </div>
</template>

<script setup>
import ButtonComponent from './ButtonComponent.vue'

const props = defineProps({
  // Current page number
  currentPage: {
    type: Number,
    required: true,
    validator: (value) => value >= 1
  },
  // Total number of pages
  totalPages: {
    type: Number,
    required: true,
    validator: (value) => value >= 1
  },
  // Button variant
  variant: {
    type: String,
    default: 'primary',
    validator: (value) => ['primary', 'secondary'].includes(value)
  },
  // Button size
  size: {
    type: String,
    default: 'md',
    validator: (value) => ['sm', 'md', 'lg'].includes(value)
  },
  // Auto-scroll to top on page change
  scrollToTop: {
    type: Boolean,
    default: true
  }
})

const emit = defineEmits(['next', 'previous', 'change'])

const handleNext = () => {
  if (props.currentPage < props.totalPages) {
    const nextPage = props.currentPage + 1
    emit('next', nextPage)
    emit('change', nextPage)

    if (props.scrollToTop) {
      window.scrollTo({ top: 0, behavior: 'smooth' })
    }
  }
}

const handlePrevious = () => {
  if (props.currentPage > 1) {
    const previousPage = props.currentPage - 1
    emit('previous', previousPage)
    emit('change', previousPage)

    if (props.scrollToTop) {
      window.scrollTo({ top: 0, behavior: 'smooth' })
    }
  }
}
</script>
