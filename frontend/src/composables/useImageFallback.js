import { ref, computed } from 'vue'
import { getContextualIgdbImage, IMAGE_CONTEXTS, IGDB_IMAGE_SIZES } from '@/utils/igdbImageSizing'

/**
 * Unified Image Fallback Composable
 * 
 * Provides consistent fallback image handling across the entire application.
 * Uses inline SVG data URLs to prevent HTTP requests for missing images.
 * 
 * Features:
 * - Multiple fallback image types (game, avatar, general)
 * - Error handling for failed image loads
 * - Reactive image URL with automatic fallback
 * - No external file dependencies
 * 
 * Usage:
 * import { useImageFallback } from '@/composables/useImageFallback'
 * 
 * const { getImageUrl, handleImageError, fallbackImages } = useImageFallback()
 * const imageUrl = getImageUrl(game.primaryImageUrl, 'game')
 */

// Fallback SVG images as base64 data URLs
const fallbackImages = {
  // For game covers and screenshots
  game: 'data:image/svg+xml;base64,' + btoa(`
    <svg xmlns="http://www.w3.org/2000/svg" width="400" height="300" viewBox="0 0 400 300">
      <rect width="400" height="300" fill="#f8f9fa"/>
      <g fill="#6c757d" text-anchor="middle">
        <circle cx="200" cy="120" r="30" fill="none" stroke="#6c757d" stroke-width="3"/>
        <path d="M200 90 L200 150 M170 120 L230 120" stroke="#6c757d" stroke-width="3" fill="none"/>
        <rect x="140" y="180" width="120" height="60" rx="8" fill="none" stroke="#6c757d" stroke-width="3"/>
        <text x="200" y="215" font-family="Arial, sans-serif" font-size="16">No Image Available</text>
      </g>
    </svg>
  `),

  // For smaller game thumbnails
  gameSmall: 'data:image/svg+xml;base64,' + btoa(`
    <svg xmlns="http://www.w3.org/2000/svg" width="120" height="120" viewBox="0 0 120 120">
      <rect width="120" height="120" fill="#f8f9fa"/>
      <g fill="#6c757d" text-anchor="middle">
        <circle cx="60" cy="45" r="15" fill="none" stroke="#6c757d" stroke-width="2"/>
        <path d="M60 30 L60 60 M45 45 L75 45" stroke="#6c757d" stroke-width="2" fill="none"/>
        <rect x="30" y="70" width="60" height="30" rx="4" fill="none" stroke="#6c757d" stroke-width="2"/>
        <text x="60" y="90" font-family="Arial, sans-serif" font-size="10">No Image</text>
      </g>
    </svg>
  `),

  // For very small game icons
  gameIcon: 'data:image/svg+xml;base64,' + btoa(`
    <svg xmlns="http://www.w3.org/2000/svg" width="60" height="60" viewBox="0 0 60 60">
      <rect width="60" height="60" fill="#f8f9fa"/>
      <g fill="#6c757d" text-anchor="middle">
        <circle cx="30" cy="22" r="8" fill="none" stroke="#6c757d" stroke-width="1.5"/>
        <path d="M30 14 L30 30 M22 22 L38 22" stroke="#6c757d" stroke-width="1.5" fill="none"/>
        <rect x="15" y="35" width="30" height="15" rx="2" fill="none" stroke="#6c757d" stroke-width="1.5"/>
        <text x="30" y="45" font-family="Arial, sans-serif" font-size="6">No Image</text>
      </g>
    </svg>
  `),

  // For user profile avatars
  avatar: 'data:image/svg+xml;base64,' + btoa(`
    <svg xmlns="http://www.w3.org/2000/svg" width="120" height="120" viewBox="0 0 120 120">
      <rect width="120" height="120" fill="#e9ecef"/>
      <g fill="#6c757d" text-anchor="middle">
        <circle cx="60" cy="45" r="20" fill="none" stroke="#6c757d" stroke-width="3"/>
        <path d="M30 95 Q60 75 90 95" stroke="#6c757d" stroke-width="3" fill="none"/>
        <text x="60" y="110" font-family="Arial, sans-serif" font-size="10">No Avatar</text>
      </g>
    </svg>
  `),

  // Generic fallback for any image
  generic: 'data:image/svg+xml;base64,' + btoa(`
    <svg xmlns="http://www.w3.org/2000/svg" width="200" height="200" viewBox="0 0 200 200">
      <rect width="200" height="200" fill="#f8f9fa"/>
      <g fill="#6c757d" text-anchor="middle">
        <rect x="60" y="60" width="80" height="60" rx="4" fill="none" stroke="#6c757d" stroke-width="2"/>
        <circle cx="80" cy="85" r="8" fill="none" stroke="#6c757d" stroke-width="2"/>
        <path d="M60 110 L80 95 L100 105 L140 90" stroke="#6c757d" stroke-width="2" fill="none"/>
        <text x="100" y="150" font-family="Arial, sans-serif" font-size="12">Image Not Available</text>
      </g>
    </svg>
  `)
}

