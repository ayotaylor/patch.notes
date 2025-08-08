import { defineStore } from "pinia";
import { ref, computed, shallowRef } from "vue";
import { gamesService } from "@/services/gamesService";
import { Game } from "@/models/Game";

export const useGamesStore = defineStore("games", () => {
  // State
  const games = shallowRef(new Map());
  const searchResults = ref([]);
  const popularGames = ref([]);
  const similarGames = ref(new Map());
  const newGames = ref([]);
  const loading = ref(false);
  const searchLoading = ref(false);
  const error = ref(null);

  // Cache metadata
  const cacheTimestamps = ref(new Map());
  const CACHE_DURATION = 30 * 60 * 1000; // 30 minutes

  // Cover pre-caching configuration
  const ENABLE_COVER_PRECACHING = true; // Set to false to disable cover pre-caching
  const coverPreloadQueue = ref(new Set()); // Track which covers are being preloaded
  const preloadedCovers = ref(new Set()); // Track successfully preloaded covers

  // localStorage helpers
  const CACHE_KEY = 'patchnotes_games_cache';
  const TIMESTAMPS_KEY = 'patchnotes_cache_timestamps';

  const saveToLocalStorage = () => {
    try {
      const gamesArray = Array.from(games.value.entries());
      const timestampsArray = Array.from(cacheTimestamps.value.entries());
      
      localStorage.setItem(CACHE_KEY, JSON.stringify(gamesArray));
      localStorage.setItem(TIMESTAMPS_KEY, JSON.stringify(timestampsArray));
    } catch (err) {
      console.warn('Failed to save cache to localStorage:', err);
    }
  };

  const loadFromLocalStorage = () => {
    try {
      const savedGames = localStorage.getItem(CACHE_KEY);
      const savedTimestamps = localStorage.getItem(TIMESTAMPS_KEY);
      
      if (savedGames) {
        const gamesArray = JSON.parse(savedGames);
        games.value = new Map(gamesArray.map(([key, gameData]) => [key, createGame(gameData)]));
        
        // Trigger background cover preloading for restored games
        if (ENABLE_COVER_PRECACHING) {
          // Use setTimeout to avoid blocking the initial load
          setTimeout(() => {
            preloadCoversForCachedGames();
          }, 1000); // 1 second delay to let the app initialize first
        }
      }
      
      if (savedTimestamps) {
        const timestampsArray = JSON.parse(savedTimestamps);
        cacheTimestamps.value = new Map(timestampsArray);
      }
      
      // Clean expired entries
      cleanExpiredCache();
    } catch (err) {
      console.warn('Failed to load cache from localStorage:', err);
      games.value = new Map();
      cacheTimestamps.value = new Map();
    }
  };

  const cleanExpiredCache = () => {
    const now = Date.now();
    const expiredKeys = [];
    
    for (const [key, timestamp] of cacheTimestamps.value) {
      if (now - timestamp > CACHE_DURATION) {
        expiredKeys.push(key);
      }
    }
    
    expiredKeys.forEach(key => {
      games.value.delete(key);
      cacheTimestamps.value.delete(key);
    });
    
    if (expiredKeys.length > 0) {
      saveToLocalStorage();
    }
  };

  // Getters
  const getGameById = computed(() => {
    return (gameId) => {
      const key = String(gameId);
      return games.value.get(key);
    };
  });

  const getGameBySlug = computed(() => {
    return (slug) => {
      // eslint-disable-next-line no-unused-vars
      for (const [key, game] of games.value) {
        if (game && game.slug === slug) {
          return game;
        }
      }
      return null;
    };
  });

  const allGames = computed(() => {
    return Array.from(games.value.values()).filter(Boolean);
  });

  // Cover pre-caching methods
  const preloadGameCover = async (gameInstance, priority = 'low') => {
    if (!ENABLE_COVER_PRECACHING || !gameInstance) return;

    try {
      // Get the cover using the Game model's getter
      const cover = gameInstance.cover;
      if (!cover?.imageUrl) {
        console.log('preloadGameCover: No cover URL available for game:', gameInstance.name);
        return;
      }

      const coverUrl = cover.imageUrl;
      
      // Skip if already preloaded or in queue
      if (preloadedCovers.value.has(coverUrl) || coverPreloadQueue.value.has(coverUrl)) {
        return;
      }

      // Add to queue
      coverPreloadQueue.value.add(coverUrl);

      console.log('preloadGameCover: Preloading cover for game:', gameInstance.name, 'URL:', coverUrl);

      // Create lightweight Image object for preloading
      const img = new Image();
      
      // Set up promise-based loading
      const loadPromise = new Promise((resolve, reject) => {
        img.onload = () => {
          preloadedCovers.value.add(coverUrl);
          coverPreloadQueue.value.delete(coverUrl);
          console.log('preloadGameCover: Successfully preloaded cover for:', gameInstance.name);
          resolve(coverUrl);
        };
        
        img.onerror = () => {
          coverPreloadQueue.value.delete(coverUrl);
          console.warn('preloadGameCover: Failed to preload cover for:', gameInstance.name, 'URL:', coverUrl);
          reject(new Error(`Failed to load cover: ${coverUrl}`));
        };
      });

      // Set the src to trigger loading (non-blocking)
      img.src = coverUrl;

      // For high priority (like current game being viewed), await the load
      // For medium priority, await but with shorter timeout
      if (priority === 'high') {
        await loadPromise;
      } else if (priority === 'medium') {
        // Await with timeout for medium priority
        try {
          await Promise.race([
            loadPromise,
            new Promise((_, reject) => setTimeout(() => reject(new Error('Timeout')), 2000))
          ]);
        } catch (timeoutError) {
          // Continue without waiting if timeout occurs
          console.log('preloadGameCover: Medium priority timeout for:', gameInstance.name);
        }
      }

    } catch (error) {
      console.warn('preloadGameCover: Error preloading cover for game:', gameInstance?.name, error);
      if (gameInstance) {
        const cover = gameInstance.cover;
        if (cover?.imageUrl) {
          coverPreloadQueue.value.delete(cover.imageUrl);
        }
      }
    }
  };

  const preloadMultipleGameCovers = async (gameInstances, options = {}) => {
    if (!ENABLE_COVER_PRECACHING || !Array.isArray(gameInstances)) return;

    const {
      priority = 'low',
      maxConcurrent = 3, // Limit concurrent preloads to avoid overwhelming the browser
      delayBetweenBatches = 100 // ms delay between batches
    } = options;

    console.log(`preloadMultipleGameCovers: Starting preload for ${gameInstances.length} games`);

    // Process in batches to avoid overwhelming the browser
    for (let i = 0; i < gameInstances.length; i += maxConcurrent) {
      const batch = gameInstances.slice(i, i + maxConcurrent);
      
      // Process batch in parallel
      const promises = batch.map(game => preloadGameCover(game, priority));
      
      try {
        await Promise.allSettled(promises); // Don't fail if individual images fail
      } catch (error) {
        console.warn('preloadMultipleGameCovers: Batch error:', error);
      }

      // Small delay between batches for performance
      if (i + maxConcurrent < gameInstances.length && delayBetweenBatches > 0) {
        await new Promise(resolve => setTimeout(resolve, delayBetweenBatches));
      }
    }

    console.log('preloadMultipleGameCovers: Completed preload batch');
  };

  const preloadCoversForCachedGames = async () => {
    try {
      console.log('preloadCoversForCachedGames: Starting background preload for cached games');
      
      // Get all unique game instances from cache (avoid duplicates from multiple keys)
      const uniqueGames = new Map();
      for (const gameInstance of games.value.values()) {
        if (gameInstance && gameInstance.id) {
          uniqueGames.set(gameInstance.id, gameInstance);
        }
      }
      
      const gameInstances = Array.from(uniqueGames.values());
      console.log(`preloadCoversForCachedGames: Found ${gameInstances.length} unique games to preload`);
      
      if (gameInstances.length === 0) return;
      
      // Use very conservative settings for background preloading
      await preloadMultipleGameCovers(gameInstances, {
        priority: 'low',           // Non-blocking
        maxConcurrent: 2,          // Very conservative concurrency
        delayBetweenBatches: 500   // Longer delays to be less aggressive
      });
      
      console.log('preloadCoversForCachedGames: Completed background preload');
    } catch (error) {
      console.warn('preloadCoversForCachedGames: Error during background preload:', error);
    }
  };

  // Helper methods
  const createGame = (data) => {
    console.log('createGame called with:', data);
    console.log('createGame data type:', typeof data);

    if (!data) {
      console.log('createGame: data is null/undefined');
      return null;
    }

    if (data instanceof Game) {
      console.log('createGame: data is already a Game instance');
      return data;
    }

    try {
      const gameInstance = new Game(data);
      console.log('createGame: created new Game instance:', gameInstance);
      console.log('createGame: instance type:', typeof gameInstance);
      console.log('createGame: instance constructor:', gameInstance.constructor.name);
      return gameInstance;
    } catch (err) {
      console.error('createGame: Error creating Game instance:', err);
      return null;
    }
  };

  const cacheGame = (gameData, identifier = null, options = {}) => {
    if (!gameData) return null;

    const gameInstance = createGame(gameData);
    if (!gameInstance || !gameInstance.id) return null;

    const timestamp = Date.now();
    const primaryKey = String(gameInstance.id);

    // Check if already cached
    if (games.value.has(primaryKey)) {
      const cachedInstance = games.value.get(primaryKey);
      
      // Still preload cover for already cached games if requested
      if (options.preloadCover !== false) {
        const priority = options.coverPriority || 'low';
        // Non-blocking preload
        preloadGameCover(cachedInstance, priority).catch(() => {
          // Silently handle preload failures
        });
      }
      
      return cachedInstance;
    }

    // Build keys array safely
    const keys = [
      primaryKey,
      gameInstance.slug ? String(gameInstance.slug) : null,
      identifier ? String(identifier) : null
    ].filter(Boolean);

    // Cache directly in existing Map (no new Map creation)
    keys.forEach((key) => {
      games.value.set(key, gameInstance);
      cacheTimestamps.value.set(key, timestamp);
    });

    // Persist to localStorage
    saveToLocalStorage();
    
    // Preload game cover after caching (non-blocking)
    if (options.preloadCover !== false) {
      const priority = options.coverPriority || 'low';
      preloadGameCover(gameInstance, priority).catch(() => {
        // Silently handle preload failures
      });
    }
    
    return gameInstance;
  };

  const isCacheValid = (key) => {
    const timestamp = cacheTimestamps.value.get(key);
    if (!timestamp) return false;
    return Date.now() - timestamp < CACHE_DURATION;
  };

  const getCachedGame = (identifier) => {
    if (!identifier) return null;

    const key = String(identifier);
    console.log('getCachedGame: looking for key:', key);

    // Check direct cache hit
    if (games.value.has(key) && isCacheValid(key)) {
      const cachedGame = games.value.get(key);
      console.log('getCachedGame: found cached game:', cachedGame);
      
      // Trigger immediate cover preload for accessed cached games
      if (ENABLE_COVER_PRECACHING && cachedGame) {
        preloadGameCover(cachedGame, 'high').catch(() => {
          // Silently handle preload failures
        });
      }
      
      return cachedGame;
    }

    // Check by slug if identifier is not numeric
    if (isNaN(identifier)) {
      const gameBySlug = getGameBySlug.value(identifier);
      if (gameBySlug) {
        console.log('getCachedGame: found game by slug:', gameBySlug);
        
        // Trigger immediate cover preload for accessed cached games
        if (ENABLE_COVER_PRECACHING && gameBySlug) {
          preloadGameCover(gameBySlug, 'high').catch(() => {
            // Silently handle preload failures
          });
        }
        
        return gameBySlug;
      }
    }

    console.log('getCachedGame: no cached game found');
    return null;
  };

  // Actions
  const fetchGameDetails = async (identifier) => {
    console.log('fetchGameDetails called with:', identifier);

    if (!identifier) {
      console.error('fetchGameDetails: No identifier provided');
      throw new Error('No identifier provided');
    }

    try {
      // Check cache first
      const cachedGame = getCachedGame(identifier);
      if (cachedGame) {
        console.log("fetchGameDetails: Found in cache:", cachedGame);
        return cachedGame;
      }

      console.log('fetchGameDetails: Not in cache, fetching from API');
      loading.value = true;
      error.value = null;

      // Call the service
      console.log('fetchGameDetails: Calling gamesService.getGameDetails');
      const gameData = await gamesService.getGameDetails(identifier);
      console.log("fetchGameDetails: API returned:", gameData);
      console.log("fetchGameDetails: API data type:", typeof gameData);

      if (!gameData) {
        throw new Error("Game not found");
      }

      // Cache the game with high priority cover preloading for details view
      console.log('fetchGameDetails: Caching game data');
      const gameInstance = cacheGame(gameData, identifier, { 
        preloadCover: true, 
        coverPriority: 'high' 
      });
      console.log('fetchGameDetails: Cached game instance:', gameInstance);
      console.log('fetchGameDetails: Instance type:', typeof gameInstance);

      if (!gameInstance) {
        throw new Error("Failed to create game instance");
      }

      console.log('fetchGameDetails: Returning game instance:', gameInstance);
      return gameInstance;
    } catch (err) {
      console.error("fetchGameDetails: Error:", err);
      error.value = err.message;
      throw err;
    } finally {
      loading.value = false;
    }
  };

  const searchGames = async (query) => {
    if (!query || !query.trim()) {
      searchResults.value = [];
      return [];
    }

    try {
      searchLoading.value = true;
      error.value = null;

      console.log('searchGames: Searching for:', query);
      const results = await gamesService.searchGames(query);
      console.log('searchGames: API returned:', results);

      // Handle different response structures
      const gameData = results.data || results || [];
      console.log('searchGames: Processed data:', gameData);

      if (!Array.isArray(gameData)) {
        console.error('searchGames: Expected array but got:', typeof gameData, gameData);
        searchResults.value = [];
        return [];
      }

      const gameInstances = [];

      for (const gameItem of gameData) {
        try {
          const gameInstance = createGame(gameItem);
          if (gameInstance && gameInstance.id) {
            // Cache with low priority cover preloading for search results
            cacheGame(gameInstance, null, { preloadCover: true, coverPriority: 'low' });
            gameInstances.push(gameInstance);
          } else {
            console.warn('searchGames: Invalid game data, skipping:', gameItem);
          }
        } catch (gameError) {
          console.error('searchGames: Error creating game instance:', gameError, gameItem);
        }
      }

      searchResults.value = gameInstances;
      console.log('searchGames: Final results:', gameInstances.length, 'games');

      return gameInstances;
    } catch (err) {
      console.error("searchGames: Error:", err);
      error.value = err.message;
      searchResults.value = [];
      throw err;
    } finally {
      searchLoading.value = false;
    }
  };

  const fetchPopularGames = async (limit = 10, force = false) => {
    const cacheKey = `popular_games_${limit}`;

    if (!force && popularGames.value.length > 0 && isCacheValid(cacheKey)) {
      return popularGames.value;
    }

    try {
      loading.value = true;
      error.value = null;

      const results = await gamesService.getPopularGames(limit);

      if (!Array.isArray(results)) {
        console.error('fetchPopularGames: Expected array but got:', typeof results);
        return [];
      }

      const gameInstances = [];

      for (const gameItem of results) {
        try {
          const gameInstance = createGame(gameItem);
          if (gameInstance && gameInstance.id) {
            // Cache with medium priority cover preloading for popular games
            cacheGame(gameInstance, null, { preloadCover: true, coverPriority: 'medium' });
            gameInstances.push(gameInstance);
          }
        } catch (gameError) {
          console.error('fetchPopularGames: Error creating game instance:', gameError);
        }
      }

      popularGames.value = gameInstances;
      cacheTimestamps.value.set(cacheKey, Date.now());

      return gameInstances;
    } catch (err) {
      console.error("fetchPopularGames: Error:", err);
      error.value = err.message;
      throw err;
    } finally {
      loading.value = false;
    }
  };

  const fetchSimilarGames = async (gameId, limit = 10) => {
    if (!gameId) {
      console.warn('fetchSimilarGames: No gameId provided');
      return [];
    }

    try {
      loading.value = true;
      error.value = null;

      const results = await gamesService.getSimilarGames(gameId, limit);

      if (!Array.isArray(results)) {
        console.error('fetchSimilarGames: Expected array but got:', typeof results);
        return [];
      }

      const gameInstances = [];

      for (const gameItem of results) {
        try {
          const gameInstance = createGame(gameItem);
          if (gameInstance && gameInstance.id) {
            // Cache with low priority cover preloading for similar games
            cacheGame(gameInstance, null, { preloadCover: true, coverPriority: 'low' });
            gameInstances.push(gameInstance);
          }
        } catch (gameError) {
          console.error('fetchSimilarGames: Error creating game instance:', gameError);
        }
      }

      return gameInstances;
    } catch (err) {
      console.error("fetchSimilarGames: Error:", err);
      error.value = err.message;
      return [];
    } finally {
      loading.value = false;
    }
  };

  const fetchNewGames = async (limit = 10, force = false) => {
    const cacheKey = `new_games_${limit}`;

    if (!force && newGames.value.length > 0 && isCacheValid(cacheKey)) {
      return newGames.value;
    }

    try {
      loading.value = true;
      error.value = null;

      const results = await gamesService.getNewGames(limit)

      if (!Array.isArray(results)) {
        console.error('fetchNewGames: Expected array but got:', typeof results);
        return [];
      }

      const gameInstances = [];

      for (const gameItem of results) {
        try {
          const gameInstance = createGame(gameItem);
          if (gameInstance && gameInstance.id) {
            // Cache with medium priority cover preloading for new games
            cacheGame(gameInstance, null, { preloadCover: true, coverPriority: 'medium' });
            gameInstances.push(gameInstance);
          }
        } catch (gameError) {
          console.error('fetchNewGames: Error creating game instance:', gameError);
        }
      }

      newGames.value = gameInstances;
      cacheTimestamps.value.set(cacheKey, Date.now());

      return gameInstances;
    } catch (err) {
      console.error("fetchSimilarGames: Error:", err);
      error.value = err.message;
      return [];
    } finally {
      loading.value = false;
    }
  };

  const removeFromFavorites = async (userId, gameId) => {
    if (!userId) {
      console.warn('removeFromFavorites: No userId provided');
      return;
    }
    if (!gameId) {
      console.warn('removeFromFavorites: No gameId provided');
      return;
    }
    try {
      loading.value = true;
      error.value = null;

      const result = await gamesService.removeFromFavorites(userId, gameId)

      if(!result) {
        throw new Error("User favorite operation failed")
      }

      if (typeof result !== 'boolean') {
        console.error('removeFromFavorites: Expected boolean but got:', typeof result);
        return;
      }

      return result
    } catch (err) {
      console.error("removeFromFavorites: Error:", err);
      error.value = err.message;
      return [];
    } finally {
      loading.value = false;
    }
  };

  const addToFavorites = async (userId, gameId) => {
    if (!userId) {
      console.warn('addToFavorites: No userId provided');
      return;
    }
    if (!gameId) {
      console.warn('addToFavorites: No gameId provided');
      return;
    }
    try {
      loading.value = true;
      error.value = null;

      const result = await gamesService.addToFavorites(userId, gameId)

      if(!result) {
        throw new Error("User favorite operation failed")
      }

      if (typeof result !== 'boolean') {
        console.error('addToFavorites: Expected boolean but got:', typeof result);
        return;
      }

      return result
    } catch (err) {
      console.error("addToFavorites: Error:", err);
      error.value = err.message;
      return [];
    } finally {
      loading.value = false;
    }
  };

  const getUserFavorites = async (userId) => {
    if (!userId) {
      console.warn('getUserFavorites: No userId provided');
      return;
    }

    try {
      loading.value = true;
      error.value = null;

      const result = await gamesService.getUserFavorites(userId);

      if(!result) {
        throw new Error("Get User favorites operation failed");
      }

      if (!Array.isArray(result)) {
        console.error('getUserFavorites: Expected list but got: ', typeof result);
        return;
      }

      return result;
    } catch (err) {
      console.error("getUserFavorites: Error:", err);
      error.value = err.message;
      return [];
    } finally {
      loading.value = false;
    }
  };

  // Cache management
  const clearSearchResults = () => {
    searchResults.value = [];
  };

  const clearCache = () => {
    games.value = new Map();
    cacheTimestamps.value.clear();
    searchResults.value = [];
    popularGames.value = [];
    similarGames.value.clear();
    newGames.value = [];
    
    // Clear cover preload cache
    coverPreloadQueue.value.clear();
    preloadedCovers.value.clear();
    
    // Clear localStorage
    try {
      localStorage.removeItem(CACHE_KEY);
      localStorage.removeItem(TIMESTAMPS_KEY);
    } catch (err) {
      console.warn('Failed to clear localStorage cache:', err);
    }
  };

  // Cover preload management methods
  const clearCoverPreloadCache = () => {
    coverPreloadQueue.value.clear();
    preloadedCovers.value.clear();
  };

  const getCoverPreloadStats = () => {
    return {
      queued: coverPreloadQueue.value.size,
      preloaded: preloadedCovers.value.size,
      enabled: ENABLE_COVER_PRECACHING
    };
  };

  // Initialize cache from localStorage
  loadFromLocalStorage();

  return {
    // State
    games: allGames,
    searchResults,
    popularGames,
    newGames,
    loading,
    searchLoading,
    error,

    // Getters
    getGameById,
    getGameBySlug,

    // Actions
    fetchGameDetails,
    searchGames,
    fetchPopularGames,
    fetchSimilarGames,
    fetchNewGames,
    removeFromFavorites,
    addToFavorites,
    getUserFavorites,

    // Cache management
    clearSearchResults,
    clearCache,

    // Cover preloading (exposed for manual control if needed)
    preloadGameCover,
    preloadMultipleGameCovers,
    clearCoverPreloadCache,
    getCoverPreloadStats,
  };
});
