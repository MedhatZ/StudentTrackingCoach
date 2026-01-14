using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentTrackingCoach.Models;
using StudentTrackingCoach.Models.ViewModels;

namespace StudentTrackingCoach.Controllers
{
    public class StudentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public StudentsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // =====================================================
        // ADVISOR / ADMIN — STUDENT LIST
        // =====================================================
        [Authorize(Roles = "Advisor,Admin")]
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

                    HasOpenPendingActions = _context.PendingActions
                        .Any(p => p.StudentId == s.StudentId && p.Status != "Completed"),

                    RiskPriority =
                        _context.PendingActions.Any(p => p.StudentId == s.StudentId && p.Status != "Completed")
                            ? 1
                            : (!s.IsFirstGen.HasValue || !s.IsWorking.HasValue ? 2 : 3)
                });

            if (highRiskOnly)
            {
                studentsQuery = studentsQuery.Where(s => s.HasOpenPendingActions);
            }

            var students = await studentsQuery
                .OrderBy(s => s.RiskPriority)
                .ThenBy(s => s.StudentId)
                .ToListAsync();

            ViewBag.HighRiskOnly = highRiskOnly;
            return View(students);
        }

        // =====================================================
        // ADVISOR / ADMIN — STUDENT DETAILS
        // =====================================================
        [Authorize(Roles = "Advisor,Admin")]
        public async Task<IActionResult> Details(long id)
        {
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentId == id);

            if (student == null)
                return NotFound();

            return View(student);
        }

        // =====================================================
        // STUDENT / ADMIN — MY GRAD PATH
        // =====================================================
        [Authorize(Roles = "Student,Admin")]
        public async Task<IActionResult> MyGradPath(long? studentId = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            long resolvedStudentId;

            // =====================================================
            // ADMIN FALLBACK (DEV / DEMO SAFE)
            // =====================================================
            if (User.IsInRole("Admin"))
            {
                if (studentId.HasValue)
                {
                    resolvedStudentId = studentId.Value;
                }
                else
                {
                    // 🔥 Default Admin to first available student
                    resolvedStudentId = await _context.Students
                        .OrderBy(s => s.StudentId)
                        .Select(s => s.StudentId)
                        .FirstAsync();
                }
            }
            else
            {
                // =================================================
                // STUDENT — STRICT MODE
                // =================================================
                if (!user.StudentId.HasValue)
                    return Forbid();

                resolvedStudentId = user.StudentId.Value;
            }

            var model = new StudentMyGradPathViewModel
            {
                StudentId = resolvedStudentId,
                StudentName = $"Student {resolvedStudentId}"
            };

            var gradPathRows = await _context
                .Set<SuccessStudentMyGradPath>()
                .FromSqlRaw(
                    "SELECT * FROM success.vw_StudentMyGradPath WHERE StudentID = @p0",
                    resolvedStudentId)
                .ToListAsync();

            model.ActiveItems = gradPathRows
                .Where(r => r.StudentStatus != "Completed")
                .Select(r => new GradPathItem
                {
                    AlertTitle = r.AlertTitle,
                    ActionNotes = r.ActionNotes,
                    DueDate = r.DueDate
                })
                .ToList();

            model.CompletedItems = gradPathRows
                .Where(r => r.StudentStatus == "Completed")
                .OrderByDescending(r => r.ResolvedAt)
                .Select(r => new GradPathItem
                {
                    AlertTitle = r.AlertTitle,
                    ActionNotes = r.ActionNotes,
                    ResolvedAt = r.ResolvedAt
                })
                .ToList();

            model.OverallStatus = model.ActiveItems.Any()
                ? "Needs Attention"
                : "On Track";

            model.CurrentFocusMessage = model.ActiveItems.Any()
                ? "You have an item that needs attention. Review the next steps below and stay on track."
                : "You are currently on track. Keep going!";

            model.AdvisorName = "Assigned Academic Advisor";

            if (model.CompletedItems.Any())
            {
                var lastCompleted = model.CompletedItems.First();
                model.AdvisorLastAction = lastCompleted.ActionNotes;
                model.AdvisorLastContactDate = lastCompleted.ResolvedAt;
            }
            else
            {
                model.AdvisorLastAction = "Your advisor is monitoring your progress.";
            }

            return View(model);
        }
    }
}
