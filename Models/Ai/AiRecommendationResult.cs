namespace StudentTrackingCoach.Models.Ai
{
    public class AiRecommendationResult
    {
        public string FocusAreas { get; set; } = string.Empty;
        public string StudySchedule { get; set; } = string.Empty;
        public string StudyTechniques { get; set; } = string.Empty;
        public string Resources { get; set; } = string.Empty;
        public DateTime? FollowUpDate { get; set; }
        public string ExpectedOutcome { get; set; } = string.Empty;
        public double? ConfidenceScore { get; set; }
    }
}
