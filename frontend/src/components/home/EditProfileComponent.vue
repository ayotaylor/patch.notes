<template>
  <div class="min-h-screen bg-theme-bg-primary dark:bg-theme-bg-primary-dark transition-colors duration-200 py-8 px-4">
    <div class="max-w-6xl mx-auto">
      <!-- Header -->
      <div class="mb-6">
        <router-link
          to="/profile"
          class="inline-flex items-center text-theme-text-secondary dark:text-theme-text-secondary-dark hover:text-theme-text-primary dark:hover:text-theme-text-primary-dark font-tinos text-sm mb-4"
        >
          <svg class="w-4 h-4 mr-2" fill="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
            <path d="M15.41 7.41L14 6l-6 6 6 6 1.41-1.41L10.83 12z"/>
          </svg>
          Back to Profile
        </router-link>
        <h1 class="font-newsreader text-3xl font-bold text-theme-text-primary dark:text-theme-text-primary-dark">Edit Profile</h1>
      </div>

      <form @submit.prevent="handleSave">
        <div class="grid grid-cols-1 lg:grid-cols-8 gap-6">
          <!-- Column 1: User Details (3/8ths width) -->
          <div class="lg:col-span-3">
            <div class="bg-theme-bg-secondary dark:bg-theme-bg-secondary-dark rounded-xl shadow-sm border border-theme-border dark:border-theme-border-dark transition-colors duration-200 p-6">
              <h2 class="font-newsreader text-xl font-bold text-theme-text-primary dark:text-theme-text-primary-dark mb-6">Personal Information</h2>

              <!-- Profile Image Upload -->
              <div class="mb-6">
                <label class="block font-tinos text-sm font-semibold text-theme-text-secondary dark:text-theme-text-secondary-dark mb-2">Profile Image</label>
                <div class="flex items-center gap-4">
                  <div class="relative">
                    <img
                      :src="formData.profileImageUrl || defaultAvatar"
                      alt="Profile"
                      class="w-24 h-24 rounded-full border-4 border-theme-border dark:border-theme-border-dark object-cover"
                    >
                    <button
                      type="button"
                      @click="triggerImageUpload"
                      class="absolute bottom-0 right-0 w-8 h-8 bg-theme-btn-primary dark:bg-theme-btn-primary-dark text-white rounded-full flex items-center justify-center hover:opacity-90 transition-opacity shadow-lg"
                      title="Change profile picture"
                    >
                      <svg class="w-4 h-4" fill="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                        <path d="M12 12.75c1.63 0 3.07.39 4.24.9 1.08.48 1.76 1.56 1.76 2.73V18H6v-1.61c0-1.18.68-2.26 1.76-2.73 1.17-.52 2.61-.91 4.24-.91zM4 13h3v-2.5H4V8l-3 4 3 4v-3zm16-3h-3v2.5h3V15l3-4-3-4v3zm-8-7c-1.66 0-3 1.34-3 3s1.34 3 3 3 3-1.34 3-3-1.34-3-3-3z"/>
                      </svg>
                    </button>
                    <input
                      ref="imageInput"
                      type="file"
                      accept="image/*"
                      @change="handleImageUpload"
                      class="hidden"
                    >
                  </div>
                  <div class="flex-1">
                    <p class="font-tinos text-xs text-theme-text-secondary dark:text-theme-text-secondary-dark">
                      Click the icon to upload a new profile picture. Maximum size: 5MB
                    </p>
                  </div>
                </div>
              </div>

              <!-- First Name -->
              <div class="mb-4">
                <label for="firstName" class="block font-tinos text-sm font-semibold text-theme-text-secondary dark:text-theme-text-secondary-dark mb-2">First Name</label>
                <input
                  id="firstName"
                  v-model="formData.firstName"
                  type="text"
                  maxlength="50"
                  class="w-full px-4 py-2 font-tinos text-base bg-theme-bg-primary dark:bg-theme-bg-primary-dark text-theme-text-primary dark:text-theme-text-primary-dark border border-theme-border dark:border-theme-border-dark rounded-lg focus:outline-none focus:ring-2 focus:ring-theme-btn-primary dark:focus:ring-theme-btn-primary-dark"
                  placeholder="Enter your first name"
                >
              </div>

              <!-- Last Name -->
              <div class="mb-4">
                <label for="lastName" class="block font-tinos text-sm font-semibold text-theme-text-secondary dark:text-theme-text-secondary-dark mb-2">Last Name</label>
                <input
                  id="lastName"
                  v-model="formData.lastName"
                  type="text"
                  maxlength="50"
                  class="w-full px-4 py-2 font-tinos text-base bg-theme-bg-primary dark:bg-theme-bg-primary-dark text-theme-text-primary dark:text-theme-text-primary-dark border border-theme-border dark:border-theme-border-dark rounded-lg focus:outline-none focus:ring-2 focus:ring-theme-btn-primary dark:focus:ring-theme-btn-primary-dark"
                  placeholder="Enter your last name"
                >
              </div>

              <!-- Display Name -->
              <div class="mb-4">
                <label for="displayName" class="block font-tinos text-sm font-semibold text-theme-text-secondary dark:text-theme-text-secondary-dark mb-2">Display Name</label>
                <input
                  id="displayName"
                  v-model="formData.displayName"
                  type="text"
                  maxlength="100"
                  class="w-full px-4 py-2 font-tinos text-base bg-theme-bg-primary dark:bg-theme-bg-primary-dark text-theme-text-primary dark:text-theme-text-primary-dark border border-theme-border dark:border-theme-border-dark rounded-lg focus:outline-none focus:ring-2 focus:ring-theme-btn-primary dark:focus:ring-theme-btn-primary-dark"
                  placeholder="Enter your display name"
                >
              </div>

              <!-- Bio -->
              <div class="mb-4">
                <label for="bio" class="block font-tinos text-sm font-semibold text-theme-text-secondary dark:text-theme-text-secondary-dark mb-2">Bio</label>
                <textarea
                  id="bio"
                  v-model="formData.bio"
                  rows="4"
                  maxlength="500"
                  class="w-full px-4 py-2 font-tinos text-base bg-theme-bg-primary dark:bg-theme-bg-primary-dark text-theme-text-primary dark:text-theme-text-primary-dark border border-theme-border dark:border-theme-border-dark rounded-lg focus:outline-none focus:ring-2 focus:ring-theme-btn-primary dark:focus:ring-theme-btn-primary-dark resize-none"
                  placeholder="Tell us about yourself..."
                ></textarea>
                <div class="text-right mt-1">
                  <span class="font-tinos text-xs text-theme-text-secondary dark:text-theme-text-secondary-dark">
                    {{ (formData.bio || '').length }}/500
                  </span>
                </div>
              </div>
            </div>
          </div>

          <!-- Column 2: Favorite Games (5/8ths width) -->
          <div class="lg:col-span-5">
            <div class="bg-theme-bg-secondary dark:bg-theme-bg-secondary-dark rounded-xl shadow-sm border border-theme-border dark:border-theme-border-dark transition-colors duration-200 p-6">
              <h2 class="font-newsreader text-xl font-bold text-theme-text-primary dark:text-theme-text-primary-dark mb-2">Favorite Games</h2>
              <p class="font-tinos text-sm text-theme-text-secondary dark:text-theme-text-secondary-dark mb-6">Select up to 5 of your favorite games</p>

              <!-- Game Slots (5 outlined cards with plus icons) -->
              <div v-if="selectedGames.length < 5" class="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-5 gap-4 mb-6">
                <button
                  v-for="index in (5 - selectedGames.length)"
                  :key="index"
                  type="button"
                  @click="openGameSearchModal"
                  class="aspect-[3/4] border-2 border-dashed border-theme-border dark:border-theme-border-dark rounded-lg flex items-center justify-center bg-theme-bg-primary dark:bg-theme-bg-primary-dark hover:border-theme-btn-primary dark:hover:border-theme-btn-primary-dark hover:bg-opacity-80 transition-all duration-200 cursor-pointer"
                >
                  <div class="text-center p-2">
                    <svg class="w-8 h-8 text-theme-btn-primary dark:text-theme-btn-primary-dark mx-auto mb-1" fill="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                      <path d="M19 13h-6v6h-2v-6H5v-2h6V5h2v6h6v2z"/>
                    </svg>
                    <p class="font-tinos text-xs text-theme-text-secondary dark:text-theme-text-secondary-dark">Add Game</p>
                  </div>
                </button>
              </div>

              <!-- Selected Games (Draggable) -->
              <div v-if="selectedGames.length > 0">
                <h3 class="font-tinos text-sm font-semibold text-theme-text-secondary dark:text-theme-text-secondary-dark mb-3">
                  Your Favorite Games ({{ selectedGames.length }}/5)
                  <span v-if="selectedGames.length > 1" class="font-normal text-xs ml-2">(Drag to reorder)</span>
                </h3>
                <div class="space-y-2">
                  <div
                    v-for="(game, index) in selectedGames"
                    :key="game.id"
                    :draggable="selectedGames.length > 1"
                    @dragstart="handleDragStart(index)"
                    @dragend="handleDragEnd"
                    @dragover.prevent="handleDragOver(index)"
                    @drop="handleDrop(index)"
                    class="flex items-center justify-between p-3 bg-theme-bg-primary dark:bg-theme-bg-primary-dark border border-theme-border dark:border-theme-border-dark rounded-lg transition-all duration-200"
                    :class="{
                      'cursor-move hover:border-theme-btn-primary dark:hover:border-theme-btn-primary-dark': selectedGames.length > 1,
                      'opacity-50': draggedIndex === index,
                      'border-theme-btn-primary dark:border-theme-btn-primary-dark': dragOverIndex === index && draggedIndex !== index
                    }"
                  >
                    <div class="flex items-center flex-grow">
                      <!-- Drag Handle -->
                      <div v-if="selectedGames.length > 1" class="mr-3 text-theme-text-secondary dark:text-theme-text-secondary-dark">
                        <svg class="w-5 h-5" fill="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                          <path d="M9 3h2v2H9V3zm0 4h2v2H9V7zm0 4h2v2H9v-2zm0 4h2v2H9v-2zm0 4h2v2H9v-2zm4-16h2v2h-2V3zm0 4h2v2h-2V7zm0 4h2v2h-2v-2zm0 4h2v2h-2v-2zm0 4h2v2h-2v-2z"/>
                        </svg>
                      </div>

                      <!-- Rank Badge -->
                      <span class="w-6 h-6 bg-theme-btn-primary dark:bg-theme-btn-primary-dark text-white rounded-full flex items-center justify-center font-tinos text-xs font-bold mr-3">
                        {{ index + 1 }}
                      </span>

                      <!-- Game Image -->
                      <img
                        :src="getGameImageUrl(game)"
                        :alt="game.name"
                        class="w-10 h-10 rounded-lg object-cover mr-3"
                      >

                      <!-- Game Info -->
                      <div class="flex-1">
                        <h6 class="font-tinos text-base font-medium text-theme-text-primary dark:text-theme-text-primary-dark">{{ game.name }}</h6>
                        <p class="font-tinos text-xs text-theme-text-secondary dark:text-theme-text-secondary-dark">{{ game.primaryGenre || 'Unknown Genre' }}</p>
                      </div>
                    </div>

                    <!-- Remove Button -->
                    <button
                      type="button"
                      @click="removeGame(index)"
                      class="p-2 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 text-red-600 dark:text-red-400 rounded hover:bg-red-100 dark:hover:bg-red-900/30 transition-colors duration-200"
                      title="Remove"
                    >
                      <svg class="w-4 h-4" fill="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                        <path d="M6 19c0 1.1.9 2 2 2h8c1.1 0 2-.9 2-2V7H6v12zM19 4h-3.5l-1-1h-5l-1 1H5v2h14V4z"/>
                      </svg>
                    </button>
                  </div>
                </div>
              </div>

              <!-- Empty State -->
              <div v-else class="text-center py-12 bg-theme-bg-primary dark:bg-theme-bg-primary-dark border border-theme-border dark:border-theme-border-dark rounded-lg">
                <svg class="w-12 h-12 text-theme-text-secondary dark:text-theme-text-secondary-dark mx-auto mb-2" fill="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                  <path d="M12 21.35l-1.45-1.32C5.4 15.36 2 12.28 2 8.5 2 5.42 4.42 3 7.5 3c1.74 0 3.41.81 4.5 2.09C13.09 3.81 14.76 3 16.5 3 19.58 3 22 5.42 22 8.5c0 3.78-3.4 6.86-8.55 11.54L12 21.35z"/>
                </svg>
                <p class="font-tinos text-sm text-theme-text-secondary dark:text-theme-text-secondary-dark">No games selected. Click the slots above to add your favorite games.</p>
              </div>
            </div>
          </div>
        </div>

        <!-- Save Button -->
        <div class="mt-6">
          <button
            type="submit"
            :disabled="isSaving"
            class="px-8 py-3 bg-green-600 hover:bg-green-700 text-white rounded-lg font-tinos text-base font-medium transition-all duration-200 disabled:opacity-50 disabled:cursor-not-allowed flex items-center"
          >
            <span v-if="isSaving" class="inline-block w-5 h-5 border-2 border-white border-t-transparent rounded-full animate-spin mr-2"></span>
            <svg v-else class="w-5 h-5 mr-2" fill="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
              <path d="M17 3H5c-1.11 0-2 .9-2 2v14c0 1.1.89 2 2 2h14c1.1 0 2-.9 2-2V7l-4-4zm-5 16c-1.66 0-3-1.34-3-3s1.34-3 3-3 3 1.34 3 3-1.34 3-3 3zm3-10H5V5h10v4z"/>
            </svg>
            {{ isSaving ? 'Saving...' : 'Save Changes' }}
          </button>
        </div>
      </form>
    </div>

    <!-- Game Search Modal -->
    <Teleport to="body">
      <div
        v-if="showGameSearchModal"
        class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4"
        @click.self="closeGameSearchModal"
      >
        <div class="bg-theme-bg-secondary dark:bg-theme-bg-secondary-dark rounded-xl shadow-xl border border-theme-border dark:border-theme-border-dark max-w-2xl w-full max-h-[80vh] overflow-hidden flex flex-col">
          <!-- Modal Header -->
          <div class="p-6 border-b border-theme-border dark:border-theme-border-dark flex justify-between items-center">
            <h3 class="font-newsreader text-2xl font-bold text-theme-text-primary dark:text-theme-text-primary-dark">Pick Your Favorite Game</h3>
            <button
              type="button"
              @click="closeGameSearchModal"
              class="p-2 hover:bg-theme-bg-primary dark:hover:bg-theme-bg-primary-dark rounded-lg transition-colors duration-200"
            >
              <svg class="w-6 h-6 text-theme-text-secondary dark:text-theme-text-secondary-dark" fill="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                <path d="M19 6.41L17.59 5 12 10.59 6.41 5 5 6.41 10.59 12 5 17.59 6.41 19 12 13.41 17.59 19 19 17.59 13.41 12z"/>
              </svg>
            </button>
          </div>

          <!-- Modal Body -->
          <div class="p-6 overflow-y-auto flex-1">
            <GamePicker
              @select-game="handleGameSelectFromModal"
              :excluded-game-ids="selectedGameIds"
            />
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted, onBeforeUnmount } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/authStore'
import { useProfileStore } from '@/stores/profileStore'
import { useGamesStore } from '@/stores/gamesStore'
import { useToast } from 'vue-toastification'
import GamePicker from './GamePicker.vue'

