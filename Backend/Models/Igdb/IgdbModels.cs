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

    public List<IgdbGenre>? Genres { get; set; }
    public List<IgdbAgeRating>? AgeRatings { get; set; }
    public List<IgdbAlternativeName>? AlternativeNames { get; set; }
    public IgdbCover? Cover { get; set; }
    public List<IgdbScreenshot>? Screenshots { get; set; }
    public List<IgdbReleaseDate>? ReleaseDates { get; set; }
    public List<IgdbFranchise>? Franchises { get; set; }
    public List<IgdbGameMode>? GameModes { get; set; }
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
}

public class IgdbPlayerPerspective
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Slug { get; set; }
}

public class IgdbGameMode
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Slug { get; set; }
}

public class IgdbFranchise
{
    public int Id { get; set; }
    public List<int>? GameIds { get; set; } // not sure if this is needed
    public string? Name { get; set; }
    public string? Slug { get; set; }
}

public class IgdbScreenshot
{
    public int Id { get; set; }
    public int GameId { get; set; }
    public string? ImageId { get; set; }
    public string? Url { get; set; }
    public int Height { get; set; }
    public int Width { get; set; }
}

public class IgdbCover
{
    public int Id { get; set; }
    public int GameId { get; set; }
    public string? ImageId { get; set; }
    public string? Url { get; set; }
    public int Height { get; set; }
    public int Width { get; set; }
}

public class IgdbAlternativeName
{
    public int Id { get; set; }
    public string? GameId { get; set; }
    public string? Name { get; set; }
    public string? Comment { get; set; }
}

public class IgdbAgeRating
{
    public int Id { get; set; }
    public Guid RatingOrganizationId { get; set; }
    public string RatingCategory { get; set; } = string.Empty;

}

public class IgdbGenre : IHasId
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Slug { get; set; }
}

public class IgdbPlatform : IHasId
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Abbreviation { get; set; }
    public string? Slug { get; set; }
}

public class IgdbCompany : IHasId
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public class IgdbInvolvedCompany
{
    public int Id { get; set; }
    public IgdbCompany? Company { get; set; }
    public bool? Developer { get; set; }
    public bool? Publisher { get; set; }
    public bool? Porting { get; set; }
    public bool? Supporting { get; set; }
}

public class IgdbReleaseDate
{
    public int Id { get; set; }
    public long? Date { get; set; }
    public string? Human { get; set; }
    public IgdbPlatform? Platform { get; set; }
}