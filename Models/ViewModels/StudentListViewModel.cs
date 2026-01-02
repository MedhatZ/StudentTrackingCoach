namespace StudentTrackingCoach.Models.ViewModels
{
    public class StudentListViewModel
    {
        public long StudentId { get; set; }

        // Nullable-safe reference types
        public string? EnrollmentStatus { get; set; }
        public string? PreferredModality { get; set; }

        public bool? IsFirstGen { get; set; }
        public bool? IsWorking { get; set; }
        public DateTime? CreatedAt { get; set; }

        // 🔥 Risk signal
        public bool HasOpenPendingActions { get; set; }

        // 🔢 Risk Priority (REQUIRED by controller)
        // 1 = High, 2 = Medium, 3 = Low
        public int RiskPriority { get; set; }
    }
}
