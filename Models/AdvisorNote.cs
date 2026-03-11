using System;

namespace StudentTrackingCoach.Models
{
    public class AdvisorNote : ITenantScopedEntity
    {
        public long AdvisorNoteId { get; set; }

        public long StudentId { get; set; }
        public int TenantId { get; set; } = 1;

        public string AdvisorUserId { get; set; } = string.Empty;

        public string ActionTaken { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        // Phase-2 ready
        public Advisor? Advisor { get; set; }
    }
}