export function useImageFallback() {
  // Track which images have failed to load
  const failedImages = ref(new Set())

  /**
   * Get the appropriate image URL with fallback and IGDB sizing
   * @param {string|null|undefined} imageUrl - The primary image URL
   * @param {string} fallbackType - Type of fallback ('game', 'gameSmall', 'gameIcon', 'avatar', 'generic')
   * @param {string} context - IGDB image context for sizing (optional)
   * @param {string} customSize - Custom IGDB size override (optional)
   * @returns {string} - The image URL to use
   */
  const getImageUrl = (imageUrl, fallbackType = 'generic', context = null, customSize = null) => {
    // If no image URL provided, return fallback immediately
    if (!imageUrl || failedImages.value.has(imageUrl)) {
      return fallbackImages[fallbackType] || fallbackImages.generic
    }
    
    // Apply IGDB sizing if context is provided
    if (context) {
      return getContextualIgdbImage(imageUrl, context, customSize)
    }
    
    return imageUrl
  }

  /**
   * Handle image loading errors
   * @param {Event} event - The error event from the img element
   * @param {string} fallbackType - Type of fallback to use
   */
  const handleImageError = (event, fallbackType = 'generic') => {
    const img = event.target
    const originalSrc = img.src
    
    // Mark this URL as failed
    if (originalSrc && !originalSrc.startsWith('data:')) {
      failedImages.value.add(originalSrc)
    }
    
    // Set the fallback image
    img.src = fallbackImages[fallbackType] || fallbackImages.generic
  }

  /**
   * Create a reactive image URL that automatically handles fallbacks and IGDB sizing
   * @param {Ref|ComputedRef|string} imageUrlRef - Reactive reference to the image URL
   * @param {string} fallbackType - Type of fallback to use
   * @param {string} context - IGDB image context for sizing (optional)
   * @param {string} customSize - Custom IGDB size override (optional)
   * @returns {ComputedRef} - Reactive image URL with fallback and sizing
   */
  const createReactiveImageUrl = (imageUrlRef, fallbackType = 'generic', context = null, customSize = null) => {
    return computed(() => {
      const url = typeof imageUrlRef === 'string' ? imageUrlRef : imageUrlRef.value
      return getImageUrl(url, fallbackType, context, customSize)
    })
  }

  /**
   * Clear the failed images cache
   * Useful when you want to retry loading images that previously failed
   */
  const clearFailedImageCache = () => {
    failedImages.value.clear()
  }

  /**
   * Check if an image URL has failed to load
   * @param {string} imageUrl - The image URL to check
   * @returns {boolean} - True if the image has failed to load
   */
  const hasImageFailed = (imageUrl) => {
    return failedImages.value.has(imageUrl)
  }

  /**
   * Get a specific fallback image by type
   * @param {string} fallbackType - Type of fallback image
   * @returns {string} - The fallback image data URL
   */
  const getFallbackImage = (fallbackType) => {
    return fallbackImages[fallbackType] || fallbackImages.generic
  }

  // Expose the composable API
  return {
    // Core functions
    getImageUrl,
    handleImageError,
    createReactiveImageUrl,
    
    // Utility functions
    clearFailedImageCache,
    hasImageFailed,
    getFallbackImage,
    
    // Direct access to fallback images
    fallbackImages: Object.freeze(fallbackImages),
    
    // Reactive failed images set (read-only)
    failedImages: computed(() => Array.from(failedImages.value)),
    
    // IGDB sizing utilities
    IMAGE_CONTEXTS,
    IGDB_IMAGE_SIZES
  }
}

// Export individual fallback types for convenience
export const FALLBACK_TYPES = {
  GAME: 'game',
  GAME_SMALL: 'gameSmall', 
  GAME_ICON: 'gameIcon',
  AVATAR: 'avatar',
  GENERIC: 'generic'
}

// Export fallback images directly for use in models or non-Vue contexts
export const getStaticFallbackImage = (type = 'generic') => {
  return fallbackImages[type] || fallbackImages.generic
}