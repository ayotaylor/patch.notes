<template>
  <div class="bg-theme-bg-secondary dark:bg-theme-bg-secondary-dark rounded-xl shadow-sm border border-theme-border dark:border-theme-border-dark transition-colors duration-200">
    <div class="p-6">
      <div class="flex flex-col lg:flex-row gap-6">
        <!-- Column 1: Profile Image (1/8 width on lg) -->
        <div class="flex-shrink-0 flex flex-col items-center lg:items-start">
          <div class="relative inline-block">
            <img
              :src="profileImageUrl || defaultAvatar"
              :alt="`${displayName}'s profile`"
              class="w-32 h-32 rounded-full border-4 border-theme-border dark:border-theme-border-dark shadow-lg object-cover"
            >
          </div>
        </div>

        <!-- Remaining 7/8 width: Split between User Info (1/2) and Stats (1/2) -->
        <div class="flex-grow flex flex-col lg:flex-row gap-6">
          <!-- Left Side: Username, Bio, and Button (takes 1/2 of remaining space on lg) -->
          <div class="flex-grow lg:w-1/2">
            <!-- Username and Follow Button Row -->
            <div class="flex flex-col sm:flex-row justify-between items-start gap-3 mb-4">
              <div class="flex-grow">
                <h1 class="font-newsreader text-3xl font-bold text-theme-text-primary dark:text-theme-text-primary-dark mb-1">
                  {{ displayName }}
                </h1>

                <!-- Full Name (if different from display name) -->
                <p v-if="fullName && fullName !== displayName" class="font-tinos text-base text-theme-text-secondary dark:text-theme-text-secondary-dark mb-2">
                  {{ fullName }}
                </p>

                <!-- Email -->
                <p class="font-tinos text-sm text-theme-text-secondary dark:text-theme-text-secondary-dark flex items-center">
                  <svg class="w-4 h-4 mr-1" fill="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                    <path d="M20 4H4c-1.1 0-1.99.9-1.99 2L2 18c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V6c0-1.1-.9-2-2-2zm0 4l-8 5-8-5V6l8 5 8-5v2z"/>
                  </svg>
                  {{ email }}
                </p>
              </div>

              <!-- Follow/Following Button (for other users) -->
              <div v-if="!isOwnProfile" class="flex-shrink-0">
                <button
                  @click="handleFollowToggle"
                  :disabled="followingInProgress"
                  class="px-4 py-2 rounded-lg font-tinos text-base font-medium transition-all duration-200 flex items-center whitespace-nowrap"
                  :class="isFollowing
                    ? 'bg-green-600 hover:bg-green-700 text-white'
                    : 'bg-theme-btn-primary dark:bg-theme-btn-primary-dark text-white hover:opacity-90'"
                >
                  <span v-if="followingInProgress" class="inline-block w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin mr-2"></span>
                  <svg v-else class="w-5 h-5 mr-2" fill="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                    <path v-if="isFollowing" d="M9 16.2L4.8 12l-1.4 1.4L9 19 21 7l-1.4-1.4L9 16.2z"/>
                    <path v-else d="M19 13h-6v6h-2v-6H5v-2h6V5h2v6h6v2z"/>
                  </svg>
                  {{ isFollowing ? 'Following' : 'Follow' }}
                </button>
              </div>
            </div>

            <!-- Bio -->
            <div class="mb-4">
              <h3 class="font-tinos text-sm font-semibold text-theme-text-secondary dark:text-theme-text-secondary-dark mb-2 uppercase tracking-wide">Bio</h3>
              <p v-if="bio" class="font-tinos text-base text-theme-text-primary dark:text-theme-text-primary-dark leading-relaxed">
                {{ bio }}
              </p>
              <p v-else class="font-tinos text-base text-theme-text-secondary dark:text-theme-text-secondary-dark italic">
                {{ isOwnProfile ? 'Add a bio to tell others about yourself' : 'No bio available' }}
              </p>
            </div>

            <!-- Edit Profile Button (for current user) - shown below bio -->
            <div v-if="isOwnProfile">
              <router-link
                to="/profile/edit"
                class="inline-flex items-center px-4 py-2 bg-theme-bg-primary dark:bg-theme-bg-primary-dark text-theme-text-primary dark:text-theme-text-primary-dark border border-theme-border dark:border-theme-border-dark rounded-lg font-tinos text-sm hover:bg-theme-border dark:hover:bg-theme-border-dark transition-all duration-200"
              >
                <svg class="w-4 h-4 mr-2" fill="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                  <path d="M3 17.25V21h3.75L17.81 9.94l-3.75-3.75L3 17.25zM20.71 7.04c.39-.39.39-1.02 0-1.41l-2.34-2.34c-.39-.39-1.02-.39-1.41 0l-1.83 1.83 3.75 3.75 1.83-1.83z"/>
                </svg>
                Edit Profile
              </router-link>
            </div>
          </div>

          <!-- Right Side: User Stats (takes 1/2 of remaining space on lg, full width below on smaller screens) -->
          <div class="lg:w-1/2">
            <h3 class="font-tinos text-sm font-semibold text-theme-text-secondary dark:text-theme-text-secondary-dark mb-3 uppercase tracking-wide">Stats</h3>
            <div class="flex gap-6 flex-wrap">
              <div class="text-center">
                <div class="font-newsreader text-2xl font-bold text-blue-600 dark:text-blue-400">{{ gamesCount }}</div>
                <div class="font-tinos text-sm text-theme-text-secondary dark:text-theme-text-secondary-dark">Games</div>
              </div>
              <div class="text-center">
                <div class="font-newsreader text-2xl font-bold text-green-600 dark:text-green-400">{{ achievementsCount }}</div>
                <div class="font-tinos text-sm text-theme-text-secondary dark:text-theme-text-secondary-dark">Achievements</div>
              </div>
              <div class="text-center">
                <div class="font-newsreader text-2xl font-bold text-yellow-600 dark:text-yellow-400">{{ totalPlayTime }}</div>
                <div class="font-tinos text-sm text-theme-text-secondary dark:text-theme-text-secondary-dark">Play Time</div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({
  profileImageUrl: {
    type: String,
    default: ''
  },
  displayName: {
    type: String,
    required: true
  },
  firstName: {
    type: String,
    default: ''
  },
  lastName: {
    type: String,
    default: ''
  },
  email: {
    type: String,
    required: true
  },
  bio: {
    type: String,
    default: ''
  },
  gamesCount: {
    type: Number,
    default: 0
  },
  achievementsCount: {
    type: Number,
    default: 0
  },
  totalPlayTime: {
    type: String,
    default: '0h'
  },
  isOwnProfile: {
    type: Boolean,
    default: false
  },
  isFollowing: {
    type: Boolean,
    default: false
  },
  followingInProgress: {
    type: Boolean,
    default: false
  }
})

const emit = defineEmits(['toggle-follow'])

const fullName = computed(() => {
  if (!props.firstName && !props.lastName) return ''
  return `${props.firstName || ''} ${props.lastName || ''}`.trim()
})

const defaultAvatar = computed(() => {
  const name = props.displayName || fullName.value || 'User'
  return `https://ui-avatars.com/api/?name=${encodeURIComponent(name)}&size=128&background=6c757d&color=ffffff`
})

const handleFollowToggle = () => {
  emit('toggle-follow')
}
</script>
