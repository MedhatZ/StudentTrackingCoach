using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentTrackingCoach.Models;

namespace StudentTrackingCoach.Controllers
{
    public class AdvisorController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdvisorController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Advisor/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            // 🔢 SUMMARY COUNTS
            int totalStudents = await _context.Students.CountAsync();

            int highRiskStudents = await _context.Students
                .CountAsync(s =>
                    _context.PendingActions.Any(p =>
                        p.StudentId == s.StudentId &&
                        p.Status != "Completed"));

            int openTasks = await _context.PendingActions
                .CountAsync(p => p.Status != "Completed");

            // 📋 STUDENTS NEEDING ATTENTION
            var studentsNeedingAttention = await _context.Students
                .Where(s =>
                    _context.PendingActions.Any(p =>
                        p.StudentId == s.StudentId &&
                        p.Status != "Completed"))
                .Select(s => new AdvisorDashboardStudentView
                {
                    StudentId = s.StudentId,
                    RiskScore = _context.PendingActions
                        .Where(p => p.StudentId == s.StudentId && p.Status != "Completed")
                        .Count() * 0.25
                })
                .OrderByDescending(s => s.RiskScore)
                .ToListAsync();

            var viewModel = new AdvisorDashboardViewModel
            {
                TotalStudents = totalStudents,
                HighRiskStudents = highRiskStudents,
                OpenTasks = openTasks,
                StudentsNeedingAttention = studentsNeedingAttention
            };

            return View(viewModel);
        }
    }

    // 📦 DASHBOARD VIEW MODELS
    public class AdvisorDashboardViewModel
    {
        public int TotalStudents { get; set; }
        public int HighRiskStudents { get; set; }
        public int OpenTasks { get; set; }

        public List<AdvisorDashboardStudentView> StudentsNeedingAttention { get; set; }
            = new();
    }

    public class AdvisorDashboardStudentView
    {
        public long StudentId { get; set; }
        public double RiskScore { get; set; }
    }
}
