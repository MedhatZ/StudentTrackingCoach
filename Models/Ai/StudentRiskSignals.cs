namespace StudentTrackingCoach.Models.Ai
{
    public class StudentRiskSignals
    {
        public long StudentId { get; set; }
        public string RiskLevel { get; set; } = "Low";
        public List<string> RiskDrivers { get; set; } = new();
        public decimal? CurrentGrade { get; set; }
        public Dictionary<string, decimal> CourseGrades { get; set; } = new();
        public List<string> RecentNotes { get; set; } = new();
        public decimal? Attendance { get; set; }
        public decimal? TermAverage { get; set; }
    }
}
