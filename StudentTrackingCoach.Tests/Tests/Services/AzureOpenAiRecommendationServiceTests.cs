using System.Net;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StudentTrackingCoach.Data;
using StudentTrackingCoach.Models;
using StudentTrackingCoach.Services.Implementations;
using StudentTrackingCoach.Services.Interfaces;
using Xunit;

namespace StudentTrackingCoach.Tests.Tests.Services
{
    public class AzureOpenAiRecommendationServiceTests
    {
        private static ApplicationDbContext CreateDb()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        private static IHttpContextAccessor CreateHttpAccessor()
        {
            var context = new DefaultHttpContext();
            context.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "advisor-1")
            }, "test"));
            return new HttpContextAccessor { HttpContext = context };
        }

        [Fact]
        public async Task GenerateRecommendationsAsync_FallsBackToMock_WhenNotConfigured()
        {
            using var db = CreateDb();
            db.Students.Add(new Student { StudentId = 1, InstitutionId = 1, IsFirstGen = true, IsWorking = false, EnrollmentStatus = "Active" });
            await db.SaveChangesAsync();

            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureOpenAI:Endpoint"] = "",
                ["AzureOpenAI:ApiKey"] = "",
                ["AiFeatures:MaxRecommendationsPerDay"] = "10",
                ["AiFeatures:CacheDurationHours"] = "24"
            }).Build();

            var riskService = new Mock<IRiskCalculationService>();
            riskService.Setup(x => x.CalculateStudentRiskLevelAsync(It.IsAny<long>())).ReturnsAsync("High");
            riskService.Setup(x => x.GetRiskDriversAsync(It.IsAny<long>())).ReturnsAsync(new List<string> { "risk" });
            riskService.Setup(x => x.GetSimulatedAverageGradeAsync(It.IsAny<long>())).ReturnsAsync(62m);

            var mockAi = new MockAiRecommendationService(db, riskService.Object);
            var factory = new Mock<IHttpClientFactory>();
            var usage = new AiUsageTrackingService();
            var cache = new MemoryCacheFallbackService(new MemoryCache(new MemoryCacheOptions()));

            var service = new AzureOpenAiRecommendationService(
                factory.Object, config, cache, db, riskService.Object, mockAi, usage, CreateHttpAccessor(),
                NullLogger<AzureOpenAiRecommendationService>.Instance, new NullTelemetryService());

            var result = await service.GenerateRecommendationsAsync(1);

            result.FocusAreas.Should().Contain("Risk-driven focus");
            usage.GetMetricsForDay(DateOnly.FromDateTime(DateTime.UtcNow)).Sum(m => m.FallbackCalls).Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task GenerateRecommendationsFromSignalsAsync_UsesCache_OnSecondCall()
        {
            using var db = CreateDb();
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureOpenAI:Endpoint"] = "https://test.openai.azure.com",
                ["AzureOpenAI:ApiKey"] = "abc",
                ["AzureOpenAI:DeploymentName"] = "gpt-4",
                ["AzureOpenAI:ApiVersion"] = "2024-02-15-preview",
                ["AiFeatures:MaxRecommendationsPerDay"] = "10",
                ["AiFeatures:CacheDurationHours"] = "24"
            }).Build();

            var handler = new CountingHandler();
            var client = new HttpClient(handler);
            var factory = new Mock<IHttpClientFactory>();
            factory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(client);

            var riskService = new Mock<IRiskCalculationService>();
            var mockAi = new MockAiRecommendationService(db, riskService.Object);
            var cache = new MemoryCacheFallbackService(new MemoryCache(new MemoryCacheOptions()));
            var service = new AzureOpenAiRecommendationService(
                factory.Object, config, cache, db, riskService.Object, mockAi, new AiUsageTrackingService(),
                CreateHttpAccessor(), NullLogger<AzureOpenAiRecommendationService>.Instance, new NullTelemetryService());

            var signals = new Models.Ai.StudentRiskSignals
            {
                StudentId = 12,
                RiskLevel = "Medium",
                RiskDrivers = new List<string> { "driver" },
                TermAverage = 75
            };

            var r1 = await service.GenerateRecommendationsFromSignalsAsync(signals);
            var r2 = await service.GenerateRecommendationsFromSignalsAsync(signals);

            r1.FocusAreas.Should().NotBeNullOrEmpty();
            r2.FocusAreas.Should().Be(r1.FocusAreas);
            handler.CallCount.Should().Be(1);
        }

        [Fact]
        public async Task GenerateRecommendationsFromSignalsAsync_RetriesAndSucceeds_OnSecondAttempt()
        {
            using var db = CreateDb();
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureOpenAI:Endpoint"] = "https://test.openai.azure.com",
                ["AzureOpenAI:ApiKey"] = "abc",
                ["AzureOpenAI:DeploymentName"] = "gpt-4",
                ["AzureOpenAI:ApiVersion"] = "2024-02-15-preview",
                ["AiFeatures:MaxRecommendationsPerDay"] = "10",
                ["AiFeatures:CacheDurationHours"] = "24"
            }).Build();

            var handler = new FlakyHandler();
            var client = new HttpClient(handler);
            var factory = new Mock<IHttpClientFactory>();
            factory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(client);

            var riskService = new Mock<IRiskCalculationService>();
            var mockAi = new MockAiRecommendationService(db, riskService.Object);
            var cache = new MemoryCacheFallbackService(new MemoryCache(new MemoryCacheOptions()));
            var service = new AzureOpenAiRecommendationService(
                factory.Object, config, cache, db, riskService.Object, mockAi, new AiUsageTrackingService(),
                CreateHttpAccessor(), NullLogger<AzureOpenAiRecommendationService>.Instance, new NullTelemetryService());

            var result = await service.GenerateRecommendationsFromSignalsAsync(new Models.Ai.StudentRiskSignals
            {
                StudentId = 50,
                RiskLevel = "Low",
                TermAverage = 90
            });

            result.ExpectedOutcome.Should().Be("E");
            handler.CallCount.Should().Be(2);
        }

        private sealed class CountingHandler : HttpMessageHandler
        {
            public int CallCount { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                CallCount++;
                const string body = """
                {
                  "choices": [
                    {
                      "message": {
                        "content": "{\"focusAreas\":\"A\",\"studySchedule\":\"B\",\"studyTechniques\":\"C\",\"resources\":\"D\",\"followUpDate\":\"2026-01-01\",\"expectedOutcome\":\"E\",\"confidenceScore\":0.9}"
                      }
                    }
                  ]
                }
                """;

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(body)
                });
            }
        }

        private sealed class FlakyHandler : HttpMessageHandler
        {
            public int CallCount { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                CallCount++;
                if (CallCount == 1)
                {
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
                    {
                        Content = new StringContent("boom")
                    });
                }

                const string body = """
                {
                  "choices": [
                    {
                      "message": {
                        "content": "{\"focusAreas\":\"A\",\"studySchedule\":\"B\",\"studyTechniques\":\"C\",\"resources\":\"D\",\"followUpDate\":\"2026-01-01\",\"expectedOutcome\":\"E\",\"confidenceScore\":0.9}"
                      }
                    }
                  ]
                }
                """;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(body)
                });
            }
        }
    }
}
