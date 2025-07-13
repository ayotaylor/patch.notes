export class Game {
    constructor(data = {}) {
        this.id = data.igdbId || null;
        this.name = data.name || '';
        this.slug = data.slug || '';
        this.storyline = data.storyline || '';
        this.summary = data.summary || '';
        this.firstReleaseDate = data.firstReleaseDate ? new Date(data.firstReleaseDate) : null;
        this.hypes = data.hypes || 0;
        this.igdbRating = data.igdbRating || 0.0;
        this.createdAt = data.createdAt ? new Date(data.createdAt) : new Date();
        this.updatedAt = data.updatedAt ? new Date(data.updatedAt) : new Date();
        this.genres = this.getGenres(data.genres || []);
        this.ageRatings = this.getAgeRatings(data.ageRatings || []);
        this.altNames = this.getAltNames(data.altNames || []);
        this.cover = this.getCovers(data.covers || [])[0] || null;
        this.screenshots = this.getScreenshots(data.screenshots || []);
        this.franchises = this.getFranchises(data.franchises || []);
        this.gameModes = this.getGameModes(data.gameModes || []);
        this.gameType = data.gameType || '';
        this.publishers = this.getPublishers(data.companies || []);
        this.developers = this.getDevelopers(data.companies || []);
        this.portingStudios = this.getPortingStudios(data.companies || []);
        this.supportingStudios = this.getSupportingStudios(data.companies || []);
        this.platforms = this.getPlatforms(data.platforms || []);
        this.releaseDates = this.getReleaseDates(data.releaseDates || []);
        this.likes = data.likesCount || 0;
        this.favorites = data.favoritesCount || 0;
        this.isLikedByUser = data.isLikedByUser || false;
        this.isFavoritedByUser = data.isFavoritedByUser || false;

        // TODO: get related games
        this.dlcs = data.dlcs || [];
        this.expansions = data.expansions || [];
        this.ports = data.ports || [];
        this.remakes = data.remakes || [];
        this.remasters = data.remasters || [];
        this.similarGames = data.similarGames || [];
    }

    // Computed properties/getters
    get formattedPrice() {
        return new Intl.NumberFormat('en-US', {
            style: 'currency',
            currency: 'USD'
        }).format(this.price);
    }

    get formattedReleaseDate() {
        return this.firstReleaseDate ?
            this.firstReleaseDate.toLocaleDateString('en-US', {
                year: 'numeric',
                month: 'long',
                day: 'numeric'
            }) : 'TBA';
    }

    get isNewRelease() {
        if (!this.firstReleaseDate) return false;
        const threeMonthsAgo = new Date();
        threeMonthsAgo.setMonth(threeMonthsAgo.getMonth() - 3);
        return this.firstReleaseDate >= threeMonthsAgo;
    }

    get ratingStars() {
        return '★'.repeat(Math.floor(this.rating)) + '☆'.repeat(5 - Math.floor(this.rating));
    }

    // Methods
    getGenres(data = []) {
        if (!Array.isArray(data) || data.length === 0) return [];
        return data.map(genre => ({
            id: genre.igbdId || null,
            name: genre.name,
            slug: genre.slug || '',
        }));
    }

    getAgeRatings(data = []) {
        if (!Array.isArray(data) || data.length === 0) return [];
        return data.map(rating => ({
            name: rating.name || '',
            slug: rating.slug || '',
            ratingOrganization: rating.ratingOrganization.name || '',
        }));
    }

    getAltNames(data = []) {
        if (!Array.isArray(data) || data.length === 0) return [];
        return data.map(name => ({
            name: name.name || '',
        }));
    }

    getCovers(data = []) {
        if (data == null || !Array.isArray(data) || data.length === 0) return [];
        return data.map(cover => ({
            id: cover.igbdId || null,
            imageUrl: cover.url || '',
            width: cover.width || 0,
            height: cover.height || 0,
        }));
    }

    getScreenshots(data = []) {
        if (!Array.isArray(data) || data.length === 0) return [];
        return data.map(screenshot => ({
            id: screenshot.igbdId || null,
            imageUrl: screenshot.url || '',
            width: screenshot.width || 0,
            height: screenshot.height || 0,
        }));
    }

    getFranchises(data = []) {
        if (!Array.isArray(data) || data.length === 0) return [];
        return data.map(franchise => ({
            id: franchise.igbdId || null,
            name: franchise.name || '',
            slug: franchise.slug || '',
        }));
    }

    getGameModes(data = []) {
        if (!Array.isArray(data) || data.length === 0) return [];
        return data.map(mode => ({
            id: mode.igbdId || null,
            name: mode.name || '',
            slug: mode.slug || '',
        }));
    }

    getCompanies(data = []) {
        if (!Array.isArray(data) || data.length === 0) return [];
        return data.map(company => ({ 
            id: company.igbdId || null,
            name: company.name || '',
            country: company.country || '',
            description: company.description || '',
            url: company.url || '',
            website: company.website || '',
            roles: company.roles || [],
        }));
    }

    getPublishers(data = []) {
        if (!Array.isArray(data) || data.length === 0) return null;
        const publishers = data.filter(company => company.roles.includes('publisher'));
        return publishers.map(publisher => ({
            id: publisher.igbdId || null,
            name: publisher.name || '',
            description: publisher.description || '',
        }));
    }

    getDevelopers(data = []) {
        if (!Array.isArray(data) || data.length === 0) return null;
        const developers = data.filter(company => company.roles.includes('developer'));
        return developers.map(developer => ({
            id: developer.igbdId || null,
            name: developer.name || '',
            description: developer.description || '',
        }));
    }

    getPortingStudios(data = []) {
        if (!Array.isArray(data) || data.length === 0) return null;
        const portingStudios = data.filter(company => company.roles.includes('porting'));
        return portingStudios.map(porting => ({
            id: porting.igbdId || null,
            name: porting.name || '',
            description: porting.description || '',
        }));
    }

    getSupportingStudios(data = []) {
        if (!Array.isArray(data) || data.length === 0) return null;
        const supporting = data.filter(company => company.roles.includes('supporting'));
        return supporting ? {
            id: supporting.igbdId || null,
            name: supporting.name || '',
            description: supporting.description || '',
        } : null;
    }

    getPlatforms(data = []) {
        if (!Array.isArray(data) || data.length === 0) return [];
        return data.map(platform => ({
            id: platform.igbdId || null,
            name: platform.name || '',
            slug: platform.slug || '',
            abbreviation: platform.abbreviation || '',
        }));
    }

    getPlayerPerspectives(data = []) {
        if (!Array.isArray(data) || data.length === 0) return [];
        return data.map(perspective => ({
            id: perspective.igbdId || null,
            name: perspective.name || '',
            slug: perspective.slug || '',
        }));
    }

    getReleaseDates(data = []) {
        if (!Array.isArray(data) || data.length === 0) return [];
        return data.map(release => ({
            id: release.igbdId || null,
            platform: release.platform ? release.platform : null,
            date: release.date ? new Date(release.date) : null,
            region: release.region.name || '',
            category: release.category || '',
        }));
    }

    toJSON() {
        return {
            id: this.id,
            title: this.title,
            description: this.description,
            genre: this.genre,
            platform: this.platform,
            releaseDate: this.releaseDate?.toISOString(),
            rating: this.rating,
            price: this.price,
            imageUrl: this.imageUrl,
            publisher: this.publisher,
            developer: this.developer,
            tags: this.tags,
            screenshots: this.screenshots,
            systemRequirements: this.systemRequirements,
            createdAt: this.createdAt.toISOString(),
            updatedAt: this.updatedAt.toISOString()
        };
    }

    update(data) {
        Object.assign(this, data);
        this.updatedAt = new Date();
        return this;
    }

    hasTag(tag) {
        return this.tags.includes(tag);
    }

    isAvailableOn(platform) {
        return this.platform.includes(platform);
    }
}