using System.ComponentModel.DataAnnotations;

namespace FeatureFlagAPI.Models
{
    public class FeatureFlag
    {
        public Guid Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        public bool DefaultState { get; set; }
    }
}
