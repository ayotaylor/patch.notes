<template>
  <div class="auth-container">
    <div class="container">
      <div class="row justify-content-center">
        <div class="col-md-8 col-lg-6">
          <div class="card auth-card shadow-lg">
            <div class="card-body p-5">
              <!-- Header -->
              <div class="text-center mb-4">
                <i class="fas fa-user-plus text-primary" style="font-size: 3rem;"></i>
                <h2 class="card-title text-primary mt-2">Create Account</h2>
                <p class="text-muted">Join us today and get started</p>
              </div>

              <!-- Error Alert -->
              <div v-if="error" class="alert alert-danger" role="alert">
                <i class="fas fa-exclamation-triangle me-2"></i>
                {{ error }}
              </div>

              <!-- Registration Form -->
              <form @submit.prevent="register">
                <div class="row">
                  <div class="col-md-6 mb-3">
                    <label for="firstName" class="form-label">
                      <i class="fas fa-user me-1"></i>
                      First Name
                    </label>
                    <input
                      type="text"
                      class="form-control form-control-lg"
                      id="firstName"
                      v-model="registerForm.firstName"
                      placeholder="John"
                      required
                      :disabled="loading"
                    >
                  </div>

                  <div class="col-md-6 mb-3">
                    <label for="lastName" class="form-label">
                      <i class="fas fa-user me-1"></i>
                      Last Name
                    </label>
                    <input
                      type="text"
                      class="form-control form-control-lg"
                      id="lastName"
                      v-model="registerForm.lastName"
                      placeholder="Doe"
                      required
                      :disabled="loading"
                    >
                  </div>
                </div>

                <div class="mb-3">
                  <label for="email" class="form-label">
                    <i class="fas fa-envelope me-1"></i>
                    Email Address
                  </label>
                  <input
                    type="email"
                    class="form-control form-control-lg"
                    id="email"
                    v-model="registerForm.email"
                    placeholder="john.doe@example.com"
                    required
                    :disabled="loading"
                  >
                </div>

                <div class="mb-3">
                  <label for="password" class="form-label">
                    <i class="fas fa-lock me-1"></i>
                    Password
                  </label>
                  <input
                    type="password"
                    class="form-control form-control-lg"
                    id="password"
                    v-model="registerForm.password"
                    placeholder="Enter a strong password"
                    required
                    :disabled="loading"
                  >
                  <div class="form-text">
                    Password must be at least 6 characters long with uppercase, lowercase, and digit.
                  </div>
                </div>

                <div class="mb-4">
                  <label for="confirmPassword" class="form-label">
                    <i class="fas fa-lock me-1"></i>
                    Confirm Password
                  </label>
                  <input
                    type="password"
                    class="form-control form-control-lg"
                    id="confirmPassword"
                    v-model="registerForm.confirmPassword"
                    placeholder="Confirm your password"
                    required
                    :disabled="loading"
                  >
                </div>

                <button
                  type="submit"
                  class="btn btn-primary btn-lg w-100 mb-3"
                  :disabled="loading || !isFormValid"
                >
                  <span v-if="loading" class="spinner-border spinner-border-sm me-2"></span>
                  <i v-else class="fas fa-user-plus me-2"></i>
                  Create Account
                </button>
              </form>

              <!-- Divider -->
              <div class="divider-container mb-3">
                <div class="divider-line"></div>
                <span class="divider-text">or sign up with</span>
                <div class="divider-line"></div>
              </div>

              <!-- Social Registration Buttons -->
              <div class="d-grid gap-2 mb-4">
                <button
                  class="btn btn-outline-danger btn-lg social-btn"
                  @click="signUpWithGoogle"
                  :disabled="loading"
                >
                  <i class="fab fa-google me-2"></i>
                  Continue with Google
                </button>

                <button
                  class="btn btn-outline-primary btn-lg social-btn"
                  @click="signUpWithFacebook"
                  :disabled="loading"
                >
                  <i class="fab fa-facebook-f me-2"></i>
                  Continue with Facebook
                </button>
              </div>

              <!-- Login Link -->
              <div class="text-center">
                <p class="mb-0">
                  Already have an account?
                  <router-link to="/login" class="text-primary text-decoration-none fw-bold">
                    Sign in here
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
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/authStore'
import { useToast } from 'vue-toastification'

const router = useRouter()
const authStore = useAuthStore()
const toast = useToast()

const registerForm = ref({
  firstName: '',
  lastName: '',
  email: '',
  password: '',
  confirmPassword: ''
})

const loading = ref(false)
const error = ref('')

// TODO: maybe move validation logic to a separate function or store
const validationError = computed(() => {
  const { firstName, lastName, email, password, confirmPassword } = registerForm.value
  if (!firstName || !lastName || !email || !password || !confirmPassword) {
    return 'All fields are required'
  }
  if (password.length < 6) {
    return 'Password must be at least 6 characters'
  }
  if (password !== confirmPassword) {
    return 'Passwords do not match'
  }
  return ''
})

const isFormValid = computed(() => !validationError.value)

const register = async () => {
  if (!isFormValid.value) return

  loading.value = true
  error.value = ''

  if (registerForm.value.password !== registerForm.value.confirmPassword) {
    error.value = 'Passwords do not match'
    loading.value = false
    return
  }

  try {
    const response = await authStore.register(registerForm.value)

    if (response.success) {
      toast.success('Registration successful! Redirecting to dashboard...')
      router.push('/dashboard')
    } else {
      error.value = response.message || 'Registration failed'
    }
  } catch (err) {
    error.value = err.message || 'An error occurred during registration'
    toast.error(error.value)
  } finally {
    loading.value = false
  }
}

const signUpWithGoogle = async () => {
  loading.value = true
  error.value = ''

  try {
    const response = await authStore.signUpWithGoogle()
    if (response.success) {
      //authStore.setAuthData(response.token, response.user)
      toast.success('Google registration successful! Redirecting to dashboard...')
      router.push('/dashboard')
    } else {
      error.value = response.message || 'Google registration failed'
    }
  } catch (err) {
    error.value = err.message || 'An error occurred during Google registration'
    toast.error(error.value)
  } finally {
    loading.value = false
  }
}

const signUpWithFacebook = async () => {
  loading.value = true
  error.value = ''

  try {
    const response = await authStore.signUpWithFacebook()
    if (response.success) {
      //authStore.setAuthData(response.token, response.user)
      toast.success('Facebook registration successful! Redirecting to dashboard...')
      router.push('/dashboard')
    } else {
      error.value = response.message || 'Facebook registration failed'
    }
  } catch (err) {
    error.value = err.message || 'An error occurred during Facebook registration'
    toast.error(error.value)
  } finally {
    loading.value = false
  }
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
  box-shadow: 0 4px 8px rgba(0,0,0,0.1);
}

.form-control:focus {
  border-color: #667eea;
  box-shadow: 0 0 0 0.2rem rgba(102, 126, 234, 0.25);
}

.btn-primary {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  border: none;
}

.btn-primary:hover:not(:disabled) {
  background: linear-gradient(135deg, #5a6fd8 0%, #6a4190 100%);
  transform: translateY(-1px);
}

.btn-primary:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}
</style>