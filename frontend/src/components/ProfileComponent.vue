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
                  <div class="ms-3">
                    <!-- Follow Button for Other Users -->
                    <div v-if="!isOwnProfile && authStore.user" class="mb-2">
                      <button
                        @click="toggleFollow"
                        :disabled="followingInProgress"
                        class="btn"
                        :class="isFollowed ? 'btn-success' : 'btn-primary'"
                      >
                        <span v-if="followingInProgress" class="spinner-border spinner-border-sm me-2"></span>
                        <i v-else :class="isFollowed ? 'fas fa-check' : 'fas fa-plus'" class="me-2"></i>
                        {{ isFollowed ? 'Following' : 'Follow' }}
                      </button>
                    </div>

                    <!-- Login Prompt for Non-Authenticated Users -->
                    <div v-else-if="!isOwnProfile && !authStore.user" class="mb-2">
                      <router-link to="/login" class="btn btn-outline-primary">
                        <i class="fas fa-sign-in-alt me-2"></i>
                        Login to Follow
                      </router-link>
                    </div>

                    <!-- Edit Profile Button for Own Profile -->
                  <div v-if="isOwnProfile">
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
              <div>
                <h3 class="h5 mb-0 fw-bold">
                  <i class="fas fa-heart text-danger me-2"></i>
                  Top 5 Favorite Games
                </h3>
                <small class="text-muted">Showcase your favorite games (1-5 games)</small>
              </div>
              <button
                v-if="isOwnProfile && !isEditing"
                @click="toggleGamesEditing"
                class="btn btn-sm btn-outline-primary"
              >
                <i class="fas fa-edit me-1"></i>
                Edit Favorites
              </button>
            </div>
          </div>

          <div class="card-body p-4">
            <!-- No Games State -->
            <div v-if="!topGames || topGames.length === 0" class="text-center py-4">
              <i class="fas fa-heart text-muted mb-3" style="font-size: 3rem;"></i>
              <h5 class="text-muted">No favorite games yet</h5>
              <p class="text-muted">
                {{ isOwnProfile ? 'Add 1-5 of your favorite games to showcase them on your profile.' : 'This user hasn\'t added any favorite games yet.' }}
              </p>
              <button
                v-if="isOwnProfile"
                @click="toggleGamesEditing"
                class="btn btn-primary"
              >
                <i class="fas fa-heart me-2"></i>
                Add Favorite Games
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
                      :src="getImageUrl(game.primaryImageUrl, FALLBACK_TYPES.GAME_ICON, IMAGE_CONTEXTS.PROFILE_GAME)"
                      :alt="game.name"
                      class="rounded"
                      style="width: 60px; height: 60px; object-fit: cover;"
                      @error="(e) => handleImageError(e, 'gameIcon')"
                    >
                  </div>

                  <!-- Game Info -->
                  <div class="flex-grow-1">
                    <h6 class="mb-1 fw-bold">{{ game.name }}</h6>
                    <!-- <div class="d-flex gap-3 small text-muted">
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
                    </div> -->
                  </div>
                </div>
              </div>
            </div>

            <!-- Games Editing Mode -->
            <div v-else class="games-editing">
              <!-- Game Search Component -->
              <div class="mb-4">
                <GameSearchComponent
                  :show-card="false"
                  :show-title="true"
                  :show-results="true"
                  :show-load-more="false"
                  title="Search and Add Games"
                  placeholder="Search for games..."
                  results-title="Search Results"
                  results-mode="compact"
                  pagination-mode="infinite-scroll"
                  max-height="250px"
                  :auto-search="true"
                  :debounce-ms="300"
                  :is-game-disabled="(game) => isGameInTop5(game.id) || editTopGames.length >= 5"
                  :is-game-selected="(game) => isGameInTop5(game.id)"
                  @select-game="addGameToTop5"
                />
              </div>

              <!-- Current Top 5 (Editing) -->
              <div class="mb-3">
                <div class="d-flex justify-content-between align-items-center mb-2">
                  <h6 class="fw-bold mb-0">Your Top 5 Games ({{ editTopGames.length }}/5)</h6>
                  <div class="text-end">
                    <small class="text-muted">
                      {{ editTopGames.length === 0 ? 'Add at least 1 game' :
                         editTopGames.length === 5 ? 'Maximum reached' :
                         `Add ${5 - editTopGames.length} more` }}
                    </small>
                  </div>
                </div>
                <div v-if="editTopGames.length === 0" class="text-center py-3 text-muted border rounded-3 bg-light">
                  <i class="fas fa-search mb-2"></i>
                  <p class="mb-0">No games selected. Search and click games above to add them.</p>
                  <small class="text-muted">You must add between 1-5 games to your favorites.</small>
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
                        :src="getImageUrl(game.primaryImageUrl, 'gameIcon', IMAGE_CONTEXTS.PROFILE_GAME)"
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
                  :disabled="isSavingGames || editTopGames.length === 0 || editTopGames.length > 5"
                  class="btn btn-success"
                  :class="{ 'btn-outline-success': editTopGames.length === 0 }"
                >
                  <span v-if="isSavingGames" class="spinner-border spinner-border-sm me-2"></span>
                  <i v-else class="fas fa-save me-2"></i>
                  {{ isSavingGames ? 'Saving...' :
                     editTopGames.length === 0 ? 'Add Games First' :
                     'Save Games' }}
                </button>
                <button @click="cancelGamesEditing" class="btn btn-outline-secondary">
                  <i class="fas fa-times me-2"></i>
                  Cancel
                </button>
              </div>

              <!-- Validation hint -->
              <div v-if="editTopGames.length === 0" class="mt-2">
                <small class="text-muted">
                  <i class="fas fa-info-circle me-1"></i>
                  You must add at least 1 game before saving.
                </small>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- User Reviews Section -->
      <div class="col-12">
        <div class="card shadow-sm border-0 mb-4">
          <div class="card-header bg-white border-bottom">
            <div class="d-flex justify-content-between align-items-center">
              <h3 class="h5 mb-0 fw-bold">
                <i class="fas fa-star text-primary me-2"></i>
                {{ isOwnProfile ? 'My ' : ''}} Reviews
                <span v-if="userReviews.length > 0" class="text-muted fw-normal">({{ totalReviews }})</span>
              </h3>
              <router-link
                v-if="userReviews.length > displayedReviewsLimit"
                :to="`/profile/${profile.id}/reviews`"
                class="btn btn-sm btn-outline-primary"
              >
                View All Reviews
                <i class="fas fa-arrow-right ms-1"></i>
              </router-link>
            </div>
          </div>
          <div class="card-body p-4">
            <!-- Loading State -->
            <div v-if="loadingReviews" class="text-center py-5">
              <div class="spinner-border text-primary mb-3"></div>
              <p class="text-muted">Loading reviews...</p>
            </div>

            <!-- Reviews List -->
            <div v-else-if="userReviews.length > 0">
              <div class="row g-3">
                <div
                  v-for="review in displayedReviews"
                  :key="review.id"
                  class="col-12"
                >
                  <ReviewCard
                    :review="review"
                    :show-game="true"
                    :show-date="true"
                    :truncated="true"
                    :max-length="150"
                    :is-liked="likedReviews.has(review.id)"
                    :is-processing-like="processingLikeReviews.has(review.id)"
                    @toggleLike="handleToggleLike"
                    @showComments="handleShowComments"
                  />
                </div>
              </div>

              <!-- Show More Button -->
              <div v-if="userReviews.length > displayedReviewsLimit" class="text-center mt-4">
                <router-link
                  :to="`/profile/${profile.id}/reviews`"
                  class="btn btn-outline-primary"
                >
                  <i class="fas fa-plus me-2"></i>
                  View All {{ totalReviews }} Reviews
                </router-link>
              </div>
            </div>

            <!-- Empty State -->
            <div v-else class="text-center py-5">
              <i class="fas fa-star-o text-muted mb-3" style="font-size: 2rem;"></i>
              <h6 class="text-muted mb-2">No Reviews Yet</h6>
              <p class="text-muted small mb-3">
                {{ isOwnProfile ? "You haven't written any reviews yet." : `${profile.displayName || 'This user'} hasn't written any reviews yet.` }}
              </p>
              <router-link
                v-if="isOwnProfile"
                to="/dashboard"
                class="btn btn-primary"
              >
                <i class="fas fa-search me-2"></i>
                Find Games to Review
              </router-link>
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
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/authStore'
import { useProfileStore } from '@/stores/profileStore'
import { useGamesStore } from '@/stores/gamesStore'
import { useToast } from 'vue-toastification'
import { useImageFallback, FALLBACK_TYPES } from '@/composables/useImageFallback'
import { useAuthRedirect } from '@/utils/authRedirect'
import GameSearchComponent from '@/components/GameSearchComponent.vue'
import ReviewCard from '@/components/ReviewCard.vue'
import { reviewsService } from '@/services/reviewsService'
import { socialService } from '@/services/socialService'
import { commentsService } from '@/services/commentsService'

