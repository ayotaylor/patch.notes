<template>
  <div class="min-h-screen bg-theme-bg-primary dark:bg-theme-bg-primary-dark transition-colors duration-200 py-8 px-4">
    <div class="max-w-3xl mx-auto">
      <div class="bg-theme-bg-secondary dark:bg-theme-bg-secondary-dark rounded-xl shadow-lg overflow-hidden transition-colors duration-200">
        <!-- Header -->
        <div class="bg-theme-btn-primary dark:bg-theme-btn-primary-dark text-white py-6 px-8 text-center">
          <div class="flex justify-center mb-3">
            <svg class="w-10 h-10" fill="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
              <path d="M3 17.25V21h3.75L17.81 9.94l-3.75-3.75L3 17.25zM20.71 7.04c.39-.39.39-1.02 0-1.41l-2.34-2.34c-.39-.39-1.02-.39-1.41 0l-1.83 1.83 3.75 3.75 1.83-1.83z"/>
            </svg>
          </div>
          <h2 class="font-newsreader text-2xl font-bold mb-1">Complete Your Profile</h2>
          <p class="font-tinos text-sm opacity-90">Help others get to know you better</p>
        </div>

        <div class="p-8">
          <!-- Skip Notice -->
          <div class="mb-6 p-4 bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg">
            <div class="flex items-start">
              <svg class="w-5 h-5 text-blue-600 dark:text-blue-400 mr-3 mt-0.5 flex-shrink-0" fill="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 15h-2v-2h2v2zm0-4h-2V7h2v6z"/>
              </svg>
              <div>
                <strong class="font-tinos text-sm font-semibold text-blue-900 dark:text-blue-300">Optional Step:</strong>
                <span class="font-tinos text-sm text-blue-800 dark:text-blue-300 ml-1">You can complete this later or skip to your dashboard.</span>
              </div>
            </div>
          </div>

          <form @submit.prevent="handleSubmit">
            <!-- Profile Image Section -->
            <div class="text-center mb-8">
              <div class="inline-block relative">
                <img
                  :src="profileImagePreview || defaultAvatar"
                  alt="Profile preview"
                  class="w-32 h-32 rounded-full border-4 border-theme-border dark:border-theme-border-dark shadow-lg object-cover"
                >
                <label
                  for="profileImage"
                  class="absolute bottom-0 right-0 w-10 h-10 bg-theme-btn-primary dark:bg-theme-btn-primary-dark text-white rounded-full flex items-center justify-center cursor-pointer hover:opacity-90 transition-opacity shadow-lg"
                >
                  <svg class="w-5 h-5" fill="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                    <path d="M12 12.75c1.63 0 3.07.39 4.24.9 1.08.48 1.76 1.56 1.76 2.73V18H6v-1.61c0-1.18.68-2.26 1.76-2.73 1.17-.52 2.61-.91 4.24-.91zM4 13h3v-2.5H4V8l-3 4 3 4v-3zm16-3h-3v2.5h3V15l3-4-3-4v3zm-8-7c-1.66 0-3 1.34-3 3s1.34 3 3 3 3-1.34 3-3-1.34-3-3-3z"/>
                  </svg>
                </label>
                <input
                  id="profileImage"
                  ref="fileInput"
                  type="file"
                  accept="image/*"
                  @change="handleImageUpload"
                  class="hidden"
                >
              </div>
              <p class="mt-3 font-tinos text-sm text-theme-text-secondary dark:text-theme-text-secondary-dark">
                Click the camera icon to upload a photo
              </p>
            </div>

            <!-- Form Fields -->
            <div class="space-y-6">
              <!-- Display Name -->
              <div>
                <label for="displayName" class="block font-tinos text-sm font-medium text-theme-text-primary dark:text-theme-text-primary-dark mb-2">
                  <svg class="w-4 h-4 inline mr-1" fill="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                    <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm0 3c1.66 0 3 1.34 3 3s-1.34 3-3 3-3-1.34-3-3 1.34-3 3-3zm0 14.2c-2.5 0-4.71-1.28-6-3.22.03-1.99 4-3.08 6-3.08 1.99 0 5.97 1.09 6 3.08-1.29 1.94-3.5 3.22-6 3.22z"/>
                  </svg>
                  Display Name
                </label>
                <input
                  id="displayName"
                  v-model="form.displayName"
                  type="text"
                  :class="{ 'border-red-500 dark:border-red-600': errors.displayName }"
                  class="w-full px-4 py-3 font-tinos text-base bg-theme-bg-primary dark:bg-theme-bg-primary-dark text-theme-text-primary dark:text-theme-text-primary-dark border border-theme-border dark:border-theme-border-dark rounded-lg focus:outline-none focus:ring-2 focus:ring-theme-btn-primary dark:focus:ring-theme-btn-primary-dark focus:border-transparent transition-colors duration-200"
                  placeholder="How would you like to be known?"
                  maxlength="100"
                >
                <p class="mt-1 font-tinos text-xs text-theme-text-secondary dark:text-theme-text-secondary-dark">
                  This is how your name will appear to other users
                </p>
                <p v-if="errors.displayName" class="mt-1 font-tinos text-xs text-red-600 dark:text-red-400">
                  {{ errors.displayName }}
                </p>
              </div>

              <!-- Phone Number and Date of Birth -->
              <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
                <!-- Phone Number -->
                <div>
                  <label for="phoneNumber" class="block font-tinos text-sm font-medium text-theme-text-primary dark:text-theme-text-primary-dark mb-2">
                    <svg class="w-4 h-4 inline mr-1" fill="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                      <path d="M6.62 10.79c1.44 2.83 3.76 5.14 6.59 6.59l2.2-2.2c.27-.27.67-.36 1.02-.24 1.12.37 2.33.57 3.57.57.55 0 1 .45 1 1V20c0 .55-.45 1-1 1-9.39 0-17-7.61-17-17 0-.55.45-1 1-1h3.5c.55 0 1 .45 1 1 0 1.25.2 2.45.57 3.57.11.35.03.74-.25 1.02l-2.2 2.2z"/>
                    </svg>
                    Phone Number
                  </label>
                  <input
                    id="phoneNumber"
                    v-model="form.phoneNumber"
                    type="tel"
                    :class="{ 'border-red-500 dark:border-red-600': errors.phoneNumber }"
                    class="w-full px-4 py-3 font-tinos text-base bg-theme-bg-primary dark:bg-theme-bg-primary-dark text-theme-text-primary dark:text-theme-text-primary-dark border border-theme-border dark:border-theme-border-dark rounded-lg focus:outline-none focus:ring-2 focus:ring-theme-btn-primary dark:focus:ring-theme-btn-primary-dark focus:border-transparent transition-colors duration-200"
                    placeholder="(555) 123-4567"
                    maxlength="20"
                  >
                  <p v-if="errors.phoneNumber" class="mt-1 font-tinos text-xs text-red-600 dark:text-red-400">
                    {{ errors.phoneNumber }}
                  </p>
                </div>

                <!-- Date of Birth -->
                <div>
                  <label for="dateOfBirth" class="block font-tinos text-sm font-medium text-theme-text-primary dark:text-theme-text-primary-dark mb-2">
                    <svg class="w-4 h-4 inline mr-1" fill="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                      <path d="M19 3h-1V1h-2v2H8V1H6v2H5c-1.11 0-1.99.9-1.99 2L3 19c0 1.1.89 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm0 16H5V8h14v11zM7 10h5v5H7z"/>
                    </svg>
                    Date of Birth
                  </label>
                  <input
                    id="dateOfBirth"
                    v-model="form.dateOfBirth"
                    type="date"
                    :max="maxDate"
                    :class="{ 'border-red-500 dark:border-red-600': errors.dateOfBirth }"
                    class="w-full px-4 py-3 font-tinos text-base bg-theme-bg-primary dark:bg-theme-bg-primary-dark text-theme-text-primary dark:text-theme-text-primary-dark border border-theme-border dark:border-theme-border-dark rounded-lg focus:outline-none focus:ring-2 focus:ring-theme-btn-primary dark:focus:ring-theme-btn-primary-dark focus:border-transparent transition-colors duration-200"
                  >
                  <p v-if="errors.dateOfBirth" class="mt-1 font-tinos text-xs text-red-600 dark:text-red-400">
                    {{ errors.dateOfBirth }}
                  </p>
                </div>
              </div>

              <!-- Bio -->
              <div>
                <label for="bio" class="block font-tinos text-sm font-medium text-theme-text-primary dark:text-theme-text-primary-dark mb-2">
                  <svg class="w-4 h-4 inline mr-1" fill="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                    <path d="M3 17.25V21h3.75L17.81 9.94l-3.75-3.75L3 17.25zM20.71 7.04c.39-.39.39-1.02 0-1.41l-2.34-2.34c-.39-.39-1.02-.39-1.41 0l-1.83 1.83 3.75 3.75 1.83-1.83z"/>
                  </svg>
                  Bio
                </label>
                <textarea
                  id="bio"
                  v-model="form.bio"
                  :class="{ 'border-red-500 dark:border-red-600': errors.bio }"
                  class="w-full px-4 py-3 font-tinos text-base bg-theme-bg-primary dark:bg-theme-bg-primary-dark text-theme-text-primary dark:text-theme-text-primary-dark border border-theme-border dark:border-theme-border-dark rounded-lg focus:outline-none focus:ring-2 focus:ring-theme-btn-primary dark:focus:ring-theme-btn-primary-dark focus:border-transparent transition-colors duration-200 resize-none"
                  rows="4"
                  placeholder="Tell us a little about yourself..."
                  maxlength="500"
                ></textarea>
                <div class="mt-1 flex justify-between">
                  <p class="font-tinos text-xs text-theme-text-secondary dark:text-theme-text-secondary-dark">
                    Share your interests, hobbies, or anything you'd like others to know
                  </p>
                  <p class="font-tinos text-xs text-theme-text-secondary dark:text-theme-text-secondary-dark">
                    {{ form.bio?.length || 0 }}/500
                  </p>
                </div>
                <p v-if="errors.bio" class="mt-1 font-tinos text-xs text-red-600 dark:text-red-400">
                  {{ errors.bio }}
                </p>
              </div>
            </div>

            <!-- Error Alert -->
            <div v-if="submitError" class="mt-6 p-4 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg">
              <div class="flex items-center">
                <svg class="w-5 h-5 text-red-600 dark:text-red-400 mr-2" fill="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                  <path d="M12 2L1 21h22L12 2zm0 3.83L19.53 19H4.47L12 5.83zM11 16h2v2h-2v-2zm0-6h2v4h-2v-4z"/>
                </svg>
                <span class="font-tinos text-sm text-red-800 dark:text-red-300">{{ submitError }}</span>
              </div>
            </div>

            <!-- General validation error -->
            <div v-if="errors.general" class="mt-6 p-4 bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-800 rounded-lg">
              <div class="flex items-center">
                <svg class="w-5 h-5 text-yellow-600 dark:text-yellow-400 mr-2" fill="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                  <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 15h-2v-2h2v2zm0-4h-2V7h2v6z"/>
                </svg>
                <span class="font-tinos text-sm text-yellow-800 dark:text-yellow-300">{{ errors.general }}</span>
              </div>
            </div>

            <!-- Action Buttons -->
            <div class="mt-8 pt-6 border-t border-theme-border dark:border-theme-border-dark flex flex-col sm:flex-row gap-3 sm:justify-between">
              <button
                type="button"
                @click="skipProfile"
                :disabled="isSubmitting"
                class="px-6 py-3 bg-theme-bg-primary dark:bg-theme-bg-primary-dark text-theme-text-primary dark:text-theme-text-primary-dark border border-theme-border dark:border-theme-border-dark font-tinos text-base rounded-lg hover:bg-theme-border dark:hover:bg-theme-border-dark focus:outline-none focus:ring-2 focus:ring-theme-btn-primary dark:focus:ring-theme-btn-primary-dark transition-all duration-200 disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center"
              >
                <svg class="w-5 h-5 mr-2" fill="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                  <path d="M4 18l8.5-6L4 6v12zm11-12v12l8.5-6L15 6z"/>
                </svg>
                Skip for Now
              </button>

              <button
                type="submit"
                :disabled="isSubmitting"
                class="px-8 py-3 bg-theme-btn-primary dark:bg-theme-btn-primary-dark text-white font-tinos text-base font-medium rounded-lg hover:opacity-90 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-theme-btn-primary dark:focus:ring-theme-btn-primary-dark transition-all duration-200 disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center"
              >
                <span v-if="isSubmitting" class="inline-block w-5 h-5 border-2 border-white border-t-transparent rounded-full animate-spin mr-2"></span>
                <svg v-else class="w-5 h-5 mr-2" fill="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                  <path d="M9 16.2L4.8 12l-1.4 1.4L9 19 21 7l-1.4-1.4L9 16.2z"/>
                </svg>
                {{ isSubmitting ? 'Saving...' : 'Complete Profile' }}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/authStore'
