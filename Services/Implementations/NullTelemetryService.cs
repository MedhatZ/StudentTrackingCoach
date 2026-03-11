using StudentTrackingCoach.Services.Interfaces;

namespace StudentTrackingCoach.Services.Implementations
{
    public class NullTelemetryService : ITelemetryService
    {
        public void TrackEvent(string eventName, IDictionary<string, string>? properties = null, IDictionary<string, double>? metrics = null) { }
        public void TrackMetric(string metricName, double value, IDictionary<string, string>? properties = null) { }
        public void TrackException(Exception exception, IDictionary<string, string>? properties = null) { }
        public void TrackDependency(string dependencyType, string target, string operation, DateTimeOffset startTime, TimeSpan duration, bool success) { }
        public void TrackAiCall(string provider, bool success, double durationMs, bool cacheHit, bool fallbackUsed, string advisorId) { }
        public void TrackRiskCalculation(string riskLevel, double durationMs) { }
    }
}
