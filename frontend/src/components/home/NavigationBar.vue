<script setup>
import { ref, watch, onMounted, onUnmounted } from 'vue'
import { useAuthStore } from '@/stores/authStore'
import SearchBar from './SearchBar.vue'
import UserDropdown from './UserDropdown.vue'

const authStore = useAuthStore()
const isSticky = ref(false)
const navbarRef = ref(null)
const searchBarRef = ref(null)
const isMobileMenuOpen = ref(false)

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

const toggleMobileMenu = () => {
  isMobileMenuOpen.value = !isMobileMenuOpen.value
}

const closeMobileMenu = () => {
  isMobileMenuOpen.value = false
}

const handleLogout = async () => {
  const { getLogoutRedirect } = await import('@/utils/authRedirect')
  const redirectPath = getLogoutRedirect()
  await authStore.logout(redirectPath)
  closeMobileMenu()
}
</script>

<template>
  <!-- Navigation (outside container for sticky to work, but maintains border width) -->
  <div class="sticky top-0 z-50 bg-[#F6F7F7] flex justify-center px-4 md:px-8 lg:px-40">
    <nav ref="navbarRef" class="w-full max-w-1280 border-b border-gray-300 py-4 transition-all duration-300">
      <div class="w-full flex items-center" :class="isSticky ? 'justify-between gap-4' : 'justify-center'">
        <!-- Mobile: Non-sticky - Hamburger left, Auth icon right -->
        <div v-if="!isSticky" class="lg:hidden w-full flex items-center justify-between">
          <!-- Hamburger Button (Left) -->
          <button
            @click="toggleMobileMenu"
            class="p-2 text-cod-gray hover:opacity-70 transition-opacity"
            aria-label="Toggle menu"
          >
            <svg
              v-if="!isMobileMenuOpen"
              class="w-6 h-6"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16" />
            </svg>
            <svg
              v-else
              class="w-6 h-6"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>

          <!-- Auth Icon (Right) -->
          <router-link
            v-if="authStore.user"
            to="/profile"
            class="p-2 text-cod-gray hover:opacity-70 transition-opacity"
            aria-label="Profile"
          >
            <svg class="w-6 h-6" fill="currentColor" viewBox="0 0 20 20">
              <path fill-rule="evenodd" d="M10 9a3 3 0 100-6 3 3 0 000 6zm-7 9a7 7 0 1114 0H3z" clip-rule="evenodd" />
            </svg>
          </router-link>
          <router-link
            v-else
            to="/login"
            class="p-2 text-cod-gray hover:opacity-70 transition-opacity"
            aria-label="Login"
          >
            <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 16l-4-4m0 0l4-4m-4 4h14m-5 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h7a3 3 0 013 3v1" />
            </svg>
          </router-link>
        </div>

        <!-- Mobile: Sticky - Hamburger left, Title center, Auth right -->
        <div v-if="isSticky" class="lg:hidden w-full flex items-center justify-between">
          <!-- Hamburger Button (Left) -->
          <button
            @click="toggleMobileMenu"
            class="p-2 text-cod-gray hover:opacity-70 transition-opacity"
            aria-label="Toggle menu"
          >
            <svg
              v-if="!isMobileMenuOpen"
              class="w-6 h-6"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16" />
            </svg>
            <svg
              v-else
              class="w-6 h-6"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>

          <!-- Title (Center) -->
          <h2 class="absolute left-1/2 transform -translate-x-1/2 text-xl font-newsreader font-bold text-cod-gray">Patchnotes*</h2>

          <!-- Auth Icon (Right) -->
          <router-link
            v-if="authStore.user"
            to="/profile"
            class="p-2 text-cod-gray hover:opacity-70 transition-opacity"
            aria-label="Profile"
          >
            <svg class="w-6 h-6" fill="currentColor" viewBox="0 0 20 20">
              <path fill-rule="evenodd" d="M10 9a3 3 0 100-6 3 3 0 000 6zm-7 9a7 7 0 1114 0H3z" clip-rule="evenodd" />
            </svg>
          </router-link>
          <router-link
            v-else
            to="/login"
            class="p-2 text-cod-gray hover:opacity-70 transition-opacity"
            aria-label="Login"
          >
            <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 16l-4-4m0 0l4-4m-4 4h14m-5 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h7a3 3 0 013 3v1" />
            </svg>
          </router-link>
        </div>

        <!-- Desktop: Title (only when sticky) -->
        <div v-if="isSticky" class="hidden lg:flex items-center transition-opacity duration-300">
          <h2 class="text-xl md:text-2xl font-newsreader font-bold text-cod-gray whitespace-nowrap">Patchnotes*</h2>
        </div>

        <!-- Desktop: Navigation Links -->
        <div class="hidden lg:flex items-center gap-6 md:gap-8">
          <router-link to="#" class="font-tinos text-sm md:text-base text-cod-gray hover:opacity-70 transition-opacity">Games</router-link>
          <router-link to="/reviews" class="font-tinos text-sm md:text-base text-cod-gray hover:opacity-70 transition-opacity">Reviews</router-link>
          <router-link to="/lists" class="font-tinos text-sm md:text-base text-cod-gray hover:opacity-70 transition-opacity">Lists</router-link>
          <router-link to="#" class="font-tinos text-sm md:text-base text-cod-gray hover:opacity-70 transition-opacity">Members</router-link>
          <router-link to="/recommendations" class="font-tinos text-sm md:text-base text-cod-gray hover:opacity-70 transition-opacity whitespace-nowrap">Ask Me Something</router-link>
        </div>

        <!-- Desktop: Right side (Search and Auth/User - only when sticky) -->
        <div v-if="isSticky" class="hidden lg:flex items-center gap-4 transition-opacity duration-300">
          <!-- Search Bar -->
          <SearchBar ref="searchBarRef" />

          <!-- User Dropdown (when logged in) or Auth Buttons (when logged out) -->
          <div v-if="authStore.user">
            <UserDropdown />
          </div>
          <div v-else class="flex items-center gap-2">
            <router-link
              to="/register"
              class="h-9 min-w-21 px-5 bg-cod-gray text-white font-roboto font-bold text-sm tracking-wide rounded flex items-center justify-center hover:bg-opacity-90 transition-opacity whitespace-nowrap"
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

      <!-- Mobile Menu Dropdown -->
      <div
        v-if="isMobileMenuOpen"
        class="lg:hidden mt-4 py-4 border-t border-gray-300"
      >
        <!-- Search Bar -->
        <div class="mb-4">
          <SearchBar ref="searchBarRef" container-class="w-full" />
        </div>

        <!-- Navigation Links -->
        <div class="flex flex-col gap-3 mb-4">
          <router-link
            to="#"
            @click="closeMobileMenu"
            class="font-tinos text-base text-cod-gray hover:opacity-70 transition-opacity py-2"
          >
            Games
          </router-link>
          <router-link
            to="/reviews"
            @click="closeMobileMenu"
            class="font-tinos text-base text-cod-gray hover:opacity-70 transition-opacity py-2"
          >
            Reviews
          </router-link>
          <router-link
            to="/lists"
            @click="closeMobileMenu"
            class="font-tinos text-base text-cod-gray hover:opacity-70 transition-opacity py-2"
          >
            Lists
          </router-link>
          <router-link
            to="#"
            @click="closeMobileMenu"
            class="font-tinos text-base text-cod-gray hover:opacity-70 transition-opacity py-2"
          >
            Members
          </router-link>
          <router-link
            to="/recommendations"
            @click="closeMobileMenu"
            class="font-tinos text-base text-cod-gray hover:opacity-70 transition-opacity py-2"
          >
            Ask Me Something
          </router-link>
        </div>

        <!-- Auth Buttons or User Info -->
        <div class="border-t border-gray-300 pt-4">
          <div v-if="authStore.user" class="flex flex-col gap-3">
            <router-link
              to="/profile"
              @click="closeMobileMenu"
              class="font-tinos text-base text-cod-gray hover:opacity-70 transition-opacity py-2"
            >
              Profile
            </router-link>
            <router-link
              to="/home-page"
              @click="closeMobileMenu"
              class="font-tinos text-base text-cod-gray hover:opacity-70 transition-opacity py-2"
            >
              Home
            </router-link>
            <button
              @click="handleLogout"
              class="font-tinos text-base text-cod-gray hover:opacity-70 transition-opacity py-2 text-left"
            >
              Logout
            </button>
          </div>
          <div v-else class="flex flex-col gap-3">
            <router-link
              to="/register"
              @click="closeMobileMenu"
              class="h-10 px-5 bg-cod-gray text-white font-roboto font-bold text-sm tracking-wide rounded flex items-center justify-center hover:bg-opacity-90 transition-opacity"
            >
              Create Account
            </router-link>
            <router-link
              to="/login"
              @click="closeMobileMenu"
              class="h-10 px-5 bg-white text-cod-gray border border-cod-gray font-roboto font-bold text-sm tracking-wide rounded flex items-center justify-center hover:bg-gray-50 transition-colors"
            >
              Log In
            </router-link>
          </div>
        </div>
      </div>
    </nav>
  </div>
</template>
