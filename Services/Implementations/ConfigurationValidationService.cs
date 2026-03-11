using StudentTrackingCoach.Services.Interfaces;

namespace StudentTrackingCoach.Services.Implementations
{
    public class ConfigurationValidationService : IConfigurationValidationService
    {
        private readonly IConfiguration _configuration;

        public ConfigurationValidationService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public bool IsAzureOpenAiConfigured()
        {
            var endpoint = _configuration["AzureOpenAI:Endpoint"];
            var apiKey = _configuration["AzureOpenAI:ApiKey"];

            // Backward compatibility with older key casing.
            endpoint ??= _configuration["AzureOpenAi:Endpoint"];
            apiKey ??= _configuration["AzureOpenAi:ApiKey"];

            return !string.IsNullOrWhiteSpace(endpoint) && !string.IsNullOrWhiteSpace(apiKey);
        }

        public string GetConfigurationStatus()
        {
            var aiEnabled = _configuration.GetValue<bool>("AiFeatures:Enabled");
            if (!aiEnabled)
            {
                return "AI features are disabled; mock recommendation service will be used.";
            }

            if (!IsAzureOpenAiConfigured())
            {
                return "Azure OpenAI is not configured (Endpoint or ApiKey missing); mock fallback is active.";
            }

            return "Azure OpenAI is configured and ready.";
        }
    }
}
