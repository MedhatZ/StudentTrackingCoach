using StudentTrackingCoach.Models.Ai;

namespace StudentTrackingCoach.Models.ViewModels
{
    public class StudyGuideInterventionContent
    {
        public RecommendedStudyGuideViewModel StudyGuide { get; set; } = new();
        public AiRecommendationResult? OriginalAI { get; set; }
        public List<string> AdvisorChanges { get; set; } = new();
    }
}
