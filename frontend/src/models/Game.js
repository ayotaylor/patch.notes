/**
 * Game Model
 * 
 * Note: This model returns null for missing images. Use the useImageFallback composable
 * in Vue components to handle fallback images consistently across the application.
 * 
 * For non-Vue contexts, import getStaticFallbackImage from '@/composables/useImageFallback'
 */
export class Game {
  // Private fields for internal state
  _rawData = null;
  _processedData = new Map();
  _isProcessing = new Set(); // Track which properties are currently being processed

  constructor(data = {}) {
    // Store raw data for lazy processing
    this._rawData = { ...data };

    // Core properties that are always needed
    this.id = data.igdbId || data.id || null;
    this.name = data.name || "";
    this.slug = data.slug || "";
    this.summary = data.summary || "";
    this.storyline = data.storyline || "";
    this.gameType = data.gameType || "";
    this.hypes = data.hypes || 0;
    this.igdbRating = data.igdbRating || 0.0;
    this.likesCount = data.likesCount !== undefined && data.likesCount !== null ? data.likesCount : 0;
    this.favoritesCount = data.favoritesCount !== undefined && data.favoritesCount !== null ? data.favoritesCount : 0;
    this.isLikedByUser = Boolean(data.isLikedByUser);
    this.isFavoritedByUser = Boolean(data.isFavoritedByUser);
    
    // Review statistics
    this.reviewsCount = data.reviewsCount || 0;
    this.averageRating = data.averageRating || 0.0;

    // Timestamps
    this.createdAt = this._parseDate(data.createdAt) || new Date();
    this.updatedAt = this._parseDate(data.updatedAt) || new Date();
    this.firstReleaseDate = this._parseDate(data.firstReleaseDate);

    // List of getter-only properties that shouldn't be directly assigned
    this._getterOnlyProperties = new Set([
      "genres",
      "cover",
      "covers",
      "screenshots",
      "developers",
      "publishers",
      "supportingStudios",
      "portingStudios",
      "platforms",
      "ageRatings",
      "gameModes",
      "franchises",
      "releaseDates",
      "altNames",
      "primaryImageUrl",
      "primaryGenre",
      "allGenres",
      "primaryDeveloper",
      "allDevelopers",
      "formattedReleaseDate",
      "shortReleaseDate",
      "isNewRelease",
      "rating",
      "ratingStars",
    ]);

    // Pre-process some critical properties to avoid template issues
    this._preProcessCriticalProperties();
  }

  // Pre-process properties that are commonly used in templates
  _preProcessCriticalProperties() {
    // Pre-process these to avoid [object Promise] issues
    const criticalProps = [
      "genres",
      "cover",
      "covers",
      "developers",
      "publishers",
      "platforms",
    ];
    criticalProps.forEach((prop) => {
      try {
        // Trigger the getter to process and cache the property
        this[prop];
      } catch (error) {
        console.warn(`Error pre-processing ${prop}:`, error);
      }
    });
  }
  // Getters for lazy-loaded properties - now with better error handling
  get genres() {
    return this._getProcessedSync("genres", () => {
      if (this._rawData.genres && Array.isArray(this._rawData.genres)) {
        // Original API format or already processed array
        return this._processGenres(this._rawData.genres);
      }
      return [];
    });
  }

  get cover() {
    return this._getProcessedSync("cover", () => {
      // Handle both original API format (covers array) and serialized format (single cover object)
      if (this._rawData.covers && Array.isArray(this._rawData.covers)) {
        // Original API format - process the covers array
        const covers = this._processCovers(this._rawData.covers);
        return covers?.[0] || null;
      } else if (this._rawData.cover && typeof this._rawData.cover === 'object') {
        // Single cover object - process it to ensure consistent format
        const processedCover = this._processCovers([this._rawData.cover]);
        return processedCover?.[0] || null;
      }
      return null;
    });
  }

  get covers() {
    return this._getProcessedSync("covers", () => {
      if (this._rawData.covers && Array.isArray(this._rawData.covers)) {
        // Original API format - process the covers array
        return this._processCovers(this._rawData.covers);
      } else if (this._rawData.cover && typeof this._rawData.cover === 'object') {
        // Single cover object - process it to ensure consistent format
        return this._processCovers([this._rawData.cover]);
      }
      return [];
    });
  }

