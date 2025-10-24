<template>
  <div
    class="list-card-popular cursor-pointer"
    @click="navigateToList"
  >
    <!-- Overlapping Game Images -->
    <OverlappingGameImages
      :games="list.games || []"
      :max-display="5"
      class="mb-3"
    />

    <!-- List Info -->
    <div class="flex flex-col gap-2">
      <!-- List Title -->
      <h4 class="font-newsreader text-lg font-bold text-cod-gray line-clamp-2 hover:underline">
        {{ list.title }}
      </h4>

      <!-- User, Game Count, and Likes -->
      <div class="flex items-center justify-between text-sm">
        <div class="flex items-center gap-2">
          <img
            v-if="list.user?.profileImageUrl"
            :src="list.user.profileImageUrl"
            :alt="list.user.displayName || list.user.username"
            class="w-6 h-6 rounded-full object-cover"
            @error="handleUserImageError"
          />
          <span class="font-tinos text-river-bed">
            {{ list.user?.displayName || list.user?.username || 'Unknown' }}
          </span>

          <!-- Game count badge -->
          <span class="bg-gray-200 text-river-bed px-2 py-0.5 rounded text-xs font-tinos">
            {{ list.gameCount || 0 }} {{ list.gameCount === 1 ? 'game' : 'games' }}
          </span>
        </div>

        <!-- Like count -->
        <div class="flex items-center gap-1 text-river-bed">
          <svg class="w-4 h-5" viewBox="0 0 17 21" fill="none" xmlns="http://www.w3.org/2000/svg">
            <path
              d="M12.6879 16.8906H5.35454V8.22396L10.0212 3.55729L10.8545 4.39062C10.9323 4.4684 10.9962 4.57396 11.0462 4.70729C11.0962 4.84062 11.1212 4.9684 11.1212 5.09062V5.32396L10.3879 8.22396H14.6879C15.0434 8.22396 15.3545 8.35729 15.6212 8.62396C15.8879 8.89062 16.0212 9.20174 16.0212 9.55729V10.8906C16.0212 10.9684 16.0101 11.0517 15.9879 11.1406C15.9657 11.2295 15.9434 11.3128 15.9212 11.3906L13.9212 16.0906C13.8212 16.3128 13.6545 16.5017 13.4212 16.6573C13.1879 16.8128 12.9434 16.8906 12.6879 16.8906ZM6.68788 15.5573H12.6879L14.6879 10.8906V9.55729H8.68788L9.58788 5.89062L6.68788 8.79062V15.5573ZM6.68788 8.79062V9.55729V10.8906V15.5573V8.79062ZM5.35454 8.22396V9.55729H3.35454V15.5573H5.35454V16.8906H2.02121V8.22396H5.35454Z"
              fill="#4B5563"
            />
          </svg>
          <span class="font-tinos text-sm">{{ formatLikes(list.likeCount) }}</span>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { useRouter } from 'vue-router'
import OverlappingGameImages from './OverlappingGameImages.vue'

const props = defineProps({
  list: {
    type: Object,
    required: true
  }
})

const router = useRouter()

// Methods
const navigateToList = () => {
  router.push(`/lists/${props.list.id}`)
}

const formatLikes = (count) => {
  if (!count) return '0'
  if (count >= 1000) {
    return `${(count / 1000).toFixed(1)}k`
  }
  return count.toString()
}

const handleUserImageError = (e) => {
  e.target.style.display = 'none'
}
</script>

<style scoped>
/* .list-card-popular {
  transition: transform 0.2s ease, box-shadow 0.2s ease;
}

.list-card-popular:hover {
  transform: translateY(-4px);
  box-shadow: 0 8px 16px rgba(0, 0, 0, 0.1);
} */

.line-clamp-2 {
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}
</style>
