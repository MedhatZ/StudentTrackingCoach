using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentTrackingCoach.Models
{
    public class Intervention : ITenantScopedEntity
    {
        public int Id { get; set; }

        [Required]
        public long StudentId { get; set; }
        public int TenantId { get; set; } = 1;

        public string AdvisorId { get; set; } = string.Empty;

        public string Type { get; set; } = "Study Guide";

        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ApprovedAt { get; set; }

        public string Status { get; set; } = "Pending";

        public string? StudentName { get; set; }

        [NotMapped]
        public bool IsAIGenerated { get; set; }
    }
}
