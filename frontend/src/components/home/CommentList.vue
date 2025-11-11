<script setup>
import { ref, computed, onMounted, watch } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/authStore'
import { useComments } from '@/composables/comments/useComments'
import CommentItem from './CommentItem.vue'
import PaginationControls from '@/components/home/buttons/PaginationControls.vue'

const props = defineProps({
  // Type of content: 'review' or 'list'
  contentType: {
    type: String,
    required: true,
    validator: (value) => ['review', 'list'].includes(value)
  },
  // ID of the content (reviewId or listId)
  contentId: {
    type: [String, Number],
    required: true
  }
})

const router = useRouter()
const route = useRoute()
const authStore = useAuthStore()
const { loadReviewComments, loadListComments, addReviewComment, addListComment } = useComments()

// State
const comments = ref([])
const loading = ref(true)
const error = ref(null)
const currentPage = ref(1)
const totalPages = ref(0)
const totalCount = ref(0)
const hasNextPage = ref(false)
const hasPreviousPage = ref(false)
const commentsPerPage = 10

// Comment input state
const newCommentContent = ref('')
const isSubmitting = ref(false)
const MAX_COMMENT_LENGTH = 480

// Computed
const isAuthenticated = computed(() => {
  return authStore.isAuthenticated && authStore.user
})

const currentUsername = computed(() => {
  return authStore.user?.username || ''
})

const commentPlaceholder = computed(() => {
  return `Reply as ${currentUsername.value}`
})

const characterCount = computed(() => {
  return newCommentContent.value.length
})

const characterCountClass = computed(() => {
  if (characterCount.value > MAX_COMMENT_LENGTH) {
    return 'text-red-500 dark:text-red-400'
  } else if (characterCount.value > MAX_COMMENT_LENGTH * 0.9) {
    return 'text-yellow-600 dark:text-yellow-500'
  }
  return 'text-theme-text-secondary dark:text-theme-text-secondary-dark'
})

const canSubmit = computed(() => {
  return (
    newCommentContent.value.trim().length > 0 &&
    characterCount.value <= MAX_COMMENT_LENGTH &&
    !isSubmitting.value
  )
})

const hasComments = computed(() => {
  return comments.value.length > 0
})

// Methods
const loadComments = async () => {
  loading.value = true
  error.value = null

  try {
    let response
    if (props.contentType === 'review') {
      response = await loadReviewComments(props.contentId, currentPage.value, commentsPerPage)
    } else {
      response = await loadListComments(props.contentId, currentPage.value, commentsPerPage)
    }

    comments.value = response.data || []
    totalPages.value = response.totalPages || 0
    totalCount.value = response.totalCount || 0
    hasNextPage.value = response.hasNextPage || false
    hasPreviousPage.value = response.hasPreviousPage || false
  } catch (err) {
    error.value = 'Failed to load comments'
    console.error('Error loading comments:', err)
  } finally {
    loading.value = false
  }
}

const handleSubmitComment = async () => {
  if (!canSubmit.value) return

  isSubmitting.value = true

  try {
    let newComment
    if (props.contentType === 'review') {
      newComment = await addReviewComment(props.contentId, newCommentContent.value, (comment) => {
        // Add the new comment to the beginning of the list
        comments.value.unshift(comment)
        totalCount.value++
        newCommentContent.value = ''
      })
    } else {
      newComment = await addListComment(props.contentId, newCommentContent.value, (comment) => {
        // Add the new comment to the beginning of the list
        comments.value.unshift(comment)
        totalCount.value++
        newCommentContent.value = ''
      })
    }

    // If we're not on the first page and a comment was successfully added,
    // navigate to the first page to show the new comment
    if (newComment && currentPage.value !== 1) {
      currentPage.value = 1
      await loadComments()
    }
  } catch (err) {
    console.error('Error submitting comment:', err)
  } finally {
    isSubmitting.value = false
  }
}

const handlePageChange = (newPage) => {
  currentPage.value = newPage
}

