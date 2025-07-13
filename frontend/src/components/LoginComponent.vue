<template>
  <div class="auth-container">
    <div class="container">
      <div class="row justify-content-center">
        <div class="col-md-6 col-lg-5">
          <div class="card auth-card shadow-lg">
            <div class="card-body p-5">
              <!-- Header -->
              <div class="text-center mb-4">
                <i class="fas fa-shield-alt text-primary" style="font-size: 3rem;"></i>
                <h2 class="card-title text-primary mt-2">Welcome Back</h2>
                <p class="text-muted">Sign in to your account</p>
              </div>

              <!-- Error Alert -->
              <div v-if="error" class="alert alert-danger" role="alert">
                <i class="fas fa-exclamation-triangle me-2"></i>
                {{ error }}
              </div>

              <!-- Login Form -->
              <form @submit.prevent="login">
                <div class="mb-3">
                  <label for="email" class="form-label">
                    <i class="fas fa-envelope me-1"></i>
                    Email Address
                  </label>
                  <input type="email" class="form-control form-control-lg" id="email" v-model="loginForm.email"
                    placeholder="Enter your email" required :disabled="loading">
                </div>

                <div class="mb-4">
                  <label for="password" class="form-label">
                    <i class="fas fa-lock me-1"></i>
                    Password
                  </label>
                  <input type="password" class="form-control form-control-lg" id="password" v-model="loginForm.password"
                    placeholder="Enter your password" required :disabled="loading">
                </div>

                <button type="submit" class="btn btn-primary btn-lg w-100 mb-3" :disabled="loading">
                  <span v-if="loading" class="spinner-border spinner-border-sm me-2"></span>
                  <i v-else class="fas fa-sign-in-alt me-2"></i>
                  Sign In
                </button>
              </form>

              <!-- Divider -->
              <div class="divider-container mb-3">
                <div class="divider-line"></div>
                <span class="divider-text">or continue with</span>
                <div class="divider-line"></div>
              </div>

              <!-- Social Login Buttons -->
              <div class="d-grid gap-2 mb-4">
                <button class="btn btn-outline-danger btn-lg social-btn" @click="loginWithGoogle" :disabled="loading">
                  <i class="fab fa-google me-2"></i>
                  Continue with Google
                </button>

                <button class="btn btn-outline-primary btn-lg social-btn" @click="loginWithFacebook"
                  :disabled="loading">
                  <i class="fab fa-facebook-f me-2"></i>
                  Continue with Facebook
                </button>
              </div>

              <!-- Register Link -->
              <div class="text-center">
                <p class="mb-0">
                  Don't have an account?
                  <router-link to="/register" class="text-primary text-decoration-none fw-bold">
                    Create one here
                  </router-link>
                </p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/authStore'
import { useProfileStore } from '@/stores/profileStore'
import { useToast } from 'vue-toastification'

const router = useRouter()
const route = useRoute()
const authStore = useAuthStore()
const profileStore = useProfileStore()
const toast = useToast()

const loginForm = ref({
  email: '',
  password: ''
})
const loading = ref(false)
const error = ref('')

const login = async () => {
  loading.value = true
  error.value = ''

  try {
    const response = await authStore.login(loginForm.value)

    if (response.success) {
      const profileResponse = await profileStore.fetchProfile();
      if (profileResponse) {
        toast.success('Login successful!')

        let isProfileUpdated = profileResponse.isProfileUpdated

        if (isProfileUpdated !== null && !isProfileUpdated) {
          // Redirect to profile update page if profile is not updated
          router.push('/complete-profile')
          return
        }
        // Redirect to intended route or dashboard
        const redirect = route.query.redirect || '/dashboard'
        router.push(redirect)
      } else {
        error.value = profileResponse.message || 'Failed to fetch user profile'
        toast.error(error.value)
        return
      }
    } else {
      error.value = response.message
    }
  } catch (err) {
    error.value = err.message || 'An error occurred during login'
    toast.error(error.value)
  } finally {
    loading.value = false
  }
}
const loginWithGoogle = async () => {
  // Google Sign-In implementation would go here
  // This requires the Google Sign-In JavaScript library
  toast.info('Google Sign-In integration coming soon!')
}
const loginWithFacebook = async () => {
  // Facebook Login implementation would go here
  // This requires the Facebook SDK for JavaScript
  toast.info('Facebook Login integration coming soon!')
}
</script>

<style scoped>
.auth-container {
  min-height: 100vh;
  display: flex;
  align-items: center;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  padding: 2rem 0;
}

.auth-card {
  border: none;
  border-radius: 1rem;
  backdrop-filter: blur(10px);
  background-color: rgba(255, 255, 255, 0.95);
}

.divider-container {
  display: flex;
  align-items: center;
  text-align: center;
}

.divider-line {
  flex: 1;
  height: 1px;
  background-color: #dee2e6;
}

.divider-text {
  padding: 0 1rem;
  color: #6c757d;
  font-size: 0.875rem;
  background-color: white;
}

.social-btn {
  transition: all 0.3s ease;
}

.social-btn:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);
}

.form-control:focus {
  border-color: #667eea;
  box-shadow: 0 0 0 0.2rem rgba(102, 126, 234, 0.25);
}

.btn-primary {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  border: none;
}

.btn-primary:hover {
  background: linear-gradient(135deg, #5a6fd8 0%, #6a4190 100%);
  transform: translateY(-1px);
}
</style>