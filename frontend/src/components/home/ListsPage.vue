<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import OverlappingGameImages from './OverlappingGameImages.vue'
import { useLists } from '@/composables/lists/useLists'
import { useListLikes } from '@/composables/lists/useListLikes'
import PaginationControls from '@/components/home/buttons/PaginationControls.vue'

const router = useRouter()
const { loadLists } = useLists()
const { loadLikeStatusBatch } = useListLikes()

// Props for configurable layout
const props = defineProps({
  columnSplit: {
    type: String,
    default: '50/50', // Options: '50/50', '40/60', '30/70'
    validator: (value) => ['50/50', '40/60', '30/70', '60/40', '70/30'].includes(value)
  }
})

// State
const lists = ref([])
const loading = ref(true)
const error = ref(null)
const currentPage = ref(1)
const totalPages = ref(0)
const totalCount = ref(0)
const listsPerPage = 20

// Computed
const columnWidths = computed(() => {
  const [left, right] = props.columnSplit.split('/')
  return {
    left: `${left}%`,
    right: `${right}%`
  }
})

// Methods
const getMaxDisplayGames = (gameCount) => {
  if (gameCount >= 10) return 10
  return Math.min(gameCount, 5)
}

const formatLikes = (count) => {
  if (!count) return '0'
  if (count >= 1000) {
    return `${(count / 1000).toFixed(1)}k`
  }
  return count.toString()
}

const loadListsData = async (page = 1) => {
  loading.value = true
  error.value = null

  try {
    const result = await loadLists({ page, pageSize: listsPerPage })
    lists.value = result.data
    totalPages.value = result.totalPages
    totalCount.value = result.totalCount
    currentPage.value = result.page

    // Load like status for all lists if user is authenticated
    if (result.data.length > 0) {
      await loadLikeStatusBatch(result.data)
    }
  } catch (err) {
    error.value = 'Failed to load lists'
    console.error('Error loading lists:', err)
  } finally {
    loading.value = false
  }
}

const goToListDetails = (listId) => {
  router.push(`/lists/${listId}`)
}

const goToUserProfile = (userId) => {
  router.push(`/profile/${userId}`)
}

onMounted(() => {
  loadListsData(1)
})
</script>

