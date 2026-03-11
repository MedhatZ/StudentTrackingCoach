namespace StudentTrackingCoach.Services.Interfaces
{
    public interface IConfigurationValidationService
    {
        bool IsAzureOpenAiConfigured();
        string GetConfigurationStatus();
    }
}
