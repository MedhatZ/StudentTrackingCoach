using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StudentTrackingCoach.Data;
using StudentTrackingCoach.Services.Implementations;
using StudentTrackingCoach.Tests.Tests.Helpers;
using Xunit;

namespace StudentTrackingCoach.Tests.Tests.Services
{
    public class StudentServiceTests
    {
        private static ApplicationDbContext CreateDb()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task GetStudentByIdAsync_ReturnsStudent_WhenExists()
        {
            using var db = CreateDb();
            db.Students.Add(TestDataFactory.CreateStudent(1001, true, false));
            await db.SaveChangesAsync();
            var service = new StudentService(db);

            var student = await service.GetStudentByIdAsync(1001);

            student.Should().NotBeNull();
            student!.StudentId.Should().Be(1001);
        }

        [Fact]
        public async Task GetStudentByIdAsync_ReturnsNull_WhenMissing()
        {
            using var db = CreateDb();
            var service = new StudentService(db);

            var student = await service.GetStudentByIdAsync(5000);

            student.Should().BeNull();
        }

        [Fact]
        public async Task GetAllStudentsAsync_HighRiskOnlyFiltersByLegacyRule()
        {
            using var db = CreateDb();
            db.Students.AddRange(
                TestDataFactory.CreateStudent(1, false, true),
                TestDataFactory.CreateStudent(2, true, true),
                TestDataFactory.CreateStudent(3, false, false));
            await db.SaveChangesAsync();
            var service = new StudentService(db);

            var students = await service.GetAllStudentsAsync(highRiskOnly: true);

            students.Select(s => s.StudentId).Should().BeEquivalentTo(new[] { 2L, 3L });
        }
    }
}
