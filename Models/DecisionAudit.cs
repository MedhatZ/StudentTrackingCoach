using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentTrackingCoach.Models
{
    [Table("DecisionAudit", Schema = "dbo")]
    public class DecisionAudit
    {
        [Key]
        public long AuditId { get; set; }

        public long? StudentId { get; set; }

        [Required]
        public string Decision { get; set; } = string.Empty;

        public string? Reason { get; set; }

        [Required]
        public string Source { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}
