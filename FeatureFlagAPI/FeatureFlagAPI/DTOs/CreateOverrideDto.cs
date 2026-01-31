using System.ComponentModel.DataAnnotations;

namespace FeatureFlagAPI.DTOs
{
    public class CreateOverrideDto
    {
        [Required]
        [StringLength(50)]
        public string OverrideType { get; set; } = string.Empty; // User, Group, Region

        [Required]
        [StringLength(100)]
        public string OverrideKey { get; set; } = string.Empty;

        [Required]
        public bool State { get; set; }
    }
}
