namespace StudentTrackingCoach.Models
{
    public class AdvisorRiskDashboardDto
    {
        public long StudentID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public decimal AverageScore { get; set; }
        public int RiskScore { get; set; }
        public string RiskLevel { get; set; }
        public string PrimaryRiskDriver { get; set; }
        public string SecondaryRiskDriver { get; set; }
        public DateTime SnapshotDate { get; set; }
    }
}

