namespace StudentTrackingCoach.Models.ViewModels
{
    public class AdminDataQualityViewModel
    {
        public int TotalStudents { get; set; }
        public int StudentsWithNotes { get; set; }
        public int HighRiskStudents { get; set; }
        public int MediumRiskStudents { get; set; }
        public int LowRiskStudents { get; set; }
        public List<StudentDataIssue> StudentsWithIssues { get; set; } = new();
    }

    public class StudentDataIssue
    {
        public long StudentId { get; set; }
        public string Issue { get; set; } = string.Empty;
    }
}
