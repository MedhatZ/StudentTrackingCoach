using System;
using System.Collections.Generic;

namespace StudentTrackingCoach.Models.ViewModels
{
    public class StudentMyGradPathViewModel
    {
        public long StudentId { get; set; }

        public string StudentName { get; set; } = "Student";

        public string OverallStatus { get; set; } = "On Track";

        public string CurrentFocusMessage { get; set; } = "";

        // ✅ Advisor panel fields
        public string AdvisorName { get; set; } = "Your Advisor";
        public string AdvisorLastAction { get; set; } = "No recent advisor actions.";
        public DateTime? AdvisorLastContactDate { get; set; }

        public List<GradPathItem> ActiveItems { get; set; } = new();

        public List<GradPathItem> CompletedItems { get; set; } = new();
    }

    public class GradPathItem
    {
        public string AlertTitle { get; set; } = "";
        public string ActionNotes { get; set; } = "";
        public DateTime? DueDate { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }
}
