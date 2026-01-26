using System;

namespace StudentTrackingCoach.Models.ViewModels
{
    public class StudentListViewModel
    {
        // ================================
        // Core Identity
        // ================================
        public long StudentId { get; set; }

        public int InstitutionId { get; set; }

        // ================================
        // Enrollment & Demographics
        // ================================
        public string? EnrollmentStatus { get; set; }

        public bool? IsFirstGen { get; set; }

        public bool? IsWorking { get; set; }

        public string? PreferredModality { get; set; }

        public DateTime? CreatedAt { get; set; }

        // ================================
        // Risk Signals (UI + Analytics)
        // ================================

        /// <summary>
        /// True if the student has unresolved alerts, tasks, or interventions
        /// </summary>
        public bool HasOpenPendingActions { get; set; }

        /// <summary>
        /// Risk Priority used by UI + filters
        /// 1 = High, 2 = Medium, 3 = Low
        /// </summary>
        public int RiskPriority { get; set; }
    }
}
