public class IgdbGame : IHasId
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Slug { get; set; }
    public string? Storyline { get; set; }
    public string? Summary { get; set; }
    public long? FirstReleaseDate { get; set; }
    public int? Hypes { get; set; }
    public double? Rating { get; set; }
    public double? TotalRating { get; set; }
    public int? TotalRatingCount { get; set; }

    public List<IgdbGenre>? Genres { get; set; }
    public List<IgdbAgeRating>? AgeRatings { get; set; }
    public List<IgdbAlternativeName>? AlternativeNames { get; set; }
    public IgdbCover? Cover { get; set; }
    public List<IgdbScreenshot>? Screenshots { get; set; }
    public List<IgdbReleaseDate>? ReleaseDates { get; set; }
    //public IgdbFranchise? Franchise { get; set; }
    public List<IgdbFranchise>? Franchises { get; set; }
    public List<IgdbGameMode>? GameModes { get; set; }
    public IgdbGameType? GameType { get; set; }
    public List<IgdbInvolvedCompany>? InvolvedCompanies { get; set; }
    public List<IgdbPlatform>? Platforms { get; set; }
    public List<IgdbPlayerPerspective>? PlayerPerspectives { get; set; }

    // Relationship IDs
    public List<int>? Dlcs { get; set; }
    public List<int>? Expansions { get; set; }
    public List<int>? Ports { get; set; }
    public List<int>? Remakes { get; set; }
    public List<int>? Remasters { get; set; }
    public List<int>? SimilarGames { get; set; }
    public List<IgdbTheme>? Themes { get; set; }
}

public class IgdbTheme : IHasId
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Slug { get; set; }
}

public class IgdbAgeRating: IHasId
{
    public int Id { get; set; }
    public IgdbAgeRatingCategory? RatingCategory { get; set; } 
}

public class IgdbAgeRatingCategory: IHasId
{
    public int Id { get; set; }
    public string? Rating { get; set; }
    public IgdbAgeRatingOrganization? Organization { get; set; }
}

public class IgdbAgeRatingOrganization : IHasId
{
    public int Id { get; set; }
    public string? Name { get; set; }
}

public class IgdbAlternativeName : IHasId
{
    public int Id { get; set; }
    public int Game { get; set; }
    public string? Name { get; set; }
    public string? Comment { get; set; }
}

public class IgdbCover : IHasId
{
    public int Id { get; set; }
    public int Game { get; set; }
    public string? ImageId { get; set; }
    public string? Url { get; set; }
    public int Height { get; set; }
    public int Width { get; set; }
}

public class IgdbScreenshot : IHasId
{
    public int Id { get; set; }
    public int Game { get; set; }
    public string? ImageId { get; set; }
    public string? Url { get; set; }
    public int Height { get; set; }
    public int Width { get; set; }
}

public class IgdbReleaseDate : IHasId
{
    public int Id { get; set; }
    public long? Date { get; set; }
    public int Game { get; set; }
    // public string? Human { get; set; }
    public IgdbPlatform? Platform { get; set; }
    public IgdbRegion? ReleaseRegion { get; set; }
}

public class IgdbPlatform : IHasId
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Abbreviation { get; set; }
    public string? Slug { get; set; }
}

public class IgdbRegion : IHasId
{
    public int Id { get; set; }
    public string? Region { get; set; }
}

public class IgdbPlayerPerspective : IHasId
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Slug { get; set; }
}

public class IgdbGameMode : IHasId
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Slug { get; set; }
}

public class IgdbGameType : IHasId
{
    public int Id { get; set; }
    public string? Type { get; set; }
}


public class IgdbFranchise : IHasId
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Slug { get; set; }
}

public class IgdbGenre : IHasId
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Slug { get; set; }
}

public class IgdbCompany : IHasId
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int? Country { get; set; }  // ISO 3166-1 country code
    public string? Description { get; set; }
    public string? Slug { get; set; }
    public string? Url { get; set; }
}

public class IgdbInvolvedCompany : IHasId
{
    public int Id { get; set; }
    public IgdbCompany? Company { get; set; }
    public bool? Developer { get; set; }
    public bool? Publisher { get; set; }
    public bool? Porting { get; set; }
    public bool? Supporting { get; set; }
}