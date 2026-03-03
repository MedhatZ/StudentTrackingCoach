using System;
using System.ComponentModel.DataAnnotations;

namespace StudentTrackingCoach.Models
{
    public class Intervention
    {
        public int Id { get; set; }

        [Required]
        public long StudentId { get; set; }

        public string AdvisorId { get; set; } = string.Empty;

        public string Type { get; set; } = "Study Guide";

        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ApprovedAt { get; set; }

        public string Status { get; set; } = "Pending";

        public string? StudentName { get; set; }
    }
}
