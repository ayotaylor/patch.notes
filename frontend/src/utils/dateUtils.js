/**
 * Date utility functions for formatting dates
 */

/**
 * Format date to relative time (e.g., "2 days ago", "yesterday", "today")
 * @param {Date|string|number} dateValue - Date to format
 * @returns {string} Relative time string
 */
export function getRelativeTime(dateValue) {
  if (!dateValue) return ''

  try {
    let date = dateValue

    // Handle string dates
    if (typeof dateValue === 'string') {
      date = new Date(dateValue)
    }

    // Handle Unix timestamps
    if (typeof dateValue === 'number') {
      date = new Date(dateValue * 1000)
    }

    if (!(date instanceof Date) || isNaN(date.getTime())) {
      return ''
    }

    const now = new Date()
    const diffTime = Math.abs(now - date)
    const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24))

    if (diffDays === 0) return 'today'
    if (diffDays === 1) return 'yesterday'
    if (diffDays < 7) return `${diffDays} days ago`
    if (diffDays < 30) return `${Math.ceil(diffDays / 7)} weeks ago`
    if (diffDays < 365) return `${Math.ceil(diffDays / 30)} months ago`
    return `${Math.ceil(diffDays / 365)} years ago`
  } catch (error) {
    console.warn('Error calculating relative date:', error)
    return ''
  }
}

/**
 * Format date to full date string (e.g., "January 1, 2024")
 * @param {Date|string|number} dateValue - Date to format
 * @returns {string} Formatted date string
 */
export function formatDate(dateValue) {
  if (!dateValue) return ''

  try {
    let date = dateValue

    // Handle string dates
    if (typeof dateValue === 'string') {
      date = new Date(dateValue)
    }

    // Handle Unix timestamps
    if (typeof dateValue === 'number') {
      date = new Date(dateValue * 1000)
    }

    if (!(date instanceof Date) || isNaN(date.getTime())) {
      return ''
    }

    return date.toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric'
    })
  } catch (error) {
    console.warn('Error formatting date:', error)
    return ''
  }
}