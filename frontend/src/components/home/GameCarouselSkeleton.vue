<template>
  <div class="animate-pulse">
    <div class="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6 gap-6">
      <div v-for="n in skeletonCount" :key="`skeleton-${n}`" class="flex flex-col">
        <!-- Skeleton game image -->
        <div class="bg-gray-300 rounded-lg w-full aspect-[3/4]"></div>
        <!-- Skeleton game title -->
        <div class="mt-3 h-4 bg-gray-300 rounded w-3/4"></div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({
  count: {
    type: Number,
    default: 6
  }
})

// Computed to handle responsive skeleton count
const skeletonCount = computed(() => props.count)
</script>
