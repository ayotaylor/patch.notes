import apiClient from "./apiClient";

export const gamesService = {
    // Search games
    async searchGames(query) {
        try {
            if (!query || typeof query !== 'string') {
                throw new Error("Query must be a non-empty string")
            }
            if (query.trim() === '') {
                return [] // Return empty array for empty queries   
            }
            query = query.trim()
            const response = await apiClient.get(`/games/search?Search=${encodeURIComponent(query)}`)
            return response.data.data || {}
        } catch (error) {
            console.log(error)
            throw new Error(
                error.response?.data?.message || "Failed to search games"
            )
        }
        // const { get } = useApi()
        // const response = await get(`/api/games/search?q=${encodeURIComponent(query)}`)
        // return response.games || []
    },

    // Get popular games
    async getPopularGames(limit = 10) {
        try {
            if (typeof limit !== 'number' || limit <= 0) {
                throw new Error("Limit must be a positive number")
            }
            const response = await apiClient.get(`/games/popular?limit=${limit}`)
            return response.data || []
        } catch (error) {
            throw new Error(
                error.response?.data?.message || "Failed to fetch popular games"
            )
        }
        // const { get } = useApi()
        // const response = await get(`/api/games/popular?limit=${limit}`)
        // return response.games || []
    },

    // Get game details
    async getGameDetails(identifier) {
        try {
             if (!identifier 
                    || (typeof identifier !== 'string' 
                            && typeof identifier !== 'number')) {
                throw new Error("Game identifier must be a non-empty string or number")
            }
            const response = await apiClient.get(`/games/${identifier}`)
            console.log('gamesService.getGameDetails called with:', identifier, typeof identifier)
            console.log('API response:', response.data)
            return response.data.data || null
        } catch (error) {
            throw new Error(
                error.response?.data?.message || "Failed to fetch game details"
            )
        }
    },

    // async getGameDetailsBySlug(slug) {
    //     try {
    //         if (!slug || typeof slug !== 'string') {
    //             throw new Error("Game ID must be a non-empty string")
    //         }
    //         const response = await apiClient.get(`/games/${encodeURIComponent(slug)}`)
    //         return response.data.data || null
    //     } catch (error) {
    //         throw new Error(
    //             error.response?.data?.message || "Failed to fetch game details"
    //         )
    //     }
    // },

    async getSimilarGames(gameId, limit) {
        try {
            if (!gameId || (typeof gameId !== 'string' && typeof gameId !== 'number')) {
                throw new Error("Game ID must be a non-empty string or number")
            }
            const response = await apiClient.get(`/games/${gameId}/similar?limit=${limit}`)
            return response.similarGames || []
        } catch (error) {
            throw new Error(
                error.response?.data?.message || "Failed to fetch similar games"
            )
        }
    }
}