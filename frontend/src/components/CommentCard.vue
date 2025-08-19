<template>
  <div class="comment-card" :class="{ 'reply-comment': isReply }">
    <div class="d-flex align-items-start">
      <!-- User Avatar -->
      <router-link :to="`/profile/${comment.user.id}`" class="text-decoration-none flex-shrink-0">
        <img
          :src="userImageUrl"
          :alt="`${comment.user.displayName || comment.user.username}'s profile`"
          class="rounded-circle"
          :style="{ width: avatarSize, height: avatarSize, objectFit: 'cover' }"
          @error="handleImageError"
        >
      </router-link>

      <!-- Comment Content -->
      <div class="flex-grow-1 ms-3">
        <!-- Comment Header -->
        <div class="d-flex justify-content-between align-items-start mb-2">
          <div>
            <!-- User Name -->
            <router-link :to="`/profile/${comment.user.id}`" class="text-decoration-none">
              <span class="fw-semibold text-dark">
                {{ comment.user.displayName || comment.user.username }}
              </span>
            </router-link>
            
            <!-- Comment Date -->
            <small class="text-muted ms-2">{{ formattedDate }}</small>
          </div>

          <!-- Actions Menu (only for owner) -->
          <div v-if="canEdit" class="dropdown">
            <button class="btn btn-sm btn-link text-muted p-0" data-bs-toggle="dropdown">
              <i class="fas fa-ellipsis-h"></i>
            </button>
            <ul class="dropdown-menu dropdown-menu-end">
              <li>
                <button @click="$emit('edit', comment)" class="dropdown-item">
                  <i class="fas fa-edit me-2"></i>Edit
                </button>
              </li>
              <li><hr class="dropdown-divider"></li>
              <li>
                <button @click="$emit('delete', comment)" class="dropdown-item text-danger">
                  <i class="fas fa-trash me-2"></i>Delete
                </button>
              </li>
            </ul>
          </div>
        </div>

        <!-- Comment Text -->
        <div class="comment-content mb-2">
          <div v-if="isEditing" class="edit-form">
            <textarea
              v-model="editContent"
              class="form-control"
              rows="3"
              placeholder="Edit your comment..."
              :disabled="isSubmitting"
            ></textarea>
            <div class="mt-2">
              <button 
                @click="saveEdit"
                class="btn btn-sm btn-primary me-2"
                :disabled="isSubmitting || !editContent.trim()"
              >
                <span v-if="isSubmitting" class="spinner-border spinner-border-sm me-1" role="status"></span>
                Save
              </button>
              <button @click="cancelEdit" class="btn btn-sm btn-outline-secondary">
                Cancel
              </button>
            </div>
          </div>
          
          <p v-else class="mb-0" style="line-height: 1.5; white-space: pre-wrap;">{{ comment.content }}</p>
        </div>

        <!-- Comment Actions -->
        <div class="comment-actions d-flex align-items-center">
          <!-- Like Button -->
          <button 
            v-if="showLikeButton"
            @click="$emit('toggleLike', comment)" 
            class="btn btn-sm btn-link text-muted p-0 me-3"
            :class="{ 'text-primary': isLiked }"
            :disabled="isProcessingLike"
          >
            <span v-if="isProcessingLike" class="spinner-border spinner-border-sm me-1" role="status" style="width: 12px; height: 12px;"></span>
            <i class="fas fa-heart me-1"></i>
            {{ comment.likeCount || 0 }}
          </button>

          <!-- Reply Button -->
          <button 
            v-if="!isReply && showReplyButton"
            @click="toggleReplyForm" 
            class="btn btn-sm btn-link text-muted p-0 me-3"
          >
            <i class="fas fa-reply me-1"></i>
            Reply
          </button>

          <!-- Show Replies Button -->
          <button 
            v-if="!isReply && hasReplies && !showReplies"
            @click="$emit('loadReplies', comment)" 
            class="btn btn-sm btn-link text-muted p-0"
          >
            <i class="fas fa-comment me-1"></i>
            Show {{ comment.replyCount || 0 }} {{ comment.replyCount === 1 ? 'reply' : 'replies' }}
          </button>

          <!-- Hide Replies Button -->
          <button 
            v-if="!isReply && showReplies && hasReplies"
            @click="$emit('hideReplies', comment)" 
            class="btn btn-sm btn-link text-muted p-0"
          >
            <i class="fas fa-chevron-up me-1"></i>
            Hide replies
          </button>
        </div>

        <!-- Reply Form -->
        <div v-if="showReplyForm" class="reply-form mt-3">
          <div class="d-flex align-items-start">
            <img
              :src="currentUserImageUrl"
              alt="Your profile"
              class="rounded-circle flex-shrink-0"
              style="width: 32px; height: 32px; object-fit: cover;"
            >
            <div class="flex-grow-1 ms-2">
              <textarea
                v-model="replyContent"
                class="form-control form-control-sm"
                rows="2"
                placeholder="Write a reply..."
                :disabled="isSubmittingReply"
              ></textarea>
              <div class="mt-2">
                <button 
                  @click="submitReply"
                  class="btn btn-sm btn-primary me-2"
                  :disabled="isSubmittingReply || !replyContent.trim()"
                >
                  <span v-if="isSubmittingReply" class="spinner-border spinner-border-sm me-1" role="status"></span>
                  Reply
                </button>
                <button @click="cancelReply" class="btn btn-sm btn-outline-secondary">
                  Cancel
                </button>
              </div>
            </div>
          </div>
        </div>

        <!-- Replies -->
        <div v-if="showReplies && replies.length > 0" class="replies mt-3">
          <CommentCard
            v-for="reply in replies"
            :key="reply.id"
            :comment="reply"
            :is-reply="true"
            :is-liked="isReplyLiked(reply.id)"
            :is-processing-like="isProcessingReplyLike(reply.id)"
            :show-reply-button="false"
            @edit="$emit('edit', $event)"
            @delete="$emit('delete', $event)"
            @toggleLike="$emit('toggleLike', $event)"
          />
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, defineProps, defineEmits, defineExpose } from 'vue'
import { useAuthStore } from '@/stores/authStore'
import { useImageFallback, FALLBACK_TYPES } from '@/composables/useImageFallback'

