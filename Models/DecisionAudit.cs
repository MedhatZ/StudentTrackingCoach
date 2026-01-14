using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentTrackingCoach.Models
{
    [Table("DecisionAudits")]   // ✅ THIS IS THE FIX
    public class DecisionAudit
    {
        public long DecisionAuditId { get; set; }

        public long StudentId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DecisionDate { get; set; }

        public string? ActionTaken { get; set; }
        public string? Notes { get; set; }

        public string? Decision { get; set; }
        public string? Reason { get; set; }
        public string Source { get; set; } = "StudentTrackingCoach";
    }
}
