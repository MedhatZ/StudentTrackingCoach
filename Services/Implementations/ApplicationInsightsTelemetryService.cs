using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using StudentTrackingCoach.Services.Interfaces;

namespace StudentTrackingCoach.Services.Implementations
{
    public class ApplicationInsightsTelemetryService : ITelemetryService
    {
        private readonly TelemetryClient _telemetryClient;

        public ApplicationInsightsTelemetryService(TelemetryClient telemetryClient)
        {
            _telemetryClient = telemetryClient;
        }

        public void TrackEvent(string eventName, IDictionary<string, string>? properties = null, IDictionary<string, double>? metrics = null)
            => _telemetryClient.TrackEvent(eventName, properties, metrics);

        public void TrackMetric(string metricName, double value, IDictionary<string, string>? properties = null)
        {
            var metric = new MetricTelemetry(metricName, value);
            if (properties != null)
            {
                foreach (var kv in properties)
                {
                    metric.Properties[kv.Key] = kv.Value;
                }
            }
            _telemetryClient.TrackMetric(metric);
        }

        public void TrackException(Exception exception, IDictionary<string, string>? properties = null)
            => _telemetryClient.TrackException(exception, properties);

        public void TrackDependency(string dependencyType, string target, string operation, DateTimeOffset startTime, TimeSpan duration, bool success)
            => _telemetryClient.TrackDependency(dependencyType, target, operation, startTime, duration, success);

        public void TrackAiCall(string provider, bool success, double durationMs, bool cacheHit, bool fallbackUsed, string advisorId)
        {
            var props = new Dictionary<string, string>
            {
                ["provider"] = provider,
                ["success"] = success.ToString(),
                ["cacheHit"] = cacheHit.ToString(),
                ["fallbackUsed"] = fallbackUsed.ToString(),
                ["advisorId"] = advisorId
            };
            var metrics = new Dictionary<string, double>
            {
                ["durationMs"] = durationMs
            };
            TrackEvent("AiRecommendationCall", props, metrics);
        }

        public void TrackRiskCalculation(string riskLevel, double durationMs)
        {
            var props = new Dictionary<string, string> { ["riskLevel"] = riskLevel };
            var metrics = new Dictionary<string, double> { ["durationMs"] = durationMs };
            TrackEvent("RiskCalculation", props, metrics);
        }
    }
}