import { useToast } from 'vue-toastification'

// Router and stores
const router = useRouter()
const authStore = useAuthStore()
const toast = useToast()

// Reactive state
const form = reactive({
  firstName: '',
  lastName: '',
  displayName: '',
  bio: '',
  profileImageUrl: '',
  dateOfBirth: '',
  phoneNumber: '',
  email: ''
})

const errors = reactive({})
const isSubmitting = ref(false)
const submitError = ref('')
const profileImagePreview = ref('')
const fileInput = ref(null)

// Computed properties
const defaultAvatar = computed(() =>
  `https://ui-avatars.com/api/?name=${encodeURIComponent(form.displayName || form.firstName + ' ' + form.lastName || 'User')}&size=120&background=6c757d&color=ffffff`
)

const maxDate = computed(() => {
  const today = new Date()
  return today.toISOString().split('T')[0]
})

// Form validation
const validateForm = () => {
  const newErrors = {}

  // Phone number validation (if provided)
  if (form.phoneNumber && !/^[\d\s\-+().]+$/.test(form.phoneNumber)) {
    newErrors.phoneNumber = 'Please enter a valid phone number'
  }

  // Date of birth validation (if provided)
  if (form.dateOfBirth) {
    const birthDate = new Date(form.dateOfBirth)
    const today = new Date()
    if (birthDate > today) {
      newErrors.dateOfBirth = 'Date of birth cannot be in the future'
    }
  }

  // Character limits
  if (form.displayName && form.displayName.length > 100) {
    newErrors.displayName = 'Display name must be 100 characters or less'
  }

  if (form.bio && form.bio.length > 500) {
    newErrors.bio = 'Bio must be 500 characters or less'
  }

  // Check if at least one field has a valid value
  const hasAtLeastOneField = checkAtLeastOneFieldFilled()
  if (!hasAtLeastOneField) {
    newErrors.general = 'Please fill in at least one field to complete your profile'
  }

  // Clear previous errors and set new ones
  Object.keys(errors).forEach(key => delete errors[key])
  Object.assign(errors, newErrors)

  return Object.keys(newErrors).length === 0
}

