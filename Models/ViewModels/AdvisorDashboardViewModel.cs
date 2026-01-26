using System;
using System.Collections.Generic;

namespace StudentTrackingCoach.Models.ViewModels
{
    public class AdvisorDashboardViewModel
    {
        public string AdvisorName { get; set; } = "Advisor";

        public List<AdvisorStudentAttentionViewModel> StudentsRequiringAttention { get; set; }
            = new();
    }

    public class AdvisorStudentAttentionViewModel
    {
        public long StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string RiskLevel { get; set; } = "High";
        public string Reason { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; }
    }
}
