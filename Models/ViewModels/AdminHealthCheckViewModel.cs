namespace StudentTrackingCoach.Models.ViewModels
{
    public class AdminHealthCheckViewModel
    {
        public bool AzureOpenAiConfigured { get; set; }
        public string AzureOpenAiStatus { get; set; } = string.Empty;
        public bool DatabaseConnected { get; set; }
        public bool MockFallbackActive { get; set; }
        public bool CacheAvailable { get; set; }
        public string CacheStatus { get; set; } = string.Empty;
        public bool RedisEnabled { get; set; }
        public bool RedisConfigured { get; set; }
        public string RedisStatus { get; set; } = string.Empty;
        public bool ApplicationInsightsEnabled { get; set; }
        public bool ApplicationInsightsConfigured { get; set; }
        public string ApplicationInsightsStatus { get; set; } = string.Empty;
        public string AiServiceType { get; set; } = string.Empty;
        public string? LastAiTestResult { get; set; }
    }
}
