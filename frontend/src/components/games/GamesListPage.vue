<template>
  <div class="flex justify-center px-4 md:px-8 lg:px-40 min-h-screen bg-theme-bg-primary dark:bg-theme-bg-primary-dark transition-colors duration-200">
    <div class="w-full max-w-1280">
      <!-- First Row: Title and Controls -->
      <div class="flex justify-between items-center mb-4 mt-8">
        <!-- Left: Title -->
        <h1 class="font-newsreader text-3xl font-bold text-theme-text-primary dark:text-theme-text-primary-dark">Games</h1>

        <!-- Right: Sorting Options and Grid Toggle -->
        <div class="flex items-center gap-4">
          <!-- Decade Dropdown (Placeholder) -->
          <select
            class="px-3 py-2 border border-theme-border dark:border-theme-border-dark rounded-md text-sm bg-theme-bg-secondary dark:bg-theme-bg-secondary-dark text-theme-text-primary dark:text-theme-text-primary-dark focus:outline-none focus:ring-2 focus:ring-blue-500 dark:focus:ring-blue-400"
            disabled
          >
            <option>Decade</option>
          </select>

          <!-- Genre Dropdown (Placeholder) -->
          <select
            class="px-3 py-2 border border-theme-border dark:border-theme-border-dark rounded-md text-sm bg-theme-bg-secondary dark:bg-theme-bg-secondary-dark text-theme-text-primary dark:text-theme-text-primary-dark focus:outline-none focus:ring-2 focus:ring-blue-500 dark:focus:ring-blue-400"
            disabled
          >
            <option>Genre</option>
          </select>

          <!-- Grid Toggle Buttons -->
          <div class="flex gap-2">
            <!-- 6 Games Per Row -->
            <button
              @click="gamesPerRow = 6"
              :class="[
                'w-10 h-10 flex items-center justify-center border-2 rounded transition-colors',
                gamesPerRow === 6
                  ? 'border-blue-600 dark:border-blue-400 bg-blue-50 dark:bg-blue-900 text-blue-600 dark:text-blue-400'
                  : 'border-theme-border dark:border-theme-border-dark text-theme-text-secondary dark:text-theme-text-secondary-dark hover:border-gray-400 dark:hover:border-gray-500'
              ]"
              aria-label="Show 6 games per row"
            >
              <span class="font-bold text-sm">6</span>
            </button>

            <!-- 12 Games Per Row -->
            <button
              @click="gamesPerRow = 12"
              :class="[
                'w-10 h-10 flex items-center justify-center border-2 rounded transition-colors',
                gamesPerRow === 12
                  ? 'border-blue-600 dark:border-blue-400 bg-blue-50 dark:bg-blue-900 text-blue-600 dark:text-blue-400'
                  : 'border-theme-border dark:border-theme-border-dark text-theme-text-secondary dark:text-theme-text-secondary-dark hover:border-gray-400 dark:hover:border-gray-500'
              ]"
              aria-label="Show 12 games per row"
            >
              <span class="font-bold text-sm">12</span>
            </button>
          </div>
        </div>
      </div>

      <!-- Second Row: Category Banner -->
      <div v-if="categoryDisplayName" class="filtered-message body-text -small mb-6 py-3 px-4">
        Showing {{ categoryDisplayName }}
      </div>

      <!-- Third Row and Below: Games Grid -->
      <div v-if="loading" class="grid mb-8" :class="[gridClasses, gridGapClass]">
        <!-- Loading Skeleton -->
        <div
          v-for="i in gamesPerRow * 2"
          :key="`skeleton-${i}`"
          class="animate-pulse"
        >
          <div class="bg-gray-300 rounded-lg aspect-[3/4]"></div>
        </div>
      </div>

      <div v-else-if="error" class="text-center py-12">
        <p class="text-red-600 dark:text-red-400 mb-4">{{ error }}</p>
        <button
          @click="loadGames"
          class="px-4 py-2 bg-blue-600 dark:bg-blue-500 text-white rounded hover:bg-blue-700 dark:hover:bg-blue-600 transition-colors"
        >
          Retry
        </button>
      </div>

      <div v-else-if="displayGames.length === 0" class="text-center py-12">
        <p class="text-gray-600 dark:text-gray-400">No games found.</p>
      </div>

      <div v-else class="grid mb-8" :class="[gridClasses, gridGapClass]">
        <GameCard
          v-for="game in displayGames"
          :key="game.id"
          :game="game"
          :image-size="imageSize"
          :hide-title="true"
          @click="handleGameClick(game)"
        />
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useGamesStore } from '@/stores/gamesStore';
import { useTheme } from '@/composables/useTheme';
import GameCard from '@/components/home/GameCard.vue';