// Props for viewing other users' profiles
const props = defineProps({
  userId: {
    type: String,
    default: null
  }
})

// Composables
const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()
const profileStore = useProfileStore()
const gamesStore = useGamesStore()
const toast = useToast()
const { handleImageError, getImageUrl, IMAGE_CONTEXTS } = useImageFallback()
const { redirectToLoginWithReturn } = useAuthRedirect()

// State
const profile = ref(null)
const loading = ref(true)
const error = ref('')
const saveError = ref('')

// Reviews state
const userReviews = ref([])
const loadingReviews = ref(false)
const totalReviews = ref(0)
const displayedReviewsLimit = 3
const likedReviews = ref(new Set())
const processingLikeReviews = ref(new Set())

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
const userFavorites = ref([])
const isEditingGames = ref(false)
const editTopGames = ref([])
const originalTopGames = ref([]) // Track original games for comparison
const isSavingGames = ref(false)
const imageInput = ref(null)

// Follow state
const isFollowed = ref(false)
const followingInProgress = ref(false)

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

// Reviews computed properties
const displayedReviews = computed(() => {
  return userReviews.value.slice(0, displayedReviewsLimit)
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

    // Load user's favorites to populate top games
    if (profileUserId.value) {
      await loadUserFavorites()
    }

    // Load user's reviews
    await loadUserReviews()

    // Check follow status if viewing another user's profile
    if (!isOwnProfile.value && authStore.user) {
      await checkFollowStatus()
    }
  } catch (err) {
    error.value = err.message || 'Failed to load profile'
    console.error('Error fetching profile:', err)
  } finally {
    loading.value = false
  }
}