const navigateToLogin = () => {
  // Get current route path to redirect back after login
  const currentPath = route.fullPath
  router.push(`/login?redirect=${encodeURIComponent(currentPath)}`)
}

// Watch for page changes
watch(currentPage, () => {
  loadComments()
})

// Load comments on mount
onMounted(() => {
  loadComments()
})
</script>

<template>
  <div class="border-t border-theme-border dark:border-theme-border-dark pt-8 mt-8">
    <!-- Comments Header -->
    <h3 class="font-newsreader text-2xl font-bold text-theme-text-primary dark:text-theme-text-primary-dark mb-6">
      Comments ({{ totalCount }})
    </h3>

    <!-- Loading State -->
    <div v-if="loading" class="text-center py-8">
      <div class="inline-block animate-spin rounded-full h-8 w-8 border-b-2 border-theme-text-primary dark:border-theme-text-primary-dark"></div>
      <p class="font-tinos text-sm text-theme-text-secondary dark:text-theme-text-secondary-dark mt-2">Loading comments...</p>
    </div>

    <!-- Error State -->
    <div v-else-if="error" class="text-center py-8">
      <p class="font-tinos text-base text-red-500">{{ error }}</p>
    </div>

    <!-- Comments Content -->
    <div v-else>
      <!-- Comments List -->
      <div v-if="hasComments" class="space-y-6 mb-8">
        <CommentItem
          v-for="comment in comments"
          :key="comment.id"
          :comment="comment"
        />
      </div>

      <!-- Empty State -->
      <div v-else class="text-center py-8 mb-8">
        <p class="font-tinos text-base text-theme-text-secondary dark:text-theme-text-secondary-dark">
          No comments yet. Be the first to comment!
        </p>
      </div>

      <!-- Pagination Controls -->
      <PaginationControls
        :current-page="currentPage"
        :total-pages="totalPages"
        @change="handlePageChange"
      />

      <!-- Comment Input Section -->
      <div v-if="isAuthenticated" class="mt-8">
        <div class="grid grid-cols-4 gap-4">
          <!-- Empty first column (25% width) -->
          <div class="col-span-1"></div>

          <!-- Comment input (75% width) -->
          <div class="col-span-3">
            <textarea
              v-model="newCommentContent"
              :placeholder="commentPlaceholder"
              :maxlength="MAX_COMMENT_LENGTH"
              rows="3"
              class="w-full px-4 py-3 rounded-lg border border-theme-border dark:border-theme-border-dark bg-theme-bg-secondary dark:bg-theme-bg-secondary-dark text-theme-text-primary dark:text-theme-text-primary-dark font-tinos text-base focus:outline-none focus:ring-2 focus:ring-theme-btn-primary dark:focus:ring-theme-btn-primary-dark resize-none"
            ></textarea>

            <!-- Character count and submit button -->
            <div class="flex items-center justify-between mt-2">
              <span :class="['font-tinos text-sm', characterCountClass]">
                {{ characterCount }}/{{ MAX_COMMENT_LENGTH }} characters
              </span>
              <button
                :disabled="!canSubmit"
                :class="[
                  'px-6 py-2 rounded font-tinos text-base transition-colors',
                  canSubmit
                    ? 'bg-theme-btn-primary dark:bg-theme-btn-primary-dark text-white hover:bg-opacity-90'
                    : 'bg-gray-300 dark:bg-gray-700 text-gray-500 dark:text-gray-400 cursor-not-allowed'
                ]"
                @click="handleSubmitComment"
              >
                {{ isSubmitting ? 'Posting...' : 'Post' }}
              </button>
            </div>
          </div>
        </div>
      </div>

      <!-- Sign in prompt for unauthenticated users -->
      <div v-else class="mt-8 text-center py-4">
        <p class="font-tinos text-base text-theme-text-secondary dark:text-theme-text-secondary-dark">
          <span
            class="text-theme-btn-primary dark:text-theme-btn-primary-dark hover:underline cursor-pointer"
            @click="navigateToLogin"
          >
            Sign in
          </span>
          to comment
        </p>
      </div>
    </div>
  </div>
</template>

<style scoped>
</style>
