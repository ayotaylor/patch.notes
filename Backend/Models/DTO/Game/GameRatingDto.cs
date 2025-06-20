namespace Backend.Models.DTO.Game
{
    public class AgeRatingDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public RatingOrganizationDto RatingOrganization { get; set; } = new();
    }
}
