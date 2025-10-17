<script setup>
import { ref, watch, onMounted, onUnmounted } from 'vue'
import { useAuthStore } from '@/stores/authStore'
import SearchBar from './SearchBar.vue'
import UserDropdown from './UserDropdown.vue'

const authStore = useAuthStore()
const isSticky = ref(false)
const navbarRef = ref(null)
const searchBarRef = ref(null)

onMounted(() => {
  const observer = new IntersectionObserver(
    ([entry]) => {
      // When the navbar is NOT intersecting with its original position, it's sticky
      isSticky.value = !entry.isIntersecting
    },
    {
      threshold: [1],
      rootMargin: '-1px 0px 0px 0px'
    }
  )

  if (navbarRef.value) {
    observer.observe(navbarRef.value)
  }

  onUnmounted(() => {
    if (navbarRef.value) {
      observer.unobserve(navbarRef.value)
    }
  })
})

// Watch isSticky and clear search when navbar becomes non-sticky
watch(isSticky, (newValue) => {
  if (!newValue && searchBarRef.value) {
    // Navbar is no longer sticky, clear the search
    searchBarRef.value.clearSearch()
  }
})
</script>

<template>
  <!-- Navigation (outside container for sticky to work, but maintains border width) -->
  <div class="sticky top-0 z-50 bg-[#F6F7F7] flex justify-center px-4 md:px-8 lg:px-40">
    <nav ref="navbarRef" class="w-full max-w-1280 border-b border-gray-300 py-4 transition-all duration-300">
      <div class="w-full flex items-center" :class="isSticky ? 'justify-between gap-8' : 'justify-center'">
        <!-- Left: Title (only when sticky) -->
        <div v-if="isSticky" class="flex items-center transition-opacity duration-300">
          <h2 class="text-xl md:text-2xl font-newsreader font-bold text-cod-gray">Patchnotes*</h2>
        </div>

        <!-- Center: Navigation Links -->
        <div class="flex items-center gap-6 md:gap-8">
          <router-link to="#" class="font-tinos text-sm md:text-base text-cod-gray hover:opacity-70 transition-opacity">Games</router-link>
          <router-link to="/reviews" class="font-tinos text-sm md:text-base text-cod-gray hover:opacity-70 transition-opacity">Reviews</router-link>
          <router-link to="/lists" class="font-tinos text-sm md:text-base text-cod-gray hover:opacity-70 transition-opacity">Lists</router-link>
          <router-link to="#" class="font-tinos text-sm md:text-base text-cod-gray hover:opacity-70 transition-opacity">Members</router-link>
          <router-link to="/recommendations" class="font-tinos text-sm md:text-base text-cod-gray hover:opacity-70 transition-opacity">Ask Me Something</router-link>
        </div>

        <!-- Right: Search and Auth/User (only when sticky) -->
        <div v-if="isSticky" class="flex items-center gap-4 transition-opacity duration-300">
          <!-- Search Bar -->
          <div class="hidden md:block">
            <SearchBar ref="searchBarRef" />
          </div>

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
      </div>
    </nav>
  </div>
</template>
