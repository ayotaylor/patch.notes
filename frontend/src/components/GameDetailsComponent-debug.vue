<template>
  <div class="container py-4">
    <h1>Game Details Debug</h1>

    <!-- Basic Info -->
    <div class="alert alert-info">
      <h3>Basic State:</h3>
      <p>Loading: {{ loading }}</p>
      <p>Error: {{ error }}</p>
      <p>Route ID: {{ route.params.identifier }}</p>
      <p>Game exists: {{ !!game }}</p>
      <p>Game type: {{ typeof game }}</p>

      <div v-if="game">
        <h4>Game Object:</h4>
        <p>Name: {{ game.name }} (type: {{ typeof game.name }})</p>
        <p>ID: {{ game.id }} (type: {{ typeof game.id }})</p>
        <p>Summary length: {{ game.summary ? game.summary.length : 'no summary' }}</p>
      </div>
    </div>

    <!-- Loading State -->
    <div v-if="loading">
      <h2>Loading...</h2>
    </div>

    <!-- Error State -->
    <div v-else-if="error">
      <h2>Error: {{ error }}</h2>
      <button @click="reload">Reload</button>
    </div>

    <!-- Game Content -->
    <div v-else-if="game">
      <h2>Game Found!</h2>
      <h3>{{ game.name || 'No name' }}</h3>
      <p>{{ game.summary || 'No summary' }}</p>

      <!-- Try to access properties that might be Promises -->
      <div class="mt-4">
        <h4>Property Tests:</h4>
        <p>Direct name: {{ game.name }}</p>
        <p>Direct summary: {{ game.summary }}</p>
        <p>Has developers: {{ !!game.developers }}</p>
        <p>Has genres: {{ !!game.genres }}</p>
      </div>
    </div>

    <!-- No Game State -->
    <div v-else>
      <h2>No game data</h2>
      <button @click="reload">Try Loading</button>
    </div>

    <!-- Force show raw game object -->
    <div class="mt-4 p-3 bg-light">
      <h4>Raw Game Object (first 500 chars):</h4>
      <pre>{{ gameString }}</pre>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, defineProps } from 'vue'
import { useRoute } from 'vue-router'
import { useGamesStore } from '@/stores/gamesStore'

// Props
const props = defineProps({
  gameId: {
    type: [Number, String],
    default: null
  },
  slug: {
    type: [String],
    default: null
  }
})

// Composables
const route = useRoute()
const gamesStore = useGamesStore()

// State
const game = ref(null)
const loading = ref(true)
const error = ref('')

// Computed
const gameIdentifier = computed(() => {
  return props.gameId || props.slug || route.params.identifier || null
})

const gameString = computed(() => {
  try {
    if (!game.value) return 'null'
    const str = JSON.stringify(game.value, null, 2)
    return str.length > 500 ? str.substring(0, 500) + '...' : str
  } catch (err) {
    return `Error stringifying: ${err.message}`
  }
})

// Methods
const loadGame = async () => {
  const identifier = gameIdentifier.value
  console.log('🎯 loadGame called with identifier:', identifier)

  if (!identifier) {
    loading.value = false
    error.value = 'No identifier'
    return
  }

  loading.value = true
  error.value = ''
  game.value = null

  try {
    console.log('📡 Calling store.fetchGameDetails...')
    const result = await gamesStore.fetchGameDetails(identifier)
    console.log('📦 Store returned:', result)
    console.log('📦 Result type:', typeof result)
    console.log('📦 Result constructor:', result?.constructor?.name)

    if (result instanceof Promise) {
      console.error('🚨 STORE RETURNED A PROMISE!')
      const resolved = await result
      console.log('📦 Resolved promise to:', resolved)
      game.value = resolved
    } else {
      game.value = result
    }

    console.log('✅ Game set to:', game.value)
    console.log('✅ Game name:', game.value?.name)

  } catch (err) {
    console.error('❌ Load error:', err)
    error.value = err.message
  } finally {
    loading.value = false
  }
}

const reload = () => {
  loadGame()
}

// Lifecycle
onMounted(() => {
  console.log('🚀 Minimal component mounted')
  console.log('🚀 Route params:', route.params)
  console.log('🚀 Props:', props)
  console.log('🚀 Computed identifier:', gameIdentifier.value)

  loadGame()
})
</script>