const router = useRouter()
const authStore = useAuthStore()
const profileStore = useProfileStore()
const gamesStore = useGamesStore()
const toast = useToast()

// State
const isSaving = ref(false)
const hasUnsavedChanges = ref(false)
const imageInput = ref(null)
const selectedGames = ref([])
const draggedIndex = ref(null)
const dragOverIndex = ref(null)
const showGameSearchModal = ref(false)

const formData = reactive({
  firstName: '',
  lastName: '',
  displayName: '',
  bio: '',
  profileImageUrl: ''
})

// Computed
const defaultAvatar = computed(() => {
  const name = formData.displayName || 'User'
  return `https://ui-avatars.com/api/?name=${encodeURIComponent(name)}&size=96&background=6c757d&color=ffffff`
})

const selectedGameIds = computed(() => {
  return selectedGames.value.map(game => game.id)
})

// Methods
const loadProfile = async () => {
  try {
    const profile = await profileStore.fetchProfile()
    Object.assign(formData, {
      firstName: profile.firstName || '',
      lastName: profile.lastName || '',
      displayName: profile.displayName || '',
      bio: profile.bio || '',
      profileImageUrl: profile.profileImageUrl || ''
    })

    // Load favorite games
    const favorites = await gamesStore.getUserFavorites(authStore.user?.id)
    selectedGames.value = favorites ? favorites.slice(0, 5) : []
  } catch (error) {
    console.error('Error loading profile:', error)
    toast.error('Failed to load profile data')
  }
}

