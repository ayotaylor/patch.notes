<template>
  <div class="flex justify-center px-4 md:px-8 lg:px-40 bg-theme-bg-primary dark:bg-theme-bg-primary-dark transition-colors duration-200">
    <div class="w-full max-w-1280">
      <!-- Top Bar - Hidden on mobile (< lg), shown on desktop -->
      <div class="hidden lg:flex items-center justify-between border-b border-theme-border dark:border-theme-border-dark py-2">
        <!-- Search Bar -->
        <SearchBar />

        <!-- User Dropdown (when logged in) or Auth Buttons (when logged out) -->
        <div v-if="authStore.user">
          <UserDropdown />
        </div>
        <div v-else class="flex items-center gap-2">
          <router-link
            to="/register"
            class="h-9 min-w-21 px-5 bg-cod-gray dark:bg-gray-700 text-white font-roboto font-bold text-sm tracking-wide rounded flex items-center justify-center hover:bg-opacity-90 transition-opacity"
          >
            Create Account
          </router-link>
          <router-link
            to="/login"
            class="h-9 min-w-21 px-5 bg-theme-btn-secondary dark:bg-theme-btn-secondary-dark text-theme-text-primary dark:text-theme-text-primary-dark border border-theme-border dark:border-theme-border-dark font-roboto font-bold text-sm tracking-wide rounded flex items-center justify-center hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors"
          >
            Log In
          </router-link>
        </div>
      </div>

      <!-- Title Bar -->
      <div
        class="flex flex-col md:flex-row items-center justify-between border-b border-theme-border dark:border-theme-border-dark py-8 gap-4"
      >
        <div class="text-sm font-tinos text-theme-text-secondary dark:text-theme-text-secondary-dark">Monday, May 20, 2024</div>
        <h1 class="text-5xl md:text-7xl font-newsreader font-bold text-theme-text-primary dark:text-theme-text-primary-dark">Patchnotes*</h1>
        <div class="text-sm font-tinos text-theme-text-secondary dark:text-theme-text-secondary-dark">Today in video games</div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { useTheme } from '@/composables/useTheme';
import { useAuthStore } from '@/stores/authStore';
import SearchBar from '@/components/home/SearchBar.vue';
import UserDropdown from './UserDropdown.vue';

useTheme();
const authStore = useAuthStore();
</script>
