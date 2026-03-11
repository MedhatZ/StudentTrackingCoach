using System;

namespace StudentTrackingCoach.Models
{
    public class AdminAuditLog : ITenantScopedEntity
    {
        public int Id { get; set; }
        public string Action { get; set; } = "";
        public string TargetUserId { get; set; } = "";
        public string PerformedByUserId { get; set; } = "";
        public int TenantId { get; set; } = 1;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