  get screenshots() {
    return this._getProcessedSync("screenshots", () => {
      if (this._rawData.screenshots && Array.isArray(this._rawData.screenshots)) {
        // Original API format or already processed array
        return this._processScreenshots(this._rawData.screenshots);
      }
      return [];
    });
  }

  get developers() {
    return this._getProcessedSync("developers", () =>
      this._processDevelopers(this._rawData.companies)
    );
  }

  get publishers() {
    return this._getProcessedSync("publishers", () =>
      this._processPublishers(this._rawData.companies)
    );
  }

  get supportingStudios() {
    return this._getProcessedSync("supportingStudios", () =>
      this._processSupportingStudios(this._rawData.supportingStudios)
    );
  }

  get portingStudios() {
    return this._getProcessedSync("portingStudios", () =>
      this._processPortingStudios(this._rawData.portingStudios)
    );
  }

  get platforms() {
    return this._getProcessedSync("platforms", () =>
      this._processPlatforms(this._rawData.platforms)
    );
  }

  get ageRatings() {
    return this._getProcessedSync("ageRatings", () =>
      this._processAgeRatings(this._rawData.ageRatings)
    );
  }

  get gameModes() {
    return this._getProcessedSync("gameModes", () =>
      this._processGameModes(this._rawData.gameModes)
    );
  }

  get franchises() {
    return this._getProcessedSync("franchises", () =>
      this._processFranchises(this._rawData.franchises)
    );
  }

  get releaseDates() {
    return this._getProcessedSync("releaseDates", () =>
      this._processReleaseDates(this._rawData.releaseDates)
    );
  }

  get altNames() {
    return this._getProcessedSync("altNames", () =>
      this._processAltNames(this._rawData.altNames)
    );
  }

  // Convenience getters for common UI needs - with safe fallbacks
  get primaryImageUrl() {
    try {
      // First check for direct CoverUrl field (from GameSummaryDto)
      if (this._rawData.coverUrl || this._rawData.CoverUrl) {
        return this._rawData.coverUrl || this._rawData.CoverUrl;
      }
      
      // Fallback to cover object (from full GameDto)
      const cover = this.cover;
      return cover?.imageUrl || cover?.url || null;
    } catch (error) {
      console.warn("Error getting primaryImageUrl:", error);
      return null;
    }
  }

  get primaryGenre() {
    try {
      const genres = this.genres;
      return Array.isArray(genres) && genres.length > 0
        ? genres[0]?.name || "Unknown"
        : "Unknown";
    } catch (error) {
      console.warn("Error getting primaryGenre:", error);
      return "Unknown";
    }
  }

  get allGenres() {
    try {
      const genres = this.genres;
      return Array.isArray(genres) && genres.length > 0
        ? genres
            .map((g) => g?.name)
            .filter(Boolean)
            .join(", ") || "Unknown"
        : "Unknown";
    } catch (error) {
      console.warn("Error getting allGenres:", error);
      return "Unknown";
    }
  }

  get primaryDeveloper() {
    try {
      const devs = this.developers;
      return Array.isArray(devs) && devs.length > 0
        ? devs[0]?.name || "Unknown"
        : "Unknown";
    } catch (error) {
      console.warn("Error getting primaryDeveloper:", error);
      return "Unknown";
    }
  }

  get allDevelopers() {
    try {
      const devs = this.developers;
      return Array.isArray(devs) && devs.length > 0
        ? devs
            .map((d) => d?.name)
            .filter(Boolean)
            .join(", ") || "Unknown"
        : "Unknown";
    } catch (error) {
      console.warn("Error getting allDevelopers:", error);
      return "Unknown";
    }
  }

  get formattedReleaseDate() {
    try {
      if (!this.firstReleaseDate) return "TBA";
      return this.firstReleaseDate.toLocaleDateString("en-US", {
        year: "numeric",
        month: "long",
        day: "numeric",
      });
    } catch (error) {
      console.warn("Error getting formattedReleaseDate:", error);
      return "TBA";
    }
  }

  get shortReleaseDate() {
    try {
      if (!this.firstReleaseDate) return "TBA";
      return this.firstReleaseDate.toLocaleDateString("en-US", {
        month: "short",
        year: "numeric",
      });
    } catch (error) {
      console.warn("Error getting shortReleaseDate:", error);
      return "TBA";
    }
  }

