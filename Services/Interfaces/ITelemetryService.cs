namespace StudentTrackingCoach.Services.Interfaces
{
    public interface ITelemetryService
    {
        void TrackEvent(string eventName, IDictionary<string, string>? properties = null, IDictionary<string, double>? metrics = null);
        void TrackMetric(string metricName, double value, IDictionary<string, string>? properties = null);
        void TrackException(Exception exception, IDictionary<string, string>? properties = null);
        void TrackDependency(string dependencyType, string target, string operation, DateTimeOffset startTime, TimeSpan duration, bool success);
        void TrackAiCall(string provider, bool success, double durationMs, bool cacheHit, bool fallbackUsed, string advisorId);
        void TrackRiskCalculation(string riskLevel, double durationMs);
    }
}
