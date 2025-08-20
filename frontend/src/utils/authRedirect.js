import { useRouter, useRoute } from 'vue-router'

/**
 * Utility functions for handling authentication redirects
 */

/**
 * Redirects user to login page with current route as redirect parameter
 * @param {Object} router - Vue Router instance
 * @param {Object} route - Current route object
 * @param {string} message - Optional message to display to user
 */
export function redirectToLoginWithReturn(router, route, message = 'Please sign in to continue') {
  // Build redirect URL from current route
  const redirectPath = buildRedirectPath(route)
  
  // Navigate to login with redirect parameter
  router.push({
    name: 'Login',
    query: {
      redirect: redirectPath
    }
  })
  
  return { redirected: true, message }
}

/**
 * Builds a redirect path from the current route including query parameters
 * @param {Object} route - Current route object
 * @returns {string} - Full path with query parameters
 */
export function buildRedirectPath(route) {
  let redirectPath = route.fullPath
  
  // Avoid infinite redirect loops by not redirecting to auth pages
  if (isAuthPage(route.path)) {
    redirectPath = '/dashboard'
  }
  
  return redirectPath
}

/**
 * Checks if a path is an authentication-related page
 * @param {string} path - Route path to check
 * @returns {boolean}
 */
export function isAuthPage(path) {
  const authPaths = ['/login', '/register', '/forgot-password', '/reset-password']
  return authPaths.some(authPath => path.startsWith(authPath))
}

/**
 * Composable for authentication redirect functionality
 * @returns {Object} - Object with redirect utilities
 */
export function useAuthRedirect() {
  const router = useRouter()
  const route = useRoute()
  
  /**
   * Redirects to login if user is not authenticated
   * @param {boolean} isAuthenticated - Whether user is authenticated
   * @param {string} message - Optional message
   * @returns {boolean} - True if redirected, false if authenticated
   */
  const requireAuth = (isAuthenticated, message) => {
    if (!isAuthenticated) {
      redirectToLoginWithReturn(router, route, message)
      return true
    }
    return false
  }
  
  /**
   * Gets the redirect path from query parameters
   * @returns {string} - Redirect path or default dashboard
   */
  const getRedirectPath = () => {
    const redirect = route.query.redirect
    
    // Validate redirect path (basic security)
    if (redirect && typeof redirect === 'string' && redirect.startsWith('/') && !isAuthPage(redirect)) {
      return redirect
    }
    
    return '/dashboard'
  }
  
  return {
    requireAuth,
    getRedirectPath,
    redirectToLoginWithReturn: (message) => redirectToLoginWithReturn(router, route, message),
    buildRedirectPath: () => buildRedirectPath(route)
  }
}