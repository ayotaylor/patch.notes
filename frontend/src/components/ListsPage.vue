<template>
  <div class="lists-page">
    <div class="container-fluid py-4">
      <!-- Page Header -->
      <div class="row mb-4">
        <div class="col">
          <div class="d-flex justify-content-between align-items-center">
            <div>
              <h1 class="mb-1">Game Lists</h1>
              <p class="text-muted mb-0">Discover and create curated game collections</p>
            </div>
            
            <!-- Create List Button -->
            <button 
              v-if="authStore.user"
              @click="showCreateListModal"
              class="btn btn-primary"
            >
              <i class="fas fa-plus me-2"></i>Create List
            </button>
          </div>
        </div>
      </div>

      <!-- Filter Tabs -->
      <div class="row mb-4">
        <div class="col">
          <ul class="nav nav-tabs">
            <li class="nav-item">
              <button 
                class="nav-link"
                :class="{ active: activeTab === 'public' }"
                @click="setActiveTab('public')"
              >
                <i class="fas fa-globe me-2"></i>Public Lists
              </button>
            </li>
            <li v-if="authStore.user" class="nav-item">
              <button 
                class="nav-link"
                :class="{ active: activeTab === 'my' }"
                @click="setActiveTab('my')"
              >
                <i class="fas fa-user me-2"></i>My Lists
              </button>
            </li>
          </ul>
        </div>
      </div>

      <!-- Lists Content -->
      <div class="row">
        <div class="col">
          <!-- Loading State -->
          <div v-if="loading && lists.length === 0" class="text-center py-5">
            <div class="spinner-border text-primary" role="status">
              <span class="visually-hidden">Loading...</span>
            </div>
            <p class="mt-3 text-muted">Loading lists...</p>
          </div>

          <!-- Error State -->
          <div v-else-if="error" class="alert alert-danger" role="alert">
            <h4 class="alert-heading">Error Loading Lists</h4>
            <p>{{ error }}</p>
            <button @click="loadLists" class="btn btn-outline-danger">
              <i class="fas fa-redo me-2"></i>Try Again
            </button>
          </div>

          <!-- Lists Grid -->
          <div v-else-if="lists.length > 0">
            <div class="row g-4">
              <div v-for="list in lists" :key="list.id" class="col-12 col-lg-6 col-xl-4">
                <ListCard
                  :list="list"
                  :is-liked="likedLists.has(list.id)"
                  :is-processing-like="processingLikeLists.has(list.id)"
                  @edit="handleEditList"
                  @delete="handleDeleteList"
                  @toggleLike="handleToggleLike"
                  @showComments="handleShowComments"
                />
              </div>
            </div>

            <!-- Load More Button -->
            <div v-if="hasMoreLists" class="text-center mt-4">
              <button 
                @click="loadMoreLists"
                class="btn btn-outline-primary"
                :disabled="loadingMore"
              >
                <span v-if="loadingMore" class="spinner-border spinner-border-sm me-2" role="status"></span>
                Load More Lists
              </button>
            </div>
          </div>

          <!-- Empty State -->
          <div v-else class="text-center text-muted py-5">
            <i class="fas fa-list-ul fa-3x mb-3 opacity-50"></i>
            <h3>{{ emptyStateTitle }}</h3>
            <p class="mb-3">{{ emptyStateMessage }}</p>
            <button 
              v-if="authStore.user && (activeTab === 'my' || activeTab === 'public')"
              @click="showCreateListModal"
              class="btn btn-primary"
            >
              <i class="fas fa-plus me-2"></i>Create Your First List
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Create/Edit List Modal -->
    <ListForm
      modal-id="listFormModal"
      :list="editingList"
      @submit="handleSubmitList"
      @close="handleCloseListModal"
      ref="listFormRef"
    />

    <!-- Delete Confirmation Modal -->
    <div class="modal fade" id="deleteListModal" tabindex="-1" aria-hidden="true">
      <div class="modal-dialog">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">Delete List</h5>
            <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
          </div>
          <div class="modal-body">
            <p>Are you sure you want to delete <strong>"{{ deletingList?.title }}"</strong>?</p>
            <p class="text-muted mb-0">This action cannot be undone.</p>
          </div>
          <div class="modal-footer">
            <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">
              Cancel
            </button>
            <button 
              @click="confirmDeleteList"
              class="btn btn-danger"
              :disabled="isDeletingList"
            >
              <span v-if="isDeletingList" class="spinner-border spinner-border-sm me-2" role="status"></span>
              Delete List
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, watch, nextTick } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/authStore'
import { listsService } from '@/services/listsService'
import { socialService } from '@/services/socialService'
import ListCard from './ListCard.vue'
import ListForm from './ListForm.vue'
import { useAuthRedirect } from '@/utils/authRedirect'