const loadUserFavorites = async () => {
  try {
    const favorites = await gamesStore.getUserFavorites(profileUserId.value)
    userFavorites.value = favorites || []

    // Set top games from first 5 favorites
    topGames.value = userFavorites.value.slice(0, 5)
  } catch (err) {
    console.error('Error loading user favorites:', err)
    userFavorites.value = []
    topGames.value = []
  }
}

const loadUserReviews = async () => {
  if (!profileUserId.value) return

  try {
    loadingReviews.value = true
    const response = await reviewsService.getUserReviews(profileUserId.value, 1, displayedReviewsLimit + 5)
    const reviewsWithCommentCounts = await commentsService.loadCommentCountsForReviews(response.data || [])
    userReviews.value = reviewsWithCommentCounts
    totalReviews.value = response.totalCount || 0

    // Load like status for each review if current user is authenticated
    if (authStore.user) {
      likedReviews.value.clear()
      const likeStatusPromises = userReviews.value.map(async (review) => {
        try {
          const isLiked = await socialService.isReviewLiked(review.id)
          if (isLiked) {
            likedReviews.value.add(review.id)
          }
        } catch (error) {
          console.warn(`Failed to check like status for review ${review.id}:`, error)
        }
      })
      await Promise.all(likeStatusPromises)
    }
  } catch (error) {
    console.error('Error loading user reviews:', error)
    userReviews.value = []
    totalReviews.value = 0
  } finally {
    loadingReviews.value = false
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
    originalTopGames.value = [...topGames.value] // Store original for comparison
    // Load current favorites to show what's available
    loadUserFavorites()
  }
}

const cancelGamesEditing = () => {
  isEditingGames.value = false
  editTopGames.value = []
  originalTopGames.value = []
}

