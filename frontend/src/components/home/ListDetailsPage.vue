<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/authStore'
import ListActionsPanel from './ActionsPanel.vue'
import GameCard from './GameCard.vue'
import CommentList from './CommentList.vue'
import { useLists } from '@/composables/lists/useLists'
import { useListLikes } from '@/composables/lists/useListLikes'
import PaginationControls from '@/components/home/buttons/PaginationControls.vue'

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()
const { loadList, updateList, deleteList } = useLists()
const { toggleLike, isLiked, loadLikeStatus } = useListLikes()

// Props from route
const listId = ref(route.params.listId)

// State
const list = ref(null)
const loading = ref(true)
const error = ref(null)
const currentPage = ref(1)
const gamesPerPage = 20
//const gamesPerRow = 5

// Computed
const canEdit = computed(() => {
  return authStore.user && list.value?.userId === authStore.user.id
})

const canDelete = computed(() => {
  return authStore.user && list.value?.userId === authStore.user.id
})

const isListLiked = computed(() => {
  return list.value ? isLiked(list.value.id) : false
})

const paginatedGames = computed(() => {
  if (!list.value?.games) return []
  const start = (currentPage.value - 1) * gamesPerPage
  const end = start + gamesPerPage
  return list.value.games.slice(start, end)
})

const totalPages = computed(() => {
  if (!list.value?.games) return 0
  return Math.ceil(list.value.games.length / gamesPerPage)
})

const relativeDate = computed(() => {
  if (!list.value?.updatedAt) return ''

  const now = new Date()
  const updated = new Date(list.value.updatedAt)
  const diffInMs = now - updated
  const diffInDays = Math.floor(diffInMs / (1000 * 60 * 60 * 24))

  if (diffInDays === 0) return 'today'
  if (diffInDays === 1) return 'yesterday'
  return `${diffInDays} days ago`
})

// Methods
const loadListDetails = async () => {
  loading.value = true
  error.value = null

  try {
    const data = await loadList(listId.value)
    list.value = data

    // Load like status if user is authenticated
    if (authStore.user && list.value) {
      await loadLikeStatus(list.value.id)
    }
  } catch (err) {
    error.value = 'Failed to load list details'
    console.error('Error loading list:', err)
  } finally {
    loading.value = false
  }
}

const handleEdit = () => {
  // Navigate to edit page or open edit modal
  router.push(`/lists/${listId.value}/edit`)
}

const handleDelete = async () => {
  if (!list.value) return

  /*const success = */await deleteList(list.value, () => {
    router.push('/lists')
  })
}

const handleLike = async () => {
  if (!list.value) return

  await toggleLike(list.value, (wasLiked) => {
    list.value.likeCount = (list.value.likeCount || 0) + (wasLiked ? -1 : 1)
  })
}

const handleTogglePrivacy = async () => {
  if (!list.value) return

  const updatedData = {
    ...list.value,
    isPublic: !list.value.isPublic
  }

  /*const result = */await updateList(list.value.id, updatedData, (updated) => {
    list.value = updated
  })
}

const handleGameClick = (game) => {
  if (game.slug) {
    router.push({ name: 'GameDetails', params: { identifier: game.slug } })
  } else if (game.id) {
    router.push({ name: 'GameDetails', params: { identifier: game.id } })
  }
}

const handlePageChange = (newPage) => {
  currentPage.value = newPage
}

onMounted(() => {
  loadListDetails()
})
</script>

<template>
  <div class="min-h-screen bg-theme-bg-primary dark:bg-theme-bg-primary-dark transition-colors duration-200">

    <!-- List Details Section -->
    <div class="flex justify-center px-4 md:px-8 lg:px-40 mt-8">
      <div class="w-full max-w-1280">
        <!-- Loading State -->
        <div v-if="loading" class="text-center py-16">
          <div class="inline-block animate-spin rounded-full h-12 w-12 border-b-2 border-theme-text-primary dark:border-theme-text-primary-dark"></div>
          <p class="font-tinos text-base text-theme-text-secondary dark:text-theme-text-secondary-dark mt-4">Loading list...</p>
        </div>

        <!-- Error State -->
        <div v-else-if="error" class="text-center py-16">
          <p class="font-tinos text-lg text-red-500">{{ error }}</p>
        </div>

        <!-- List Content -->
        <div v-else-if="list" class="flex flex-col lg:flex-row gap-6">
          <!-- Column 1: 75% - Main Content -->
          <div class="flex-1" style="width: 75%">
            <!-- Row 1: User info -->
            <div class="flex items-center gap-3 mb-3">
              <img
                v-if="list.user?.profileImageUrl"
                :src="list.user.profileImageUrl"
                :alt="list.user.displayName || list.user.username"
                class="w-10 h-10 rounded-full object-cover"
                @error="(e) => (e.target.style.display = 'none')"
              />
              <span class="font-tinos text-base text-theme-text-secondary dark:text-theme-text-secondary-dark">
                List by <span class="font-semibold">{{ list.user?.displayName || list.user?.username || 'Unknown' }}</span>
              </span>
            </div>

            <!-- Row 2: Updated date -->
            <div class="mb-2">
              <span class="font-tinos text-sm text-theme-text-secondary dark:text-theme-text-secondary-dark">Updated {{ relativeDate }}</span>
            </div>

            <!-- Row 3: List title -->
            <h2 class="font-newsreader text-3xl font-bold text-theme-text-primary dark:text-theme-text-primary-dark mb-4">
              {{ list.title }}
            </h2>

            <!-- Row 4: List description -->
            <div v-if="list.description" class="mb-6">
              <p class="font-tinos text-base text-theme-text-primary dark:text-theme-text-primary-dark leading-6 whitespace-pre-wrap">
                {{ list.description }}
              </p>
            </div>

            <!-- Row 5+: Game grid -->
            <div v-if="paginatedGames.length > 0" class="mb-8">
              <div class="grid grid-cols-5 gap-4">
                <GameCard
                  v-for="game in paginatedGames"
                  :key="game.id"
                  :game="game"
                  image-size="default"
                  @click="handleGameClick"
                />
              </div>

              <!-- Pagination Controls -->
              <PaginationControls
                :current-page="currentPage"
                :total-pages="totalPages"
                @change="handlePageChange"
              />
            </div>

            <!-- Empty State -->
            <div v-else class="text-center py-12">
              <p class="font-tinos text-lg text-theme-text-secondary dark:text-theme-text-secondary-dark">No games in this list yet.</p>
            </div>

            <!-- Comments Section -->
            <CommentList
              v-if="list"
              content-type="list"
              :content-id="list.id"
            />
          </div>

          <!-- Column 2: 25% - Actions Panel -->
          <div style="width: 25%">
            <div class="lg:sticky lg:top-4">
              <ListActionsPanel
                context="list"
                :can-edit="canEdit"
                :can-delete="canDelete"
                :is-liked="isListLiked"
                :like-count="list.likeCount || 0"
                :is-public="list.isPublic !== false"
                @edit="handleEdit"
                @delete="handleDelete"
                @like="handleLike"
                @toggle-privacy="handleTogglePrivacy"
              />
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.max-w-1280 {
  max-width: 1280px;
}
</style>
