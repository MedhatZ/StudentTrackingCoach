using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StudentTrackingCoach.Data;
using StudentTrackingCoach.Models.Ai;
using StudentTrackingCoach.Services.Interfaces;

namespace StudentTrackingCoach.Services.Implementations
{
    public class AzureOpenAiRecommendationService : IAiRecommendationService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ICacheService _cacheService;
        private readonly ApplicationDbContext _db;
        private readonly IRiskCalculationService _riskService;
        private readonly MockAiRecommendationService _mockService;
        private readonly IAiUsageTrackingService _usageTracking;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AzureOpenAiRecommendationService> _logger;
        private readonly ITelemetryService _telemetryService;
        private readonly bool _isConfigured;
        private readonly string _configurationStatus;

        public AzureOpenAiRecommendationService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ICacheService cacheService,
            ApplicationDbContext db,
            IRiskCalculationService riskService,
            MockAiRecommendationService mockService,
            IAiUsageTrackingService usageTracking,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AzureOpenAiRecommendationService> logger,
            ITelemetryService telemetryService)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _cacheService = cacheService;
            _db = db;
            _riskService = riskService;
            _mockService = mockService;
            _usageTracking = usageTracking;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _telemetryService = telemetryService;

            var endpoint = _configuration["AzureOpenAI:Endpoint"] ?? _configuration["AzureOpenAi:Endpoint"];
            var apiKey = _configuration["AzureOpenAI:ApiKey"] ?? _configuration["AzureOpenAi:ApiKey"];
            _isConfigured = !string.IsNullOrWhiteSpace(endpoint) && !string.IsNullOrWhiteSpace(apiKey);
            _configurationStatus = _isConfigured
                ? "Azure OpenAI configured."
                : "Azure OpenAI not configured. Endpoint or ApiKey is missing.";

            if (!_isConfigured)
            {
                _logger.LogWarning("{Status} Real AI calls will fallback to Mock AI.", _configurationStatus);
            }
        }

        public async Task<AiRecommendationResult> GenerateRecommendationsAsync(long studentId)
        {
            var advisorId = GetAdvisorId();
            if (!_isConfigured)
            {
                _logger.LogWarning("Azure OpenAI not configured. Falling back to Mock AI. Details: {Status}", _configurationStatus);
                _usageTracking.RecordCall(advisorId, success: false, fallbackUsed: true);
                _telemetryService.TrackAiCall("AzureOpenAI", success: false, durationMs: 0, cacheHit: false, fallbackUsed: true, advisorId);
                return await _mockService.GenerateRecommendationsAsync(studentId);
            }

            var signals = await BuildSignalsAsync(studentId);
            return await GenerateRecommendationsFromSignalsAsync(signals);
        }

        public async Task<AiRecommendationResult> GenerateRecommendationsFromSignalsAsync(StudentRiskSignals signals)
        {
            var advisorId = GetAdvisorId();
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var maxPerDay = _configuration.GetValue<int>("AiFeatures:MaxRecommendationsPerDay");
            var callsToday = _usageTracking.GetAdvisorDailyCallCount(advisorId, today);

            if (!_isConfigured)
            {
                _logger.LogWarning("Azure OpenAI not configured. Falling back to Mock AI. Details: {Status}", _configurationStatus);
                _usageTracking.RecordCall(advisorId, success: false, fallbackUsed: true);
                return await _mockService.GenerateRecommendationsFromSignalsAsync(signals);
            }

            if (maxPerDay > 0 && callsToday >= maxPerDay)
            {
                _logger.LogWarning("AI daily limit reached for advisor {AdvisorId}. Falling back to mock.", advisorId);
                var limitedResult = await _mockService.GenerateRecommendationsFromSignalsAsync(signals);
                _usageTracking.RecordCall(advisorId, success: false, fallbackUsed: true);
                return limitedResult;
            }

            var cacheHours = _configuration.GetValue<int>("AiFeatures:CacheDurationHours");
            if (cacheHours <= 0) cacheHours = 24;
            var cacheKey = $"ai-rec:{signals.StudentId}:{signals.RiskLevel}:{signals.TermAverage:0.0}";
            var cached = await _cacheService.GetAsync<AiRecommendationResult>(cacheKey);
            if (cached != null)
            {
                _logger.LogInformation("Using cached AI recommendation for student {StudentId}", signals.StudentId);
                _telemetryService.TrackAiCall("AzureOpenAI", success: true, durationMs: 0, cacheHit: true, fallbackUsed: false, advisorId);
                return cached;
            }

            var start = DateTimeOffset.UtcNow;
            try
            {
                var endpoint = (_configuration["AzureOpenAI:Endpoint"] ?? _configuration["AzureOpenAi:Endpoint"])?.TrimEnd('/');
                var apiKey = _configuration["AzureOpenAI:ApiKey"] ?? _configuration["AzureOpenAi:ApiKey"];
                var deployment = _configuration["AzureOpenAI:DeploymentName"] ?? _configuration["AzureOpenAi:DeploymentName"];
                var apiVersion = _configuration["AzureOpenAI:ApiVersion"] ?? _configuration["AzureOpenAi:ApiVersion"] ?? "2024-02-15-preview";

                if (string.IsNullOrWhiteSpace(endpoint) ||
                    string.IsNullOrWhiteSpace(apiKey) ||
                    string.IsNullOrWhiteSpace(deployment))
                {
                    throw new InvalidOperationException("Azure OpenAI configuration is incomplete.");
                }

                var url = $"{endpoint}/openai/deployments/{deployment}/chat/completions?api-version={apiVersion}";
                var payload = new
                {
                    messages = new object[]
                    {
                        new { role = "system", content = AiPromptTemplate.SystemPrompt },
                        new { role = "user", content = AiPromptTemplate.BuildUserPrompt(signals) }
                    },
                    temperature = 0.2,
                    response_format = new { type = "json_object" }
                };

                var responseBody = await SendWithRetryAsync(url, apiKey!, payload);

                var parsed = ParseRecommendationResponse(responseBody);
                await _cacheService.SetAsync(cacheKey, parsed, TimeSpan.FromHours(cacheHours));
                _usageTracking.RecordCall(advisorId, success: true, fallbackUsed: false);
                _logger.LogInformation("Azure AI recommendation generated for student {StudentId}", signals.StudentId);
                var duration = (DateTimeOffset.UtcNow - start).TotalMilliseconds;
                _telemetryService.TrackAiCall("AzureOpenAI", success: true, duration, cacheHit: false, fallbackUsed: false, advisorId);
                return parsed;
            }
            catch (Exception ex)
            {
                _usageTracking.RecordCall(advisorId, success: false, fallbackUsed: true);
                _logger.LogError(ex, "Azure AI failed for student {StudentId}. Falling back to mock.", signals.StudentId);
                await LogAiErrorAsync(signals.StudentId, ex.Message);
                var duration = (DateTimeOffset.UtcNow - start).TotalMilliseconds;
                _telemetryService.TrackAiCall("AzureOpenAI", success: false, duration, cacheHit: false, fallbackUsed: true, advisorId);
                _telemetryService.TrackException(ex, new Dictionary<string, string>
                {
                    ["component"] = "AzureOpenAiRecommendationService",
                    ["studentId"] = signals.StudentId.ToString()
                });
                return await _mockService.GenerateRecommendationsFromSignalsAsync(signals);
            }
        }

        private async Task<StudentRiskSignals> BuildSignalsAsync(long studentId)
        {
            var riskLevel = await _riskService.CalculateStudentRiskLevelAsync(studentId);
            var riskDrivers = await _riskService.GetRiskDriversAsync(studentId);
            var currentGrade = await _riskService.GetSimulatedAverageGradeAsync(studentId);
            var recentNotes = await _db.AdvisorNotes
                .AsNoTracking()
                .Where(n => n.StudentId == studentId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(5)
                .Select(n => n.Notes ?? string.Empty)
                .ToListAsync();

            var courseGrades = new Dictionary<string, decimal>
            {
                ["MAT101"] = Math.Clamp((currentGrade ?? 72m) - 8m, 0m, 100m),
                ["ENG102"] = Math.Clamp((currentGrade ?? 72m) + 2m, 0m, 100m),
                ["SCI110"] = Math.Clamp((currentGrade ?? 72m) - 3m, 0m, 100m)
            };

            return new StudentRiskSignals
            {
                StudentId = studentId,
                RiskLevel = riskLevel,
                RiskDrivers = riskDrivers,
                CurrentGrade = currentGrade,
                CourseGrades = courseGrades,
                RecentNotes = recentNotes,
                Attendance = riskLevel == "High" ? 68m : riskLevel == "Medium" ? 80m : 92m,
                TermAverage = currentGrade
            };
        }

        private static AiRecommendationResult ParseRecommendationResponse(string responseBody)
        {
            using var doc = JsonDocument.Parse(responseBody);
            var content = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            if (string.IsNullOrWhiteSpace(content))
            {
                throw new InvalidOperationException("Azure OpenAI returned empty content.");
            }

            using var contentDoc = JsonDocument.Parse(content);
            var root = contentDoc.RootElement;

            DateTime? followUp = null;
            if (root.TryGetProperty("followUpDate", out var dateEl))
            {
                if (DateTime.TryParse(dateEl.GetString(), out var parsedDate))
                {
                    followUp = parsedDate;
                }
            }

            double? confidence = null;
            if (root.TryGetProperty("confidenceScore", out var confEl) && confEl.ValueKind == JsonValueKind.Number)
            {
                confidence = confEl.GetDouble();
            }

            return new AiRecommendationResult
            {
                FocusAreas = root.GetProperty("focusAreas").GetString() ?? string.Empty,
                StudySchedule = root.GetProperty("studySchedule").GetString() ?? string.Empty,
                StudyTechniques = root.GetProperty("studyTechniques").GetString() ?? string.Empty,
                Resources = root.GetProperty("resources").GetString() ?? string.Empty,
                FollowUpDate = followUp,
                ExpectedOutcome = root.GetProperty("expectedOutcome").GetString() ?? string.Empty,
                ConfidenceScore = confidence
            };
        }

        private async Task LogAiErrorAsync(long studentId, string error)
        {
            try
            {
                _db.AdminAuditLogs.Add(new StudentTrackingCoach.Models.AdminAuditLog
                {
                    Action = "AI Recommendation Failure",
                    TargetUserId = studentId.ToString(),
                    PerformedByUserId = "SYSTEM-AI"
                });
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write AI error admin audit log.");
            }

            _logger.LogWarning("AI fallback activated for student {StudentId}: {Error}", studentId, error);
        }

        private async Task<string> SendWithRetryAsync(string url, string apiKey, object payload)
        {
            Exception? lastException = null;
            for (var attempt = 1; attempt <= 2; attempt++)
            {
                try
                {
                    var client = _httpClientFactory.CreateClient();
                    using var request = new HttpRequestMessage(HttpMethod.Post, url);
                    request.Headers.Add("api-key", apiKey);
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                    using var response = await client.SendAsync(request);
                    var responseBody = await response.Content.ReadAsStringAsync();
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new HttpRequestException($"Azure OpenAI request failed ({(int)response.StatusCode}): {responseBody}");
                    }

                    return responseBody;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    if (attempt < 2)
                    {
                        await Task.Delay(250);
                    }
                }
            }

            throw lastException ?? new InvalidOperationException("Azure OpenAI request failed after retry.");
        }

        private string GetAdvisorId()
        {
            return _httpContextAccessor.HttpContext?.User?.Identity?.Name
                ?? _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? "UNKNOWN";
        }
    }
}
