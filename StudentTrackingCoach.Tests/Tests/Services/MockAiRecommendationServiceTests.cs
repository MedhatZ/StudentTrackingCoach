using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StudentTrackingCoach.Data;
using StudentTrackingCoach.Models.Ai;
using StudentTrackingCoach.Services.Implementations;
using Moq;
using StudentTrackingCoach.Services.Interfaces;
using Xunit;

namespace StudentTrackingCoach.Tests.Tests.Services
{
    public class MockAiRecommendationServiceTests
    {
        private static ApplicationDbContext CreateDb()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        [Theory]
        [InlineData("High")]
        [InlineData("Medium")]
        [InlineData("Low")]
        public async Task GenerateRecommendationsFromSignalsAsync_ReturnsExpectedPayload(string riskLevel)
        {
            using var db = CreateDb();
            var risk = new Mock<IRiskCalculationService>();
            var service = new MockAiRecommendationService(db, risk.Object);

            var result = await service.GenerateRecommendationsFromSignalsAsync(new StudentRiskSignals
            {
                StudentId = 1,
                RiskLevel = riskLevel,
                RiskDrivers = new List<string> { "First generation student" }
            });

            result.FocusAreas.Should().Contain("Risk-driven focus");
            result.ConfidenceScore.Should().NotBeNull();
            result.ExpectedOutcome.Should().NotBeNullOrWhiteSpace();
        }
    }
}