  get isNewRelease() {
    try {
      if (!this.firstReleaseDate) return false;
      const threeMonthsAgo = new Date();
      threeMonthsAgo.setMonth(threeMonthsAgo.getMonth() - 3);
      return this.firstReleaseDate >= threeMonthsAgo;
    } catch (error) {
      console.warn("Error getting isNewRelease:", error);
      return false;
    }
  }

  get rating() {
    try {
      // Convert IGDB rating (0-100) to 0-5 scale
      return this.igdbRating
        ? Math.round((this.igdbRating / 100) * 5 * 10) / 10
        : 0;
    } catch (error) {
      console.warn("Error getting rating:", error);
      return 0;
    }
  }

  get ratingStars() {
    try {
      const rating = this.rating;
      return (
        "★".repeat(Math.floor(rating)) + "☆".repeat(5 - Math.floor(rating))
      );
    } catch (error) {
      console.warn("Error getting ratingStars:", error);
      return "☆☆☆☆☆";
    }
  }

  // Fixed synchronous processing method
  _getProcessedSync(key, processor) {
    try {
      // Return cached result if available
      if (this._processedData.has(key)) {
        const cached = this._processedData.get(key);
        // Make sure we're not returning a promise
        if (cached && typeof cached.then === "function") {
          console.warn(
            `Cached value for ${key} is a promise, processing synchronously`
          );
          // Process synchronously and cache the result
          const result = processor();
          this._processedData.set(key, result);
          return result;
        }
        return cached;
      }

      // Process and cache the result synchronously
      const result = processor();
      this._processedData.set(key, result);
      return result;
    } catch (error) {
      console.error(`Error processing ${key}:`, error);
      // Return safe fallback based on property type
      if (
        [
          "genres",
          "covers",
          "screenshots",
          "developers",
          "publishers",
          "platforms",
          "ageRatings",
          "gameModes",
          "franchises",
          "releaseDates",
          "altNames",
        ].includes(key)
      ) {
        return [];
      }
      return null;
    }
  }

  // Keep the old async method for backward compatibility but mark it as deprecated
  _getProcessed(key, processor) {
    if (!this._processedData.has(key)) {
      const result = processor();
      console.log(`Processing ${key}:`, result, typeof result);
      this._processedData.set(key, processor());
    }
    return this._processedData.get(key);
  }

  // Private helper methods
  _parseDate(dateValue) {
    if (!dateValue) return null;

    try {
      // Handle Unix timestamps
      if (typeof dateValue === "number") {
        return new Date(dateValue * 1000);
      }

      // Handle Date objects
      if (dateValue instanceof Date) {
        return dateValue;
      }

      // Handle date strings
      const parsed = new Date(dateValue);
      return isNaN(parsed.getTime()) ? null : parsed;
    } catch (error) {
      console.warn("Error parsing date:", dateValue, error);
      return null;
    }
  }

  _validateArray(data) {
    return Array.isArray(data) && data.length > 0 ? data : [];
  }

  _processGenres(data = []) {
    try {
      return this._validateArray(data).map((genre) => ({
        id: genre.igdbId || genre.id || null,
        name: genre.name || "",
        slug: genre.slug || "",
      }));
    } catch (error) {
      console.warn("Error processing genres:", error);
      return [];
    }
  }

  _processCovers(data = []) {
    try {
      return this._validateArray(data).map((cover) => ({
        id: cover.igdbId || cover.id || null,
        imageUrl: cover.url || cover.imageUrl || "",
        url: cover.url || cover.imageUrl || "", // Alias for compatibility
        width: cover.width || 0,
        height: cover.height || 0,
      }));
    } catch (error) {
      console.warn("Error processing covers:", error);
      return [];
    }
  }

  _processScreenshots(data = []) {
    try {
      return this._validateArray(data).map((screenshot) => ({
        id: screenshot.igdbId || screenshot.id || null,
        imageUrl: screenshot.url || screenshot.imageUrl || "",
        url: screenshot.url || screenshot.imageUrl || "", // Alias for compatibility
        width: screenshot.width || 0,
        height: screenshot.height || 0,
      }));
    } catch (error) {
      console.warn("Error processing screenshots:", error);
      return [];
    }
  }

