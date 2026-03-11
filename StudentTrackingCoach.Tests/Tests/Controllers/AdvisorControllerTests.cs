using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using StudentTrackingCoach.Controllers;
using StudentTrackingCoach.Data;
using StudentTrackingCoach.Models;
using StudentTrackingCoach.Models.Ai;
using StudentTrackingCoach.Models.ViewModels;
using StudentTrackingCoach.Services.Interfaces;
using Xunit;

namespace StudentTrackingCoach.Tests.Tests.Controllers
{
    public class AdvisorControllerTests
    {
        private static ApplicationDbContext CreateDb()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task Index_AppliesSearchFilter()
        {
            using var db = CreateDb();
            var advisorService = new Mock<IAdvisorService>();
            advisorService.Setup(x => x.GetStudentsRequiringAttentionAsync(It.IsAny<string>()))
                .ReturnsAsync(new List<AdvisorRiskDashboardDto>
                {
                    new() { StudentId = 1001, RiskLevel = "High" },
                    new() { StudentId = 2002, RiskLevel = "Low" }
                });

            var controller = BuildController(db, advisorService.Object, aiResult: null);
            var result = await controller.Index(search: "1001") as ViewResult;

            var model = result!.Model as List<AdvisorRiskDashboardDto>;
            model!.Count.Should().Be(1);
            model[0].StudentId.Should().Be(1001);
        }

        [Fact]
        public async Task Student_LoadsDetailsAndAiRecommendation()
        {
            using var db = CreateDb();
            var advisorService = new Mock<IAdvisorService>();
            advisorService.Setup(x => x.GetStudentDetailForAdvisorAsync(1001))
                .ReturnsAsync(new AdvisorStudentDetailViewModel { StudentId = 1001, StudentName = "Student 1001", RiskLevel = "Medium" });
            advisorService.Setup(x => x.GetStudentCoursesAsync(1001))
                .ReturnsAsync(new StudentCoursesViewModel { StudentId = 1001 });

            var controller = BuildController(db, advisorService.Object, new AiRecommendationResult { ExpectedOutcome = "test" });
            var result = await controller.Student(1001) as ViewResult;

            result.Should().NotBeNull();
            result!.ViewData.ContainsKey("AiRecommendation").Should().BeTrue();
        }

        [Fact]
        public async Task PendingReviews_ReturnsOnlyPendingItems()
        {
            using var db = CreateDb();
            db.Interventions.AddRange(
                new Intervention { StudentId = 1, Status = "Pending", Content = "{}", Type = "Study Guide", AdvisorId = "a1" },
                new Intervention { StudentId = 2, Status = "Completed", Content = "{}", Type = "Study Guide", AdvisorId = "a1" });
            await db.SaveChangesAsync();

            var advisorService = new Mock<IAdvisorService>();
            var controller = BuildController(db, advisorService.Object, new AiRecommendationResult());
            var result = await controller.PendingReviews() as ViewResult;
            var model = result!.Model as List<Intervention>;

            Assert.NotNull(model);
            model!.Should().HaveCount(1);
            var pending = model[0];
            pending.Status.Should().Be("Pending");
        }

        private static AdvisorController BuildController(ApplicationDbContext db, IAdvisorService advisorService, AiRecommendationResult? aiResult)
        {
            var userStore = new Mock<IUserStore<ApplicationUser>>();
            var userManager = new Mock<UserManager<ApplicationUser>>(
                userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);
            userManager.Setup(x => x.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync(new ApplicationUser { Id = "advisor-1" });

            var aiService = new Mock<IAiRecommendationService>();
            aiService.Setup(x => x.GenerateRecommendationsAsync(It.IsAny<long>()))
                .ReturnsAsync(aiResult ?? new AiRecommendationResult());

            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RiskThresholds:PassingGrade"] = "70",
                ["AiFeatures:ShowConfidenceScore"] = "false"
            }).Build();

            var controller = new AdvisorController(
                userManager.Object,
                advisorService,
                db,
                config,
                aiService.Object,
                new global::StudentTrackingCoach.Services.Implementations.NullTelemetryService());

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            return controller;
        }
    }
}
