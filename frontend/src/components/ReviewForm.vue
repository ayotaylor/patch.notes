<template>
  <div class="card shadow-sm border-0">
    <div class="card-header bg-white border-bottom">
      <h5 class="mb-0 fw-bold">
        <i class="fas fa-star text-primary me-2"></i>
        {{ isEditing ? 'Edit Your Review' : 'Write a Review' }}
      </h5>
    </div>
    <div class="card-body p-4">
      <form @submit.prevent="handleSubmit">
        <!-- Game Info (if provided) -->
        <div v-if="game" class="d-flex align-items-center mb-4 p-3 bg-light rounded">
          <img
            :src="gameImageUrl"
            :alt="game.name"
            class="rounded me-3"
            style="width: 60px; height: 60px; object-fit: cover;"
            @error="handleGameImageError"
          >
          <div>
            <h6 class="mb-1 fw-semibold">{{ game.name }}</h6>
            <small class="text-muted">{{ game.primaryGenre || 'Game' }}</small>
          </div>
        </div>

        <!-- Rating Selection -->
        <div class="mb-4">
          <label class="form-label fw-semibold">
            Rating <span class="text-danger">*</span>
          </label>
          <div class="d-flex align-items-center gap-3">
            <!-- Star Rating -->
            <div class="rating-stars">
              <button
                v-for="n in 5"
                :key="n"
                type="button"
                @click="setRating(n)"
                @mouseover="hoverRating = n"
                @mouseleave="hoverRating = 0"
                class="btn btn-link p-0 me-1"
                :class="{ 'text-warning': n <= (hoverRating || form.rating), 'text-muted': n > (hoverRating || form.rating) }"
              >
                <i class="fas fa-star" style="font-size: 1.5em;"></i>
              </button>
            </div>
            <!-- Rating Text -->
            <div class="fw-semibold text-muted">
              {{ form.rating > 0 ? `${form.rating}/5` : 'Select rating' }}
              <small class="d-block" v-if="form.rating > 0">{{ getRatingText(form.rating) }}</small>
            </div>
          </div>
          <div v-if="errors.rating" class="invalid-feedback d-block">
            {{ errors.rating }}
          </div>
        </div>

        <!-- Review Text -->
        <div class="mb-4">
          <label for="reviewText" class="form-label fw-semibold">
            Your Review <span class="text-danger">*</span>
          </label>
          <textarea
            id="reviewText"
            v-model="form.reviewText"
            class="form-control"
            :class="{ 'is-invalid': errors.reviewText }"
            rows="6"
            placeholder="Share your thoughts about this game... What did you like or dislike? How was the gameplay, story, graphics, etc.?"
            maxlength="2000"
          ></textarea>
          <div class="form-text d-flex justify-content-between">
            <span>Minimum 10 characters required</span>
            <span>{{ form.reviewText.length }}/2000</span>
          </div>
          <div v-if="errors.reviewText" class="invalid-feedback">
            {{ errors.reviewText }}
          </div>
        </div>

        <!-- Review Guidelines -->
        <div class="alert alert-info" role="alert">
          <small>
            <i class="fas fa-info-circle me-2"></i>
            <strong>Review Guidelines:</strong>
            Be honest and constructive. Focus on gameplay, story, graphics, and overall experience.
            Avoid spoilers and inappropriate content.
          </small>
        </div>

        <!-- Action Buttons -->
        <div class="d-flex gap-2">
          <button
            type="submit"
            :disabled="isSubmitting || !isFormValid"
            class="btn btn-primary"
          >
            <span v-if="isSubmitting" class="spinner-border spinner-border-sm me-2"></span>
            <i v-else class="fas fa-paper-plane me-2"></i>
            {{ isSubmitting ? 'Submitting...' : (isEditing ? 'Update Review' : 'Submit Review') }}
          </button>

          <button
            type="button"
            @click="$emit('cancel')"
            :disabled="isSubmitting"
            class="btn btn-outline-secondary"
          >
            Cancel
          </button>

          <button
            v-if="isEditing"
            type="button"
            @click="$emit('delete')"
            :disabled="isSubmitting"
            class="btn btn-outline-danger ms-auto"
          >
            <i class="fas fa-trash me-2"></i>
            Delete Review
          </button>
        </div>
      </form>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, defineProps, defineEmits, defineExpose, watch, onMounted } from 'vue'