  _processCompanies(data = [], role) {
    try {
      const companies = this._validateArray(data).filter(
        (company) => company.roles?.includes(role) || company.role === role
      );

      return companies.map((company) => ({
        id: company.igdbId || company.id || null,
        name: company.name || "",
        description: company.description || "",
        country: company.country || "",
        url: company.url || "",
        website: company.website || "",
      }));
    } catch (error) {
      console.warn(`Error processing companies for role ${role}:`, error);
      return [];
    }
  }

  _processDevelopers(data = []) {
    return this._processCompanies(data, "developer");
  }

  _processPublishers(data = []) {
    return this._processCompanies(data, "publisher");
  }

  _processSupportingStudios(data = []) {
    return this._processCompanies(data, "supportingStudios");
  }

  _processPortingStudios(data = []) {
    return this._processCompanies(data, "portingStudios");
  }

  _processPlatforms(data = []) {
    try {
      return this._validateArray(data).map((platform) => ({
        id: platform.igdbId || platform.id || null,
        name: platform.name || "",
        slug: platform.slug || "",
        abbreviation: platform.abbreviation || "",
      }));
    } catch (error) {
      console.warn("Error processing platforms:", error);
      return [];
    }
  }

  _processAgeRatings(data = []) {
    try {
      return this._validateArray(data).map((rating) => ({
        id: rating.igdbId || rating.id || null,
        name: rating.name || "",
        slug: rating.slug || "",
        organization:
          rating.ratingOrganization?.name || rating.organization || "",
      }));
    } catch (error) {
      console.warn("Error processing age ratings:", error);
      return [];
    }
  }

  _processGameModes(data = []) {
    try {
      return this._validateArray(data).map((mode) => ({
        id: mode.igdbId || mode.id || null,
        name: mode.name || "",
        slug: mode.slug || "",
      }));
    } catch (error) {
      console.warn("Error processing game modes:", error);
      return [];
    }
  }

  _processFranchises(data = []) {
    try {
      return this._validateArray(data).map((franchise) => ({
        id: franchise.igdbId || franchise.id || null,
        name: franchise.name || "",
        slug: franchise.slug || "",
      }));
    } catch (error) {
      console.warn("Error processing franchises:", error);
      return [];
    }
  }

  _processReleaseDates(data = []) {
    try {
      return this._validateArray(data).map((release) => ({
        id: release.igdbId || release.id || null,
        platform: release.platform || null,
        date: this._parseDate(release.date),
        region: release.region?.name || release.region || "",
        category: release.category || "",
      }));
    } catch (error) {
      console.warn("Error processing release dates:", error);
      return [];
    }
  }

  _processAltNames(data = []) {
    try {
      return this._validateArray(data).map((name) => ({
        name: name.name || name,
      }));
    } catch (error) {
      console.warn("Error processing alt names:", error);
      return [];
    }
  }

  // Public utility methods
  hasTag(tag) {
    try {
      return (
        this.genres?.some(
          (genre) =>
            genre.name.toLowerCase().includes(tag.toLowerCase()) ||
            genre.slug.toLowerCase().includes(tag.toLowerCase())
        ) || false
      );
    } catch (error) {
      console.warn("Error in hasTag:", error);
      return false;
    }
  }

  isAvailableOn(platformName) {
    try {
      return (
        this.platforms?.some(
          (platform) =>
            platform.name.toLowerCase().includes(platformName.toLowerCase()) ||
            platform.abbreviation
              .toLowerCase()
              .includes(platformName.toLowerCase())
        ) || false
      );
    } catch (error) {
      console.warn("Error in isAvailableOn:", error);
      return false;
    }
  }

  hasGenre(genreName) {
    try {
      return (
        this.genres?.some(
          (genre) => genre.name.toLowerCase() === genreName.toLowerCase()
        ) || false
      );
    } catch (error) {
      console.warn("Error in hasGenre:", error);
      return false;
    }
  }

