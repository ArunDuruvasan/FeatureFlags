namespace FeatureFlagAPI.DTOs
{
    public class OverrideResponseDto
    {
        public Guid Id { get; set; }
        public Guid FeatureId { get; set; }
        public string OverrideType { get; set; } = string.Empty;
        public string OverrideKey { get; set; } = string.Empty;
        public bool State { get; set; }
    }
}
