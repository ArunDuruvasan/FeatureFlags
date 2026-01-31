namespace FeatureFlagAPI.DTOs
{
    public class EvaluationRequestDto
    {
        public string FeatureName { get; set; } = string.Empty;
        public string? UserId { get; set; }
        public string? GroupId { get; set; }
        public string? Region { get; set; }
    }
}