// Helper function to check if at least one field is filled
const checkAtLeastOneFieldFilled = () => {
  const fieldsToCheck = [
    'displayName',
    'phoneNumber',
    'dateOfBirth',
    'bio'
  ]

  return fieldsToCheck.some(field => {
    const value = form[field]
    if (typeof value === 'string') {
      return value && value.trim() !== ''
    }
    return value != null && value !== ''
  }) || profileImagePreview.value // Also check if image is uploaded
}

// Image upload handling
const handleImageUpload = async (event) => {
  const file = event.target.files[0]
  if (!file) return

  // Validate file type
  if (!file.type.startsWith('image/')) {
    submitError.value = 'Please select a valid image file'
    return
  }

  // Validate file size (5MB limit)
  if (file.size > 5 * 1024 * 1024) {
    submitError.value = 'Image size must be less than 5MB'
    return
  }

  try {
    // Create preview
    const reader = new FileReader()
    reader.onload = (e) => {
      profileImagePreview.value = e.target.result
    }
    reader.readAsDataURL(file)

    // In a real app, you would upload the file to your server here
    // For now, we'll just use the preview URL
    // const uploadedUrl = await uploadImageToServer(file)
    // form.profileImageUrl = uploadedUrl

    submitError.value = ''
  } catch (error) {
    console.error('Error uploading image:', error)
    submitError.value = 'Failed to upload image. Please try again.'
  }
}

