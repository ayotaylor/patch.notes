<template>
  <div class="comment-section">
    <!-- Add Comment Form -->
    <div v-if="canComment" class="add-comment-form mb-4">
      <div class="d-flex align-items-start">
        <img
          :src="currentUserImageUrl"
          alt="Your profile"
          class="rounded-circle flex-shrink-0"
          style="width: 40px; height: 40px; object-fit: cover;"
        >
        <div class="flex-grow-1 ms-3">
          <textarea
            v-model="newCommentContent"
            class="form-control"
            rows="3"
            placeholder="Write a comment..."
            :disabled="isSubmittingComment"
          ></textarea>
          <div class="mt-2 d-flex justify-content-end">
            <button 
              @click="submitComment"
              class="btn btn-primary"
              :disabled="isSubmittingComment || !newCommentContent.trim()"
            >
              <span v-if="isSubmittingComment" class="spinner-border spinner-border-sm me-2" role="status"></span>
              Post Comment
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Login Prompt -->
    <div v-else-if="!authStore.user" class="text-center py-3 mb-4 border rounded">
      <p class="mb-2">Join the conversation!</p>
      <router-link to="/login" class="btn btn-primary btn-sm">
        Sign In to Comment
      </router-link>
    </div>

    <!-- Comments List -->
    <div v-if="loading && comments.length === 0" class="text-center py-4">
      <div class="spinner-border text-primary" role="status">
        <span class="visually-hidden">Loading comments...</span>
      </div>
      <p class="mt-2 text-muted">Loading comments...</p>
    </div>

    <div v-else-if="error" class="alert alert-danger" role="alert">
      <h6 class="alert-heading">Error Loading Comments</h6>
      <p class="mb-2">{{ error }}</p>
      <button @click="loadComments" class="btn btn-outline-danger btn-sm">
        <i class="fas fa-redo me-1"></i>Try Again
      </button>
    </div>

    <div v-else>
      <!-- Comments -->
      <div v-if="comments.length > 0" class="comments-list">
        <CommentCard
          v-for="comment in comments"
          :key="comment.id"
          :comment="comment"
          :is-liked="likedComments.has(comment.id)"
          :is-processing-like="processingLikeComments.has(comment.id)"
          :show-replies="showRepliesForComment.has(comment.id)"
          :replies="commentReplies.get(comment.id) || []"
          :liked-replies="likedComments"
          :processing-like-replies="processingLikeComments"
          @edit="handleEditComment"
          @delete="handleDeleteComment"
          @toggleLike="handleToggleLike"
          @reply="handleReply"
          @loadReplies="handleLoadReplies"
          @hideReplies="handleHideReplies"
        />
      </div>

      <!-- Empty State -->
      <div v-else class="text-center text-muted py-5">
        <i class="fas fa-comments fa-3x mb-3 opacity-50"></i>
        <h5>No Comments Yet</h5>
        <p class="mb-0">Be the first to share your thoughts!</p>
      </div>

      <!-- Load More Button -->
      <div v-if="hasMoreComments" class="text-center mt-4">
        <button 
          @click="loadMoreComments"
          class="btn btn-outline-primary"
          :disabled="loadingMore"
        >
          <span v-if="loadingMore" class="spinner-border spinner-border-sm me-2" role="status"></span>
          Load More Comments
        </button>
      </div>
    </div>

    <!-- Edit Comment Modal -->
    <div class="modal fade" id="editCommentModal" tabindex="-1" aria-hidden="true">
      <div class="modal-dialog">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">Edit Comment</h5>
            <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
          </div>
          <form @submit.prevent="saveEditComment">
            <div class="modal-body">
              <textarea
                v-model="editingCommentContent"
                class="form-control"
                rows="4"
                placeholder="Edit your comment..."
                :disabled="isUpdatingComment"
                required
              ></textarea>
            </div>
            <div class="modal-footer">
              <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">
                Cancel
              </button>
              <button 
                type="submit" 
                class="btn btn-primary"
                :disabled="isUpdatingComment || !editingCommentContent.trim()"
              >
                <span v-if="isUpdatingComment" class="spinner-border spinner-border-sm me-2" role="status"></span>
                Save Changes
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, defineProps, defineEmits, defineExpose, watch } from 'vue'
import { useAuthStore } from '@/stores/authStore'
import { useImageFallback, FALLBACK_TYPES } from '@/composables/useImageFallback'
import { commentsService } from '@/services/commentsService'
import { socialService } from '@/services/socialService'
import CommentCard from './CommentCard.vue'

