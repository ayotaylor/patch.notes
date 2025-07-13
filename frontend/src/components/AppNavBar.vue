<template>
    <nav class="navbar navbar-expand-lg navbar-dark bg-primary">
        <div class="container">
            <router-link class="navbar-brand" to="/dashboard">
                <i class="fas fa-shield-alt me-2"></i>
                Patch Notes!!!
            </router-link>

            <div class="navbar-nav ms-auto">
                <UserDropdown v-if="authStore.user" />

                <!-- Mobile toggle button (if you want responsive navbar) -->
                <button class="navbar-toggler" type="button" data-bs-toggle="collapse" data-bs-target="#navbarNav">
                    <span class="navbar-toggler-icon"></span>
                </button>
            </div>
        </div>
    </nav>
</template>

<script setup>
import { useAuthStore } from '@/stores/authStore'
import UserDropdown from '@/components/UserDropDown.vue'

const authStore = useAuthStore()

</script>