using Microsoft.EntityFrameworkCore;
using StudentTrackingCoach.Data;
using StudentTrackingCoach.Models;
using StudentTrackingCoach.Models.ViewModels;
using StudentTrackingCoach.Services.Interfaces;

namespace StudentTrackingCoach.Services.Implementations
{
    public class AdvisorService : IAdvisorService
    {
        private readonly ApplicationDbContext _db;
        private readonly IRiskCalculationService _riskService;
        
        public AdvisorService(ApplicationDbContext db, IRiskCalculationService riskService)
        {
            _db = db;
            _riskService = riskService;
        }
        
        public async Task<List<AdvisorRiskDashboardDto>> GetStudentsRequiringAttentionAsync(string advisorId)
        {
            var students = await _db.Students
                .AsNoTracking()
                .Take(20)
                .ToListAsync();

            var result = new List<AdvisorRiskDashboardDto>();

            foreach (var student in students)
            {
                var riskLevel = await _riskService.CalculateStudentRiskLevelAsync(student.StudentId);
                var riskDrivers = await _riskService.GetRiskDriversAsync(student.StudentId);
                var simulatedGrade = await _riskService.GetSimulatedAverageGradeAsync(student.StudentId);

                result.Add(new AdvisorRiskDashboardDto
                {
                    StudentId = student.StudentId,
                    RiskLevel = riskLevel,
                    AverageScore = simulatedGrade,
                    PrimaryRiskDriver = riskDrivers.FirstOrDefault() ?? "No specific risk drivers",
                    SecondaryRiskDriver = riskDrivers.Count > 1 ? riskDrivers[1] : "",
                    LastEvaluatedAt = DateTime.UtcNow
                });
            }

            return result
                .OrderByDescending(r => r.RiskLevel == "High")
                .ThenByDescending(r => r.RiskLevel == "Medium")
                .ToList();
        }
        
        public async Task<AdvisorStudentDetailViewModel> GetStudentDetailForAdvisorAsync(long studentId)
        {
            var student = await _db.Students
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.StudentId == studentId);

            if (student == null)
                return null;

            var riskLevel = await _riskService.CalculateStudentRiskLevelAsync(studentId);
            var riskDrivers = await _riskService.GetRiskDriversAsync(studentId);
            var simulatedGrade = await _riskService.GetSimulatedAverageGradeAsync(studentId);

            var notes = await _db.AdvisorNotes
                .AsNoTracking()
                .Where(n => n.StudentId == studentId)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new AdvisorNoteViewModel
                {
                    Title = n.ActionTaken,
                    Notes = n.Notes,
                    CreatedAt = n.CreatedAt,
                    CreatedBy = n.AdvisorUserId,
                    Severity = "Medium",
                    Category = "Advising"
                })
                .ToListAsync();
                
            return new AdvisorStudentDetailViewModel
            {
                StudentId = student.StudentId,
                StudentName = $"Student {student.StudentId}",
                EnrollmentStatus = student.EnrollmentStatus ?? "Enrolled",
                RiskLevel = riskLevel,
                AverageScore = simulatedGrade ?? 0,
                PrimaryRiskDriver = riskDrivers.FirstOrDefault() ?? "No specific risk drivers",
                SecondaryRiskDriver = riskDrivers.Count > 1 ? riskDrivers[1] : "",
                Notes = notes
            };
        }

        public async Task<StudentCoursesViewModel> GetStudentCoursesAsync(long studentId)
        {
            var studentExists = await _db.Students
                .AsNoTracking()
                .AnyAsync(s => s.StudentId == studentId);

            if (!studentExists)
            {
                return new StudentCoursesViewModel
                {
                    StudentId = studentId
                };
            }

            var simulatedAverage = await _riskService.GetSimulatedAverageGradeAsync(studentId) ?? 70m;
            var baseGrade = Math.Clamp(simulatedAverage, 45m, 96m);

            // The current schema has no persisted course-grade table yet, so we provide
            // stable current-term course grades derived from existing risk data.
            var courses = new List<CourseGradeViewModel>
            {
                new CourseGradeViewModel
                {
                    CourseCode = "MAT101",
                    CourseName = "College Algebra",
                    CurrentGrade = Math.Round(Math.Clamp(baseGrade - 8m, 0m, 100m), 1)
                },
                new CourseGradeViewModel
                {
                    CourseCode = "ENG102",
                    CourseName = "Academic Writing",
                    CurrentGrade = Math.Round(Math.Clamp(baseGrade + 3m, 0m, 100m), 1)
                },
                new CourseGradeViewModel
                {
                    CourseCode = "CIS110",
                    CourseName = "Introduction to Computing",
                    CurrentGrade = Math.Round(Math.Clamp(baseGrade + 1m, 0m, 100m), 1)
                }
            };

            return new StudentCoursesViewModel
            {
                StudentId = studentId,
                Courses = courses
            };
        }

        public async Task<bool> LogNoteAsync(AdvisorNoteInputModel input, string advisorUserId)
        {
            try
            {
                var note = new AdvisorNote
                {
                    StudentId = input.StudentId,
                    AdvisorUserId = advisorUserId,
                    ActionTaken = input.ActionTaken,
                    Notes = input.Notes,
                    CreatedAt = DateTime.UtcNow
                };

                _db.AdvisorNotes.Add(note);
                await _db.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
