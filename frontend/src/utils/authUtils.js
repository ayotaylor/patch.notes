export const AUTH_STORAGE_KEY = 'authToken'
export const USER_STORAGE_KEY = 'user'

export function getStoredToken() {
  return localStorage.getItem(AUTH_STORAGE_KEY)
}

export function getStoredUser() {
  const userStr = localStorage.getItem(USER_STORAGE_KEY)
  try {
    return userStr ? JSON.parse(userStr) : null
  } catch (error) {
    console.error('Error parsing stored user:', error)
    return null
  }
}

export function setAuthData(token, user) {
  localStorage.setItem(AUTH_STORAGE_KEY, token)
  localStorage.setItem(USER_STORAGE_KEY, JSON.stringify(user))
}

export function clearAuthData() {
  localStorage.removeItem(AUTH_STORAGE_KEY)
  localStorage.removeItem(USER_STORAGE_KEY)
}

export function isAuthenticated() {
  return !!getStoredToken()
}

// Format user display name
export function formatUserName(user) {
  if (!user) return ''
  const { firstName, lastName } = user
  return `${firstName || ''} ${lastName || ''}`.trim() || user.email || 'User'
}

// Format date
export function formatDate(dateString) {
  if (!dateString) return ''
  try {
    return new Date(dateString).toLocaleDateString()
  } catch (error) {
    return dateString
  }
}

// Format provider name
export function formatProvider(provider) {
  if (!provider) return 'Local'
  return provider.charAt(0).toUpperCase() + provider.slice(1).toLowerCase()
}