// Props
const props = defineProps({
  itemType: {
    type: String,
    required: true, // 'review' or 'list'
    validator: (value) => ['review', 'list'].includes(value)
  },
  itemId: {
    type: [String, Number],
    required: true
  },
  autoLoad: {
    type: Boolean,
    default: true
  }
})

// Emits
const emit = defineEmits(['commentAdded', 'commentUpdated', 'commentDeleted', 'countChanged'])

// Composables
const authStore = useAuthStore()
const { createReactiveImageUrl } = useImageFallback()

// State
const comments = ref([])
const commentReplies = ref(new Map())
const showRepliesForComment = ref(new Set())
const likedComments = ref(new Set())
const processingLikeComments = ref(new Set())

const loading = ref(false)
const loadingMore = ref(false)
const error = ref(null)
const hasMoreComments = ref(false)
const currentPage = ref(1)
const pageSize = ref(10)

const newCommentContent = ref('')
const isSubmittingComment = ref(false)

const editingComment = ref(null)
const editingCommentContent = ref('')
const isUpdatingComment = ref(false)

// Computed
const canComment = computed(() => {
  return !!authStore.user
})

const currentUserImageUrl = createReactiveImageUrl(
  computed(() => authStore.user?.profileImageUrl),
  FALLBACK_TYPES.PROFILE
)

// Methods
const resetAndLoadComments = () => {
  comments.value = []
  commentReplies.value.clear()
  showRepliesForComment.value.clear()
  likedComments.value.clear()
  processingLikeComments.value.clear()
  currentPage.value = 1
  hasMoreComments.value = false
  error.value = null
  
  if (props.autoLoad) {
    loadComments()
  }
}

const loadComments = async () => {
  try {
    loading.value = true
    error.value = null

    let result
    if (props.itemType === 'review') {
      result = await commentsService.getReviewComments(props.itemId, currentPage.value, pageSize.value)
    } else if (props.itemType === 'list') {
      result = await commentsService.getListComments(props.itemId, currentPage.value, pageSize.value)
    }

    if (currentPage.value === 1) {
      comments.value = result.data || []
    } else {
      comments.value.push(...(result.data || []))
    }

    hasMoreComments.value = result.hasNextPage || false
    emit('countChanged', result.totalCount || comments.value.length)

  } catch (err) {
    console.error('Error loading comments:', err)
    error.value = err.message || 'Failed to load comments'
  } finally {
    loading.value = false
    loadingMore.value = false
  }
}

const loadMoreComments = () => {
  if (hasMoreComments.value && !loadingMore.value) {
    currentPage.value++
    loadingMore.value = true
    loadComments()
  }
}

const submitComment = async () => {
  if (!newCommentContent.value.trim() || !canComment.value) return

  try {
    isSubmittingComment.value = true

    let newComment
    if (props.itemType === 'review') {
      newComment = await commentsService.addReviewComment(props.itemId, {
        content: newCommentContent.value.trim()
      })
    } else if (props.itemType === 'list') {
      newComment = await commentsService.addListComment(props.itemId, {
        content: newCommentContent.value.trim()
      })
    }

    // Add to beginning of comments list
    comments.value.unshift(newComment)
    newCommentContent.value = ''
    
    emit('commentAdded', newComment)
    emit('countChanged', comments.value.length)

  } catch (err) {
    console.error('Error submitting comment:', err)
    // You might want to show a toast notification here
  } finally {
    isSubmittingComment.value = false
  }
}

const handleEditComment = (comment) => {
  editingComment.value = comment
  editingCommentContent.value = comment.content
  
  // Show modal
  const modal = new window.bootstrap.Modal(document.getElementById('editCommentModal'))
  modal.show()
}

