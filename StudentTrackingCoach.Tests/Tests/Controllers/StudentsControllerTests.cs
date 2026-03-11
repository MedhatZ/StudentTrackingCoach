using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using StudentTrackingCoach.Controllers;
using StudentTrackingCoach.Data;
using StudentTrackingCoach.Models;
using StudentTrackingCoach.Models.ViewModels;
using StudentTrackingCoach.Services.Interfaces;
using StudentTrackingCoach.Tests.Tests.Helpers;
using Xunit;

namespace StudentTrackingCoach.Tests.Tests.Controllers
{
    public class StudentsControllerTests
    {
        private static ApplicationDbContext CreateDb()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task Index_ReturnsPaginatedStudents()
        {
            using var db = CreateDb();
            var students = Enumerable.Range(1, 30).Select(i => TestDataFactory.CreateStudent(i, false, true)).ToList();
            var studentService = new Mock<IStudentService>();
            studentService.Setup(s => s.GetAllStudentsAsync(It.IsAny<bool>())).ReturnsAsync(students);
            var riskService = new Mock<IRiskCalculationService>();
            riskService.Setup(r => r.CalculateStudentRiskLevelAsync(It.IsAny<long>())).ReturnsAsync("Low");

            var controller = BuildController(db, studentService.Object, riskService.Object, new Mock<IAiRecommendationService>().Object, user: null);

            var result = await controller.Index(pageNumber: 2, pageSize: 20) as ViewResult;

            result.Should().NotBeNull();
            var model = result!.Model as List<StudentListViewModel>;
            model.Should().NotBeNull();
            model!.Count.Should().Be(10);
        }

        [Fact]
        public async Task MyStudyGuides_AdminWithoutStudentId_ShowsAdminMessage()
        {
            using var db = CreateDb();
            var user = new ApplicationUser { Id = "admin-1", Email = "admin@test.com", StudentId = null };
            var controller = BuildController(
                db,
                new Mock<IStudentService>().Object,
                new Mock<IRiskCalculationService>().Object,
                new Mock<IAiRecommendationService>().Object,
                user,
                roles: new[] { "Admin" });

            var result = await controller.MyStudyGuides() as ViewResult;

            result.Should().NotBeNull();
            result!.ViewData["ErrorMessage"]?.ToString().Should().Contain("Admins don't have personal study guides");
        }

        [Fact]
        public async Task MyStudyGuides_StudentWithStudentId_ReturnsView()
        {
            using var db = CreateDb();
            var user = new ApplicationUser { Id = "student-1", Email = "student@test.com", StudentId = 1001 };
            var controller = BuildController(
                db,
                new Mock<IStudentService>().Object,
                new Mock<IRiskCalculationService>().Object,
                new Mock<IAiRecommendationService>().Object,
                user,
                roles: new[] { "Student" });

            var result = await controller.MyStudyGuides();

            result.Should().BeOfType<ViewResult>();
        }

        [Fact]
        public async Task SaveStudyGuide_CreatesIntervention()
        {
            using var db = CreateDb();
            var user = new ApplicationUser { Id = "advisor-1", Email = "advisor@test.com" };
            var controller = BuildController(
                db,
                new Mock<IStudentService>().Object,
                new Mock<IRiskCalculationService>().Object,
                new Mock<IAiRecommendationService>().Object,
                user);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "advisor-1") }, "test"))
                }
            };

            var model = TestDataFactory.CreateGuideVm(1001);
            var result = await controller.SaveStudyGuide(model);

            result.Should().BeOfType<RedirectToActionResult>();
            db.Interventions.Count().Should().Be(1);
        }

        private static StudentsController BuildController(
            ApplicationDbContext db,
            IStudentService studentService,
            IRiskCalculationService riskService,
            IAiRecommendationService aiService,
            ApplicationUser? user,
            string[]? roles = null)
        {
            var userManager = MockUserManager(user);
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["AiFeatures:Enabled"] = "false" })
                .Build();

            var controller = new StudentsController(
                userManager.Object,
                db,
                studentService,
                riskService,
                aiService,
                config,
                new global::StudentTrackingCoach.Services.Implementations.NullTelemetryService());

            var claims = new List<Claim>();
            if (roles != null)
            {
                claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
            }
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"))
                }
            };
            controller.TempData = new TempDataDictionary(controller.HttpContext, Mock.Of<ITempDataProvider>());
            return controller;
        }

        private static Mock<UserManager<ApplicationUser>> MockUserManager(ApplicationUser? user)
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            var mock = new Mock<UserManager<ApplicationUser>>(
                store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
            mock.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
            return mock;
        }
    }
}
