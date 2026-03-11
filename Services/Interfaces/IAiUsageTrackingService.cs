using StudentTrackingCoach.Models.ViewModels;

namespace StudentTrackingCoach.Services.Interfaces
{
    public interface IAiUsageTrackingService
    {
        void RecordCall(string advisorId, bool success, bool fallbackUsed);
        int GetAdvisorDailyCallCount(string advisorId, DateOnly day);
        List<AiUsageMetricViewModel> GetMetricsForDay(DateOnly day);
    }
}