const saveEditComment = async () => {
  if (!editingCommentContent.value.trim() || !editingComment.value) return

  try {
    isUpdatingComment.value = true

    const updatedComment = await commentsService.updateComment(editingComment.value.id, {
      content: editingCommentContent.value.trim()
    })

    // Update comment in list
    const commentIndex = comments.value.findIndex(c => c.id === editingComment.value.id)
    if (commentIndex !== -1) {
      comments.value[commentIndex] = updatedComment
    }

    // Update in replies if it's a reply
    for (const [, replies] of commentReplies.value) {
      const replyIndex = replies.findIndex(r => r.id === editingComment.value.id)
      if (replyIndex !== -1) {
        replies[replyIndex] = updatedComment
        break
      }
    }

    emit('commentUpdated', updatedComment)

    // Hide modal
    const modal = window.bootstrap.Modal.getInstance(document.getElementById('editCommentModal'))
    modal.hide()

  } catch (err) {
    console.error('Error updating comment:', err)
  } finally {
    isUpdatingComment.value = false
  }
}

const handleDeleteComment = async (comment) => {
  if (!confirm('Are you sure you want to delete this comment?')) return

  try {
    await commentsService.deleteComment(comment.id)

    // Remove from comments list
    const commentIndex = comments.value.findIndex(c => c.id === comment.id)
    if (commentIndex !== -1) {
      comments.value.splice(commentIndex, 1)
    }

    // Remove from replies if it's a reply
    for (const [, replies] of commentReplies.value) {
      const replyIndex = replies.findIndex(r => r.id === comment.id)
      if (replyIndex !== -1) {
        replies.splice(replyIndex, 1)
        break
      }
    }

    emit('commentDeleted', comment)
    emit('countChanged', comments.value.length)

  } catch (err) {
    console.error('Error deleting comment:', err)
  }
}

const handleToggleLike = async (comment) => {
  if (!canComment.value) return

  const commentId = comment.id
  const wasLiked = likedComments.value.has(commentId)

  // Prevent multiple simultaneous requests
  if (processingLikeComments.value.has(commentId)) return

  try {
    processingLikeComments.value.add(commentId)

    if (wasLiked) {
      await socialService.unlikeComment(commentId)
      likedComments.value.delete(commentId)
    } else {
      await socialService.likeComment(commentId)
      likedComments.value.add(commentId)
    }

    // Update like count in comment
    const targetComment = comments.value.find(c => c.id === commentId) || 
                         Array.from(commentReplies.value.values()).flat().find(r => r.id === commentId)
    
    if (targetComment) {
      targetComment.likeCount = (targetComment.likeCount || 0) + (wasLiked ? -1 : 1)
    }

  } catch (err) {
    console.error('Error toggling comment like:', err)
  } finally {
    processingLikeComments.value.delete(commentId)
  }
}

const handleReply = async (replyData) => {
  try {
    const newReply = await commentsService.addReply(replyData.parentCommentId, {
      content: replyData.content
    })

    // Add reply to the replies map
    if (!commentReplies.value.has(replyData.parentCommentId)) {
      commentReplies.value.set(replyData.parentCommentId, [])
    }
    commentReplies.value.get(replyData.parentCommentId).push(newReply)

    // Update reply count on parent comment
    const parentComment = comments.value.find(c => c.id === replyData.parentCommentId)
    if (parentComment) {
      parentComment.replyCount = (parentComment.replyCount || 0) + 1
    }

    // Show replies if not already shown
    showRepliesForComment.value.add(replyData.parentCommentId)

  } catch (err) {
    console.error('Error submitting reply:', err)
  }
}

const handleLoadReplies = async (comment) => {
  try {
    const replies = await commentsService.getCommentReplies(comment.id)
    commentReplies.value.set(comment.id, replies.data || [])
    showRepliesForComment.value.add(comment.id)
  } catch (err) {
    console.error('Error loading replies:', err)
  }
}

const handleHideReplies = (comment) => {
  showRepliesForComment.value.delete(comment.id)
}


// Watch for prop changes
watch(() => [props.itemType, props.itemId], () => {
  if (props.autoLoad) {
    resetAndLoadComments()
  }
}, { immediate: true })

// Expose methods for parent component
defineExpose({
  loadComments,
  resetAndLoadComments
})
</script>

<style scoped>
.comment-section {
  max-width: 800px;
}

.add-comment-form textarea {
  resize: vertical;
  min-height: 100px;
}

.comments-list {
  border-top: 1px solid #e9ecef;
}

@media (max-width: 768px) {
  .add-comment-form .d-flex {
    flex-direction: column;
  }
  
  .add-comment-form .flex-shrink-0 {
    align-self: flex-start;
    margin-bottom: 0.75rem;
  }
  
  .add-comment-form .ms-3 {
    margin-left: 0 !important;
  }
}
</style>