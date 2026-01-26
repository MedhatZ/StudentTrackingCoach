using System;
using System.Collections.Generic;

namespace StudentTrackingCoach.Models.ViewModels
{
    public class StudentMyGradPathViewModel
    {
        public long StudentId { get; set; }

        public string StudentName { get; set; } = "Student";

        public string OverallStatus { get; set; } = "On Track";

        public string CurrentFocusMessage { get; set; } = string.Empty;

        // ================================
        // Advisor Panel
        // ================================
        public string AdvisorName { get; set; } = "Your Advisor";
        public string AdvisorLastAction { get; set; } = "No recent advisor actions.";
        public DateTime? AdvisorLastContactDate { get; set; }

        // ================================
        // Grad Path Items (SAFE + INITIALIZED)
        // ================================
        public List<StudentMyGradPathItemViewModel> ActiveItems { get; set; }
            = new List<StudentMyGradPathItemViewModel>();

        public List<StudentMyGradPathItemViewModel> CompletedItems { get; set; }
            = new List<StudentMyGradPathItemViewModel>();
    }

    // ===================================
    // ITEM VIEW MODEL (USED BY CONTROLLER + VIEW)
    // ===================================
    public class StudentMyGradPathItemViewModel
    {
        public string AlertTitle { get; set; } = string.Empty;
        public string? ActionNotes { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }
}
