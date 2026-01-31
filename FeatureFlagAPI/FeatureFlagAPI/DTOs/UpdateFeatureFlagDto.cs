using System.ComponentModel.DataAnnotations;

namespace FeatureFlagAPI.DTOs
{
    public class UpdateFeatureFlagDto
    {
        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        public bool DefaultState { get; set; }
    }
}
