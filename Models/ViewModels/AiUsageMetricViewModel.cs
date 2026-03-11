namespace StudentTrackingCoach.Models.ViewModels
{
    public class AiUsageMetricViewModel
    {
        public DateOnly Date { get; set; }
        public string AdvisorId { get; set; } = string.Empty;
        public int TotalCalls { get; set; }
        public int SuccessfulCalls { get; set; }
        public int FailedCalls { get; set; }
        public int FallbackCalls { get; set; }
    }
}
