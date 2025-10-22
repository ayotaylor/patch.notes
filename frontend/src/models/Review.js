/**
 * Review Model
 *
 * Represents a game review with user information, rating, and engagement metrics
 */
export class Review {
  constructor(data = {}) {
    // Core properties
    this.id = data.id || null
    this.reviewText = data.reviewText || data.text || ''
    this.rating = data.rating || 0
    this.isLikedByCurrentUser = Boolean(data.isLikedByCurrentUser || data.isLiked)

    // User information
    this.user = {
      id: data.user?.id || data.userId || null,
      username: data.user?.username || data.username || '',
      displayName: data.user?.displayName || data.user?.username || data.displayName || '',
      profileImageUrl: data.user?.profileImageUrl || data.userAvatar || null
    }

    // Game information
    this.game = {
      id: data.game?.id || data.gameId || null,
      name: data.game?.name || data.gameName || '',
      slug: data.game?.slug || data.gameSlug || '',
      releaseYear: data.game?.releaseYear || data.releaseYear || this._extractYear(data.game?.firstReleaseDate),
      primaryImageUrl: data.game?.primaryImageUrl || data.gameImage || null
    }

    // Engagement metrics
    this.likeCount = data.likeCount || data.likesCount || 0
    this.commentCount = data.commentCount || data.commentsCount || 0

    // Timestamps
    this.createdAt = this._parseDate(data.createdAt) || new Date()
    this.updatedAt = this._parseDate(data.updatedAt) || new Date()
  }

  // Helper methods
  _parseDate(dateValue) {
    if (!dateValue) return null

    try {
      // Handle Unix timestamps
      if (typeof dateValue === 'number') {
        return new Date(dateValue * 1000)
      }

      // Handle Date objects
      if (dateValue instanceof Date) {
        return dateValue
      }

      // Handle date strings
      const parsed = new Date(dateValue)
      return isNaN(parsed.getTime()) ? null : parsed
    } catch (error) {
      console.warn('Error parsing date:', dateValue, error)
      return null
    }
  }

  _extractYear(dateValue) {
    if (!dateValue) return null

    try {
      const date = this._parseDate(dateValue)
      return date ? date.getFullYear() : null
    } catch (error) {
      console.warn('Error extracting year:', error)
      return null
    }
  }

  // Computed properties
  get formattedDate() {
    if (!this.createdAt) return ''

    try {
      return this.createdAt.toLocaleDateString('en-US', {
        year: 'numeric',
        month: 'long',
        day: 'numeric'
      })
    } catch (error) {
      console.warn('Error formatting date:', error)
      return ''
    }
  }

  get relativeDate() {
    if (!this.createdAt) return ''

    try {
      const now = new Date()
      const diffTime = Math.abs(now - this.createdAt)
      const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24))

      if (diffDays === 0) return 'Today'
      if (diffDays === 1) return 'Yesterday'
      if (diffDays < 7) return `${diffDays} days ago`
      if (diffDays < 30) return `${Math.ceil(diffDays / 7)} weeks ago`
      if (diffDays < 365) return `${Math.ceil(diffDays / 30)} months ago`
      return `${Math.ceil(diffDays / 365)} years ago`
    } catch (error) {
      console.warn('Error calculating relative date:', error)
      return ''
    }
  }

  get ratingStars() {
    try {
      const fullStars = Math.floor(this.rating)
      const hasHalfStar = this.rating % 1 >= 0.5
      const emptyStars = 5 - fullStars - (hasHalfStar ? 1 : 0)

      return {
        full: fullStars,
        half: hasHalfStar ? 1 : 0,
        empty: emptyStars
      }
    } catch (error) {
      console.warn('Error calculating rating stars:', error)
      return { full: 0, half: 0, empty: 5 }
    }
  }

  get reviewUrl() {
    return `/${this.user.username}/game/${this.game.slug}`
  }

  // Utility methods
  toggleLike() {
    this.isLikedByCurrentUser = !this.isLikedByCurrentUser
    this.likeCount += this.isLikedByCurrentUser ? 1 : -1
  }

  // Serialization methods
  toJSON() {
    return {
      id: this.id,
      reviewText: this.reviewText,
      rating: this.rating,
      isLikedByCurrentUser: this.isLikedByCurrentUser,
      user: this.user,
      game: this.game,
      likeCount: this.likeCount,
      commentCount: this.commentCount,
      createdAt: this.createdAt?.toISOString(),
      updatedAt: this.updatedAt?.toISOString()
    }
  }

  // Static factory methods
  static fromAPI(apiData) {
    return new Review(apiData)
  }

  static fromJSONArray(jsonArray) {
    return jsonArray.map(data => new Review(data))
  }

  // For debugging
  get debugInfo() {
    return {
      id: this.id,
      username: this.user.username,
      gameName: this.game.name,
      rating: this.rating,
      likeCount: this.likeCount,
      commentCount: this.commentCount
    }
  }
}