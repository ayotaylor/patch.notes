<template>
  <div class="members-page">
    <div class="container-fluid mt-4">
      <div class="row">
        <!-- Main Content -->
        <div class="col-lg-9">
          <!-- Featured Users Section -->
          <div class="card mb-4">
            <div class="card-header bg-primary text-white">
              <h5 class="mb-0">
                <i class="fas fa-star me-2"></i>Featured Users
              </h5>
            </div>
            <div class="card-body">
              <div class="row" v-if="featuredUsers.length > 0">
                <div
                  v-for="user in featuredUsers"
                  :key="user.id"
                  class="col-lg-2 col-md-3 col-sm-4 col-6 mb-3"
                >
                  <div class="user-card-featured">
                    <div class="user-image-container position-relative">
                      <img
                        :src="getImageUrl(user.profileImageUrl, FALLBACK_TYPES.AVATAR, IMAGE_CONTEXTS.PROFILE_GAME)"
                        :alt="user.displayName"
                        class="user-image"
                        @error="(e) => handleImageError(e, FALLBACK_TYPES.AVATAR)"
                      />
                      <button
                        class="follow-btn-overlay"
                        @click="toggleFollow(user)"
                        :class="{ 'following': user.isFollowed }"
                        v-if="authStore.user && authStore.user.id !== user.id"
                      >
                        <i :class="user.isFollowed ? 'fas fa-check' : 'fas fa-plus'"></i>
                      </button>
                    </div>
                    <div class="user-info text-center mt-2">
                      <h6 class="user-name mb-1">
                        <router-link :to="`/profile/${user.id}`" class="text-decoration-none">
                          {{ user.displayName }}
                        </router-link>
                      </h6>
                      <small class="text-muted">{{ user.reviewCount || 0 }} reviews</small>
                    </div>
                  </div>
                </div>
              </div>
              <div v-else class="text-center text-muted py-4">
                <i class="fas fa-users fa-3x mb-3"></i>
                <p>No featured users available</p>
              </div>
            </div>
          </div>

          <!-- Popular Users Section -->
          <div class="card mb-4">
            <div class="card-header bg-success text-white">
              <h5 class="mb-0">
                <i class="fas fa-fire me-2"></i>Popular Users
              </h5>
            </div>
            <div class="card-body">
              <div class="row" v-if="popularUsers.length > 0">
                <div
                  v-for="user in popularUsers"
                  :key="user.id"
                  class="col-lg-2 col-md-3 col-sm-4 col-6 mb-3"
                >
                  <div class="user-card-featured">
                    <div class="user-image-container position-relative">
                      <img
                        :src="getImageUrl(user.profileImageUrl, FALLBACK_TYPES.AVATAR, IMAGE_CONTEXTS.PROFILE_GAME)"
                        :alt="user.displayName"
                        class="user-image"
                        @error="(e) => handleImageError(e, FALLBACK_TYPES.AVATAR)"
                      />
                      <button
                        class="follow-btn-overlay"
                        @click="toggleFollow(user)"
                        :class="{ 'following': user.isFollowed }"
                        v-if="authStore.user && authStore.user.id !== user.id"
                      >
                        <i :class="user.isFollowed ? 'fas fa-check' : 'fas fa-plus'"></i>
                      </button>
                    </div>
                    <div class="user-info text-center mt-2">
                      <h6 class="user-name mb-1">
                        <router-link :to="`/profile/${user.id}`" class="text-decoration-none">
                          {{ user.displayName }}
                        </router-link>
                      </h6>
                      <small class="text-muted">{{ user.reviewCount || 0 }} reviews</small>
                    </div>
                  </div>
                </div>
              </div>
              <div v-else class="text-center text-muted py-4">
                <i class="fas fa-users fa-3x mb-3"></i>
                <p>No popular users available</p>
              </div>
            </div>
          </div>

          <!-- All Users List -->
          <div class="card">
            <div class="card-header bg-info text-white d-flex justify-content-between align-items-center">
              <h5 class="mb-0">
                <i class="fas fa-list me-2"></i>All Members
              </h5>
              <small>{{ totalUsers }} total members</small>
            </div>
            <div class="card-body">
              <div v-if="users.length > 0">
                <div
                  v-for="user in users"
                  :key="user.id"
                  class="user-row d-flex align-items-center justify-content-between py-3 border-bottom"
                >
                  <div class="d-flex align-items-center">
                    <img
                      :src="getImageUrl(user.profileImageUrl, FALLBACK_TYPES.AVATAR, IMAGE_CONTEXTS.PROFILE_GAME)"
                      :alt="user.displayName"
                      class="user-avatar me-3"
                      @error="(e) => handleImageError(e, FALLBACK_TYPES.AVATAR)"
                    />
                    <div>
                      <h6 class="mb-1">
                        <router-link :to="`/profile/${user.id}`" class="text-decoration-none">
                          {{ user.displayName }}
                        </router-link>
                      </h6>
                      <div class="user-stats d-flex gap-3">
                        <span
                          class="stat-item cursor-pointer"
                          @click="navigateToUserReviews(user)"
                          title="Reviews"
                        >
                          <i class="fas fa-star text-warning me-1"></i>
                          {{ user.reviewCount || 0 }} reviews
                        </span>
                        <span
                          class="stat-item cursor-pointer"
                          @click="navigateToUserLikes(user)"
                          title="Likes"
                        >
                          <i class="fas fa-heart text-danger me-1"></i>
                          {{ user.likeCount || 0 }} likes
                        </span>
                        <span
                          class="stat-item cursor-pointer"
                          @click="navigateToUserLists(user)"
                          title="Lists"
                        >
                          <i class="fas fa-list text-primary me-1"></i>
                          {{ user.listCount || 0 }} lists
                        </span>
                      </div>
                    </div>
                  </div>
                  <div class="follow-actions">
                    <button
                      v-if="authStore.user && authStore.user.id !== user.id"
                      class="btn btn-sm follow-btn"
                      :class="user.isFollowed ? 'btn-success' : 'btn-outline-primary'"
                      @click="toggleFollow(user)"
                      :disabled="followingInProgress[user.id]"
                    >
                      <i
                        :class="followingInProgress[user.id] ? 'fas fa-spinner fa-spin' : (user.isFollowed ? 'fas fa-check' : 'fas fa-plus')"
                        class="me-1"
                      ></i>
                      {{ user.isFollowed ? 'Following' : 'Follow' }}
                    </button>
                  </div>
                </div>
              </div>
              <div v-else class="text-center text-muted py-4">
                <i class="fas fa-users fa-3x mb-3"></i>
                <p>No members found</p>
              </div>

              <!-- Pagination -->
              <nav v-if="totalPages > 1" class="mt-4">
                <ul class="pagination justify-content-center">
                  <li class="page-item" :class="{ disabled: currentPage === 1 }">
                    <button class="page-link" @click="loadPage(currentPage - 1)" :disabled="currentPage === 1">
                      Previous
                    </button>
                  </li>

                  <li
                    v-for="page in getVisiblePages()"
                    :key="page"
                    class="page-item"
                    :class="{ active: page === currentPage }"
                  >
                    <button class="page-link" @click="loadPage(page)">{{ page }}</button>
                  </li>

                  <li class="page-item" :class="{ disabled: currentPage === totalPages }">
                    <button class="page-link" @click="loadPage(currentPage + 1)" :disabled="currentPage === totalPages">
                      Next
                    </button>
                  </li>
                </ul>
              </nav>
            </div>
          </div>
        </div>

        <!-- Sidebar -->
        <div class="col-lg-3">
          <div class="card sticky-top" style="top: 1rem;">
            <div class="card-header bg-secondary text-white">
              <h6 class="mb-0">
                <i class="fas fa-user-friends me-2"></i>Following
              </h6>
            </div>
            <div class="card-body">
              <div v-if="following.length > 0">
                <div class="row">
                  <div
                    v-for="followedUser in displayedFollowing"
                    :key="followedUser.followingUserId"
                    class="col-4 col-md-6 col-lg-4 mb-3"
                  >
                    <div class="following-user text-center">
                      <router-link :to="`/profile/${followedUser.followingUserId}`">
                        <img
                          :src="getImageUrl(followedUser.following.profileImageUrl, FALLBACK_TYPES.AVATAR, IMAGE_CONTEXTS.PROFILE_GAME)"
                          :alt="followedUser.displayName"
                          class="following-avatar"
                          @error="(e) => handleImageError(e, FALLBACK_TYPES.AVATAR)"
                        />
                      </router-link>
                      <small class="d-block text-truncate mt-1" :title="followedUser.following.displayName">
                        {{ followedUser.following.displayName }}
                      </small>
                    </div>
                  </div>
                </div>
                <button
                  v-if="following.length > defaultFollowingLimit"
                  class="btn btn-sm btn-outline-primary w-100 mt-2"
                  @click="showMoreFollowing = !showMoreFollowing"
                >
                  {{ showMoreFollowing ? 'Show Less' : `View More (${following.length - defaultFollowingLimit})` }}
                </button>
              </div>
              <div v-else-if="authStore.user" class="text-center text-muted">
                <i class="fas fa-user-plus fa-2x mb-2"></i>
                <p class="small">You're not following anyone yet</p>
              </div>
              <div v-else class="text-center text-muted">
                <i class="fas fa-sign-in-alt fa-2x mb-2"></i>
                <p class="small">
                  <button @click="redirectToLogin" class="btn btn-link p-0 text-decoration-none">Login</button>
                  to see who you're following
                </p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Loading Overlay -->
    <LoadingOverlay v-if="loading" />
  </div>
