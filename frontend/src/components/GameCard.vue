<template>
    <div @click="$emit('click', game)" class="card border-0 shadow-sm cursor-pointer game-card"
        :class="{ 'in-library': isInLibrary }">
        <!-- Game Image -->
        <div class="position-relative">
            <img :src="game.cover ? game.cover.imageUrl : '/default-game.png'" :alt="game.name" class="card-img-top"
                style="height: 200px; object-fit: cover;">

            <!-- Rating Badge -->
            <div v-if="game.rating" class="position-absolute top-0 start-0 m-2">
                <span class="badge bg-dark bg-opacity-75 d-flex align-items-center">
                    <i class="fas fa-star text-warning me-1"></i>
                    {{ game.rating }}/5
                </span>
            </div>

            <!-- Price Badge -->
            <div v-if="game.price !== undefined" class="position-absolute top-0 end-0 m-2">
                <span class="badge" :class="game.price === 0 ? 'bg-success' : 'bg-primary'">
                    {{ game.price === 0 ? 'Free' : `$${game.price}` }}
                </span>
            </div>

            <!-- Library Status -->
            <div v-if="isInLibrary" class="position-absolute bottom-0 end-0 m-2">
                <span class="badge bg-success">
                    <i class="fas fa-check me-1"></i>
                    In Library
                </span>
            </div>
        </div>

        <!-- Card Body -->
        <div class="card-body p-3">
            <!-- Game Title -->
            <h6 class="card-title mb-2 fw-bold text-truncate" :title="game.name">
                {{ game.name }}
            </h6>

            <!-- Game Info -->
            <div class="mb-2">
                <p v-if="game.genres" class="small text-muted mb-1">
                    <i class="fas fa-tag me-1"></i>
                    Genres: {{ game.genres.map(genre => genre.name).join(', ') }}
                </p>
                <p v-if="game.developers" class="small text-muted mb-1">
                    <i class="fas fa-code me-1"></i>
                    Developers: {{ game.developers.map(developer => developer.name).join(', ') }}
                </p>
                <p v-if="game.firstReleaseDate" class="small text-muted mb-1">
                    <i class="fas fa-calendar me-1"></i>
                    {{ formatDate(game.firstReleaseDate) }}
                </p>
            </div>

            <!-- Description -->
            <p v-if="game.summary" class="card-text small text-muted mb-3" style="height: 2.4em; overflow: hidden;">
                {{ game.summary }}
            </p>

            <!-- Game Stats -->
            <div v-if="showStats" class="d-flex justify-content-between align-items-center mb-3 small">
                <span v-if="game.players" class="text-muted">
                    <i class="fas fa-users me-1"></i>
                    {{ game.players }}
                </span>
                <span v-if="game.size" class="text-muted">
                    <i class="fas fa-download me-1"></i>
                    {{ game.size }}
                </span>
                <span v-if="game.playTime" class="text-muted">
                    <i class="fas fa-clock me-1"></i>
                    {{ game.playTime }}
                </span>
            </div>

            <!-- Action Buttons -->
            <div class="d-flex gap-2">
                <button @click.stop="$emit('add-to-library', game)" :disabled="isInLibrary"
                    class="btn btn-sm flex-grow-1" :class="isInLibrary ? 'btn-success' : 'btn-primary'">
                    <i class="fas" :class="isInLibrary ? 'fa-check' : 'fa-plus'"></i>
                    {{ isInLibrary ? 'Added' : 'Add to Library' }}
                </button>

                <button @click.stop="$emit('add-to-wishlist', game)" class="btn btn-sm btn-outline-secondary"
                    :class="{ 'active': isInWishlist }"
                    :title="isInWishlist ? 'Remove from Wishlist' : 'Add to Wishlist'">
                    <i class="fas fa-heart" :class="{ 'text-danger': isInWishlist }"></i>
                </button>
            </div>
        </div>
    </div>
</template>

<script setup>
import { Game } from '@/models/Game';
import { defineProps, defineEmits } from 'vue';
// Props
defineProps({
    game: {
        type: Game,
        required: true
    },
    isInLibrary: {
        type: Boolean,
        default: false
    },
    isInWishlist: {
        type: Boolean,
        default: false
    },
    showStats: {
        type: Boolean,
        default: true
    }
})

// Emits
defineEmits(['click', 'add-to-library', 'add-to-wishlist'])

// Methods
const formatDate = (dateString) => {
    if (!dateString) return 'TBA'
    //const date = new Date(dateString)
    return dateString.toLocaleDateString('en-US', {
        month: 'short',
        day: 'numeric',
        year: 'numeric'
    })
}
</script>

<style scoped>
.game-card {
    transition: transform 0.2s ease, box-shadow 0.2s ease;
    border-radius: 15px;
}

.game-card:hover {
    transform: translateY(-5px);
    box-shadow: 0 8px 25px rgba(0, 0, 0, 0.15);
}

.game-card.in-library {
    border: 2px solid #198754;
}

.cursor-pointer {
    cursor: pointer;
}

.card-img-top {
    border-radius: 15px 15px 0 0;
}

.text-truncate {
    overflow: hidden;
    white-space: nowrap;
    text-overflow: ellipsis;
}

@media (max-width: 576px) {
    .card-img-top {
        height: 150px !important;
    }

    .card-body {
        padding: 1rem !important;
    }

    .d-flex.gap-2 {
        flex-direction: column;
    }

    .d-flex.gap-2 .btn {
        margin-bottom: 0.5rem;
    }
}
</style>