// Composables
const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()
const { requireAuth } = useAuthRedirect()

// State
const lists = ref([])
const activeTab = ref('public')
const loading = ref(false)
const loadingMore = ref(false)
const error = ref(null)
const hasMoreLists = ref(false)
const currentPage = ref(1)
const pageSize = ref(12)

const editingList = ref(null)
const deletingList = ref(null)
const isDeletingList = ref(false)

const likedLists = ref(new Set())
const processingLikeLists = ref(new Set())

const listFormRef = ref(null)

// Computed
const emptyStateTitle = computed(() => {
  if (activeTab.value === 'my') {
    return 'No Lists Created Yet'
  }
  return 'No Public Lists Found'
})

const emptyStateMessage = computed(() => {
  if (activeTab.value === 'my') {
    return 'Create your first game list to get started organizing your favorite games.'
  }
  return 'Be the first to create a public game list for others to discover!'
})

// Methods
const handleRouteChange = () => {
  const tab = route.query.tab || 'public'
  if (['public', 'my'].includes(tab)) {
    activeTab.value = tab
  }
}

const setActiveTab = (tab) => {
  activeTab.value = tab
  // Update URL without triggering navigation
  const newQuery = { ...route.query, tab }
  if (tab === 'public') {
    delete newQuery.tab // Remove tab from URL for public (default)
  }
  
  router.replace({ query: newQuery }).catch(() => {
    // Ignore navigation errors (same route)
  })
}

const resetAndLoadLists = () => {
  lists.value = []
  currentPage.value = 1
  hasMoreLists.value = false
  error.value = null
  likedLists.value.clear()
  processingLikeLists.value.clear()
  loadLists()
}

const loadLists = async () => {
  try {
    loading.value = currentPage.value === 1
    error.value = null

    let result
    if (activeTab.value === 'public') {
      result = await listsService.getPublicLists(currentPage.value, pageSize.value)
    } else if (activeTab.value === 'my' && authStore.user) {
      result = await listsService.getUserLists(authStore.user.id, currentPage.value, pageSize.value)
    }

    if (currentPage.value === 1) {
      lists.value = result.data || []
    } else {
      lists.value.push(...(result.data || []))
    }

    hasMoreLists.value = result.hasNextPage || false

    // Load like status for authenticated users
    if (authStore.user && lists.value.length > 0) {
      await loadLikeStatus()
    }

  } catch (err) {
    console.error('Error loading lists:', err)
    error.value = err.message || 'Failed to load lists'
  } finally {
    loading.value = false
    loadingMore.value = false
  }
}

const loadMoreLists = () => {
  if (hasMoreLists.value && !loadingMore.value) {
    currentPage.value++
    loadingMore.value = true
    loadLists()
  }
}

const loadLikeStatus = async () => {
  if (!authStore.user || lists.value.length === 0) return

  try {
    // Clear existing liked lists
    likedLists.value.clear()
    
    // Check like status for each list
    const likeStatusPromises = lists.value.map(async (list) => {
      try {
        const isLiked = await socialService.isListLiked(list.id)
        if (isLiked) {
          likedLists.value.add(list.id)
        }
      } catch (error) {
        console.warn(`Failed to check like status for list ${list.id}:`, error)
      }
    })

    // Wait for all like status checks to complete
    await Promise.all(likeStatusPromises)
  } catch (err) {
    console.error('Error loading like status:', err)
  }
}

const showCreateListModal = () => {
  editingList.value = null
  nextTick(() => {
    if (listFormRef.value) {
      listFormRef.value.resetForm()
    }
    const modal = new window.bootstrap.Modal(document.getElementById('listFormModal'))
    modal.show()
  })
}

const handleEditList = (list) => {
  editingList.value = list
  nextTick(() => {
    const modal = new window.bootstrap.Modal(document.getElementById('listFormModal'))
    modal.show()
  })
}

