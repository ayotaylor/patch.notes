<template>
  <div class="container py-4">
    <!-- Loading State -->
    <div v-if="loading" class="text-center py-5">
      <div class="spinner-border text-primary" role="status">
        <span class="visually-hidden">Loading...</span>
      </div>
      <p class="mt-3 text-muted">Loading profile...</p>
    </div>

    <!-- Error State -->
    <div v-else-if="error" class="alert alert-danger" role="alert">
      <i class="fas fa-exclamation-triangle me-2"></i>
      {{ error }}
    </div>

    <!-- Profile Content -->
    <div v-else-if="profile" class="row">
      <!-- Profile Header -->
      <div class="col-12">
        <div class="card shadow-sm border-0 mb-4">
          <div class="card-body p-4">
            <div class="row align-items-center">
              <!-- Profile Image -->
              <div class="col-auto">
                <div class="position-relative">
                  <img
                    :src="profile.profileImageUrl || defaultAvatar"
                    :alt="`${profile.displayName || 'User'}'s profile`"
                    class="rounded-circle border border-3 border-light shadow-sm"
                    style="width: 120px; height: 120px; object-fit: cover;"
                  >
                  <button
                    v-if="isOwnProfile && isEditing"
                    @click="triggerImageUpload"
                    class="btn btn-primary btn-sm rounded-circle position-absolute bottom-0 end-0 d-flex align-items-center justify-content-center"
                    style="width: 35px; height: 35px;"
                    title="Change profile picture"
                  >
                    <i class="fas fa-camera"></i>
                  </button>
                  <input
                    ref="imageInput"
                    type="file"
                    accept="image/*"
                    @change="handleImageUpload"
                    class="d-none"
                  >
                </div>
              </div>

              <!-- Profile Info -->
              <div class="col">
                <div class="d-flex justify-content-between align-items-start mb-3">
                  <div>
                    <!-- Display Name -->
                    <h1 v-if="!isEditing" class="h2 mb-1 fw-bold">
                      {{ profile.displayName || fullName || 'User' }}
                    </h1>
                    <input
                      v-else
                      v-model="editForm.displayName"
                      type="text"
                      class="form-control form-control-lg fw-bold border-0 ps-0"
                      placeholder="Display name"
                      maxlength="100"
                      style="background: transparent;"
                    >

                    <!-- Full Name -->
                    <p v-if="!isEditing && fullName && fullName !== profile.displayName" class="text-muted mb-2">
                      {{ fullName }}
                    </p>
                    <div v-else-if="isEditing" class="row g-2 mb-2">
                      <div class="col-md-6">
                        <input
                          v-model="editForm.firstName"
                          type="text"
                          class="form-control"
                          placeholder="First name"
                          maxlength="50"
                        >
                      </div>
                      <div class="col-md-6">
                        <input
                          v-model="editForm.lastName"
                          type="text"
                          class="form-control"
                          placeholder="Last name"
                          maxlength="50"
                        >
                      </div>
                    </div>

                    <!-- Email -->
                    <p class="text-muted small mb-0">
                      <i class="fas fa-envelope me-1"></i>
                      {{ profile.email }}
                    </p>
                  </div>

                  <!-- Action Buttons -->
                  <div v-if="isOwnProfile" class="ms-3">
                    <template v-if="!isEditing">
                      <button @click="startEditing" class="btn btn-outline-primary">
                        <i class="fas fa-edit me-2"></i>
                        Edit Profile
                      </button>
                    </template>
                    <template v-else>
                      <div class="btn-group">
                        <button
                          @click="saveProfile"
                          :disabled="isSaving"
                          class="btn btn-success"
                        >
                          <span v-if="isSaving" class="spinner-border spinner-border-sm me-2"></span>
                          <i v-else class="fas fa-save me-2"></i>
                          {{ isSaving ? 'Saving...' : 'Save' }}
                        </button>
                        <button @click="cancelEditing" class="btn btn-outline-secondary">
                          <i class="fas fa-times me-2"></i>
                          Cancel
                        </button>
                      </div>
                    </template>
                  </div>
                </div>

                <!-- Bio -->
                <div class="mb-3">
                  <label v-if="isEditing" class="form-label small text-muted">Bio</label>
                  <p v-if="!isEditing && profile.bio" class="mb-0">{{ profile.bio }}</p>
                  <p v-else-if="!isEditing && !profile.bio" class="text-muted fst-italic mb-0">
                    No bio available
                  </p>
                  <textarea
                    v-else
                    v-model="editForm.bio"
                    class="form-control"
                    rows="3"
                    placeholder="Tell us about yourself..."
                    maxlength="500"
                  ></textarea>
                  <div v-if="isEditing" class="form-text text-end">
                    {{ (editForm.bio || '').length }}/500
                  </div>
                </div>

                <!-- Profile Stats -->
                <div class="d-flex gap-4">
                  <div class="text-center">
                    <div class="h5 mb-0 fw-bold text-primary">{{ profile.gamesCount || 0 }}</div>
                    <small class="text-muted">Games</small>
                  </div>
                  <div class="text-center">
                    <div class="h5 mb-0 fw-bold text-success">{{ profile.achievementsCount || 0 }}</div>
                    <small class="text-muted">Achievements</small>
                  </div>
                  <div class="text-center">
                    <div class="h5 mb-0 fw-bold text-warning">{{ profile.totalPlayTime || '0h' }}</div>
                    <small class="text-muted">Play Time</small>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Top 5 Games Section -->
      <div class="col-12">
        <div class="card shadow-sm border-0">
          <div class="card-header bg-white border-bottom">
            <div class="d-flex justify-content-between align-items-center">
              <h3 class="h5 mb-0 fw-bold">
                <i class="fas fa-trophy text-warning me-2"></i>
                Top 5 Games
              </h3>
              <button
                v-if="isOwnProfile && !isEditing"
                @click="toggleGamesEditing"
                class="btn btn-sm btn-outline-primary"
              >
                <i class="fas fa-edit me-1"></i>
                Edit Games
              </button>
            </div>
          </div>

          <div class="card-body p-4">
            <!-- No Games State -->
            <div v-if="!topGames || topGames.length === 0" class="text-center py-4">
              <i class="fas fa-gamepad text-muted mb-3" style="font-size: 3rem;"></i>
              <h5 class="text-muted">No games added yet</h5>
              <p class="text-muted">
                {{ isOwnProfile ? 'Add your favorite games to showcase them on your profile.' : 'This user hasn\'t added any games yet.' }}
              </p>
              <button
                v-if="isOwnProfile"
                @click="toggleGamesEditing"
                class="btn btn-primary"
              >
                <i class="fas fa-plus me-2"></i>
                Add Games
              </button>
            </div>

            <!-- Games List -->
            <div v-else-if="!isEditingGames" class="row g-3">
              <div
                v-for="(game, index) in topGames"
                :key="game.id"
                class="col-12"
              >
                <div class="d-flex align-items-center p-3 border rounded-3 bg-light">
                  <!-- Rank Badge -->
                  <div class="me-3">
                    <span class="badge bg-primary rounded-circle d-flex align-items-center justify-content-center" style="width: 30px; height: 30px;">
                      {{ index + 1 }}
                    </span>
                  </div>

                  <!-- Game Image -->
                  <div class="me-3">
                    <img
                      :src="getImageUrl(game.imageUrl, FALLBACK_TYPES.GAME_ICON)"
                      :alt="game.name"
                      class="rounded"
                      style="width: 60px; height: 60px; object-fit: cover;"
                      @error="(e) => handleImageError(e, 'gameIcon')"
                    >
                  </div>

                  <!-- Game Info -->
                  <div class="flex-grow-1">
                    <h6 class="mb-1 fw-bold">{{ game.name }}</h6>
                    <p class="mb-1 text-muted small">{{ game.genre || 'Unknown Genre' }}</p>
                    <div class="d-flex gap-3 small text-muted">
                      <span v-if="game.playTime">
                        <i class="fas fa-clock me-1"></i>
                        {{ game.playTime }}
                      </span>
                      <span v-if="game.achievements">
                        <i class="fas fa-trophy me-1"></i>
                        {{ game.achievements }} achievements
                      </span>
                      <span v-if="game.rating">
                        <i class="fas fa-star me-1 text-warning"></i>
                        {{ game.rating }}/5
                      </span>
                    </div>
                  </div>
                </div>
              </div>
            </div>

            <!-- Games Editing Mode -->
            <div v-else class="games-editing">
              <div class="mb-3">
                <label class="form-label fw-bold">Search and Add Games</label>
                <div class="input-group">
                  <input
                    v-model="gameSearchQuery"
                    @input="searchGames"
                    type="text"
                    class="form-control"
                    placeholder="Search for games..."
                  >
                  <button class="btn btn-outline-secondary" type="button">
                    <i class="fas fa-search"></i>
                  </button>
                </div>
              </div>

              <!-- Search Results -->
              <div v-if="gameSearchResults.length > 0" class="mb-4">
                <h6 class="fw-bold mb-2">Search Results</h6>
                <div class="list-group" style="max-height: 200px; overflow-y: auto;">
                  <button
                    v-for="game in gameSearchResults"
                    :key="game.id"
                    @click="addGameToTop5(game)"
                    :disabled="isGameInTop5(game.id) || editTopGames.length >= 5"
                    class="list-group-item list-group-item-action d-flex align-items-center"
                    :class="{ 'disabled': isGameInTop5(game.id) || editTopGames.length >= 5 }"
                  >
                    <img
                      :src="getImageUrl(game.imageUrl, 'gameIcon')"
                      :alt="game.name"
                      class="rounded me-3"
                      style="width: 40px; height: 40px; object-fit: cover;"
                      @error="(e) => handleImageError(e, 'gameIcon')"
                    >
                    <div>
                      <h6 class="mb-0">{{ game.name }}</h6>
                      <small class="text-muted">{{ game.genre }}</small>
                    </div>
                    <i v-if="isGameInTop5(game.id)" class="fas fa-check text-success ms-auto"></i>
                  </button>
                </div>
              </div>

              <!-- Current Top 5 (Editing) -->
              <div class="mb-3">
                <h6 class="fw-bold mb-2">Your Top 5 Games ({{ editTopGames.length }}/5)</h6>
                <div v-if="editTopGames.length === 0" class="text-center py-3 text-muted">
                  No games selected. Search and click games above to add them.
                </div>
                <div v-else class="list-group">
                  <div
                    v-for="(game, index) in editTopGames"
                    :key="game.id"
                    class="list-group-item d-flex align-items-center justify-content-between"
                  >
                    <div class="d-flex align-items-center">
                      <span class="badge bg-primary me-3">{{ index + 1 }}</span>
                      <img
                        :src="getImageUrl(game.imageUrl, 'gameIcon')"
                        :alt="game.name"
                        class="rounded me-3"
                        style="width: 40px; height: 40px; object-fit: cover;"
                        @error="(e) => handleImageError(e, 'gameIcon')"
                      >
                      <div>
                        <h6 class="mb-0">{{ game.name }}</h6>
                        <small class="text-muted">{{ game.genre }}</small>
                      </div>
                    </div>
                    <div class="btn-group">
                      <button
                        v-if="index > 0"
                        @click="moveGameUp(index)"
                        class="btn btn-sm btn-outline-secondary"
                        title="Move up"
                      >
                        <i class="fas fa-chevron-up"></i>
                      </button>
                      <button
                        v-if="index < editTopGames.length - 1"
                        @click="moveGameDown(index)"
                        class="btn btn-sm btn-outline-secondary"
                        title="Move down"
                      >
                        <i class="fas fa-chevron-down"></i>
                      </button>
                      <button
                        @click="removeGameFromTop5(index)"
                        class="btn btn-sm btn-outline-danger"
                        title="Remove"
                      >
                        <i class="fas fa-trash"></i>
                      </button>
                    </div>
                  </div>
                </div>
              </div>

              <!-- Games Editing Actions -->
              <div class="d-flex gap-2">
                <button
                  @click="saveTopGames"
                  :disabled="isSavingGames"
                  class="btn btn-success"
                >
                  <span v-if="isSavingGames" class="spinner-border spinner-border-sm me-2"></span>
                  <i v-else class="fas fa-save me-2"></i>
                  {{ isSavingGames ? 'Saving...' : 'Save Games' }}
                </button>
                <button @click="cancelGamesEditing" class="btn btn-outline-secondary">
                  <i class="fas fa-times me-2"></i>
                  Cancel
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Error Alert -->
    <div v-if="saveError" class="alert alert-danger mt-3" role="alert">
      <i class="fas fa-exclamation-triangle me-2"></i>
      {{ saveError }}
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted, watch } from 'vue'
import { defineProps } from 'vue'
import { useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/authStore'
import { useProfileStore } from '@/stores/profileStore'
import { useGamesStore } from '@/stores/gamesStore'
import { useToast } from 'vue-toastification'
import { useImageFallback, FALLBACK_TYPES } from '@/composables/useImageFallback'

// Props for viewing other users' profiles
const props = defineProps({
  userId: {
    type: String,
    default: null
  }
})

// Composables
const route = useRoute()
const authStore = useAuthStore()
const profileStore = useProfileStore()
const gamesStore = useGamesStore()
const toast = useToast()
const { handleImageError, getImageUrl } = useImageFallback()

// State
const profile = ref(null)
const loading = ref(true)
const error = ref('')
const saveError = ref('')

// Editing state
const isEditing = ref(false)
const isSaving = ref(false)
const editForm = reactive({
  firstName: '',
  lastName: '',
  displayName: '',
  bio: '',
  profileImageUrl: ''
})

// Games state
const topGames = ref([])
const isEditingGames = ref(false)
const editTopGames = ref([])
const isSavingGames = ref(false)
const gameSearchQuery = ref('')
const gameSearchResults = ref([])
const imageInput = ref(null)

// Computed properties
const profileUserId = computed(() => props.userId || route.params.userId || authStore.user?.id)
const isOwnProfile = computed(() => profileUserId.value === authStore.user?.id)

const fullName = computed(() => {
  if (!profile.value) return ''
  const { firstName, lastName } = profile.value
  return `${firstName || ''} ${lastName || ''}`.trim()
})

const defaultAvatar = computed(() => {
  const name = profile.value?.displayName || fullName.value || 'User'
  return `https://ui-avatars.com/api/?name=${encodeURIComponent(name)}&size=120&background=6c757d&color=ffffff`
})

// Methods
const fetchProfile = async () => {
  try {
    loading.value = true
    error.value = ''

    const response = await profileStore.fetchProfile(
      isOwnProfile.value ? null : profileUserId.value
    )

    profile.value = response
    topGames.value = response.topGames || []
  } catch (err) {
    error.value = err.message || 'Failed to load profile'
    console.error('Error fetching profile:', err)
  } finally {
    loading.value = false
  }
}

const startEditing = () => {
  isEditing.value = true
  Object.assign(editForm, {
    firstName: profile.value.firstName || '',
    lastName: profile.value.lastName || '',
    displayName: profile.value.displayName || '',
    bio: profile.value.bio || '',
    profileImageUrl: profile.value.profileImageUrl || ''
  })
}

const cancelEditing = () => {
  isEditing.value = false
  saveError.value = ''
}

const saveProfile = async () => {
  try {
    isSaving.value = true
    saveError.value = ''

    const response = await profileStore.updateProfile(editForm)

    // Update profile data
    Object.assign(profile.value, response)

    // Update auth store if it's own profile
    if (isOwnProfile.value) {
      authStore.updateUserProfileState(response)
    }

    isEditing.value = false
    toast.success('Profile updated successfully!')
  } catch (err) {
    saveError.value = err.message || 'Failed to save profile'
    console.error('Error saving profile:', err)
  } finally {
    isSaving.value = false
  }
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
    // Create preview
    const reader = new FileReader()
    reader.onload = (e) => {
      editForm.profileImageUrl = e.target.result
    }
    reader.readAsDataURL(file)

    // In a real app, upload to your server here
    // const uploadedUrl = await uploadImage(file)
    // editForm.profileImageUrl = uploadedUrl
  } catch (err) {
    toast.error('Failed to upload image')
    console.error('Error uploading image:', err)
  }
}