<template>
  <div class="min-h-screen bg-theme-bg-primary dark:bg-theme-bg-primary-dark transition-colors duration-200">

    <!-- Lists Section -->
    <div class="flex justify-center px-4 md:px-8 lg:px-40 mt-8">
      <div class="w-full max-w-1280">
        <!-- Page Header -->
        <div class="mb-6 border-b border-theme-border dark:border-theme-border-dark pb-4">
          <h2 class="font-newsreader text-3xl font-bold text-theme-text-primary dark:text-theme-text-primary-dark">
            All Lists
          </h2>
          <p v-if="totalCount > 0" class="font-tinos text-base text-theme-text-secondary dark:text-theme-text-secondary-dark mt-2">
            {{ totalCount }} {{ totalCount === 1 ? 'list' : 'lists' }} total
          </p>
        </div>

        <!-- Loading State -->
        <div v-if="loading" class="text-center py-16">
          <div class="inline-block animate-spin rounded-full h-12 w-12 border-b-2 border-theme-text-primary dark:border-theme-text-primary-dark"></div>
          <p class="font-tinos text-base text-theme-text-secondary dark:text-theme-text-secondary-dark mt-4">Loading lists...</p>
        </div>

        <!-- Error State -->
        <div v-else-if="error" class="text-center py-16">
          <p class="font-tinos text-lg text-red-500">{{ error }}</p>
          <button
            class="mt-4 px-6 py-2 bg-theme-btn-primary dark:bg-theme-btn-primary-dark text-white rounded font-tinos text-base hover:bg-opacity-90"
            @click="loadListsData(currentPage)"
          >
            Try Again
          </button>
        </div>

        <!-- Lists Grid -->
        <div v-else-if="lists.length > 0" class="space-y-6">
          <!-- Each List Row -->
          <div
            v-for="list in lists"
            :key="list.id"
            class="bg-theme-bg-secondary dark:bg-theme-bg-secondary-dark rounded-lg shadow-sm overflow-hidden hover:shadow-md transition-shadow"
          >
            <div class="flex">
              <!-- Column 1: Overlapping Game Images -->
              <div
                class="flex-shrink-0 cursor-pointer"
                :style="{ width: columnWidths.left }"
                @click="goToListDetails(list.id)"
              >
                <OverlappingGameImages
                  :games="list.games || []"
                  :max-display="getMaxDisplayGames(list.gameCount || 0)"
                  class="h-48"
                />
              </div>

              <!-- Column 2: List Info -->
              <div
                class="flex flex-col justify-center p-6"
                :style="{ width: columnWidths.right }"
              >
                <!-- Row 1: List Title -->
                <h3
                  class="font-newsreader text-xl font-bold text-theme-text-primary dark:text-theme-text-primary-dark mb-3 hover:underline cursor-pointer"
                  @click="goToListDetails(list.id)"
                >
                  {{ list.title }}
                </h3>

                <!-- Row 2: User info and Like count -->
                <div class="flex items-center gap-4 mb-3">
                  <div class="flex items-center gap-2">
                    <img
                      v-if="list.user?.profileImageUrl"
                      :src="list.user.profileImageUrl"
                      :alt="list.user.displayName || list.user.username"
                      class="w-6 h-6 rounded-full object-cover cursor-pointer"
                      @click.stop="goToUserProfile(list.user.id)"
                      @error="(e) => (e.target.style.display = 'none')"
                    />
                    <span
                      class="font-tinos text-sm text-theme-text-secondary dark:text-theme-text-secondary-dark hover:underline cursor-pointer"
                      @click.stop="goToUserProfile(list.user?.id)"
                    >
                      {{ list.user?.displayName || list.user?.username || 'Unknown' }}
                    </span>
                  </div>

                  <!-- Game count badge -->
                  <span class="bg-gray-200 dark:bg-gray-700 text-theme-text-secondary dark:text-theme-text-secondary-dark px-2 py-0.5 rounded text-xs font-tinos">
                    {{ list.gameCount || 0 }} {{ list.gameCount === 1 ? 'game' : 'games' }}
                  </span>

                  <!-- Like count -->
                  <div class="flex items-center gap-1 text-theme-text-secondary dark:text-theme-text-secondary-dark">
                    <svg class="w-4 h-5" viewBox="0 0 17 21" fill="none" xmlns="http://www.w3.org/2000/svg">
                      <path
                        d="M12.6879 16.8906H5.35454V8.22396L10.0212 3.55729L10.8545 4.39062C10.9323 4.4684 10.9962 4.57396 11.0462 4.70729C11.0962 4.84062 11.1212 4.9684 11.1212 5.09062V5.32396L10.3879 8.22396H14.6879C15.0434 8.22396 15.3545 8.35729 15.6212 8.62396C15.8879 8.89062 16.0212 9.20174 16.0212 9.55729V10.8906C16.0212 10.9684 16.0101 11.0517 15.9879 11.1406C15.9657 11.2295 15.9434 11.3128 15.9212 11.3906L13.9212 16.0906C13.8212 16.3128 13.6545 16.5017 13.4212 16.6573C13.1879 16.8128 12.9434 16.8906 12.6879 16.8906ZM6.68788 15.5573H12.6879L14.6879 10.8906V9.55729H8.68788L9.58788 5.89062L6.68788 8.79062V15.5573ZM6.68788 8.79062V9.55729V10.8906V15.5573V8.79062ZM5.35454 8.22396V9.55729H3.35454V15.5573H5.35454V16.8906H2.02121V8.22396H5.35454Z"
                        fill="#4B5563"
                      />
                    </svg>
                    <span class="font-tinos text-sm">{{ formatLikes(list.likeCount) }}</span>
                  </div>
                </div>

                <!-- Row 3: List Description -->
                <p
                  v-if="list.description"
                  class="font-tinos text-sm text-theme-text-primary dark:text-theme-text-primary-dark leading-5 line-clamp-3"
                >
                  {{ list.description }}
                </p>
              </div>
            </div>
          </div>

          <!-- Pagination Controls -->
          <PaginationControls
            :current-page="currentPage"
            :total-pages="totalPages"
            @change="loadListsData"
          />
        </div>

        <!-- Empty State -->
        <div v-else class="text-center py-16">
          <p class="font-tinos text-lg text-theme-text-secondary dark:text-theme-text-secondary-dark">No lists found.</p>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.line-clamp-3 {
  display: -webkit-box;
  -webkit-line-clamp: 3;
  -webkit-box-orient: vertical;
  overflow: hidden;
}
</style>