const triggerImageUpload = () => {
  imageInput.value?.click()
}

const handleImageUpload = async (event) => {
  const file = event.target.files[0]
  if (!file) return

  // Validate file
  if (!file.type.startsWith('image/')) {
    toast.error('Please select a valid image file')
    return
  }

  if (file.size > 5 * 1024 * 1024) {
    toast.error('Image size must be less than 5MB')
    return
  }

  try {
    const reader = new FileReader()
    reader.onload = (e) => {
      formData.profileImageUrl = e.target.result
      hasUnsavedChanges.value = true
    }
    reader.readAsDataURL(file)
  } catch (err) {
    toast.error('Failed to upload image')
    console.error('Error uploading image:', err)
  }
}

const openGameSearchModal = () => {
  showGameSearchModal.value = true
}

const closeGameSearchModal = () => {
  showGameSearchModal.value = false
}

const handleGameSelectFromModal = (game) => {
  if (selectedGames.value.length >= 5) {
    toast.warning('You can only select up to 5 favorite games')
    closeGameSearchModal()
    return
  }

  if (selectedGames.value.some(g => g.id === game.id)) {
    toast.info('This game is already in your favorites')
    closeGameSearchModal()
    return
  }

  selectedGames.value.push(game)
  hasUnsavedChanges.value = true
  toast.success(`${game.name} added to favorites`)
  closeGameSearchModal()
}

