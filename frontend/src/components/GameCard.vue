<template>
    <div @click="$emit('click', game.id)" class="card border-0 shadow-sm cursor-pointer game-card"
        :class="{ 'in-library': isInLibrary }">
        <!-- Game Image -->
        <div class="position-relative">
            <img
                :src="gameImageUrl"
                :alt="game.name"
                class="card-img-top"
                style="height: 200px; object-fit: cover;"
                loading="lazy"
                @error="(e) => handleImageError(e, 'gameSmall')"
            >

            <!-- Rating Badge - prioritize user reviews over IGDB rating -->
            <div v-if="game.averageRating > 0 || game.rating > 0" class="position-absolute top-0 start-0 m-2">
                <span class="badge bg-dark bg-opacity-75 d-flex align-items-center">
                    <i class="fas fa-star text-warning me-1"></i>
                    <span v-if="game.averageRating > 0">
                        {{ game.averageRating.toFixed(1) }}/5
                        <small class="opacity-75">({{ game.reviewsCount }})</small>
                    </span>
                    <span v-else>{{ game.rating }}/5</span>
                </span>
            </div>

            <!-- Price Badge -->
            <div v-if="showPrice && game.price !== undefined" class="position-absolute top-0 end-0 m-2">
                <span class="badge" :class="game.price === 0 ? 'bg-success' : 'bg-primary'">
                    {{ game.price === 0 ? 'Free' : `$${game.price}` }}
                </span>
            </div>

            <!-- New Release Badge -->
            <div v-if="game.isNewRelease" class="position-absolute top-0 end-0 m-2" :class="{ 'mt-5': showPrice }">
                <span class="badge bg-warning text-dark">
                    <i class="fas fa-star me-1"></i>
                    New
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
                <p v-if="game.allGenres !== 'Unknown'" class="small text-muted mb-1">
                    <i class="fas fa-tag me-1"></i>
                    {{ game.allGenres }}
                </p>
                <p v-if="game.allDevelopers !== 'Unknown'" class="small text-muted mb-1">
                    <i class="fas fa-code me-1"></i>
                    {{ game.allDevelopers }}
                </p>
                <p v-if="game.shortReleaseDate !== 'TBA'" class="small text-muted mb-1">
                    <i class="fas fa-calendar me-1"></i>
                    {{ game.shortReleaseDate }}
                </p>
            </div>

            <!-- Description -->
            <p v-if="game.summary && showDescription" class="card-text small text-muted mb-3 description-text">
                {{ truncatedSummary }}
            </p>

            <!-- Game Stats -->
            <div v-if="showStats" class="d-flex justify-content-between align-items-center mb-3 small">
                <span v-if="game.platforms?.length" class="text-muted">
                    <i class="fas fa-gamepad me-1"></i>
                    {{ platformsText }}
                </span>
                <span v-if="game.reviewsCount > 0" class="text-muted">
                    <i class="fas fa-comments me-1"></i>
                    {{ game.reviewsCount }} review{{ game.reviewsCount !== 1 ? 's' : '' }}
                </span>
                <span v-if="game.hypes > 0" class="text-muted">
                    <i class="fas fa-fire me-1"></i>
                    {{ game.hypes }}
                </span>
                <span v-if="game.likesCount > 0" class="text-muted">
                    <i class="fas fa-heart me-1"></i>
                    {{ game.likesCount }}
                </span>
            </div>

            <!-- Action Buttons -->
            <div class="d-flex gap-2">
                <button
                    @click.stop="$emit('add-to-library', game)"
                    :disabled="isInLibrary"
                    class="btn btn-sm flex-grow-1"
                    :class="isInLibrary ? 'btn-success' : 'btn-primary'"
                >
                    <i class="fas" :class="isInLibrary ? 'fa-check' : 'fa-plus'"></i>
                    {{ isInLibrary ? 'Added' : 'Add to Library' }}
                </button>

                <button
                    @click.stop="$emit('add-to-wishlist', game)"
                    class="btn btn-sm btn-outline-secondary"
                    :class="{ 'active': isInWishlist }"
                    :title="isInWishlist ? 'Remove from Wishlist' : 'Add to Wishlist'"
                >
                    <i class="fas fa-heart" :class="{ 'text-danger': isInWishlist }"></i>
                </button>

                <button
                    v-if="showMoreActions"
                    @click.stop="$emit('show-details', game)"
                    class="btn btn-sm btn-outline-info"
                    title="Quick Details"
                >
                    <i class="fas fa-info"></i>
                </button>
            </div>
        </div>
    </div>
</template>

<script setup>
import { computed, defineProps, defineEmits } from 'vue'
import { Game } from '@/models/Game'
import { useImageFallback, FALLBACK_TYPES } from '@/composables/useImageFallback'

// Props
const props = defineProps({
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
    },
    showDescription: {
        type: Boolean,
        default: true
    },
    showPrice: {
        type: Boolean,
        default: false
    },
    showMoreActions: {
        type: Boolean,
        default: false
    },
    descriptionLimit: {
        type: Number,
        default: 100
    }
})

// Emits
defineEmits(['click', 'add-to-library', 'add-to-wishlist', 'show-details'])

// Image fallback composable
const { handleImageError, createReactiveImageUrl, IMAGE_CONTEXTS } = useImageFallback()

// Computed properties
const gameImageUrl = createReactiveImageUrl(
    computed(() => props.game.primaryImageUrl), 
    FALLBACK_TYPES.GAME_SMALL,
    IMAGE_CONTEXTS.GAME_CARD
)

const truncatedSummary = computed(() => {
    if (!props.game.summary) return ''

    const summary = props.game.summary
    if (summary.length <= props.descriptionLimit) return summary

    return summary.substring(0, props.descriptionLimit).trim() + '...'
})

const platformsText = computed(() => {
    const platforms = props.game.platforms
    if (!platforms || platforms.length === 0) return ''

    if (platforms.length === 1) return platforms[0].name
    if (platforms.length === 2) return platforms.map(p => p.name).join(', ')

    return `${platforms[0].name} +${platforms.length - 1}`
})
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