namespace Backend.Models.DTO.Game
{
    public class AgeRatingDto
    {
        public Guid Id { get; set; }
        public string Rating { get; set; } = string.Empty;
        public RatingOrganizationDto RatingOrganization { get; set; } = new();
    }
}
