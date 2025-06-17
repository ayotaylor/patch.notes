namespace Backend.Models.DTO.Response
{
    public class GameSearchParams
    {
        public string? Search { get; set; }
        public List<Guid>? GenreIds { get; set; }
        public List<Guid>? PlatformIds { get; set; }
        public decimal? MinRating { get; set; }
        public decimal? MaxRating { get; set; }
        public string? SortBy { get; set; } = "name"; // name, rating, hypes, release_date
        public string? SortOrder { get; set; } = "asc"; // asc, desc
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

}

// TODO: maybe move to a more appropriate location ex. search folder or something like that