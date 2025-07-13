<template>
  <div class="dashboard-container">
    <div class="container-fluid">
      <!-- Welcome Header -->
      <div class="row mb-4">
        <div class="col-12">
          <div class="welcome-banner bg-primary text-white rounded-3 p-4">
            <div class="row align-items-center">
              <div class="col-md-8">
                <h1 class="h2 mb-2">
                  <i class="fas fa-home me-2"></i>
                  Welcome back, {{ userDisplayName }}!
                </h1>
                <p class="mb-0 opacity-75">
                  You're successfully authenticated and ready to explore.
                </p>
              </div>
              <div class="col-md-4 text-end">
                <div class="badge bg-light text-primary fs-6 px-3 py-2">
                  <i class="fas fa-shield-check me-1"></i>
                  Authenticated via {{ formatProvider(user?.provider) }}
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Stats Cards -->
      <div class="row mb-4">
        <div class="col-md-3 mb-3">
          <div class="card stats-card h-100">
            <div class="card-body text-center">
              <i class="fas fa-user-circle text-primary mb-3" style="font-size: 2.5rem;"></i>
              <h5 class="card-title">Profile Status</h5>
              <p class="text-success">
                <i class="fas fa-check-circle me-1"></i>
                Active
              </p>
            </div>
          </div>
        </div>

        <div class="col-md-3 mb-3">
          <div class="card stats-card h-100">
            <div class="card-body text-center">
              <i class="fas fa-calendar text-info mb-3" style="font-size: 2.5rem;"></i>
              <h5 class="card-title">Member Since</h5>
              <p class="text-muted">{{ formatDate(user?.createdAt) }}</p>
            </div>
          </div>
        </div>

        <div class="col-md-3 mb-3">
          <div class="card stats-card h-100">
            <div class="card-body text-center">
              <i class="fas fa-shield-alt text-success mb-3" style="font-size: 2.5rem;"></i>
              <h5 class="card-title">Security</h5>
              <p class="text-success">
                <i class="fas fa-lock me-1"></i>
                Secure
              </p>
            </div>
          </div>
        </div>

        <div class="col-md-3 mb-3">
          <div class="card stats-card h-100">
            <div class="card-body text-center">
              <i class="fas fa-bell text-warning mb-3" style="font-size: 2.5rem;"></i>
              <h5 class="card-title">Notifications</h5>
              <p class="text-muted">
                <span class="badge bg-warning">0</span>
                New
              </p>
            </div>
          </div>
        </div>
      </div>

      <!-- Main Content -->
      <div class="row">
        <!-- User Info Card -->
        <div class="col-md-6 mb-4">
          <div class="card h-100">
            <div class="card-header bg-light">
              <h5 class="card-title mb-0">
                <i class="fas fa-id-card me-2"></i>
                User Information
              </h5>
            </div>
            <div class="card-body">
              <div class="row">
                <div class="col-sm-4">
                  <strong>Name:</strong>
                </div>
                <div class="col-sm-8">
                  {{ user?.firstName }} {{ user?.lastName }}
                </div>
              </div>
              <hr>
              <div class="row">
                <div class="col-sm-4">
                  <strong>Email:</strong>
                </div>
                <div class="col-sm-8">
                  {{ user?.email }}
                </div>
              </div>
              <hr>
              <div class="row">
                <div class="col-sm-4">
                  <strong>Provider:</strong>
                </div>
                <div class="col-sm-8">
                  <span class="badge bg-primary">{{ formatProvider(user?.provider) }}</span>
                </div>
              </div>
              <hr>
              <div class="row">
                <div class="col-sm-4">
                  <strong>User ID:</strong>
                </div>
                <div class="col-sm-8">
                  <code class="text-muted">{{ user?.id }}</code>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Quick Actions Card -->
        <div class="col-md-6 mb-4">
          <div class="card h-100">
            <div class="card-header bg-light">
              <h5 class="card-title mb-0">
                <i class="fas fa-bolt me-2"></i>
                Quick Actions
              </h5>
            </div>
            <div class="card-body">
              <div class="d-grid gap-2">
                <button class="btn btn-outline-primary" @click="viewProfile">
                  <i class="fas fa-user me-2"></i>
                  View Profile
                </button>
                <button class="btn btn-outline-info" @click="updateProfile">
                  <i class="fas fa-edit me-2"></i>
                  Update Profile
                </button>
                <button class="btn btn-outline-warning" @click="changePassword">
                  <i class="fas fa-key me-2"></i>
                  Change Password
                </button>
                <button class="btn btn-outline-danger" @click="logout">
                  <i class="fas fa-sign-out-alt me-2"></i>
                  Logout
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Recent Activity -->
      <div class="row">
        <div class="col-12">
          <div class="card">
            <div class="card-header bg-light">
              <h5 class="card-title mb-0">
                <i class="fas fa-history me-2"></i>
                Recent Activity
              </h5>
            </div>
            <div class="card-body">
              <div class="list-group list-group-flush">
                <div class="list-group-item d-flex justify-content-between align-items-center">
                  <div>
                    <i class="fas fa-sign-in-alt text-success me-2"></i>
                    Successfully logged in
                  </div>
                  <small class="text-muted">Just now</small>
                </div>
                <div class="list-group-item d-flex justify-content-between align-items-center">
                  <div>
                    <i class="fas fa-user-check text-info me-2"></i>
                    Account verified
                  </div>
                  <small class="text-muted">{{ formatDate(user?.createdAt) }}</small>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import {computed} from 'vue'
import { useRouter } from 'vue-router'
import { useToast } from 'vue-toastification'
import { useAuthStore } from '@/stores/authStore'
import { formatUserName, formatDate, formatProvider } from '@/utils/authUtils'

const router = useRouter()
const toast = useToast()
const authStore = useAuthStore()

// get user state from auth store
const user = computed(() => authStore.user)

// display name for the user
const userDisplayName = computed(() => formatUserName(user.value))

const viewProfile = () => {
    toast.info('Profile update feature coming soon!')
    //router.push('/profile')
}
const updateProfile = () => {
    toast.info('Redirecting to update profile...')
    // Redirect to update profile page
    // router.push({ name: 'UpdateProfile' })
}
const changePassword = () => {
    toast.info('Change password feature coming soon!')
    //router.push('/change-password')
}
const logout = () => {
    authStore.logout()
    toast.success('You have been logged out successfully!')
    router.push('/login')
}
</script>

<style scoped>
.dashboard-container {
  padding: 2rem 0;
  min-height: calc(100vh - 56px);
}

.welcome-banner {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%) !important;
}

.stats-card {
  transition: transform 0.2s;
}

.stats-card:hover {
  transform: translateY(-5px);
}

.card {
  border: none;
  box-shadow: 0 0.125rem 0.25rem rgba(0, 0, 0, 0.075);
  border-radius: 0.5rem;
}

.card-header {
  border-bottom: 1px solid rgba(0,0,0,.125);
  border-radius: 0.5rem 0.5rem 0 0 !important;
}

.badge {
  font-size: 0.75rem;
}

code {
  font-size: 0.875rem;
  word-break: break-all;
}

.list-group-item {
  border: none;
  border-bottom: 1px solid rgba(0,0,0,.125);
  padding: 1rem 0;
}

.list-group-item:last-child {
  border-bottom: none;
}
</style>
