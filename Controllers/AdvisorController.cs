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

        // ============================
        // PAGE 1: Advisor Dashboard
        // ============================
        public async Task<IActionResult> Index()
        {
            var dashboard = await _context.AdvisorRiskDashboard
                .AsNoTracking()
                .OrderByDescending(r => r.RiskScore)
                .ToListAsync();

            return View(dashboard);
        }

        // ============================
        // PAGE 2: High-Risk Triage
        // ============================
        public async Task<IActionResult> Triage()
        {
            var students = await _context.AdvisorRiskDashboard
                .AsNoTracking()
                .Where(r => r.RiskLevel == "High" || r.RiskLevel == "Medium")
                .OrderBy(r => r.RiskLevel)
                .ThenBy(r => r.AverageScore)
                .ToListAsync();

            return View(students);
        }

        // ============================
        // PAGE 3: Student Detail
        // ============================
        public async Task<IActionResult> Student(long id)
        {
            var student = await _context.AdvisorRiskDashboard
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.StudentID == id);

            if (student == null)
                return NotFound();

            var narrative = await _context.StudentRiskNarratives
                .AsNoTracking()
                .Where(n => n.StudentID == id)
                .Select(n => n.AdvisorNarrative)
                .FirstOrDefaultAsync();

            ViewBag.Narrative = narrative;

            return View(student);
        }
    }
}