const saveTopGames = async () => {
  try {
    // Validate the games count (1-5 games required)
    if (editTopGames.value.length === 0) {
      toast.error('Please add at least 1 game to your top 5')
      return
    }

    if (editTopGames.value.length > 5) {
      toast.error('You can only have a maximum of 5 games in your top 5')
      return
    }

    isSavingGames.value = true

    // First, we need to ensure all selected games are in user's favorites
    const userId = authStore.user?.id
    if (!userId) {
      throw new Error('User not authenticated')
    }

    // For each game in editTopGames, ensure it's in favorites
    for (const game of editTopGames.value) {
      const isInFavorites = userFavorites.value.some(fav => fav.id === game.id)
      if (!isInFavorites) {
        // Add to favorites first
        await gamesStore.addToFavorites(game.id)
      }
    }

    // Remove games that were in the original top 5 but not in the new top 5
    const newTopGameIds = editTopGames.value.map(game => game.igdbId)
    const originalTopGameIds = originalTopGames.value.map(game => game.igdbId)

    for (const originalGameId of originalTopGameIds) {
      if (!newTopGameIds.includes(originalGameId)) {
        console.log('Removing game from favorites:', originalGameId)
        await gamesStore.removeFromFavorites(originalGameId)
      }
    }

    // Update the local state
    const savedGamesCount = editTopGames.value.length
    topGames.value = [...editTopGames.value]
    userFavorites.value = [...editTopGames.value]
    isEditingGames.value = false
    editTopGames.value = []
    originalTopGames.value = []

    toast.success(`Top ${savedGamesCount} games updated successfully!`)
  } catch (err) {
    toast.error('Failed to save games')
    console.error('Error saving games:', err)
  } finally {
    isSavingGames.value = false
  }
}


const addGameToTop5 = (game) => {
  if (editTopGames.value.length < 5 && !isGameInTop5(game.id)) {
    editTopGames.value.push(game)
  }
}

const removeGameFromTop5 = (index) => {
  if (index >= 0 && index < editTopGames.value.length) {
    const removedGame = editTopGames.value[index]
    editTopGames.value.splice(index, 1)
    // Optional: Show feedback to user
    console.log(`Removed "${removedGame.name}" from top games list`)
  }
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

const handleToggleLike = async (review) => {
  if (!authStore.user) {
    toast.info('Please sign in to like reviews')
    return
  }

  const reviewId = review.id
  const wasLiked = likedReviews.value.has(reviewId)

  if (processingLikeReviews.value.has(reviewId)) return

  try {
    processingLikeReviews.value.add(reviewId)

    if (wasLiked) {
      await socialService.unlikeReview(reviewId)
      likedReviews.value.delete(reviewId)
    } else {
      await socialService.likeReview(reviewId)
      likedReviews.value.add(reviewId)
    }

    // Update like count in review
    const targetReview = userReviews.value.find(r => r.id === reviewId)
    if (targetReview) {
      targetReview.likeCount = (targetReview.likeCount || 0) + (wasLiked ? -1 : 1)
    }

  } catch (err) {
    console.error('Error toggling review like:', err)
    toast.error('Failed to update like')
  } finally {
    processingLikeReviews.value.delete(reviewId)
  }
}

const handleShowComments = (review) => {
  // Navigate to dedicated review details page
  router.push(`/reviews/${review.id}`)
}

// Follow methods
const checkFollowStatus = async () => {
  if (!profileUserId.value || isOwnProfile.value || !authStore.user) return

  try {
    const response = await socialService.isUserFollowed(profileUserId.value)
    isFollowed.value = response
  } catch (error) {
    console.error('Error checking follow status:', error)
    isFollowed.value = false
  }
}

const toggleFollow = async () => {
  if (!authStore.user) {
    redirectToLoginWithReturn('Please login to follow users')
    return
  }

  followingInProgress.value = true

  try {
    if (isFollowed.value) {
      await socialService.unfollowUser(profileUserId.value)
      isFollowed.value = false
      toast.success(`Unfollowed ${profile.value.displayName}`)
    } else {
      await socialService.followUser(profileUserId.value)
      isFollowed.value = true
      toast.success(`Now following ${profile.value.displayName}`)
    }
  } catch (error) {
    console.error('Error toggling follow:', error)
    toast.error(error.message || 'Failed to update follow status')
  } finally {
    followingInProgress.value = false
  }
}

// Watchers
watch(() => profileUserId.value, () => {
  if (profileUserId.value) {
    fetchProfile()
  }
}, { immediate: true })

// Search is now handled by GameSearchComponent

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