useTheme(); // Initialize theme

// Props
const props = defineProps({
  games: {
    type: Array,
    default: null
  },
  category: {
    type: String,
    default: null
  },
  defaultGamesPerRow: {
    type: Number,
    default: 12,
    validator: (value) => [6, 12].includes(value)
  }
});

// Router and Store
const route = useRoute();
const router = useRouter();
const gamesStore = useGamesStore();

// State
const gamesPerRow = ref(props.defaultGamesPerRow);
const displayGames = ref([]);
const loading = ref(false);
const error = ref(null);

// Category Configuration
const CATEGORY_CONFIG = {
  'popular': {
    displayName: 'Popular Games',
    fetchMethod: 'fetchPopularGames',
    limit: 48
  },
  'recently-reviewed': {
    displayName: 'Recently Reviewed Games',
    fetchMethod: 'fetchLatestReviewedGames',
    limit: 48
  },
  'new': {
    displayName: 'New Games',
    fetchMethod: 'fetchNewGames',
    limit: 48
  }
};

// Computed Properties
const currentCategory = computed(() => {
  return props.category || route.params.category;
});

const categoryConfig = computed(() => {
  return CATEGORY_CONFIG[currentCategory.value] || null;
});

const categoryDisplayName = computed(() => {
  return categoryConfig.value?.displayName || '';
});

const gridClasses = computed(() => {
  if (gamesPerRow.value === 6) {
    return 'grid-cols-2 md:grid-cols-3 lg:grid-cols-4 xl:grid-cols-6';
  } else {
    return 'grid-cols-3 md:grid-cols-4 lg:grid-cols-6 xl:grid-cols-12';
  }
});

const gridGapClass = computed(() => {
  // Use 4px gap system for tighter spacing between rows
  if (gamesPerRow.value === 6) {
    return 'gap-4'; // 16px for larger images (more breathing room)
  } else {
    return 'gap-1'; // 4px for smaller images (tighter grid)
  }
});

const imageSize = computed(() => {
  return gamesPerRow.value === 6 ? 'default' : 'small';
});

// Methods
const loadGames = async () => {
  // If games are passed as props, use them
  if (props.games && props.games.length > 0) {
    displayGames.value = props.games;
    return;
  }

  // Otherwise, fetch based on category
  if (!categoryConfig.value) {
    error.value = 'Invalid category';
    return;
  }

  loading.value = true;
  error.value = null;

  try {
    const fetchMethod = categoryConfig.value.fetchMethod;
    const limit = categoryConfig.value.limit;

    if (gamesStore[fetchMethod]) {
      const games = await gamesStore[fetchMethod](limit);

      // Handle the response based on the fetch method
      switch (fetchMethod) {
        case 'fetchPopularGames':
          displayGames.value = gamesStore.popularGames;
          break;
        case 'fetchLatestReviewedGames':
          // This method returns games directly, not stored in state
          displayGames.value = games || [];
          break;
        case 'fetchNewGames':
          displayGames.value = gamesStore.newGames;
          break;
        default:
          displayGames.value = [];
      }
    } else {
      throw new Error(`Fetch method ${fetchMethod} not found in games store`);
    }
  } catch (err) {
    console.error('Error loading games:', err);
    error.value = 'Failed to load games. Please try again.';
  } finally {
    loading.value = false;
  }
};

const handleGameClick = (game) => {
  router.push({
    name: 'GameDetails',
    params: { identifier: game.slug || game.id }
  });
};

// Lifecycle
onMounted(() => {
  loadGames();
});

// Watch for category changes
watch(() => currentCategory.value, () => {
  loadGames();
});

// Watch for games prop changes
watch(() => props.games, (newGames) => {
  if (newGames && newGames.length > 0) {
    displayGames.value = newGames;
  }
});
</script>

<style scoped>
.filtered-message {
  background-color: #456;
  color: #9ab;
  text-align: center;
  border-radius: 4px;
}

.body-text.-small {
  font-size: 1rem;
  line-height: 1.46153846;
}

.body-text {
  font-size: 1.15384615rem;
  word-break: break-word;
  overflow-wrap: break-word;
}
</style>