const triggerImageUpload = () => {
  imageInput.value?.click()
}

// Games methods
const toggleGamesEditing = () => {
  isEditingGames.value = !isEditingGames.value
  if (isEditingGames.value) {
    editTopGames.value = [...topGames.value]
  }
}

const cancelGamesEditing = () => {
  isEditingGames.value = false
  editTopGames.value = []
  gameSearchQuery.value = ''
  gameSearchResults.value = []
}

const saveTopGames = async () => {
  try {
    isSavingGames.value = true

    const response = await profileStore.updateTopGames({
      topGames: editTopGames.value
    })

    topGames.value = response.topGames || editTopGames.value
    isEditingGames.value = false
    editTopGames.value = []

    toast.success('Top games updated successfully!')
  } catch (err) {
    toast.error('Failed to save games')
    console.error('Error saving games:', err)
  } finally {
    isSavingGames.value = false
  }
}

const searchGames = async () => {
  if (!gameSearchQuery.value.trim()) {
    gameSearchResults.value = []
    return
  }

  try {
    const results = await gamesStore.searchGames(gameSearchQuery.value)
    gameSearchResults.value = results
  } catch (err) {
    console.error('Error searching games:', err)
    gameSearchResults.value = []
  }
}

const addGameToTop5 = (game) => {
  if (editTopGames.value.length < 5 && !isGameInTop5(game.id)) {
    editTopGames.value.push(game)
  }
}

