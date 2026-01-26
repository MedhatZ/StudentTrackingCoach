using System;

namespace StudentTrackingCoach.Models.ViewModels
{
    public class StudentCompletedItemViewModel
    {
        public string AlertTitle { get; set; } = "";
        public string ActionNotes { get; set; } = "";
        public DateTime? ResolvedAt { get; set; }
    }
}
