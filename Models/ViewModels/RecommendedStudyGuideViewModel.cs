using System;
using System.ComponentModel.DataAnnotations;

namespace StudentTrackingCoach.Models.ViewModels
{
    public class RecommendedStudyGuideViewModel
    {
        // 🔹 Context
        public long StudentId { get; set; }

        public string StudentName { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public string CurrentGrade { get; set; } = string.Empty;

        // 🔹 Study Plan
        [Required]
        public string FocusAreas { get; set; } = string.Empty;

        [Required]
        public string StudySchedule { get; set; } = string.Empty;

        [Required]
        public string StudyTechniques { get; set; } = string.Empty;

        public string? Resources { get; set; }
        public string? AdvisorNotes { get; set; }

        // 🔹 Accountability
        public DateTime? CreatedAt { get; set; }
        public DateTime? FollowUpDate { get; set; }
        public string? ExpectedOutcome { get; set; }
    }
}
