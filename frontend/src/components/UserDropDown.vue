<template>
    <div class="nav-item dropdown">
        <a class="nav-link dropdown-toggle text-white" href="#" id="navbarDropdown" role="button"
            data-bs-toggle="dropdown">
            <i class="fas fa-user-circle me-1"></i>
            {{ authStore.userFullName }}
        </a>
        <ul class="dropdown-menu">
            <li>
                <router-link class="dropdown-item" to="/profile">
                    <i class="fas fa-user me-2"></i>
                    Profile
                </router-link>
            </li>
            <li>
                <router-link class="dropdown-item" to="/dashboard">
                <i class="fas fa-tachometer-alt me-2"></i>
                Dashboard
                </router-link>
            </li>
            <li>
                <hr class="dropdown-divider">
            </li>
            <li>
                <a class="dropdown-item" href="#" @click.prevent="handleLogout">
                    <i class="fas fa-sign-out-alt me-2"></i>
                    Logout
                </a>
            </li>
        </ul>
    </div>
</template>

<script setup>
import { useAuthStore } from '@/stores/authStore'
import { useAuthRedirect } from '@/utils/authRedirect'

const authStore = useAuthStore()
const { getLogoutRedirect } = useAuthRedirect()

const handleLogout = async () => {
  const redirectPath = getLogoutRedirect()
  await authStore.logout(redirectPath)
}
</script>

<style scoped>
.dropdown-toggle::after {
  margin-left: 0.5rem;
}

.dropdown-item:hover {
  background-color: #f8f9fa;
}


.nav-link {
  cursor: pointer;
  transition: all 0.3s ease;
}

.nav-link:hover {
  opacity: 0.8;
}
</style>