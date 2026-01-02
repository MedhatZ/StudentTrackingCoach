using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentTrackingCoach.Models;
using StudentTrackingCoach.Models.ViewModels;

namespace StudentTrackingCoach.Controllers
{
    public class StudentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StudentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Students
        // ✅ Supports filtering + auto-sort by risk
        public async Task<IActionResult> Index(bool highRiskOnly = false)
        {
            var studentsQuery = _context.Students
                .Select(s => new StudentListViewModel
                {
                    StudentId = s.StudentId,
                    EnrollmentStatus = s.EnrollmentStatus,
                    IsFirstGen = s.IsFirstGen,
                    IsWorking = s.IsWorking,
                    PreferredModality = s.PreferredModality,
                    CreatedAt = s.CreatedAt,

                    // 🔥 REAL RISK SIGNAL
                    HasOpenPendingActions = _context.PendingActions
                        .Any(p => p.StudentId == s.StudentId && p.Status != "Completed"),

                    // 🔢 Risk Priority (lower = higher priority)
                    // 1 = High, 2 = Medium, 3 = Low
                    RiskPriority =
                        _context.PendingActions.Any(p => p.StudentId == s.StudentId && p.Status != "Completed")
                            ? 1
                            : (!s.IsFirstGen.HasValue || !s.IsWorking.HasValue ? 2 : 3)
                });

            // ✅ Filter: High Risk Only
            if (highRiskOnly)
            {
                studentsQuery = studentsQuery
                    .Where(s => s.HasOpenPendingActions);
            }

            // ✅ Auto-sort by risk, then StudentId for stability
            var students = await studentsQuery
                .OrderBy(s => s.RiskPriority)
                .ThenBy(s => s.StudentId)
                .ToListAsync();

            ViewBag.HighRiskOnly = highRiskOnly;

            return View(students);
        }

        // GET: /Students/Details/{id}
        public async Task<IActionResult> Details(long id)
        {
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentId == id);

            if (student == null)
                return NotFound();

            var decisions = await _context.DecisionAudits
                .Where(d => d.StudentId == id)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            var pendingActions = await _context.PendingActions
                .Where(p => p.StudentId == id && p.Status != "Completed")
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            ViewBag.Decisions = decisions;
            ViewBag.PendingActions = pendingActions;

            return View(student);
        }

        // POST: /Students/ResolveAction
        [HttpPost]
        public async Task<IActionResult> ResolveAction(long actionId, long studentId)
        {
            var action = await _context.PendingActions
                .FirstOrDefaultAsync(a => a.ActionId == actionId);

            if (action == null)
                return NotFound();

            // Mark action completed
            action.Status = "Completed";

            // Write audit record
            var audit = new DecisionAudit
            {
                StudentId = studentId,
                Decision = "Pending Action Resolved",
                Reason = action.Reason,
                Source = "StudentTrackingCoach",
                CreatedAt = DateTime.UtcNow
            };

            _context.DecisionAudits.Add(audit);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = studentId });
        }

        // POST: /Students/RunInterventions  ⭐ SMART RULE EXECUTION ⭐
        [HttpPost]
        public async Task<IActionResult> RunInterventions(long studentId)
        {
            // 1. Check for existing open pending actions
            bool hasOpenActions = await _context.PendingActions
                .AnyAsync(p => p.StudentId == studentId && p.Status != "Completed");

            if (hasOpenActions)
            {
                TempData["RuleMessage"] =
                    "Pending actions already exist. No new actions generated.";

                return RedirectToAction(nameof(Details), new { id = studentId });
            }

            // 2. Run stored procedure
            await _context.Database.ExecuteSqlRawAsync(
                "EXEC dbo.usp_DetermineInterventions @StudentId = {0}",
                studentId
            );

            // 3. Check if anything was created
            bool createdActions = await _context.PendingActions
                .AnyAsync(p => p.StudentId == studentId && p.Status != "Completed");

            TempData["RuleMessage"] = createdActions
                ? "Coaching rules executed. New pending actions were created."
                : "Coaching rules executed. No actions were triggered.";

            return RedirectToAction(nameof(Details), new { id = studentId });
        }
    }
}
