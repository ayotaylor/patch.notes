<template>
  <div class="bg-cod-gray text-white rounded-lg overflow-hidden">
    <!-- GAME/REVIEW CONTEXT -->
    <template v-if="context === 'game' || context === 'review'">
      <!-- Like Action (Row 1) -->
      <div
        class="p-4 hover:bg-opacity-90 cursor-pointer transition-colors border-b border-gray-600"
        @click="$emit('like')"
      >
        <div class="flex items-center gap-3">
          <svg
            class="w-5 h-5"
            :fill="isLiked ? 'currentColor' : 'none'"
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4.318 6.318a4.5 4.5 0 000 6.364L12 20.364l7.682-7.682a4.5 4.5 0 00-6.364-6.364L12 7.636l-1.318-1.318a4.5 4.5 0 00-6.364 0z" />
          </svg>
          <span class="font-tinos text-base">{{ isLiked ? 'Unlike' : 'Like' }}</span>
          <span class="font-tinos text-sm text-gray-300 ml-auto">{{ likeCount }}</span>
        </div>
      </div>

      <!-- Wishlist Action (Row 1 for games) -->
      <div
        v-if="context === 'game'"
        class="p-4 hover:bg-opacity-90 cursor-pointer transition-colors border-b border-gray-600"
        @click="$emit('wishlist')"
      >
        <div class="flex items-center gap-3">
          <svg
            class="w-5 h-5"
            :fill="isInWishlist ? 'currentColor' : 'none'"
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 5a2 2 0 012-2h10a2 2 0 012 2v16l-7-3.5L5 21V5z" />
          </svg>
          <span class="font-tinos text-base">{{ isInWishlist ? 'Remove from Wishlist' : 'Wishlist' }}</span>
        </div>
      </div>

      <!-- Rate Action (Row 2) -->
      <div
        class="p-4 hover:bg-opacity-90 cursor-pointer transition-colors border-b border-gray-600"
        @click="$emit('rate')"
      >
        <div class="flex items-center gap-3">
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11.049 2.927c.3-.921 1.603-.921 1.902 0l1.519 4.674a1 1 0 00.95.69h4.915c.969 0 1.371 1.24.588 1.81l-3.976 2.888a1 1 0 00-.363 1.118l1.518 4.674c.3.922-.755 1.688-1.538 1.118l-3.976-2.888a1 1 0 00-1.176 0l-3.976 2.888c-.783.57-1.838-.197-1.538-1.118l1.518-4.674a1 1 0 00-.363-1.118l-3.976-2.888c-.784-.57-.38-1.81.588-1.81h4.914a1 1 0 00.951-.69l1.519-4.674z" />
          </svg>
          <span class="font-tinos text-base">Rate</span>
        </div>
      </div>

      <!-- Edit/Delete for Review Owner (Row 3) -->
      <div
        v-if="context === 'review' && (canEdit || canDelete)"
        class="p-4 hover:bg-opacity-90 cursor-pointer transition-colors border-b border-gray-600"
        @click="$emit('edit')"
      >
        <div class="flex items-center gap-3">
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
          </svg>
          <span class="font-tinos text-base">Edit Review</span>
        </div>
      </div>

      <div
        v-if="context === 'review' && (canEdit || canDelete)"
        class="p-4 hover:bg-opacity-90 cursor-pointer transition-colors border-b border-gray-600"
        @click="$emit('delete')"
      >
        <div class="flex items-center gap-3">
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
          </svg>
          <span class="font-tinos text-base">Delete Review</span>
        </div>
      </div>

      <!-- Review Action (Row 3 for games) -->
      <div
        v-if="context === 'game'"
        class="p-4 hover:bg-opacity-90 cursor-pointer transition-colors border-b border-gray-600"
        @click="$emit('review')"
      >
        <div class="flex items-center gap-3">
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
          </svg>
          <span class="font-tinos text-base">Review</span>
        </div>
      </div>

      <!-- Add to List Action (Row 4) -->
      <div
        class="p-4 hover:bg-opacity-90 cursor-pointer transition-colors"
        @click="$emit('add-to-list')"
      >
        <div class="flex items-center gap-3">
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
          </svg>
          <span class="font-tinos text-base">Add to List</span>
        </div>
      </div>
    </template>

    <!-- LIST CONTEXT -->
    <template v-else-if="context === 'list'">
      <!-- Edit Action -->
      <div
        v-if="canEdit"
        class="p-4 hover:bg-opacity-90 cursor-pointer transition-colors border-b border-gray-600"
        @click="$emit('edit')"
      >
        <div class="flex items-center gap-3">
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
          </svg>
          <span class="font-tinos text-base">Edit</span>
        </div>
      </div>

      <!-- Delete Action -->
      <div
        v-if="canDelete"
        class="p-4 hover:bg-opacity-90 cursor-pointer transition-colors border-b border-gray-600"
        @click="$emit('delete')"
      >
        <div class="flex items-center gap-3">
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
          </svg>
          <span class="font-tinos text-base">Delete</span>
        </div>
      </div>

      <!-- Like Action (only if not owner) -->
      <div
        v-if="!canEdit"
        class="p-4 hover:bg-opacity-90 cursor-pointer transition-colors border-b border-gray-600"
        @click="$emit('like')"
      >
        <div class="flex items-center gap-3">
          <svg
            class="w-5 h-5"
            :fill="isLiked ? 'currentColor' : 'none'"
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4.318 6.318a4.5 4.5 0 000 6.364L12 20.364l7.682-7.682a4.5 4.5 0 00-6.364-6.364L12 7.636l-1.318-1.318a4.5 4.5 0 00-6.364 0z" />
          </svg>
          <span class="font-tinos text-base">{{ isLiked ? 'Unlike' : 'Like' }}</span>
          <span class="font-tinos text-sm text-gray-300 ml-auto">{{ likeCount }}</span>
        </div>
      </div>

      <!-- Privacy Toggle -->
      <div
        v-if="canEdit"
        class="p-4 hover:bg-opacity-90 cursor-pointer transition-colors"
        @click="$emit('toggle-privacy')"
      >
        <div class="flex items-center gap-3">
          <svg v-if="isPublic" class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3.055 11H5a2 2 0 012 2v1a2 2 0 002 2 2 2 0 012 2v2.945M8 3.935V5.5A2.5 2.5 0 0010.5 8h.5a2 2 0 012 2 2 2 0 104 0 2 2 0 012-2h1.064M15 20.488V18a2 2 0 012-2h3.064M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
          <svg v-else class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
          </svg>
          <span class="font-tinos text-base">{{ isPublic ? 'Public' : 'Private' }}</span>
        </div>
      </div>
    </template>
  </div>
</template>

<script setup>
/*const props = */defineProps({
  // Context: 'list', 'game', or 'review'
  context: {
    type: String,
    required: true,
    validator: (value) => ['list', 'game', 'review'].includes(value)
  },
  // Common props
  canEdit: {
    type: Boolean,
    default: false
  },
  canDelete: {
    type: Boolean,
    default: false
  },
  isLiked: {
    type: Boolean,
    default: false
  },
  likeCount: {
    type: Number,
    default: 0
  },
  // List-specific props
  isPublic: {
    type: Boolean,
    default: true
  },
  // Game-specific props
  isInWishlist: {
    type: Boolean,
    default: false
  }
})

defineEmits(['edit', 'delete', 'like', 'toggle-privacy', 'wishlist', 'rate', 'review', 'add-to-list'])
</script>