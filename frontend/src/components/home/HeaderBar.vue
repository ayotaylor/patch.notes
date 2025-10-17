<template>
  <div class="flex justify-center px-4 md:px-8 lg:px-40">
    <div class="w-full max-w-1280">
      <!-- Top Bar -->
      <div class="flex items-center justify-between border-b border-gray-300 py-2">
        <!-- Search Bar -->
        <SearchBar />

        <!-- User Dropdown (when logged in) or Auth Buttons (when logged out) -->
        <div v-if="authStore.user">
          <UserDropdown />
        </div>
        <div v-else class="flex items-center gap-2">
          <router-link
            to="/register"
            class="h-9 min-w-21 px-5 bg-cod-gray text-white font-roboto font-bold text-sm tracking-wide rounded flex items-center justify-center hover:bg-opacity-90 transition-opacity"
          >
            Create Account
          </router-link>
          <router-link
            to="/login"
            class="h-9 min-w-21 px-5 bg-white text-cod-gray border border-cod-gray font-roboto font-bold text-sm tracking-wide rounded flex items-center justify-center hover:bg-gray-50 transition-colors"
          >
            Log In
          </router-link>
        </div>
      </div>

      <!-- Title Bar -->
      <div
        class="flex flex-col md:flex-row items-center justify-between border-b border-gray-300 py-8 gap-4"
      >
        <div class="text-sm font-tinos text-river-bed">Monday, May 20, 2024</div>
        <h1 class="text-5xl md:text-7xl font-newsreader font-bold text-cod-gray">Patchnotes*</h1>
        <div class="text-sm font-tinos text-river-bed">Today in video games</div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { useAuthStore } from '@/stores/authStore'
import SearchBar from '@/components/home/SearchBar.vue'
import UserDropdown from './UserDropdown.vue'

const authStore = useAuthStore()
</script>
