namespace FeatureFlagAPI.DTOs
{
    public class FeatureFlagResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool DefaultState { get; set; }
    }
}
