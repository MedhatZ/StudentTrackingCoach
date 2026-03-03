using System.Collections.Generic;

namespace StudentTrackingCoach.Models.ViewModels
{
    public class AdvisorStudentDetailViewModel
    {
        public long StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string? EnrollmentStatus { get; set; }

        public string RiskLevel { get; set; } = "Low";
        public decimal AverageScore { get; set; }
        public string? PrimaryRiskDriver { get; set; }
        public string? SecondaryRiskDriver { get; set; }

        public List<AdvisorNoteViewModel> Notes { get; set; } = new();
    }
}
