<script setup>
import { computed } from 'vue'
import { getRelativeTimeDetailed } from '@/utils/dateUtils'

const props = defineProps({
  comment: {
    type: Object,
    required: true
  }
})

const relativeDate = computed(() => {
  return getRelativeTimeDetailed(props.comment?.createdAt)
})

const username = computed(() => {
  return props.comment?.user?.displayName || 'Unknown'
})

const profileImageUrl = computed(() => {
  return props.comment?.user?.profileImageUrl || 'https://via.placeholder.com/40'
})

const commentText = computed(() => {
  return props.comment?.content || props.comment?.text || ''
})
</script>

<template>
  <div class="border-b border-theme-border dark:border-theme-border-dark pb-6 last:border-b-0">
    <div class="grid grid-cols-4 gap-4">
      <!-- Column 1: User info (25% width) -->
      <div class="col-span-1">
        <div class="flex items-center gap-2">
          <img
            :src="profileImageUrl"
            :alt="username"
            class="w-8 h-8 rounded-full object-cover"
            @error="(e) => (e.target.style.display = 'none')"
          />
          <div class="flex flex-col">
            <span class="font-tinos text-sm text-theme-text-primary dark:text-theme-text-primary-dark font-semibold">
              {{ username }}
            </span>
            <span class="font-tinos text-xs text-theme-text-secondary dark:text-theme-text-secondary-dark">
              {{ relativeDate }}
            </span>
          </div>
        </div>
      </div>

      <!-- Column 2: Comment text (75% width) -->
      <div class="col-span-3">
        <p class="font-tinos text-base text-theme-text-primary dark:text-theme-text-primary-dark leading-6 whitespace-pre-wrap">
          {{ commentText }}
        </p>
      </div>
    </div>
  </div>
</template>

<style scoped>
</style>
