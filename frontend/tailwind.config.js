/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{vue,js,ts,jsx,tsx}'],
  theme: {
    extend: {
      fontFamily: {
        newsreader: ['Newsreader', 'serif'],
        tinos: ['Tinos', 'serif'],
        roboto: ['Roboto', 'sans-serif'],
      },
      colors: {
        'cod-gray': '#121212',
        'river-bed': '#4B5563',
        'pale-sky': '#6B7280',
        'ebony-clay': '#1F2937',
      },
      maxWidth: {
        '1280': '1280px',
      },
    },
  },
  plugins: [],
}
