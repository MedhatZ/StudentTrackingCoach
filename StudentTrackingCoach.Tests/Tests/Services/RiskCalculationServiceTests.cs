using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StudentTrackingCoach.Data;
using StudentTrackingCoach.Services.Implementations;
using StudentTrackingCoach.Tests.Tests.Helpers;
using Xunit;

namespace StudentTrackingCoach.Tests.Tests.Services
{
    public class RiskCalculationServiceTests
    {
        private static ApplicationDbContext CreateDb()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task CalculateStudentRiskLevelAsync_ReturnsHigh_WhenMultipleRiskFactors()
        {
            using var db = CreateDb();
            db.Students.Add(TestDataFactory.CreateStudent(1, true, false, "Probation"));
            await db.SaveChangesAsync();
            var service = new RiskCalculationService(db, new NullTelemetryService());

            var level = await service.CalculateStudentRiskLevelAsync(1);

            level.Should().Be("High");
        }

        [Fact]
        public async Task CalculateStudentRiskLevelAsync_ReturnsMedium_WhenSingleRiskFactor()
        {
            using var db = CreateDb();
            db.Students.Add(TestDataFactory.CreateStudent(2, true, true, "Active"));
            await db.SaveChangesAsync();
            var service = new RiskCalculationService(db, new NullTelemetryService());

            var level = await service.CalculateStudentRiskLevelAsync(2);

            level.Should().Be("Medium");
        }

        [Fact]
        public async Task CalculateStudentRiskLevelAsync_ReturnsLow_WhenNoRiskFactors()
        {
            using var db = CreateDb();
            db.Students.Add(TestDataFactory.CreateStudent(3, false, true, "Active"));
            await db.SaveChangesAsync();
            var service = new RiskCalculationService(db, new NullTelemetryService());

            var level = await service.CalculateStudentRiskLevelAsync(3);

            level.Should().Be("Low");
        }

        [Fact]
        public async Task CalculateStudentRiskLevelAsync_ReturnsHigh_WhenNegativeNotesAreMany()
        {
            using var db = CreateDb();
            db.Students.Add(TestDataFactory.CreateStudent(4, false, true, "Active"));
            db.AdvisorNotes.AddRange(
                TestDataFactory.CreateAdvisorNote(4, "student fail class"),
                TestDataFactory.CreateAdvisorNote(4, "student struggles"),
                TestDataFactory.CreateAdvisorNote(4, "miss attendance"));
            await db.SaveChangesAsync();
            var service = new RiskCalculationService(db, new NullTelemetryService());

            var level = await service.CalculateStudentRiskLevelAsync(4);

            level.Should().Be("High");
        }

        [Fact]
        public async Task CalculateStudentRiskLevelAsync_ReturnsLow_WhenStudentMissing()
        {
            using var db = CreateDb();
            var service = new RiskCalculationService(db, new NullTelemetryService());

            var level = await service.CalculateStudentRiskLevelAsync(999);

            level.Should().Be("Low");
        }
    }
}
