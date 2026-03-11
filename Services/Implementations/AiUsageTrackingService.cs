using System.Collections.Concurrent;
using StudentTrackingCoach.Models.ViewModels;
using StudentTrackingCoach.Services.Interfaces;

namespace StudentTrackingCoach.Services.Implementations
{
    public class AiUsageTrackingService : IAiUsageTrackingService
    {
        private static readonly ConcurrentDictionary<string, AiUsageMetricViewModel> _metrics = new();

        public void RecordCall(string advisorId, bool success, bool fallbackUsed)
        {
            var safeAdvisorId = string.IsNullOrWhiteSpace(advisorId) ? "UNKNOWN" : advisorId;
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var key = $"{today:yyyy-MM-dd}:{safeAdvisorId}";

            _metrics.AddOrUpdate(
                key,
                _ => new AiUsageMetricViewModel
                {
                    Date = today,
                    AdvisorId = safeAdvisorId,
                    TotalCalls = 1,
                    SuccessfulCalls = success ? 1 : 0,
                    FailedCalls = success ? 0 : 1,
                    FallbackCalls = fallbackUsed ? 1 : 0
                },
                (_, existing) =>
                {
                    existing.TotalCalls++;
                    if (success) existing.SuccessfulCalls++;
                    else existing.FailedCalls++;
                    if (fallbackUsed) existing.FallbackCalls++;
                    return existing;
                });
        }

        public int GetAdvisorDailyCallCount(string advisorId, DateOnly day)
        {
            var safeAdvisorId = string.IsNullOrWhiteSpace(advisorId) ? "UNKNOWN" : advisorId;
            var key = $"{day:yyyy-MM-dd}:{safeAdvisorId}";
            return _metrics.TryGetValue(key, out var metric) ? metric.TotalCalls : 0;
        }

        public List<AiUsageMetricViewModel> GetMetricsForDay(DateOnly day)
        {
            var prefix = $"{day:yyyy-MM-dd}:";
            return _metrics
                .Where(kv => kv.Key.StartsWith(prefix, StringComparison.Ordinal))
                .Select(kv => kv.Value)
                .OrderByDescending(m => m.TotalCalls)
                .ThenBy(m => m.AdvisorId)
                .ToList();
        }
    }
}
