using System;

namespace StudentTrackingCoach.Models.ViewModels
{
    public class AdvisorNoteViewModel
    {
        public string Title { get; set; } = string.Empty;
        public string Severity { get; set; } = "Low";
        public string Category { get; set; } = "Academic";

        // MUST BE PLURAL — matches DB column
        public string Notes { get; set; } = string.Empty;

        public string CreatedBy { get; set; } = "Advisor";
        public DateTime CreatedAt { get; set; }
    }
}
