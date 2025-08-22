<template>
  <div id="app">
    <!-- Navigation Bar -->
     <AppNavbar v-if="showNavBar" />

     <!-- Main Content -->
    <div class="main-content">
      <router-view />
    </div>

    <!-- Loading Overlay -->
     <LoadingOverlay v-if="authStore.loading" :message="loadingMessage" />

  </div>
</template>

<script setup>
import { computed, onMounted, onUnmounted, ref } from 'vue';
import { useRoute } from 'vue-router';
import { useAuthStore } from '@/stores/authStore';
import AppNavbar from '@/components/AppNavBar.vue';
import LoadingOverlay from '@/components/LoadingOverlay.vue';

const route = useRoute();
const authStore = useAuthStore();
const loadingMessage = ref('');

const showNavBar = computed(() => {
  const authRoutes = [
    '/login', '/register', '/forgot-password', '/reset-password'];
  return !authRoutes.includes(route.path);
});

// Storage event handler for multi-tab sync
const handleStorageChange = (event) => {
  authStore.handleStorageChange(event);
};

onMounted(async () => {
  // load user from storage on mount
  authStore.loadUserFromStorage()

  // validate token if user exists
  if (authStore.token) {
    loadingMessage.value = 'Validating session...';
    await authStore.validateToken();
    loadingMessage.value = '';
  }

  // listen for storage changes (multi-tab sync)
  window.addEventListener('storage', handleStorageChange);

});

onUnmounted(() => {
  window.removeEventListener('storage', handleStorageChange);
})
</script>

<style>
/* Global styles */
#app {
  font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
  color: #2c3e50;
  min-height: 100vh;
  display: flex;
  flex-direction: column;
}

.main-content {
  flex: 1;
  padding-top: 0;
}

/* Ensure content doesn't overlap with fixed navbar */
.navbar {
  box-shadow: 0 2px 4px rgba(0,0,0,.1);
}


/* Responsive adjustments */
@media (max-width: 768px) {
  .main-content {
    padding: 1rem;
  }
}

/* Custom scrollbar */
::-webkit-scrollbar {
  width: 8px;
}

::-webkit-scrollbar-track {
  background: #f1f1f1;
}

::-webkit-scrollbar-thumb {
  background: #c1c1c1;
  border-radius: 4px;
}

::-webkit-scrollbar-thumb:hover {
  background: #a1a1a1;
}
</style>
