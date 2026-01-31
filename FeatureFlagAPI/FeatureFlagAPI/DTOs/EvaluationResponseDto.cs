namespace FeatureFlagAPI.DTOs
{
    public class EvaluationResponseDto
    {
        public bool IsEnabled { get; set; }
        public string? AppliedOverride { get; set; } // "User", "Group", "Region", or "Default"
        public string? AppliedOverrideKey { get; set; }
    }
}
