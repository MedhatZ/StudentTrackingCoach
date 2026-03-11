using System.ComponentModel.DataAnnotations;

namespace StudentTrackingCoach.Models
{
    public class Tenant
    {
        public int TenantId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Slug { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? ConnectionString { get; set; }

        public int PassingGrade { get; set; } = 70;

        public decimal HighRiskThreshold { get; set; } = 0.70m;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<TenantFeature> Features { get; set; } = new List<TenantFeature>();
        public ICollection<TenantUserRole> TenantUsers { get; set; } = new List<TenantUserRole>();
    }
}
