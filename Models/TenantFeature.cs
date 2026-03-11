using System.ComponentModel.DataAnnotations;

namespace StudentTrackingCoach.Models
{
    public class TenantFeature
    {
        public int Id { get; set; }

        public int TenantId { get; set; }

        [Required]
        [MaxLength(120)]
        public string FeatureKey { get; set; } = string.Empty;

        public bool IsEnabled { get; set; }

        [MaxLength(500)]
        public string? FeatureValue { get; set; }

        public Tenant? Tenant { get; set; }
    }
}