const handleSubmitList = async (listData) => {
  try {
    let result
    if (editingList.value) {
      // Update existing list
      result = await listsService.updateList(editingList.value.id, listData)
      
      // Update in lists array
      const listIndex = lists.value.findIndex(l => l.id === editingList.value.id)
      if (listIndex !== -1) {
        lists.value[listIndex] = result
      }
    } else {
      // Create new list
      result = await listsService.createList(listData)
      
      // Add to beginning of lists if on "my" tab
      if (activeTab.value === 'my') {
        lists.value.unshift(result)
      }
    }

    // Hide modal
    const modal = window.bootstrap.Modal.getInstance(document.getElementById('listFormModal'))
    modal.hide()

    editingList.value = null

  } catch (err) {
    console.error('Error submitting list:', err)
    // Handle error - could show toast notification
  }
}

const handleCloseListModal = () => {
  editingList.value = null
}

const handleDeleteList = (list) => {
  deletingList.value = list
  const modal = new window.bootstrap.Modal(document.getElementById('deleteListModal'))
  modal.show()
}

const confirmDeleteList = async () => {
  if (!deletingList.value) return

  try {
    isDeletingList.value = true
    await listsService.deleteList(deletingList.value.id)

    // Remove from lists array
    const listIndex = lists.value.findIndex(l => l.id === deletingList.value.id)
    if (listIndex !== -1) {
      lists.value.splice(listIndex, 1)
    }

    // Hide modal
    const modal = window.bootstrap.Modal.getInstance(document.getElementById('deleteListModal'))
    modal.hide()

    deletingList.value = null

  } catch (err) {
    console.error('Error deleting list:', err)
  } finally {
    isDeletingList.value = false
  }
}

const handleToggleLike = async (list) => {
  if (requireAuth(authStore.isAuthenticated, 'Please sign in to like lists')) {
    return
  }

  const listId = list.id
  const wasLiked = likedLists.value.has(listId)

  // Prevent multiple simultaneous requests
  if (processingLikeLists.value.has(listId)) return

  try {
    processingLikeLists.value.add(listId)

    if (wasLiked) {
      await socialService.unlikeList(listId)
      likedLists.value.delete(listId)
    } else {
      await socialService.likeList(listId)
      likedLists.value.add(listId)
    }

    // Update like count in list
    const targetList = lists.value.find(l => l.id === listId)
    if (targetList) {
      targetList.likeCount = (targetList.likeCount || 0) + (wasLiked ? -1 : 1)
    }

  } catch (err) {
    console.error('Error toggling list like:', err)
  } finally {
    processingLikeLists.value.delete(listId)
  }
}

const handleShowComments = (list) => {
  // Navigate to list details page where comments will be shown
  router.push(`/lists/${list.id}`)
}

// Watch for route changes
watch(() => route.query, () => {
  handleRouteChange()
}, { immediate: true })

// Watch for tab changes
watch(activeTab, () => {
  resetAndLoadLists()
})

// Watch for authentication changes to reload like status
watch(() => authStore.user, async (newUser, oldUser) => {
  // If user logs in or out, reload like status
  if (!!newUser !== !!oldUser) {
    if (newUser && lists.value.length > 0) {
      // User logged in - load like status
      await loadLikeStatus()
    } else {
      // User logged out - clear like status
      likedLists.value.clear()
    }
  }
})

// Lifecycle
onMounted(() => {
  if (activeTab.value) {
    loadLists()
  }
})
</script>

<style scoped>
.lists-page {
  min-height: 100vh;
  background-color: #f8f9fa;
}

.nav-tabs .nav-link {
  border: none;
  background: none;
  color: #6c757d;
  font-weight: 500;
}

.nav-tabs .nav-link:hover {
  border-color: transparent;
  color: #495057;
}

.nav-tabs .nav-link.active {
  color: #007bff;
  border-bottom: 2px solid #007bff;
  background: none;
}

@media (max-width: 768px) {
  .container-fluid {
    padding-left: 1rem;
    padding-right: 1rem;
  }
  
  .d-flex.justify-content-between {
    flex-direction: column;
    align-items: flex-start !important;
  }
  
  .btn-primary {
    margin-top: 1rem;
    align-self: stretch;
  }
}
</style>