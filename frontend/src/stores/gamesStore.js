import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { gamesService } from '@/services/gamesService'
import { Game } from '@/models/Game'

export const useGamesStore = defineStore('games', () => {
    // State
    const games = ref(new Map()) // Cache games by ID
    const searchResults = ref([])
    const popularGames = ref([])
    const newGames = ref([]) // For future use, e.g. new releases
    const loading = ref(false)
    const searchLoading = ref(false)
    const error = ref(null)

    // Getters
    const getGameById = computed(() => {
        return (gameId) => games.value.get(gameId)
    })

    // Actions
    const fetchGameDetails = async (identifier) => {
        try {
            console.log('gamesStore.fetchGameDetails called with:', identifier)

            if (games.value.has(String(identifier))) {
                console.log('Found in cache:', identifier)
                return games.value.get(String(identifier))
            }

            loading.value = true
            error.value = null

            // let game;
            // if (!isNaN(identifier) && !isNaN(parseFloat(identifier))) {
            //     // It's a number, use getGameDetails
            //     game = await gamesService.getGameDetails(identifier)
            // } else {
            //     // It's a string, use getGameDetailsBySlug
            //     game = await gamesService.getGameDetailsBySlug(identifier)
            // }
            const game = await gamesService.getGameDetails(identifier)

            console.log('Game fetched from API:', game)

            if (!game) {
                throw new Error('Game not found')
            }
            games.value.set(identifier, game)
            // Also cache by ID if we have it (for future lookups)
            if (game && game.id && game.id !== identifier) {
                games.value.set(game.id, game)
            }

            return game
        } catch (err) {
            error.value = err.message
            throw err
        } finally {
            loading.value = false
        }
    }

    const searchGames = async (query) => {
        if (!query.trim()) {
            searchResults.value = []
            return []
        }

        try {
            searchLoading.value = true
            error.value = null

            const results = await gamesService.searchGames(query)
            searchResults.value = results.data.map(game => new Game(game)); // Assuming results.data is an array of game objects

            // Cache the games
            searchResults.value.forEach(game => {
                if (!games.value.has(game.id)) {
                    games.value.set(game.id, game)
                }
            })

            return results
        } catch (err) {
            error.value = err.message
            searchResults.value = []
            throw err
        } finally {
            searchLoading.value = false
        }
    }

    const fetchPopularGames = async (limit = 10) => {
        try {
            loading.value = true
            error.value = null

            const results = await gamesService.getPopularGames(limit)
            popularGames.value = results

            // Cache the games
            results.forEach(game => {
                if (!games.value.has(game.id())) {
                    games.value.set(game.id, game)
                }
            })

            return results
        } catch (err) {
            error.value = err.message
            throw err
        } finally {
            loading.value = false
        }
    }

    const fetchSimilarGames = async (gameId, limit = 10) => {
        try {
            loading.value = true
            error.value = null

            const similarGames = await gamesService.getSimilarGames(gameId, limit)
            // Cache similar games if needed
            similarGames.forEach(game => {
                if (!games.value.has(game.id)) {
                    games.value.set(game.id, game)
                }
            })

            return similarGames
        } catch (err) {
            error.value = err.message
            throw err
        } finally {
            loading.value = false
        }
    }

    const clearSearchResults = () => {
        searchResults.value = []
    }

    const clearCache = () => {
        games.value.clear()
        searchResults.value = []
        popularGames.value = []
        newGames.value = []
    }

    const allGames = computed(() => {
        return Array.from(games.value.values())
    })

    const gamesByGenre = computed(() => {
        const genreMap = new Map()
        games.value.forEach(game => {
            if (game.genres && game.genres.length > 0) {
                game.genres.forEach(genre => {
                    if (!genreMap.has(genre.id)) {
                        genreMap.set(genre.id, { ...genre, games: [] })
                    }
                    genreMap.get(genre.id).games.push(game)
                })
            }
        })
        return Array.from(genreMap.values())
    })

    //   const fetchNewGames = async (limit = 10) => {
    //     try {
    //       loading.value = true
    //       error.value = null

    //       const results = await gamesService.getNewGames(limit)
    //       newGames.value = results

    //       // Cache the games
    //       results.forEach(game => {
    //         games.value.set(game.id, game)
    //       })

    //       return results
    //     } catch (err) {
    //       error.value = err.message
    //       throw err
    //     } finally {
    //       loading.value = false
    //     }
    //   }

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
        gamesByGenre,

        // Actions\
        fetchGameDetails,
        searchGames,
        fetchPopularGames,
        //fetchNewGames,
        fetchSimilarGames,
        clearSearchResults,
        clearCache,
    }
})