<template>
  <div class="container-fluid py-4">
    <div class="row justify-content-center">
      <div class="col-12 col-lg-10 col-xl-8">
        <!-- Page Header -->
        <div class="text-center mb-4">
          <h1 class="display-4 mb-3">
            <i class="fas fa-magic text-primary me-3"></i>
            Ask Me Something
          </h1>
          <p class="lead text-muted">
            Get personalized game recommendations based on your preferences and mood
          </p>
        </div>

        <!-- Search Section -->
        <div class="card shadow-sm border-0 mb-4">
          <div class="card-body p-4">
            <!-- Search Form -->
            <form @submit.prevent="submitQuery" class="mb-3">
              <div class="input-group input-group-lg">
                <span class="input-group-text border-0 bg-light">
                  <i class="fas fa-search text-muted"></i>
                </span>
                <textarea
                  v-model="currentQuery"
                  :placeholder="searchPlaceholder"
                  class="form-control border-0 bg-light"
                  rows="2"
                  maxlength="500"
                  :disabled="isLoading"
                  @keydown.ctrl.enter="submitQuery"
                  @keydown.meta.enter="submitQuery"
                ></textarea>
                <button
                  type="submit"
                  class="btn btn-primary"
                  :disabled="!currentQuery.trim() || isLoading"
                >
                  <span v-if="isLoading" class="spinner-border spinner-border-sm me-2" role="status"></span>
                  <i v-else class="fas fa-paper-plane me-2"></i>
                  {{ isLoading ? 'Searching...' : 'Ask' }}
                </button>
              </div>
              
              <!-- Character count and tips -->
              <div class="d-flex justify-content-between mt-2">
                <small class="text-muted">
                  <i class="fas fa-lightbulb me-1"></i>
                  Press Ctrl+Enter to search quickly
                </small>
                <small class="text-muted">
                  {{ currentQuery.length }}/500 characters
                </small>
              </div>
            </form>

            <!-- Example queries -->
            <div v-if="!hasSearched && exampleQueries.length > 0" class="mt-3">
              <p class="small text-muted mb-2">
                <i class="fas fa-star me-1"></i>
                Try these examples:
              </p>
              <div class="d-flex flex-wrap gap-2">
                <button
                  v-for="example in exampleQueries.slice(0, 4)"
                  :key="example"
                  @click="currentQuery = example"
                  class="btn btn-outline-primary btn-sm"
                  :disabled="isLoading"
                >
                  {{ example }}
                </button>
              </div>
            </div>
          </div>
        </div>

        <!-- Follow-up Questions -->
        <div v-if="followUpQuestions.length > 0" class="card shadow-sm border-0 mb-4">
          <div class="card-body p-4">
            <h6 class="mb-3">
              <i class="fas fa-question-circle text-info me-2"></i>
              Follow-up Questions
            </h6>
            <div class="d-flex flex-wrap gap-2">
              <button
                v-for="question in followUpQuestions"
                :key="question"
                @click="askFollowUpQuestion(question)"
                class="btn btn-outline-info btn-sm"
                :disabled="isLoading"
              >
                {{ question }}
              </button>
            </div>
          </div>
        </div>

        <!-- Loading State -->
        <div v-if="isLoading" class="text-center py-5">
          <div class="spinner-border text-primary mb-3" role="status">
            <span class="visually-hidden">Loading...</span>
          </div>
          <p class="text-muted">Finding the perfect games for you...</p>
        </div>

        <!-- Error State -->
        <div v-if="error" class="alert alert-danger d-flex align-items-center" role="alert">
          <i class="fas fa-exclamation-triangle me-2"></i>
          <div>
            <strong>Oops!</strong> {{ error }}
            <button @click="clearError" class="btn btn-sm btn-outline-danger ms-2">
              Try Again
            </button>
          </div>
        </div>

        <!-- Results Section -->
        <div v-if="hasResults && !isLoading">
          <!-- AI Response Message -->
          <div v-if="responseMessage" class="alert alert-info border-0 shadow-sm mb-4">
            <div class="d-flex">
              <i class="fas fa-robot me-3 mt-1 text-info"></i>
              <div class="flex-grow-1">
                <p class="mb-0" style="line-height: 1.6;">{{ responseMessage }}</p>
              </div>
            </div>
          </div>

          <!-- Recommendations Grid -->
          <div class="mb-4">
            <h4 class="mb-3">
              <i class="fas fa-gamepad text-success me-2"></i>
              Recommended Games ({{ recommendations.length }})
            </h4>
            
            <div v-if="recommendations.length === 0" class="text-center py-5">
              <i class="fas fa-search text-muted" style="font-size: 3rem;"></i>
              <h5 class="text-muted mt-3">No games found</h5>
              <p class="text-muted">Try adjusting your search criteria or browse our example queries.</p>
            </div>

            <div v-else class="recommendations-list">
              <RecommendationCard
                v-for="recommendation in recommendations"
                :key="recommendation.gameId"
                :recommendation="recommendation"
                @view-game="viewGameDetails"
                @add-to-library="addToLibrary"
                @add-to-wishlist="addToWishlist"
              />
            </div>
          </div>
        </div>

        <!-- Empty State -->
        <div v-if="!hasSearched && !isLoading" class="text-center py-5">
          <div class="mb-4">
            <i class="fas fa-magic text-muted" style="font-size: 4rem;"></i>
          </div>
          <h3 class="text-muted mb-3">Ready to discover your next favorite game?</h3>
          <p class="text-muted mb-4">
            Describe what kind of gaming experience you're looking for, and our AI will find the perfect matches for you.
          </p>
          <button @click="loadExamples" class="btn btn-outline-primary" :disabled="isLoadingExamples">
            <i class="fas fa-lightbulb me-2"></i>
            {{ isLoadingExamples ? 'Loading...' : 'Show Example Queries' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/authStore'
import RecommendationCard from '@/components/RecommendationCard.vue'
import recommendationService from '@/services/recommendationService'

// Router and stores
const router = useRouter()
const authStore = useAuthStore()

// Reactive state
const currentQuery = ref('')
const recommendations = ref([])
const followUpQuestions = ref([])
const responseMessage = ref('')
const conversationId = ref(null)
const isLoading = ref(false)
const isLoadingExamples = ref(false)
const error = ref('')
const hasSearched = ref(false)
const exampleQueries = ref([])

// Computed properties
const hasResults = computed(() => hasSearched.value && (recommendations.value.length > 0 || responseMessage.value))

const searchPlaceholder = computed(() => {
  const examples = [
    "I want to play an RPG that would put me in a happy mood...",
    "Show me horror games similar to Silent Hill",
    "I'm looking for relaxing games to play after work",
    "What are some good multiplayer games for playing with friends?"
  ]
  return examples[Math.floor(Math.random() * examples.length)]
})

// Methods
const submitQuery = async () => {
  if (!currentQuery.value.trim() || isLoading.value) return

  isLoading.value = true
  error.value = ''

  try {
    let result

    if (conversationId.value) {
      // Continue existing conversation
      result = await recommendationService.continueConversation(
        conversationId.value,
        currentQuery.value.trim(),
        10,
        authStore.isAuthenticated
      )
    } else {
      // Start new conversation
      result = await recommendationService.getRecommendationsAuto(
        currentQuery.value.trim(),
        10,
        authStore.isAuthenticated,
        authStore.isAuthenticated
      )
    }

    recommendations.value = result.games || []
    followUpQuestions.value = result.followUpQuestions || []
    responseMessage.value = result.responseMessage || ''
    conversationId.value = result.conversationId
    hasSearched.value = true

  } catch (err) {
    console.error('Error getting recommendations:', err)
    error.value = err.message || 'Something went wrong. Please try again.'
    recommendations.value = []
    followUpQuestions.value = []
    responseMessage.value = ''
  } finally {
    isLoading.value = false
  }
}

const askFollowUpQuestion = async (question) => {
  currentQuery.value = question
  await submitQuery()
}

const loadExamples = async () => {
  if (exampleQueries.value.length > 0) return

  isLoadingExamples.value = true
  try {
    const examples = await recommendationService.getExampleQueries()
    exampleQueries.value = examples
  } catch (err) {
    console.error('Error loading examples:', err)
    // Set some fallback examples if API fails
    exampleQueries.value = [
      "I want to play an RPG that would put me in a happy mood, released in the last few years",
      "Show me horror games similar to Silent Hill",
      "I'm looking for indie games with great storytelling",
      "What are some good multiplayer games for playing with friends?"
    ]
  } finally {
    isLoadingExamples.value = false
  }
}

const viewGameDetails = (gameId) => {
  router.push(`/games/${gameId}`)
}

const addToLibrary = (recommendation) => {
  console.log('Add to library:', recommendation)
  // TODO: Implement add to library functionality
  // This would typically call a library service
}

const addToWishlist = (recommendation) => {
  console.log('Add to wishlist:', recommendation)
  // TODO: Implement add to wishlist functionality
  // This would typically call a wishlist service
}

const clearError = () => {
  error.value = ''
}

// Lifecycle
onMounted(() => {
  loadExamples()
})
</script>

<style scoped>
.container-fluid {
  background: linear-gradient(135deg, #f8f9fa 0%, #e9ecef 100%);
  min-height: 100vh;
}

.card {
  border-radius: 15px;
  transition: all 0.3s ease;
}

.card:hover {
  transform: translateY(-2px);
  box-shadow: 0 8px 25px rgba(0,0,0,0.1) !important;
}

.input-group-text {
  border-radius: 15px 0 0 15px;
}

.form-control {
  border-radius: 0;
  resize: vertical;
  min-height: 80px;
}

.btn {
  border-radius: 0 15px 15px 0;
}

.btn-outline-primary:hover, .btn-outline-info:hover {
  transform: translateY(-1px);
}

.recommendations-list .recommendation-card:last-child {
  margin-bottom: 0;
}

@media (max-width: 768px) {
  .display-4 {
    font-size: 2rem !important;
  }

  .lead {
    font-size: 1.1rem !important;
  }

  .input-group-lg .form-control {
    font-size: 1rem !important;
    min-height: 60px;
  }

  .d-flex.flex-wrap.gap-2 {
    gap: 0.5rem !important;
  }

  .btn-sm {
    font-size: 0.8rem;
    padding: 0.25rem 0.5rem;
  }
}
</style>