const removeGameFromTop5 = (index) => {
  editTopGames.value.splice(index, 1)
}

const moveGameUp = (index) => {
  if (index > 0) {
    const game = editTopGames.value.splice(index, 1)[0]
    editTopGames.value.splice(index - 1, 0, game)
  }
}

const moveGameDown = (index) => {
  if (index < editTopGames.value.length - 1) {
    const game = editTopGames.value.splice(index, 1)[0]
    editTopGames.value.splice(index + 1, 0, game)
  }
}

const isGameInTop5 = (gameId) => {
  return editTopGames.value.some(game => game.id === gameId)
}

// Watchers
watch(() => profileUserId.value, () => {
  if (profileUserId.value) {
    fetchProfile()
  }
}, { immediate: true })

// Debounce search
let searchTimeout
watch(gameSearchQuery, () => {
  clearTimeout(searchTimeout)
  searchTimeout = setTimeout(searchGames, 300)
})

// Lifecycle
onMounted(() => {
  if (profileUserId.value) {
    fetchProfile()
  }
})
</script>

<style scoped>
.card {
  border-radius: 15px;
}

.form-control:focus {
  border-color: #0d6efd;
  box-shadow: 0 0 0 0.2rem rgba(13, 110, 253, 0.25);
}

.games-editing .list-group-item {
  transition: all 0.2s ease;
}

.games-editing .list-group-item:hover:not(.disabled) {
  background-color: #f8f9fa;
}

.games-editing .list-group-item.disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

@media (max-width: 768px) {
  .container {
    padding: 1rem;
  }

  .card-body {
    padding: 1.5rem !important;
  }

  .btn-group {
    flex-direction: column;
    width: 100%;
  }

  .btn-group .btn {
    margin-bottom: 0.5rem;
  }
}
</style>