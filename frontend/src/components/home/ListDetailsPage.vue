<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/authStore'
import HomeHeader from './HeaderBar.vue'
import HomeNavigation from './NavigationBar.vue'
import ListActionsPanel from './ActionsPanel.vue'
import GameCard from './GameCard.vue'
import { useLists } from '@/composables/lists/useLists'
import { useListLikes } from '@/composables/lists/useListLikes'

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

// Placeholder comments data
const placeholderComments = ref([
  {
    id: 1,
    user: {
      username: 'GameCollector',
      profileImageUrl: 'https://via.placeholder.com/40'
    },
    text: 'Amazing list! I\'ve been looking for something exactly like this.'
  },
  {
    id: 2,
    user: {
      username: 'RPGFanatic',
      profileImageUrl: 'https://via.placeholder.com/40'
    },
    text: 'Great selection of games. I would also add Persona 5 to this list!'
  },
  {
    id: 3,
    user: {
      username: 'IndieGamer',
      profileImageUrl: 'https://via.placeholder.com/40'
    },
    text: 'Solid recommendations. Definitely checking out some of these.'
  }
])

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

const hasMultiplePages = computed(() => {
  return totalPages.value > 1
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

const goToNextPage = () => {
  if (currentPage.value < totalPages.value) {
    currentPage.value++
    window.scrollTo({ top: 0, behavior: 'smooth' })
  }
}

const goToPreviousPage = () => {
  if (currentPage.value > 1) {
    currentPage.value--
    window.scrollTo({ top: 0, behavior: 'smooth' })
  }
}

onMounted(() => {
  loadListDetails()
})
</script>

<template>
  <div class="min-h-screen bg-[#F6F7F7]">
    <!-- Header Component -->
    <HomeHeader />

    <!-- Navigation Component -->
    <HomeNavigation />

    <!-- List Details Section -->
    <div class="flex justify-center px-4 md:px-8 lg:px-40 mt-8">
      <div class="w-full max-w-1280">
        <!-- Loading State -->
        <div v-if="loading" class="text-center py-16">
          <div class="inline-block animate-spin rounded-full h-12 w-12 border-b-2 border-cod-gray"></div>
          <p class="font-tinos text-base text-river-bed mt-4">Loading list...</p>
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
              <span class="font-tinos text-base text-river-bed">
                List by <span class="font-semibold">{{ list.user?.displayName || list.user?.username || 'Unknown' }}</span>
              </span>
            </div>

            <!-- Row 2: Updated date -->
            <div class="mb-2">
              <span class="font-tinos text-sm text-gray-500">Updated {{ relativeDate }}</span>
            </div>

            <!-- Row 3: List title -->
            <h2 class="font-newsreader text-3xl font-bold text-cod-gray mb-4">
              {{ list.title }}
            </h2>

            <!-- Row 4: List description -->
            <div v-if="list.description" class="mb-6">
              <p class="font-tinos text-base text-ebony-clay leading-6 whitespace-pre-wrap">
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
              <div v-if="hasMultiplePages" class="flex justify-center gap-4 mt-8">
                <button
                  :disabled="currentPage === 1"
                  :class="[
                    'px-6 py-2 rounded font-tinos text-base transition-colors',
                    currentPage === 1
                      ? 'bg-gray-300 text-gray-500 cursor-not-allowed'
                      : 'bg-cod-gray text-white hover:bg-opacity-90'
                  ]"
                  @click="goToPreviousPage"
                >
                  Previous
                </button>
                <span class="flex items-center font-tinos text-base text-river-bed">
                  Page {{ currentPage }} of {{ totalPages }}
                </span>
                <button
                  :disabled="currentPage === totalPages"
                  :class="[
                    'px-6 py-2 rounded font-tinos text-base transition-colors',
                    currentPage === totalPages
                      ? 'bg-gray-300 text-gray-500 cursor-not-allowed'
                      : 'bg-cod-gray text-white hover:bg-opacity-90'
                  ]"
                  @click="goToNextPage"
                >
                  Next
                </button>
              </div>
            </div>

            <!-- Empty State -->
            <div v-else class="text-center py-12">
              <p class="font-tinos text-lg text-river-bed">No games in this list yet.</p>
            </div>

            <!-- Comments Section (Placeholder) -->
            <div class="border-t border-gray-300 pt-8 mt-8">
              <!-- Comments Header -->
              <h3 class="font-newsreader text-2xl font-bold text-cod-gray mb-6">
                Comments ({{ placeholderComments.length }})
              </h3>

              <!-- Comments List -->
              <div class="space-y-6">
                <div
                  v-for="comment in placeholderComments"
                  :key="comment.id"
                  class="border-b border-gray-200 pb-6 last:border-b-0"
                >
                  <div class="grid grid-cols-4 gap-4">
                    <!-- Column 1: User info (25% width) -->
                    <div class="col-span-1">
                      <div class="flex items-center gap-2">
                        <img
                          :src="comment.user.profileImageUrl"
                          :alt="comment.user.username"
                          class="w-8 h-8 rounded-full object-cover"
                          @error="(e) => (e.target.style.display = 'none')"
                        />
                        <span class="font-tinos text-sm text-cod-gray font-semibold">
                          {{ comment.user.username }}
                        </span>
                      </div>
                    </div>

                    <!-- Column 2: Comment text (75% width) -->
                    <div class="col-span-3">
                      <p class="font-tinos text-base text-ebony-clay leading-6">
                        {{ comment.text }}
                      </p>
                    </div>
                  </div>
                </div>
              </div>

              <!-- Placeholder message -->
              <div class="mt-8 p-4 bg-gray-100 rounded-lg">
                <p class="font-tinos text-sm text-river-bed italic text-center">
                  Comment functionality will be implemented in a future update.
                </p>
              </div>
            </div>
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