const removeGame = (index) => {
  const game = selectedGames.value[index]
  selectedGames.value.splice(index, 1)
  hasUnsavedChanges.value = true
  toast.info(`${game.name} removed from favorites`)
}

const getGameImageUrl = (game) => {
  return game.primaryImageUrl || game.coverImageUrl || 'https://via.placeholder.com/150x227?text=No+Image'
}

// Drag and Drop handlers
const handleDragStart = (index) => {
  if (selectedGames.value.length <= 1) return
  draggedIndex.value = index
}

const handleDragEnd = () => {
  draggedIndex.value = null
  dragOverIndex.value = null
}

const handleDragOver = (index) => {
  if (selectedGames.value.length <= 1) return
  dragOverIndex.value = index
}

const handleDrop = (dropIndex) => {
  if (selectedGames.value.length <= 1 || draggedIndex.value === null) return

  const draggedGame = selectedGames.value[draggedIndex.value]
  selectedGames.value.splice(draggedIndex.value, 1)
  selectedGames.value.splice(dropIndex, 0, draggedGame)

  hasUnsavedChanges.value = true
  draggedIndex.value = null
  dragOverIndex.value = null
}

const handleSave = async () => {
  try {
    isSaving.value = true

    // Save profile data
    await profileStore.updateProfile(formData)

    // Save favorite games
    const userId = authStore.user?.id
    if (userId) {
      // Get current favorites
      let currentFavorites = []
      try {
        currentFavorites = await gamesStore.getUserFavorites(userId) || []
      } catch (err) {
        console.warn('Could not fetch current favorites, assuming empty:', err)
        currentFavorites = []
      }

      // Get IDs for comparison (game.id is the igdbId)
      const currentFavoriteIds = currentFavorites.map(game => game.id)
      const newFavoriteIds = selectedGames.value.map(game => game.id)

      // Remove favorites that are no longer selected
      for (const game of currentFavorites) {
        if (!newFavoriteIds.includes(game.id)) {
          try {
            await gamesStore.removeFromFavorites(game.id)
          } catch (err) {
            console.error(`Failed to remove favorite ${game.name}:`, err)
          }
        }
      }

      // Add new favorites that weren't previously favorited
      for (const game of selectedGames.value) {
        if (!currentFavoriteIds.includes(game.id)) {
          try {
            await gamesStore.addToFavorites(game.id)
          } catch (err) {
            console.error(`Failed to add favorite ${game.name}:`, err)
          }
        }
      }
    }

    hasUnsavedChanges.value = false
    toast.success('Profile updated successfully!')

    // Force a small delay to ensure backend has processed the changes
    await new Promise(resolve => setTimeout(resolve, 500))

    router.push('/profile')
  } catch (error) {
    console.error('Error saving profile:', error)
    toast.error('Failed to save profile changes')
  } finally {
    isSaving.value = false
  }
}

// Browser navigation guard
const handleBeforeUnload = (e) => {
  if (hasUnsavedChanges.value) {
    e.preventDefault()
    e.returnValue = ''
  }
}

// Lifecycle
onMounted(() => {
  loadProfile()
  window.addEventListener('beforeunload', handleBeforeUnload)
})

onBeforeUnmount(() => {
  window.removeEventListener('beforeunload', handleBeforeUnload)
})
</script>
