using System.ComponentModel.DataAnnotations;

namespace FeatureFlagAPI.DTOs
{
    public class CreateFeatureFlagDto
    {
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        public bool DefaultState { get; set; }
    }
}
