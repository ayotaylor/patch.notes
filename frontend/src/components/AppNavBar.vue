<template>
    <nav class="navbar navbar-expand-lg navbar-dark bg-primary">
        <div class="container">
            <router-link class="navbar-brand" to="/dashboard">
                <i class="fas fa-shield-alt me-2"></i>
                Patch Notes!!!
            </router-link>

            <!-- Mobile toggle button -->
            <button class="navbar-toggler" type="button" data-bs-toggle="collapse" data-bs-target="#navbarNav">
                <span class="navbar-toggler-icon"></span>
            </button>

            <!-- Navigation Links -->
            <div class="collapse navbar-collapse" id="navbarNav">
                <ul class="navbar-nav me-auto">
                    <li class="nav-item">
                        <router-link class="nav-link" to="/dashboard" active-class="active">
                            <i class="fas fa-home me-1"></i>Dashboard
                        </router-link>
                    </li>
                    <li class="nav-item">
                        <router-link class="nav-link" to="/reviews" active-class="active">
                            <i class="fas fa-star me-1"></i>Reviews
                        </router-link>
                    </li>
                    <li class="nav-item">
                        <router-link class="nav-link" to="/lists" active-class="active">
                            <i class="fas fa-list-ul me-1"></i>Lists
                        </router-link>
                    </li>
                    <li class="nav-item">
                        <router-link class="nav-link" to="/members" active-class="active">
                            <i class="fas fa-users me-1"></i>Members
                        </router-link>
                    </li>
                </ul>

                <div class="navbar-nav">
                    <UserDropdown v-if="authStore.user" />
                    <div v-else class="d-flex">
                        <router-link to="/login" class="btn btn-outline-light btn-sm me-2">
                            Login
                        </router-link>
                        <router-link to="/register" class="btn btn-light btn-sm">
                            Sign Up
                        </router-link>
                    </div>
                </div>
            </div>
        </div>
    </nav>
</template>

<script setup>
import { useAuthStore } from '@/stores/authStore'
import UserDropdown from '@/components/UserDropDown.vue'

const authStore = useAuthStore()

</script>