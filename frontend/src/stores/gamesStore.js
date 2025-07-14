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
  const CACHE_DURATION = 5 * 60 * 1000; // 5 minutes

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

  const cacheGame = (gameData, identifier = null) => {
    console.log('cacheGame called with:', gameData, identifier);

    if (!gameData) {
      console.log('cacheGame: no game data provided');
      return null;
    }

    const gameInstance = createGame(gameData);
    console.log('cacheGame: created instance:', gameInstance);

    if (!gameInstance || !gameInstance.id) {
      console.warn('cacheGame: Invalid game instance or missing ID:', gameInstance);
      return null;
    }

    const timestamp = Date.now();
    const primaryKey = String(gameInstance.id);

    // Check if we already have this game cached
    const existingGame = games.value.get(primaryKey);
    if (existingGame) {
      console.log('cacheGame: returning existing game from cache');
      return existingGame;
    }

    // Cache new game
    const keys = [
      primaryKey,
      gameInstance.slug,
      identifier ? String(identifier) : null
    ].filter(Boolean);

    console.log('cacheGame: caching with keys:', keys);

    const newGamesMap = new Map(games.value);
    keys.forEach((key) => {
      newGamesMap.set(key, gameInstance);
      cacheTimestamps.value.set(key, timestamp);
    });

    games.value = newGamesMap;
    console.log('cacheGame: cached successfully, returning:', gameInstance);
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
      return cachedGame;
    }

    // Check by slug if identifier is not numeric
    if (isNaN(identifier)) {
      const gameBySlug = getGameBySlug.value(identifier);
      if (gameBySlug) {
        console.log('getCachedGame: found game by slug:', gameBySlug);
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

      // Cache the game
      console.log('fetchGameDetails: Caching game data');
      const gameInstance = cacheGame(gameData, identifier);
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
            cacheGame(gameInstance);
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
            cacheGame(gameInstance);
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
            cacheGame(gameInstance);
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
  };

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

    // Cache management
    clearSearchResults,
    clearCache,
  };
});
