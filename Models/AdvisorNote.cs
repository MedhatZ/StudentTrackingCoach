using System;

namespace StudentTrackingCoach.Models
{
    public class AdvisorNote
    {
        public long AdvisorNoteId { get; set; }

        public long StudentId { get; set; }

        // Identity user (nvarchar(450))
        public string AdvisorUserId { get; set; } = string.Empty;

        // What advisor did (nvarchar(200))
        public string ActionTaken { get; set; } = string.Empty;

        // Notes (nvarchar(max))
        public string Notes { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}
