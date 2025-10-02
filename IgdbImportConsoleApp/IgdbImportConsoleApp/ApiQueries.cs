namespace IgdbImportConsoleApp
{
    public static class ApiQueries
    {
        public const string IGDB_GAMES = $@"
            fields 
                id, name, slug, storyline, summary, first_release_date, hypes, rating,
                genres.id, genres.name, genres.slug,
                age_ratings.id, age_ratings.rating_category.id, 
                age_ratings.rating_category.rating, 
                age_ratings.rating_category.organization.id, 
                age_ratings.rating_category.organization.name,  
                alternative_names.id, alternative_names.name, alternative_names.comment,
                alternative_names.game,
                cover.id, cover.url, cover.width, cover.height, cover.game, cover.image_id,
                screenshots.id, screenshots.url, screenshots.width, screenshots.height,
                screenshots.game, screenshots.image_id,
                release_dates.id, release_dates.date, release_dates.game, 
                release_dates.platform.id, release_dates.platform.name,
                release_dates.release_region.id, release_dates.release_region.region,
                franchises.id, franchises.name, franchises.slug,
                game_modes.id, game_modes.name, game_modes.slug,
                game_type.id, game_type.type,
                external_games.name, external_games.uid, 
                external_games.external_game_source.id, external_games.external_game_source.name,
                involved_companies.id, involved_companies.company.id, 
                involved_companies.company.name, involved_companies.company.country,
                involved_companies.company.description, 
                involved_companies.company.slug, involved_companies.company.url,
                involved_companies.developer, involved_companies.publisher, 
                involved_companies.porting, involved_companies.supporting,
                platforms.id, platforms.name, platforms.abbreviation, platforms.slug,
                player_perspectives.id, player_perspectives.name, player_perspectives.slug,
                dlcs, expansions, ports, remakes, remasters, similar_games, 
                themes.id, themes.name, themes.slug, total_rating, total_rating_count;
            where version_parent = null;
            sort id asc;";
        public const string IGDB_GAME_TYPES = $@"
            fields 
                id, type;
                sort id asc;";
        public const string IGDB_GENRES = $@"
            fields 
                id, name, slug;
                sort id asc;";
        public const string IGDB_INVOLVED_COMPANIES = $@"
            fields 
                id, company.id, company.name, company.country,
                company.description, company.slug, company.url,
                developer, publisher, porting, supporting;
            sort id asc;";
        public const string IGDB_PLATFORMS = $@"
            fields 
                id, name, alternative_name, abbreviation, slug;
            sort id asc;";
        public const string IGDB_RATING_ORGANIZATIONS = $@"
            fields 
                id, name;  
                sort id asc;";
        public const string IGDB_AGE_RATING_CATEGORIES = $@"
            fields 
                id, rating, organization.id;
                sort id asc;";
        public const string IGDB_AGE_RATINGS = $@"
            fields 
                id, rating_category.id;
            sort id asc;";
        public const string IGDB_FRANCHISES = $@"
            fields 
                id, name, slug;
                sort id asc;";
        public const string IGDB_GAME_MODES = $@"
            fields 
                id, name, slug;
                t id asc;";
        public const string IGDB_PLAYER_PERSPECTIVES = $@"
            fields 
                id, name, slug;
                sort id asc;";
        public const string IGDB_THEMES = $@"
            fields 
                id, name, slug;
                sort id asc;";
        public const string IGDB_RELEASE_DATE_REGIONS = $@"
            fields 
                id, region;
                sort id asc;";
        public const string IGDB_EXTERNAL_GAME_SOURCES = $@"
            fields 
                id, name;
                sort id asc;";
    }
}
