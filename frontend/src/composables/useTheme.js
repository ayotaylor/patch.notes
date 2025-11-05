import { ref } from 'vue'

const STORAGE_KEY = 'patchnotes_theme'
const isDark = ref(false)
let initialized = false

// Initialize theme immediately (before any component mounts)
const initializeTheme = () => {
  if (initialized) return

  // Check localStorage first
  const stored = localStorage.getItem(STORAGE_KEY)
  console.log('initializeTheme - stored value:', stored)

  if (stored) {
    isDark.value = stored === 'dark'
    console.log('initializeTheme - using stored, isDark:', isDark.value)
  } else {
    // Detect system preference
    isDark.value = window.matchMedia('(prefers-color-scheme: dark)').matches
    console.log('initializeTheme - using system preference, isDark:', isDark.value)
  }

  applyTheme()
  initialized = true
}

const applyTheme = () => {
  console.log('applyTheme called, isDark:', isDark.value)
  if (isDark.value) {
    document.documentElement.classList.add('dark')
    console.log('applyTheme - added dark class to html')
  } else {
    document.documentElement.classList.remove('dark')
    console.log('applyTheme - removed dark class from html')
  }
  console.log('applyTheme - html classes:', document.documentElement.className)
}

const toggleTheme = () => {
  console.log('toggleTheme called, current isDark:', isDark.value)
  isDark.value = !isDark.value
  console.log('toggleTheme new isDark:', isDark.value)
  localStorage.setItem(STORAGE_KEY, isDark.value ? 'dark' : 'light')
  console.log('toggleTheme - saved to localStorage:', localStorage.getItem(STORAGE_KEY))
  applyTheme()
}

// Watch for system preference changes
const setupSystemPreferenceWatcher = () => {
  const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)')

  const handleChange = (e) => {
    // Only update if user hasn't manually set a preference
    if (!localStorage.getItem(STORAGE_KEY)) {
      isDark.value = e.matches
      applyTheme()
    }
  }

  // Modern browsers
  if (mediaQuery.addEventListener) {
    mediaQuery.addEventListener('change', handleChange)
  } else {
    // Fallback for older browsers
    mediaQuery.addListener(handleChange)
  }
}

// Initialize immediately when module loads
if (typeof window !== 'undefined') {
  initializeTheme()
  setupSystemPreferenceWatcher()
}

// Debug function - expose to window for testing
// const debugTheme = () => {
//   console.log('=== THEME DEBUG INFO ===')
//   console.log('isDark.value:', isDark.value)
//   console.log('localStorage value:', localStorage.getItem(STORAGE_KEY))
//   console.log('HTML element classes:', document.documentElement.className)
//   console.log('initialized:', initialized)
//   console.log('System prefers dark:', window.matchMedia('(prefers-color-scheme: dark)').matches)
//   console.log('=======================')
// }

// Expose debug function to window
// if (typeof window !== 'undefined') {
//   window.debugTheme = debugTheme
// }

export function useTheme() {
  // Ensure theme is initialized (in case this is called before module initialization)
  if (!initialized) {
    initializeTheme()
  }

  return {
    isDark,
    toggleTheme
  }
}