import { useImageFallback, FALLBACK_TYPES } from '@/composables/useImageFallback'

// Props
const props = defineProps({
  game: {
    type: Object,
    default: null
  },
  existingReview: {
    type: Object,
    default: null
  },
  isSubmitting: {
    type: Boolean,
    default: false
  }
})

// Emits
const emits = defineEmits(['submit', 'cancel', 'delete'])

// Composables
const { handleImageError, createReactiveImageUrl } = useImageFallback()

// State
const form = ref({
  rating: 0,
  reviewText: ''
})

const errors = ref({
  rating: '',
  reviewText: ''
})

const hoverRating = ref(0)

// Computed
const isEditing = computed(() => !!props.existingReview)

const gameImageUrl = createReactiveImageUrl(
  computed(() => props.game?.primaryImageUrl),
  FALLBACK_TYPES.GAME_ICON
)

const isFormValid = computed(() => {
  return form.value.rating > 0 &&
         form.value.reviewText.length >= 10 &&
         form.value.reviewText.length <= 2000
})

// Methods
const setRating = (rating) => {
  form.value.rating = rating
  clearError('rating')
}

const getRatingText = (rating) => {
  const texts = {
    1: 'Terrible',
    2: 'Poor',
    3: 'Average',
    4: 'Good',
    5: 'Excellent'
  }
  return texts[rating] || ''
}

const validateForm = () => {
  errors.value = { rating: '', reviewText: '' }

  if (form.value.rating === 0) {
    errors.value.rating = 'Please select a rating'
  }

  if (!form.value.reviewText.trim()) {
    errors.value.reviewText = 'Review text is required'
  } else if (form.value.reviewText.length < 10) {
    errors.value.reviewText = 'Review must be at least 10 characters long'
  } else if (form.value.reviewText.length > 2000) {
    errors.value.reviewText = 'Review cannot exceed 2000 characters'
  }

  return !errors.value.rating && !errors.value.reviewText
}

const clearError = (field) => {
  if (errors.value[field]) {
    errors.value[field] = ''
  }
}

const handleSubmit = () => {
  if (validateForm()) {
    emits('submit', {
      rating: form.value.rating,
      reviewText: form.value.reviewText.trim(),
      gameId: props.game?.id
    })
  }
}

const resetForm = () => {
  form.value = {
    rating: 0,
    reviewText: ''
  }
  errors.value = {
    rating: '',
    reviewText: ''
  }
}

const handleGameImageError = (e) => {
  handleImageError(e, 'game')
}

// Watchers
watch(() => props.existingReview, (newReview) => {
  if (newReview) {
    form.value.rating = newReview.rating || 0
    form.value.reviewText = newReview.reviewText || ''
  } else {
    resetForm()
  }
}, { immediate: true })

watch(() => form.value.reviewText, () => {
  clearError('reviewText')
})

// Lifecycle
onMounted(() => {
  if (props.existingReview) {
    form.value.rating = props.existingReview.rating || 0
    form.value.reviewText = props.existingReview.reviewText || ''
  }
})

// Expose methods for parent component
defineExpose({
  resetForm,
  validateForm
})
</script>

<style scoped>
.card {
  border-radius: 12px;
}

.rating-stars .btn {
  border: none !important;
  outline: none !important;
  box-shadow: none !important;
}

.rating-stars .btn:hover {
  background: none !important;
  transform: scale(1.1);
  transition: transform 0.2s ease;
}

.form-control:focus {
  border-color: var(--bs-primary);
  box-shadow: 0 0 0 0.2rem rgba(var(--bs-primary-rgb), 0.25);
}

.alert-info {
  border-left: 4px solid var(--bs-info);
  border-radius: 8px;
}

@media (max-width: 768px) {
  .card-body {
    padding: 1.5rem !important;
  }

  .d-flex.gap-2 {
    flex-direction: column;
  }

  .d-flex.gap-2 .ms-auto {
    margin-left: 0 !important;
    margin-top: 1rem;
  }
}
</style>