// API call to save profile
const saveProfile = async (profileData) => {
  try {
    const response = await authStore.updateProfile(profileData);

    if (response.success) {
      toast.success('Profile update successful!')
      // Handle successful profile update
      console.log('Profile updated successfully:', response.data)
      // Redirect to the intended route or dashboard
      //const redirect = route.query.redirect || '/dashboard'
      router.push('/dashboard')
    } else {
      throw new Error(response.message || 'Failed to update profile')
    }
  } catch (error) {
    console.error('Error saving profile:', error)
    throw error
  }
}

// Form submission
const handleSubmit = async () => {
  submitError.value = ''

  if (!validateForm()) {
    return
  }

  isSubmitting.value = true

  try {
    // Prepare data for API (remove empty strings)
    const profileData = {}
    Object.keys(form).forEach(key => {
      if (form[key] && form[key].trim() !== '') {
        profileData[key] = form[key].trim()
      }
    })

    // adding prfile updated status in case the user doesnt have a profile
    // TODO: might not need this if the profile is always created on registration
    profileData['isProfileUpdated'] = true // Mark profile as updated

    await saveProfile(profileData)

    // Update auth store with profile completion status
    if (authStore.updateUserProfileState) {
      authStore.updateUserProfileState(profileData)
    }

    // Redirect to dashboard
    router.push('/dashboard')
  } catch (error) {
    submitError.value = error.message || 'An error occurred while saving your profile'
  } finally {
    isSubmitting.value = false
  }
}

// Skip profile completion
const skipProfile = () => {
  // Clear any existing errors
  Object.keys(errors).forEach(key => delete errors[key])
  submitError.value = ''
  router.push('/dashboard')
}

// Initialize form with user data
onMounted(() => {
  // Auto-generate display name from existing data
  if (authStore.user?.userName && !form.displayName) {
    form.displayName = authStore.user.userName
  }
})
</script>

<style scoped>
/* Additional animations can be added here if needed */
</style>