</template>

<script setup>
import { ref, onMounted, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/authStore'
import { socialService } from '@/services/socialService'
import LoadingOverlay from '@/components/LoadingOverlay.vue'
import { useAuthRedirect } from '@/utils/authRedirect'
import { useImageFallback, FALLBACK_TYPES } from '@/composables/useImageFallback'

const router = useRouter()
const authStore = useAuthStore()
const { redirectToLoginWithReturn } = useAuthRedirect()
const { handleImageError, getImageUrl, IMAGE_CONTEXTS } = useImageFallback()

// Data
const featuredUsers = ref([])
const popularUsers = ref([])
const users = ref([])
const following = ref([])
const currentPage = ref(1)
const totalPages = ref(0)
const totalUsers = ref(0)
const loading = ref(true)
const followingInProgress = ref({})
const showMoreFollowing = ref(false)
const defaultFollowingLimit = 20

// Computed
const displayedFollowing = computed(() => {
  if (showMoreFollowing.value) {
    return following.value
  }
  return following.value.slice(0, defaultFollowingLimit)
})

const loadFeaturedUsers = async () => {
  try {
    featuredUsers.value = await socialService.getFeaturedUsers(5)

    // Check follow status for each featured user
    if (authStore.user) {
      for (const user of featuredUsers.value) {
        try {
          const followStatus = await socialService.isUserFollowed(user.id)
          user.isFollowed = followStatus.isFollowed
        } catch (error) {
          user.isFollowed = false
        }
      }
    }
  } catch (error) {
    console.error('Error loading featured users:', error)
    featuredUsers.value = []
  }
}

const loadPopularUsers = async () => {
  try {
    popularUsers.value = await socialService.getPopularUsers(5)

    // Check follow status for each popular user
    if (authStore.user) {
      for (const user of popularUsers.value) {
        try {
          const followStatus = await socialService.isUserFollowed(user.id)
          user.isFollowed = followStatus.isFollowed
        } catch (error) {
          user.isFollowed = false
        }
      }
    }
  } catch (error) {
    console.error('Error loading popular users:', error)
    popularUsers.value = []
  }
}

const loadUsers = async (page = 1) => {
  try {
    const response = await socialService.getAllUsers(page, 20)
    users.value = response.data || []
    currentPage.value = response.page
    totalPages.value = response.totalPages
    totalUsers.value = response.totalCount

    // Check follow status for each user
    if (authStore.user) {
      for (const user of users.value) {
        try {
          const followStatus = await socialService.isUserFollowed(user.id)
          user.isFollowed = followStatus
        } catch (error) {
          user.isFollowed = false
        }
      }
    }
  } catch (error) {
    console.error('Error loading users:', error)
    users.value = []
  }
}

const loadFollowing = async () => {
  if (!authStore.user) return

  try {
    const response = await socialService.getFollowing(authStore.user.id, 1, 100)
    following.value = response || []
  } catch (error) {
    console.error('Error loading following:', error)
    following.value = []
  }
}

const toggleFollow = async (user) => {
  if (!authStore.user) {
    redirectToLoginWithReturn('Please login to follow users')
    return
  }

  followingInProgress.value[user.id] = true

  try {
    if (user.isFollowed) {
      await socialService.unfollowUser(user.id)
      user.isFollowed = false
      // Remove from following list
      following.value = following.value.filter(f => f.id !== user.id)
    } else {
      await socialService.followUser(user.id)
      user.isFollowed = true
      // Add to following list
      following.value.push(user)
    }
  } catch (error) {
    console.error('Error toggling follow:', error)
  } finally {
    followingInProgress.value[user.id] = false
  }
}

const redirectToLogin = () => {
  redirectToLoginWithReturn('Please login to access member features')
}

const loadPage = (page) => {
  if (page >= 1 && page <= totalPages.value) {
    loadUsers(page)
  }
}

const getVisiblePages = () => {
  const pages = []
  const start = Math.max(1, currentPage.value - 2)
  const end = Math.min(totalPages.value, currentPage.value + 2)

  for (let i = start; i <= end; i++) {
    pages.push(i)
  }

  return pages
}

const navigateToUserReviews = (user) => {
  router.push({
    name: 'UserReviews',
    params: { userId: user.id },
    query: { name: user.displayName }
  })
}

const navigateToUserLikes = (user) => {
  // Navigate to user profile with likes tab (implement if needed)
  router.push(`/profile/${user.id}?tab=likes`)
}

const navigateToUserLists = (user) => {
  router.push({
    name: 'UserLists',
    params: { userId: user.id },
    query: { name: user.displayName }
  })
}

// Lifecycle
onMounted(async () => {
  loading.value = true

  try {
    await Promise.all([
      loadFeaturedUsers(),
      loadPopularUsers(),
      loadUsers(),
      loadFollowing()
    ])
  } catch (error) {
    console.error('Error loading members page:', error)
  } finally {
    loading.value = false
  }
})
</script>

<style scoped>
.members-page {
  min-height: 100vh;
}

.user-card-featured {
  text-align: center;
}

.user-image-container {
  position: relative;
  display: inline-block;
}

.user-image {
  width: 80px;
  height: 80px;
  border-radius: 50%;
  object-fit: cover;
  border: 3px solid #dee2e6;
  transition: transform 0.2s ease;
}

.user-image:hover {
  transform: scale(1.05);
}

.follow-btn-overlay {
  position: absolute;
  bottom: 0;
  right: 0;
  width: 28px;
  height: 28px;
  border-radius: 50%;
  border: 2px solid white;
  background: #007bff;
  color: white;
  font-size: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: all 0.2s ease;
}

.follow-btn-overlay:hover {
  background: #0056b3;
  transform: scale(1.1);
}

.follow-btn-overlay.following {
  background: #28a745;
}

.follow-btn-overlay.following:hover {
  background: #1e7e34;
}

.user-name a {
  color: #333;
  font-weight: 600;
}

.user-name a:hover {
  color: #007bff;
}

.user-row {
  transition: background-color 0.2s ease;
}

.user-row:hover {
  background-color: #f8f9fa;
}

.user-avatar {
  width: 60px;
  height: 60px;
  border-radius: 50%;
  object-fit: cover;
  border: 2px solid #dee2e6;
}

.user-stats {
  font-size: 0.875rem;
}

.stat-item {
  color: #6c757d;
  transition: color 0.2s ease;
  cursor: pointer;
}

.stat-item:hover {
  color: #007bff;
}

.follow-btn {
  min-width: 100px;
  transition: all 0.2s ease;
}

.following-avatar {
  width: 50px;
  height: 50px;
  border-radius: 50%;
  object-fit: cover;
  border: 2px solid #dee2e6;
  transition: transform 0.2s ease;
}

.following-avatar:hover {
  transform: scale(1.05);
}

.following-user small {
  max-width: 100%;
  font-size: 0.75rem;
}

.cursor-pointer {
  cursor: pointer;
}

/* Responsive adjustments */
@media (max-width: 768px) {
  .user-image {
    width: 60px;
    height: 60px;
  }

  .user-avatar {
    width: 50px;
    height: 50px;
  }

  .follow-btn-overlay {
    width: 24px;
    height: 24px;
    font-size: 10px;
  }

  .user-stats {
    flex-direction: column;
    gap: 0.25rem !important;
  }

  .following-avatar {
    width: 40px;
    height: 40px;
  }
}
</style>