using StudentTrackingCoach.Models.RUM;
using StudentTrackingCoach.Services.Interfaces;

namespace StudentTrackingCoach.Services.Implementations
{
    public class ApplicationInsightsRUMService : IRUMService
    {
        private readonly ITelemetryService _telemetry;

        public ApplicationInsightsRUMService(ITelemetryService telemetry)
        {
            _telemetry = telemetry;
        }

        public Task TrackPageViewAsync(PageViewModel pageView, string? role)
        {
            var properties = new Dictionary<string, string>
            {
                ["path"] = pageView.Path,
                ["title"] = pageView.PageTitle ?? string.Empty,
                ["browser"] = pageView.Browser ?? string.Empty,
                ["deviceType"] = pageView.DeviceType ?? string.Empty,
                ["screenSize"] = pageView.ScreenSize ?? string.Empty,
                ["region"] = pageView.Region ?? string.Empty,
                ["sessionId"] = pageView.SessionId ?? string.Empty,
                ["role"] = role ?? "Anonymous"
            };

            var metrics = new Dictionary<string, double>();
            if (pageView.TtfbMs.HasValue) metrics["ttfbMs"] = pageView.TtfbMs.Value;
            if (pageView.FirstContentfulPaintMs.HasValue) metrics["fcpMs"] = pageView.FirstContentfulPaintMs.Value;
            if (pageView.TimeToInteractiveMs.HasValue) metrics["ttiMs"] = pageView.TimeToInteractiveMs.Value;
            if (pageView.PageLoadCompleteMs.HasValue) metrics["loadCompleteMs"] = pageView.PageLoadCompleteMs.Value;

            _telemetry.TrackEvent("RUM.PageView", properties, metrics);
            return Task.CompletedTask;
        }

        public Task TrackUserActionAsync(UserActionModel actionModel, string? role)
        {
            var properties = new Dictionary<string, string>
            {
                ["action"] = actionModel.ActionName,
                ["path"] = actionModel.Path ?? string.Empty,
                ["elementType"] = actionModel.ElementType ?? string.Empty,
                ["elementId"] = actionModel.ElementId ?? string.Empty,
                ["sessionId"] = actionModel.SessionId ?? string.Empty,
                ["region"] = actionModel.Region ?? string.Empty,
                ["role"] = role ?? "Anonymous",
                ["success"] = actionModel.Success.ToString()
            };

            _telemetry.TrackEvent("RUM.UserAction", properties);
            return Task.CompletedTask;
        }
    }
}
