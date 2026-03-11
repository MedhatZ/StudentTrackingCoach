using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Moq;
using StudentTrackingCoach.Controllers;
using StudentTrackingCoach.Data;
using StudentTrackingCoach.Models;
using StudentTrackingCoach.Services.Implementations;
using StudentTrackingCoach.Services.Interfaces;
using Xunit;

namespace StudentTrackingCoach.Tests.Tests.Controllers
{
    public class AdminControllerTests
    {
        private static ApplicationDbContext CreateDb()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task HealthCheck_ReturnsViewModel()
        {
            using var db = CreateDb();
            var controller = BuildController(db);

            var result = await controller.HealthCheck() as ViewResult;

            result.Should().NotBeNull();
        }

        [Fact]
        public async Task DataQuality_ReturnsView()
        {
            using var db = CreateDb();
            db.Students.Add(new Student { StudentId = 1, InstitutionId = 1, EnrollmentStatus = "Active", IsFirstGen = true, IsWorking = false });
            await db.SaveChangesAsync();

            var controller = BuildController(db);
            var result = await controller.DataQuality() as ViewResult;

            result.Should().NotBeNull();
        }

        private static AdminController BuildController(ApplicationDbContext db)
        {
            var userStore = new Mock<IUserStore<ApplicationUser>>();
            var userManager = new Mock<UserManager<ApplicationUser>>(
                userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);
            var roleStore = new Mock<IRoleStore<IdentityRole>>();
            var roleManager = new Mock<RoleManager<IdentityRole>>(
                roleStore.Object, null!, null!, null!, null!);

            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AiFeatures:Enabled"] = "true",
                ["AiFeatures:UseRealAi"] = "false",
                ["Redis:Enabled"] = "false",
                ["ApplicationInsights:Enabled"] = "false"
            }).Build();

            return new AdminController(
                userManager.Object,
                roleManager.Object,
                db,
                new AiUsageTrackingService(),
                new ConfigurationValidationService(config),
                new MemoryCache(new MemoryCacheOptions()),
                new Mock<IAiRecommendationService>().Object,
                config,
                new RiskCalculationService(db, new NullTelemetryService()),
                new NullTelemetryService(),
                new MemoryCacheFallbackService(new MemoryCache(new MemoryCacheOptions())))
            {
                TempData = new TempDataDictionary(new Microsoft.AspNetCore.Http.DefaultHttpContext(), Mock.Of<ITempDataProvider>())
            };
        }
    }
}
