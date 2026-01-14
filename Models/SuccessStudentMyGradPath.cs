using System;
using Microsoft.EntityFrameworkCore;

namespace StudentTrackingCoach.Models
{
    [Keyless]
    public class SuccessStudentMyGradPath
    {
        public long StudentID { get; set; }

        public string AlertTitle { get; set; } = "";

        public string ActionNotes { get; set; } = "";

        public string StudentStatus { get; set; } = "";

        public DateTime? DueDate { get; set; }

        public DateTime? ResolvedAt { get; set; }
    }
}
