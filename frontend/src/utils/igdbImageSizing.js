/**
 * IGDB Image Sizing Utility
 * 
 * Provides dynamic image sizing for IGDB images based on context and use case.
 * Transforms IGDB URLs from thumb size to appropriate sizes for different components.
 * 
 * IGDB Image URL Format: https://images.igdb.com/igdb/image/upload/t_{size}/{hash}.jpg
 * 
 * Available sizes and their dimensions:
 * - cover_small: 90 x 128 (Fit)
 * - cover_big: 264 x 374 (Fit)
 * - screenshot_med: 569 x 320 (Lfill, Center gravity)
 * - screenshot_big: 889 x 500 (Lfill, Center gravity)
 * - screenshot_huge: 1280 x 720 (Lfill, Center gravity)
 * - logo_med: 284 x 160 (Fit)
 * - thumb: 90 x 90 (Thumb, Center gravity)
 * - micro: 35 x 35 (Thumb, Center gravity)
 * - 720p: 1280 x 720 (Fit, Center gravity)
 * - 1080p: 1920 x 1080 (Fit, Center gravity)
 */

// Image size configurations
export const IGDB_IMAGE_SIZES = {
  // Cover art sizes
  COVER_SMALL: 'cover_small',     // 90 x 128
  COVER_BIG: 'cover_big',         // 264 x 374
  
  // Screenshot sizes
  SCREENSHOT_MED: 'screenshot_med',     // 569 x 320
  SCREENSHOT_BIG: 'screenshot_big',     // 889 x 500
  SCREENSHOT_HUGE: 'screenshot_huge',   // 1280 x 720
  
  // Logo size
  LOGO_MED: 'logo_med',           // 284 x 160
  
  // Thumbnail sizes
  THUMB: 'thumb',                 // 90 x 90
  MICRO: 'micro',                 // 35 x 35
  
  // High resolution sizes
  HD_720P: '720p',                // 1280 x 720
  FULL_HD_1080P: '1080p'          // 1920 x 1080
}

// Context-based size mapping
export const IMAGE_CONTEXTS = {
  // Game cards in lists/grids
  GAME_CARD: 'game_card',
  GAME_CARD_SMALL: 'game_card_small',
  
  // Game detail pages
  GAME_DETAIL_MAIN: 'game_detail_main',
  GAME_DETAIL_SIDEBAR: 'game_detail_sidebar',
  
  // Screenshots
  SCREENSHOT_THUMBNAIL: 'screenshot_thumbnail',
  SCREENSHOT_MODAL: 'screenshot_modal',
  
  // Similar games and recommendations
  SIMILAR_GAME: 'similar_game',
  
  // List items
  LIST_ITEM: 'list_item',
  
  // Profile and dashboard
  PROFILE_GAME: 'profile_game',
  DASHBOARD_FEATURED: 'dashboard_featured'
}

// Default size mappings for each context
const CONTEXT_SIZE_MAP = {
  [IMAGE_CONTEXTS.GAME_CARD]: IGDB_IMAGE_SIZES.COVER_BIG,
  [IMAGE_CONTEXTS.GAME_CARD_SMALL]: IGDB_IMAGE_SIZES.COVER_SMALL,
  [IMAGE_CONTEXTS.GAME_DETAIL_MAIN]: IGDB_IMAGE_SIZES.HD_720P,
  [IMAGE_CONTEXTS.GAME_DETAIL_SIDEBAR]: IGDB_IMAGE_SIZES.COVER_BIG,
  [IMAGE_CONTEXTS.SCREENSHOT_THUMBNAIL]: IGDB_IMAGE_SIZES.SCREENSHOT_MED,
  [IMAGE_CONTEXTS.SCREENSHOT_MODAL]: IGDB_IMAGE_SIZES.SCREENSHOT_HUGE,
  [IMAGE_CONTEXTS.SIMILAR_GAME]: IGDB_IMAGE_SIZES.COVER_SMALL,
  [IMAGE_CONTEXTS.LIST_ITEM]: IGDB_IMAGE_SIZES.THUMB,
  [IMAGE_CONTEXTS.PROFILE_GAME]: IGDB_IMAGE_SIZES.COVER_SMALL,
  [IMAGE_CONTEXTS.DASHBOARD_FEATURED]: IGDB_IMAGE_SIZES.COVER_BIG
}

