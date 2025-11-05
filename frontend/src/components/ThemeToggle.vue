<template>
  <button
    @click="toggleTheme"
    class="theme-toggle"
    :aria-label="isDark ? 'Switch to light mode' : 'Switch to dark mode'"
  >
    <!-- Sun Icon (Light Mode) -->
    <svg
      v-if="!isDark"
      class="theme-icon"
      fill="none"
      stroke="currentColor"
      viewBox="0 0 24 24"
    >
      <path
        stroke-linecap="round"
        stroke-linejoin="round"
        stroke-width="2"
        d="M12 3v1m0 16v1m9-9h-1M4 12H3m15.364 6.364l-.707-.707M6.343 6.343l-.707-.707m12.728 0l-.707.707M6.343 17.657l-.707.707M16 12a4 4 0 11-8 0 4 4 0 018 0z"
      />
    </svg>

    <!-- Moon Icon (Dark Mode) -->
    <svg
      v-else
      class="theme-icon"
      fill="none"
      stroke="currentColor"
      viewBox="0 0 24 24"
    >
      <path
        stroke-linecap="round"
        stroke-linejoin="round"
        stroke-width="2"
        d="M20.354 15.354A9 9 0 018.646 3.646 9.003 9.003 0 0012 21a9.003 9.003 0 008.354-5.646z"
      />
    </svg>
  </button>
</template>

<script setup>
import { useTheme } from '@/composables/useTheme'

const { isDark, toggleTheme } = useTheme()
</script>

<style scoped>
/* Theme Toggle Button */
.theme-toggle {
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 0.5rem;
  @apply text-theme-text-secondary dark:text-theme-text-primary-dark;
  background: transparent;
  border: none;
  border-radius: 0.375rem;
  cursor: pointer;
  transition: opacity 0.2s ease, background-color 0.2s ease;
}

.theme-toggle:hover {
  opacity: 0.7;
}

.theme-toggle:focus {
  outline: 2px solid #3b82f6;
  outline-offset: 2px;
}

/* Icon sizing */
.theme-icon {
  width: 1.5rem;
  height: 1.5rem;
}
</style>