// Props
const props = defineProps({
  comment: {
    type: Object,
    required: true
  },
  isReply: {
    type: Boolean,
    default: false
  },
  isLiked: {
    type: Boolean,
    default: false
  },
  isProcessingLike: {
    type: Boolean,
    default: false
  },
  showLikeButton: {
    type: Boolean,
    default: true
  },
  showReplyButton: {
    type: Boolean,
    default: true
  },
  showReplies: {
    type: Boolean,
    default: false
  },
  replies: {
    type: Array,
    default: () => []
  },
  likedReplies: {
    type: Set,
    default: () => new Set()
  },
  processingLikeReplies: {
    type: Set,
    default: () => new Set()
  }
})

// Emits
const emit = defineEmits(['edit', 'delete', 'toggleLike', 'reply', 'loadReplies', 'hideReplies'])

// Composables
const authStore = useAuthStore()
const { handleImageError: handleImgError, createReactiveImageUrl } = useImageFallback()

// State
const isEditing = ref(false)
const editContent = ref('')
const isSubmitting = ref(false)
const showReplyForm = ref(false)
const replyContent = ref('')
const isSubmittingReply = ref(false)

// Computed
const canEdit = computed(() => {
  return authStore.user && authStore.user.id === props.comment.user.id
})

const hasReplies = computed(() => {
  // Handle both camelCase and PascalCase for backwards compatibility
  const replyCount = props.comment.replyCount || props.comment.ReplyCount || 0
  return replyCount > 0 || props.replies.length > 0
})

const avatarSize = computed(() => {
  return props.isReply ? '32px' : '40px'
})

const userImageUrl = createReactiveImageUrl(
  computed(() => props.comment.user?.profileImageUrl),
  FALLBACK_TYPES.PROFILE
)

const currentUserImageUrl = createReactiveImageUrl(
  computed(() => authStore.user?.profileImageUrl),
  FALLBACK_TYPES.PROFILE
)

const formattedDate = computed(() => {
  if (!props.comment.createdAt) return ''

  try {
    const date = new Date(props.comment.createdAt)
    const now = new Date()
    const diffTime = Math.abs(now - date)
    const diffMinutes = Math.ceil(diffTime / (1000 * 60))
    const diffHours = Math.ceil(diffTime / (1000 * 60 * 60))
    const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24))

    if (diffMinutes < 60) return `${diffMinutes}m ago`
    if (diffHours < 24) return `${diffHours}h ago`
    if (diffDays < 7) return `${diffDays}d ago`
    
    return date.toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: date.getFullYear() !== now.getFullYear() ? 'numeric' : undefined
    })
  } catch (error) {
    console.warn('Error formatting date:', error)
    return ''
  }
})

// Methods
const handleImageError = (e) => {
  handleImgError(e, 'profile')
}

const startEdit = () => {
  editContent.value = props.comment.content
  isEditing.value = true
}

const cancelEdit = () => {
  isEditing.value = false
  editContent.value = ''
}

const saveEdit = async () => {
  if (!editContent.value.trim()) return

  try {
    isSubmitting.value = true
    emit('edit', {
      ...props.comment,
      content: editContent.value.trim()
    })
    isEditing.value = false
  } catch (error) {
    console.error('Error saving edit:', error)
  } finally {
    isSubmitting.value = false
  }
}

const toggleReplyForm = () => {
  showReplyForm.value = !showReplyForm.value
  if (!showReplyForm.value) {
    replyContent.value = ''
  }
}

const cancelReply = () => {
  showReplyForm.value = false
  replyContent.value = ''
}

const submitReply = async () => {
  if (!replyContent.value.trim()) return

  try {
    isSubmittingReply.value = true
    emit('reply', {
      parentCommentId: props.comment.id,
      content: replyContent.value.trim()
    })
    replyContent.value = ''
    showReplyForm.value = false
  } catch (error) {
    console.error('Error submitting reply:', error)
  } finally {
    isSubmittingReply.value = false
  }
}

const isReplyLiked = (replyId) => {
  return props.likedReplies.has(replyId)
}

const isProcessingReplyLike = (replyId) => {
  return props.processingLikeReplies.has(replyId)
}

// Handle edit event from parent
defineExpose({
  startEdit
})
</script>

<style scoped>
.comment-card {
  padding: 1rem 0;
}

.comment-card:not(:last-child) {
  border-bottom: 1px solid #f0f0f0;
}

.reply-comment {
  padding-left: 2rem;
  border-left: 2px solid #e9ecef;
  margin-top: 0.75rem;
}

.comment-content {
  word-wrap: break-word;
}

.comment-actions .btn-link {
  font-size: 0.875rem;
  text-decoration: none !important;
}

.comment-actions .btn-link:hover {
  text-decoration: none !important;
}

.replies {
  border-left: 2px solid #f8f9fa;
  padding-left: 1rem;
}

.edit-form textarea {
  resize: vertical;
  min-height: 80px;
}

.reply-form textarea {
  resize: vertical;
  min-height: 60px;
}

@media (max-width: 768px) {
  .reply-comment {
    padding-left: 1rem;
  }
  
  .replies {
    padding-left: 0.5rem;
  }
}
</style>