/**
 * Check if a URL is an IGDB image URL
 * @param {string} url - The image URL to check
 * @returns {boolean} - True if it's an IGDB image URL
 */
export function isIgdbImageUrl(url) {
  if (!url) return false
  return url.includes('images.igdb.com/igdb/image/upload/')
}

/**
 * Extract the image hash from an IGDB URL
 * @param {string} url - The IGDB image URL
 * @returns {string|null} - The image hash or null if not found
 */
export function extractIgdbImageHash(url) {
  if (!isIgdbImageUrl(url)) return null
  
  const match = url.match(/\/t_[^/]+\/([^/.]+)/)
  return match ? match[1] : null
}

/**
 * Transform an IGDB image URL to use a specific size
 * @param {string} url - The original IGDB image URL
 * @param {string} size - The desired IGDB size (e.g., 'cover_big', 'screenshot_med')
 * @returns {string} - The transformed URL with the new size
 */
export function transformIgdbImageSize(url, size) {
  if (!isIgdbImageUrl(url)) {
    return url // Return original URL if not an IGDB image
  }
  
  const hash = extractIgdbImageHash(url)
  if (!hash) {
    return url // Return original if can't extract hash
  }
  
  // Construct new URL with desired size
  return `https://images.igdb.com/igdb/image/upload/t_${size}/${hash}.jpg`
}

/**
 * Get the appropriate IGDB image URL for a given context
 * @param {string} url - The original IGDB image URL
 * @param {string} context - The context where the image will be used
 * @param {string} customSize - Optional custom size to override context default
 * @returns {string} - The optimized image URL
 */
export function getContextualIgdbImage(url, context, customSize = null) {
  if (!url) return url
  
  if (!isIgdbImageUrl(url)) {
    return url // Return original URL if not an IGDB image
  }
  
  // Use custom size if provided, otherwise use context mapping
  const targetSize = customSize || CONTEXT_SIZE_MAP[context] || IGDB_IMAGE_SIZES.COVER_BIG
  
  return transformIgdbImageSize(url, targetSize)
}

/**
 * Get multiple sized versions of an IGDB image for responsive use
 * @param {string} url - The original IGDB image URL
 * @param {string[]} sizes - Array of desired sizes
 * @returns {Object} - Object with size keys and corresponding URLs
 */
export function getMultipleSizedImages(url, sizes = []) {
  if (!isIgdbImageUrl(url)) {
    return { original: url }
  }
  
  const result = {}
  
  sizes.forEach(size => {
    result[size] = transformIgdbImageSize(url, size)
  })
  
  return result
}

/**
 * Get a srcset string for responsive images
 * @param {string} url - The original IGDB image URL
 * @param {Object} sizeMap - Map of sizes to widths (e.g., { cover_small: '90w', cover_big: '264w' })
 * @returns {string} - srcset string for responsive images
 */
export function getIgdbImageSrcSet(url, sizeMap = {}) {
  if (!isIgdbImageUrl(url)) {
    return url
  }
  
  const srcsetParts = []
  
  Object.entries(sizeMap).forEach(([size, width]) => {
    const sizedUrl = transformIgdbImageSize(url, size)
    srcsetParts.push(`${sizedUrl} ${width}`)
  })
  
  return srcsetParts.join(', ')
}

/**
 * Vue composable for IGDB image sizing
 * @returns {Object} - Object with image sizing functions
 */
export function useIgdbImageSizing() {
  return {
    isIgdbImageUrl,
    extractIgdbImageHash,
    transformIgdbImageSize,
    getContextualIgdbImage,
    getMultipleSizedImages,
    getIgdbImageSrcSet,
    IGDB_IMAGE_SIZES,
    IMAGE_CONTEXTS
  }
}

// Default export for convenience
export default {
  isIgdbImageUrl,
  extractIgdbImageHash,
  transformIgdbImageSize,
  getContextualIgdbImage,
  getMultipleSizedImages,
  getIgdbImageSrcSet,
  useIgdbImageSizing,
  IGDB_IMAGE_SIZES,
  IMAGE_CONTEXTS
}