<template>
  <div class="min-h-screen flex items-center justify-center bg-theme-bg-primary dark:bg-theme-bg-primary-dark transition-colors duration-200 py-8 px-4">
    <div class="w-full max-w-2xl">
      <div class="bg-theme-bg-secondary dark:bg-theme-bg-secondary-dark rounded-xl shadow-lg overflow-hidden transition-colors duration-200">
        <div class="p-8">
          <!-- Header -->
          <div class="text-center mb-6">
            <div class="flex justify-center mb-3">
              <svg class="w-12 h-12 text-theme-text-primary dark:text-theme-text-primary-dark" fill="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                <path d="M15 12c2.21 0 4-1.79 4-4s-1.79-4-4-4-4 1.79-4 4 1.79 4 4 4zm-9-2V7H4v3H1v2h3v3h2v-3h3v-2H6zm9 4c-2.67 0-8 1.34-8 4v2h16v-2c0-2.66-5.33-4-8-4z"/>
              </svg>
            </div>
            <h2 class="font-newsreader text-3xl font-bold text-theme-text-primary dark:text-theme-text-primary-dark">Create Account</h2>
            <p class="font-tinos text-base text-theme-text-secondary dark:text-theme-text-secondary-dark mt-2">Join us today and get started</p>
          </div>

          <!-- Error Alert -->
          <div v-if="error" class="mb-4 p-4 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg">
            <div class="flex items-center">
              <svg class="w-5 h-5 text-red-600 dark:text-red-400 mr-2" fill="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                <path d="M12 2L1 21h22L12 2zm0 3.83L19.53 19H4.47L12 5.83zM11 16h2v2h-2v-2zm0-6h2v4h-2v-4z"/>
              </svg>
              <span class="font-tinos text-sm text-red-800 dark:text-red-300">{{ error }}</span>
            </div>
          </div>

          <!-- Registration Form -->
          <form @submit.prevent="register">
            <div class="grid grid-cols-1 md:grid-cols-2 gap-4 mb-4">
              <!-- First Name -->
              <div>
                <label for="firstName" class="block font-tinos text-sm font-medium text-theme-text-primary dark:text-theme-text-primary-dark mb-2">
                  <svg class="w-4 h-4 inline mr-1" fill="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                    <path d="M12 12c2.21 0 4-1.79 4-4s-1.79-4-4-4-4 1.79-4 4 1.79 4 4 4zm0 2c-2.67 0-8 1.34-8 4v2h16v-2c0-2.66-5.33-4-8-4z"/>
                  </svg>
                  First Name
                </label>
                <input
                  type="text"
                  id="firstName"
                  v-model="registerForm.firstName"
                  placeholder="John"
                  required
                  :disabled="loading"
                  class="w-full px-4 py-3 font-tinos text-base bg-theme-bg-primary dark:bg-theme-bg-primary-dark text-theme-text-primary dark:text-theme-text-primary-dark border border-theme-border dark:border-theme-border-dark rounded-lg focus:outline-none focus:ring-2 focus:ring-theme-btn-primary dark:focus:ring-theme-btn-primary-dark focus:border-transparent transition-colors duration-200 disabled:opacity-50 disabled:cursor-not-allowed"
                >
              </div>

              <!-- Last Name -->
              <div>
                <label for="lastName" class="block font-tinos text-sm font-medium text-theme-text-primary dark:text-theme-text-primary-dark mb-2">
                  <svg class="w-4 h-4 inline mr-1" fill="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                    <path d="M12 12c2.21 0 4-1.79 4-4s-1.79-4-4-4-4 1.79-4 4 1.79 4 4 4zm0 2c-2.67 0-8 1.34-8 4v2h16v-2c0-2.66-5.33-4-8-4z"/>
                  </svg>
                  Last Name
                </label>
                <input
                  type="text"
                  id="lastName"
                  v-model="registerForm.lastName"
                  placeholder="Doe"
                  required
                  :disabled="loading"
                  class="w-full px-4 py-3 font-tinos text-base bg-theme-bg-primary dark:bg-theme-bg-primary-dark text-theme-text-primary dark:text-theme-text-primary-dark border border-theme-border dark:border-theme-border-dark rounded-lg focus:outline-none focus:ring-2 focus:ring-theme-btn-primary dark:focus:ring-theme-btn-primary-dark focus:border-transparent transition-colors duration-200 disabled:opacity-50 disabled:cursor-not-allowed"
                >
              </div>
            </div>

            <!-- Email -->
            <div class="mb-4">
              <label for="email" class="block font-tinos text-sm font-medium text-theme-text-primary dark:text-theme-text-primary-dark mb-2">
                <svg class="w-4 h-4 inline mr-1" fill="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                  <path d="M20 4H4c-1.1 0-1.99.9-1.99 2L2 18c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V6c0-1.1-.9-2-2-2zm0 4l-8 5-8-5V6l8 5 8-5v2z"/>
                </svg>
                Email Address
              </label>
              <input
                type="email"
                id="email"
                v-model="registerForm.email"
                placeholder="john.doe@example.com"
                required
                :disabled="loading"
                class="w-full px-4 py-3 font-tinos text-base bg-theme-bg-primary dark:bg-theme-bg-primary-dark text-theme-text-primary dark:text-theme-text-primary-dark border border-theme-border dark:border-theme-border-dark rounded-lg focus:outline-none focus:ring-2 focus:ring-theme-btn-primary dark:focus:ring-theme-btn-primary-dark focus:border-transparent transition-colors duration-200 disabled:opacity-50 disabled:cursor-not-allowed"
              >
            </div>

            <!-- Password -->
            <div class="mb-4">
              <label for="password" class="block font-tinos text-sm font-medium text-theme-text-primary dark:text-theme-text-primary-dark mb-2">
                <svg class="w-4 h-4 inline mr-1" fill="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                  <path d="M18 8h-1V6c0-2.76-2.24-5-5-5S7 3.24 7 6v2H6c-1.1 0-2 .9-2 2v10c0 1.1.9 2 2 2h12c1.1 0 2-.9 2-2V10c0-1.1-.9-2-2-2zm-6 9c-1.1 0-2-.9-2-2s.9-2 2-2 2 .9 2 2-.9 2-2 2zm3.1-9H8.9V6c0-1.71 1.39-3.1 3.1-3.1 1.71 0 3.1 1.39 3.1 3.1v2z"/>
                </svg>
                Password
              </label>
              <input
                type="password"
                id="password"
                v-model="registerForm.password"
                placeholder="Enter a strong password"
                required
                :disabled="loading"
                class="w-full px-4 py-3 font-tinos text-base bg-theme-bg-primary dark:bg-theme-bg-primary-dark text-theme-text-primary dark:text-theme-text-primary-dark border border-theme-border dark:border-theme-border-dark rounded-lg focus:outline-none focus:ring-2 focus:ring-theme-btn-primary dark:focus:ring-theme-btn-primary-dark focus:border-transparent transition-colors duration-200 disabled:opacity-50 disabled:cursor-not-allowed"
              >
              <p class="mt-2 font-tinos text-xs text-theme-text-secondary dark:text-theme-text-secondary-dark">
                Password must be at least 6 characters long with uppercase, lowercase, and digit.
              </p>
            </div>

            <!-- Confirm Password -->
            <div class="mb-6">
              <label for="confirmPassword" class="block font-tinos text-sm font-medium text-theme-text-primary dark:text-theme-text-primary-dark mb-2">
                <svg class="w-4 h-4 inline mr-1" fill="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                  <path d="M18 8h-1V6c0-2.76-2.24-5-5-5S7 3.24 7 6v2H6c-1.1 0-2 .9-2 2v10c0 1.1.9 2 2 2h12c1.1 0 2-.9 2-2V10c0-1.1-.9-2-2-2zm-6 9c-1.1 0-2-.9-2-2s.9-2 2-2 2 .9 2 2-.9 2-2 2zm3.1-9H8.9V6c0-1.71 1.39-3.1 3.1-3.1 1.71 0 3.1 1.39 3.1 3.1v2z"/>
                </svg>
                Confirm Password
              </label>
              <input
                type="password"
                id="confirmPassword"
                v-model="registerForm.confirmPassword"
                placeholder="Confirm your password"
                required
                :disabled="loading"
                class="w-full px-4 py-3 font-tinos text-base bg-theme-bg-primary dark:bg-theme-bg-primary-dark text-theme-text-primary dark:text-theme-text-primary-dark border border-theme-border dark:border-theme-border-dark rounded-lg focus:outline-none focus:ring-2 focus:ring-theme-btn-primary dark:focus:ring-theme-btn-primary-dark focus:border-transparent transition-colors duration-200 disabled:opacity-50 disabled:cursor-not-allowed"
              >
            </div>

            <FormButton
              title="Create Account"
              :disabled="!isFormValid"
              :loading="loading"
              icon-size="md"
            >
              <template #icon-left>
                <svg fill="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                  <path d="M15 12c2.21 0 4-1.79 4-4s-1.79-4-4-4-4 1.79-4 4 1.79 4 4 4zm-9-2V7H4v3H1v2h3v3h2v-3h3v-2H6zm9 4c-2.67 0-8 1.34-8 4v2h16v-2c0-2.66-5.33-4-8-4z"/>
                </svg>
              </template>
            </FormButton>
          </form>

          <!-- Divider -->
          <div class="flex items-center my-6">
            <div class="flex-1 h-px bg-theme-border dark:bg-theme-border-dark"></div>
            <span class="px-4 font-tinos text-sm text-theme-text-secondary dark:text-theme-text-secondary-dark">or sign up with</span>
            <div class="flex-1 h-px bg-theme-border dark:bg-theme-border-dark"></div>
          </div>

          <!-- Social Registration Buttons -->
          <div class="space-y-3 mb-6">
            <button
              @click="signUpWithGoogle"
              :disabled="loading"
              class="w-full px-6 py-3 bg-theme-bg-primary dark:bg-theme-bg-primary-dark text-theme-text-primary dark:text-theme-text-primary-dark border border-red-500 dark:border-red-600 font-tinos text-base rounded-lg hover:bg-red-50 dark:hover:bg-red-900/20 focus:outline-none focus:ring-2 focus:ring-red-500 transition-all duration-200 disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center"
            >
              <svg class="w-5 h-5 mr-2" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                <path fill="#4285F4" d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"/>
                <path fill="#34A853" d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"/>
                <path fill="#FBBC05" d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z"/>
                <path fill="#EA4335" d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z"/>
              </svg>
              Continue with Google
            </button>

            <button
              @click="signUpWithFacebook"
              :disabled="loading"
              class="w-full px-6 py-3 bg-theme-bg-primary dark:bg-theme-bg-primary-dark text-theme-text-primary dark:text-theme-text-primary-dark border border-blue-600 dark:border-blue-700 font-tinos text-base rounded-lg hover:bg-blue-50 dark:hover:bg-blue-900/20 focus:outline-none focus:ring-2 focus:ring-blue-600 transition-all duration-200 disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center"
            >
              <svg class="w-5 h-5 mr-2" fill="#1877F2" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                <path d="M24 12.073c0-6.627-5.373-12-12-12s-12 5.373-12 12c0 5.99 4.388 10.954 10.125 11.854v-8.385H7.078v-3.47h3.047V9.43c0-3.007 1.792-4.669 4.533-4.669 1.312 0 2.686.235 2.686.235v2.953H15.83c-1.491 0-1.956.925-1.956 1.874v2.25h3.328l-.532 3.47h-2.796v8.385C19.612 23.027 24 18.062 24 12.073z"/>
              </svg>
              Continue with Facebook
            </button>
          </div>

          <!-- Login Link -->
          <div class="text-center">
            <p class="font-tinos text-sm text-theme-text-secondary dark:text-theme-text-secondary-dark">
              Already have an account?
              <router-link to="/login" class="font-bold text-theme-text-primary dark:text-theme-text-primary-dark hover:underline ml-1">
                Sign in here
              </router-link>
            </p>
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
import FormButton from '@/components/home/buttons/FormButton.vue'

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
      router.push('/complete-profile');
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
/* Additional animations can be added here if needed */
</style>
