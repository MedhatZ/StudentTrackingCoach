using StudentTrackingCoach.Models.Ai;

namespace StudentTrackingCoach.Services.Interfaces
{
    public interface IAiRecommendationService
    {
        Task<AiRecommendationResult> GenerateRecommendationsAsync(long studentId);
        Task<AiRecommendationResult> GenerateRecommendationsFromSignalsAsync(StudentRiskSignals signals);
    }
}
