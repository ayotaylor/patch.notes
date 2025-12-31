<template>
  <div class="bg-theme-bg-secondary dark:bg-theme-bg-secondary-dark rounded-xl shadow-sm border border-theme-border dark:border-theme-border-dark transition-colors duration-200">
    <div class="p-6">
      <div class="flex justify-between items-center mb-6">
        <h3 class="font-newsreader text-xl font-bold text-theme-text-primary dark:text-theme-text-primary-dark flex items-center">
          <svg class="w-6 h-6 text-red-600 dark:text-red-400 mr-2" fill="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
            <path d="M12 21.35l-1.45-1.32C5.4 15.36 2 12.28 2 8.5 2 5.42 4.42 3 7.5 3c1.74 0 3.41.81 4.5 2.09C13.09 3.81 14.76 3 16.5 3 19.58 3 22 5.42 22 8.5c0 3.78-3.4 6.86-8.55 11.54L12 21.35z"/>
          </svg>
          Favorite Games
        </h3>
      </div>

      <!-- Empty State -->
      <div v-if="!games || games.length === 0" class="text-center py-12">
        <svg class="w-16 h-16 text-theme-text-secondary dark:text-theme-text-secondary-dark mx-auto mb-4" fill="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
          <path d="M12 21.35l-1.45-1.32C5.4 15.36 2 12.28 2 8.5 2 5.42 4.42 3 7.5 3c1.74 0 3.41.81 4.5 2.09C13.09 3.81 14.76 3 16.5 3 19.58 3 22 5.42 22 8.5c0 3.78-3.4 6.86-8.55 11.54L12 21.35z"/>
        </svg>
        <p class="font-tinos text-base text-theme-text-secondary dark:text-theme-text-secondary-dark">
          {{ isOwnProfile ? "Don't forget to select your favorite games!" : "They haven't added any favorite games yet" }}
        </p>
      </div>

      <!-- Games Grid -->
      <div v-else class="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 gap-4">
        <GameCard
          v-for="game in games"
          :key="game.id"
          :game="game"
          image-size="default"
          @click="navigateToGame(game)"
        />
      </div>
    </div>
  </div>
</template>

<script setup>
import { useRouter } from 'vue-router'
import GameCard from './GameCard.vue'

// const props = defineProps({
//   games: {
//     type: Array,
//     default: () => []
//   },
//   isOwnProfile: {
//     type: Boolean,
//     default: false
//   }
// })

const router = useRouter()

const navigateToGame = (game) => {
  if (game.slug) {
    router.push(`/games/${game.slug}`)
  } else if (game.id) {
    router.push(`/games/${game.id}`)
  }
}
</script>