  // Serialization methods
  toJSON() {
    try {
      return {
        // Core properties
        id: this.id,
        igdbId: this.id, // Keep both for compatibility
        name: this.name,
        slug: this.slug,
        summary: this.summary,
        storyline: this.storyline,
        gameType: this.gameType,
        hypes: this.hypes,
        igdbRating: this.igdbRating,
        likesCount: this.likesCount,
        favoritesCount: this.favoritesCount,
        isLikedByUser: this.isLikedByUser,
        isFavoritedByUser: this.isFavoritedByUser,
        reviewsCount: this.reviewsCount,
        averageRating: this.averageRating,
        
        // Dates
        firstReleaseDate: this.firstReleaseDate?.toISOString(),
        createdAt: this.createdAt.toISOString(),
        updatedAt: this.updatedAt.toISOString(),
        
        // Processed data (for compatibility with deserialization)
        genres: this.genres,
        cover: this.cover, // Processed cover object
        covers: this.covers, // Array of covers (fallback)
        screenshots: this.screenshots,
        developers: this.developers,
        publishers: this.publishers,
        platforms: this.platforms,
        ageRatings: this.ageRatings,
        gameModes: this.gameModes,
        franchises: this.franchises,
        releaseDates: this.releaseDates,
        altNames: this.altNames,
      };
    } catch (error) {
      console.warn("Error in toJSON:", error);
      return { id: this.id, name: this.name, error: "Serialization failed" };
    }
  }

  // For lightweight serialization (e.g., for lists)
  toListJSON() {
    try {
      return {
        id: this.id,
        name: this.name,
        slug: this.slug,
        summary: this.summary,
        primaryImageUrl: this.primaryImageUrl,
        primaryGenre: this.primaryGenre,
        rating: this.rating,
        shortReleaseDate: this.shortReleaseDate,
        isNewRelease: this.isNewRelease,
      };
    } catch (error) {
      console.warn("Error in toListJSON:", error);
      return { id: this.id, name: this.name, error: "Serialization failed" };
    }
  }

  update(data) {
    try {
      // Clear processed data cache when updating
      this._processedData?.clear();

      // Update raw data
      this._rawData = { ...this._rawData, ...data };

      // Only update basic properties, avoid getters
      const allowedProperties = [
        "id",
        "name",
        "slug",
        "summary",
        "storyline",
        "gameType",
        "hypes",
        "igdbRating",
        "likesCount",
        "favoritesCount",
        "isLikedByUser",
        "isFavoritedByUser",
        "reviewsCount",
        "averageRating",
        "createdAt",
        "updatedAt",
        "firstReleaseDate",
      ];

      allowedProperties.forEach((prop) => {
        if (Object.prototype.hasOwnProperty.call(data, prop)) {
          this[prop] = data[prop];
        }
      });

      this.updatedAt = new Date();

      // Re-process critical properties
      this._preProcessCriticalProperties();

      return this;
    } catch (error) {
      console.error("Error updating game:", error);
      return this;
    }
  }

  // Alternative update method for specific core properties
  updateCoreProperties(data) {
    try {
      const coreProperties = [
        "id",
        "name",
        "slug",
        "summary",
        "storyline",
        "gameType",
        "hypes",
        "igdbRating",
        "likesCount",
        "favoritesCount",
        "isLikedByUser",
        "isFavoritedByUser",
        "reviewsCount",
        "averageRating",
        "createdAt",
        "updatedAt",
        "firstReleaseDate",
      ];

      coreProperties.forEach((prop) => {
        if (Object.prototype.hasOwnProperty.call(data, prop)) {
          if (prop.includes("Date")) {
            this[prop] = this._parseDate(data[prop]);
          } else {
            this[prop] = data[prop];
          }
        }
      });

      this.updatedAt = new Date();
      return this;
    } catch (error) {
      console.error("Error updating core properties:", error);
      return this;
    }
  }

  // Static factory methods
  static fromAPI(apiData) {
    return new Game(apiData);
  }

  static fromJSONArray(jsonArray) {
    return jsonArray.map((data) => new Game(data));
  }

  // Cache management
  clearCache() {
    this._processedData.clear();
    this._isProcessing.clear();
    // Re-process critical properties after clearing cache
    this._preProcessCriticalProperties();
  }

  // For debugging
  get debugInfo() {
    return {
      id: this.id,
      name: this.name,
      cachedProperties: Array.from(this._processedData.keys()),
      rawDataKeys: Object.keys(this._rawData),
      getterOnlyProperties: Array.from(this._getterOnlyProperties),
      isProcessing: Array.from(this._isProcessing),
    };
  }
}
