/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{vue,js,ts,jsx,tsx}'],
  darkMode: 'class', // Enable class-based dark mode
  theme: {
    extend: {
      fontFamily: {
        newsreader: ['Newsreader', 'serif'],
        tinos: ['Tinos', 'serif'],
        roboto: ['Roboto', 'sans-serif'],
      },
      colors: {
        // Original colors (kept for backwards compatibility)
        'cod-gray': '#121212',
        'river-bed': '#4B5563',
        'pale-sky': '#6B7280',
        'ebony-clay': '#1F2937',

        // Semantic theme colors - SINGLE SOURCE OF TRUTH
        'theme': {
          // Backgrounds
          'bg-primary': '#F6F7F7',      // Light mode page background
          'bg-primary-dark': '#111827',  // Dark mode page background
          'bg-secondary': '#FFFFFF',     // Light mode card background
          'bg-secondary-dark': '#1F2937', // Dark mode card background

          // Text colors
          'text-primary': '#121212',     // Light mode primary text
          'text-primary-dark': '#9ab',   // Dark mode primary text
          'text-secondary': '#4B5563',   // Light mode secondary text
          'text-secondary-dark': '#9CA3AF', // Dark mode secondary text

          // Borders
          'border': '#D1D5DB',           // Light mode border
          'border-dark': '#374151',      // Dark mode border

          // Buttons
          'btn-primary': '#121212',      // Light mode primary button
          'btn-primary-dark': '#374151', // Dark mode primary button
          'btn-secondary': '#FFFFFF',    // Light mode secondary button
          'btn-secondary-dark': '#1F2937', // Dark mode secondary button
        },
      },
      maxWidth: {
        '1280': '1280px',
      },
    },
  },
  plugins: [],
}
