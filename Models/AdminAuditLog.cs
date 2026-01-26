using System;

namespace StudentTrackingCoach.Models
{
    public class AdminAuditLog
    {
        public int Id { get; set; }
        public string Action { get; set; } = "";
        public string TargetUserId { get; set; } = "";
        public string PerformedByUserId { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
