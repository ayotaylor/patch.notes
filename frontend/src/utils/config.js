// API Configuration
const API_BASE_URL = process.env.VUE_APP_API_BASE_URL || 'http://localhost:5174';

export const config = {
  apiBaseUrl: API_BASE_URL,
  endpoints: {
    recommendations: {
      search: '/api/recommendation/search',
      personalized: '/api/recommendation/personalized', 
      continue: '/api/recommendation/continue',
      examples: '/api/recommendation/examples',
      health: '/api/recommendation/health'
    }
  }
};

export default config;