<template>
  <div class="container mt-5">
    <div class="row justify-content-center">
      <div class="col-lg-8 col-xl-6">
        <div class="card shadow-lg border-0">
          <div class="card-header bg-primary text-white text-center py-4">
            <h2 class="mb-0">
              <i class="fas fa-user-edit me-2"></i>
              Complete Your Profile
            </h2>
            <p class="mb-0 opacity-75">Help others get to know you better</p>
          </div>

          <div class="card-body p-4">
            <!-- Skip Notice -->
            <div class="alert alert-info d-flex align-items-center mb-4" role="alert">
              <i class="fas fa-info-circle me-2"></i>
              <div>
                <strong>Optional Step:</strong> You can complete this later or skip to your dashboard.
              </div>
            </div>

            <form @submit.prevent="handleSubmit">
              <!-- Profile Image Section -->
              <div class="text-center mb-4">
                <div class="position-relative d-inline-block">
                  <img :src="profileImagePreview || defaultAvatar" alt="Profile preview"
                    class="rounded-circle border border-3 border-light shadow-sm"
                    style="width: 120px; height: 120px; object-fit: cover;">
                  <label for="profileImage"
                    class="btn btn-primary btn-sm rounded-circle position-absolute bottom-0 end-0 d-flex align-items-center justify-content-center"
                    style="width: 35px; height: 35px;">
                    <i class="fas fa-camera"></i>
                  </label>
                  <input id="profileImage" ref="fileInput" type="file" accept="image/*" @change="handleImageUpload"
                    class="d-none">
                </div>
                <div class="mt-2">
                  <small class="text-muted">Click the camera icon to upload a photo</small>
                </div>
              </div>

              <!-- Form Fields -->
              <div class="row g-3">

                <!-- Display Name -->
                <div class="col-12">
                  <label for="displayName" class="form-label">
                    <i class="fas fa-id-badge me-1"></i>Display Name
                  </label>
                  <input id="displayName" v-model="form.displayName" type="text" class="form-control"
                    :class="{ 'is-invalid': errors.displayName }" placeholder="How would you like to be known?"
                    maxlength="100">
                  <div class="form-text">This is how your name will appear to other users</div>
                  <div v-if="errors.displayName" class="invalid-feedback">
                    {{ errors.displayName }}
                  </div>
                </div>

                <!-- Phone Number -->
                <div class="col-md-6">
                  <label for="phoneNumber" class="form-label">
                    <i class="fas fa-phone me-1"></i>Phone Number
                  </label>
                  <input id="phoneNumber" v-model="form.phoneNumber" type="tel" class="form-control"
                    :class="{ 'is-invalid': errors.phoneNumber }" placeholder="(555) 123-4567" maxlength="20">
                  <div v-if="errors.phoneNumber" class="invalid-feedback">
                    {{ errors.phoneNumber }}
                  </div>
                </div>

                <!-- Date of Birth -->
                <div class="col-md-6">
                  <label for="dateOfBirth" class="form-label">
                    <i class="fas fa-calendar me-1"></i>Date of Birth
                  </label>
                  <input id="dateOfBirth" v-model="form.dateOfBirth" type="date" class="form-control"
                    :class="{ 'is-invalid': errors.dateOfBirth }" :max="maxDate">
                  <div v-if="errors.dateOfBirth" class="invalid-feedback">
                    {{ errors.dateOfBirth }}
                  </div>
                </div>

                <!-- Bio -->
                <div class="col-12">
                  <label for="bio" class="form-label">
                    <i class="fas fa-pen me-1"></i>Bio
                  </label>
                  <textarea id="bio" v-model="form.bio" class="form-control" :class="{ 'is-invalid': errors.bio }"
                    rows="4" placeholder="Tell us a little about yourself..." maxlength="500"></textarea>
                  <div class="form-text d-flex justify-content-between">
                    <span>Share your interests, hobbies, or anything you'd like others to know</span>
                    <span class="text-muted">{{ form.bio?.length || 0 }}/500</span>
                  </div>
                  <div v-if="errors.bio" class="invalid-feedback">
                    {{ errors.bio }}
                  </div>
                </div>
              </div>

              <!-- Error Alert -->
              <div v-if="submitError" class="alert alert-danger mt-4" role="alert">
                <i class="fas fa-exclamation-triangle me-2"></i>
                {{ submitError }}
              </div>

              <!-- General validation error (at least one field required) -->
              <div v-if="errors.general" class="alert alert-warning mt-4" role="alert">
                <i class="fas fa-info-circle me-2"></i>
                {{ errors.general }}
              </div>

              <!-- Action Buttons -->
              <div class="d-grid gap-2 d-md-flex justify-content-md-between mt-4 pt-3 border-top">
                <button type="button" @click="skipProfile" class="btn btn-outline-secondary btn-lg"
                  :disabled="isSubmitting">
                  <i class="fas fa-forward me-2"></i>
                  Skip for Now
                </button>

                <button type="submit" class="btn btn-primary btn-lg px-4" :disabled="isSubmitting">
                  <span v-if="isSubmitting" class="spinner-border spinner-border-sm me-2" role="status"></span>
                  <i v-else class="fas fa-check me-2"></i>
                  {{ isSubmitting ? 'Saving...' : 'Complete Profile' }}
                </button>
              </div>
            </form>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/authStore' // Adjust import path as needed
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

// const userEmail = computed(() => authStore.user?.email)
// const firstName = computed(() => authStore.user?.firstName || '')
// const lastName = computed(() => authStore.user?.lastName || '')

// Form validation
const validateForm = () => {
  const newErrors = {}

  // Email validation (if provided)
  // if (form.email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(form.email)) {
  //   newErrors.email = 'Please enter a valid email address'
  // }

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
  // if (form.firstName && form.firstName.length > 50) {
  //   newErrors.firstName = 'First name must be 50 characters or less'
  // }

  // if (form.lastName && form.lastName.length > 50) {
  //   newErrors.lastName = 'Last name must be 50 characters or less'
  // }

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
  // if (userEmail.value) {
  //   form.email = userEmail.value
  // }

  // if (firstName.value) {
  //   form.firstName = firstName.value
  // }
  // if (lastName.value) {
  //   form.lastName = lastName.value
  // }

  // Auto-generate display name from existing data
  if (authStore.user?.userName && !form.displayName) {
    form.displayName = authStore.user.userName
  }
})
</script>

<style scoped>
.card {
  max-width: 100%;
  border-radius: 15px;
}

.card-header {
  border-radius: 15px 15px 0 0 !important;
}

.form-control:focus {
  border-color: #0d6efd;
  box-shadow: 0 0 0 0.2rem rgba(13, 110, 253, 0.25);
}

.btn-primary {
  background-color: #0d6efd;
  border-color: #0d6efd;
}

.btn-primary:hover {
  background-color: #0b5ed7;
  border-color: #0a58ca;
}

.border-top {
  border-color: #dee2e6 !important;
}

@media (max-width: 768px) {
  .container {
    padding: 1rem;
  }

  .card-body {
    padding: 1.5rem !important;
  }

  .d-md-flex {
    flex-direction: column;
  }

  .d-md-flex .btn {
    margin-bottom: 0.5rem;
  }
}
</style>