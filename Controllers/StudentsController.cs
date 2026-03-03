using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentTrackingCoach.Data;
using StudentTrackingCoach.Models;
using StudentTrackingCoach.Models.ViewModels;

namespace StudentTrackingCoach.Controllers
{
    [Authorize]
    public class StudentsController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _db;

        public StudentsController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext db)
        {
            _userManager = userManager;
            _db = db;
        }

        // ===============================
        // GET: /Students
        // ===============================
        public async Task<IActionResult> Index(bool highRiskOnly = false)
        {
            var query = _db.Students
                .AsNoTracking()
                .Select(s => new StudentListViewModel
                {
                    StudentId = s.StudentId,
                    InstitutionId = s.InstitutionId,
                    EnrollmentStatus = s.EnrollmentStatus,
                    IsFirstGen = s.IsFirstGen,
                    IsWorking = s.IsWorking,
                    PreferredModality = s.PreferredModality,
                    CreatedAt = s.CreatedAt,

                    // Demo Risk Logic
                    RiskPriority =
                        s.IsFirstGen == true && s.IsWorking == false ? 1 :
                        s.IsFirstGen == true || s.IsWorking == false ? 2 :
                        3,

                    HasOpenPendingActions =
                        s.IsFirstGen == true && s.IsWorking == false
                });

            if (highRiskOnly)
            {
                query = query.Where(s => s.RiskPriority == 1);
            }

            var students = await query.ToListAsync();

            ViewBag.HighRiskOnly = highRiskOnly;

            return View(students);
        }

        // ===============================
        // GET: /Students/Details/{id}
        // ===============================
        public async Task<IActionResult> Details(long id)
        {
            var student = await _db.Students
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.StudentId == id);

            if (student == null)
                return NotFound();

            return View(student);
        }

        // ===============================
        // GET: /Students/RecommendedStudyGuide/{id}
        // ===============================
        public IActionResult RecommendedStudyGuide(long id)
        {
            var vm = new RecommendedStudyGuideViewModel
            {
                StudentId = id,
                StudentName = $"Student {id}",
                CourseName = "Mathematics 101",
                CurrentGrade = "D"
            };

            return View(vm);
        }

        // ===============================
        // POST: /Students/SaveStudyGuide
        // ===============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveStudyGuide(RecommendedStudyGuideViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("RecommendedStudyGuide", model);
            }

            // 🚧 Phase 1: Presentation only
            // Later we persist to DB

            TempData["SuccessMessage"] = "Study guide saved successfully.";

            return RedirectToAction("Details", new { id = model.StudentId });
        }

        // ===============================
        // GET: /Students/MyGradPath
        // ===============================
        public async Task<IActionResult> MyGradPath()
        {
            var student = await _db.Students
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (student == null)
            {
                return View(new StudentMyGradPathViewModel
                {
                    CurrentFocusMessage = "No student records found."
                });
            }

            var vm = new StudentMyGradPathViewModel
            {
                StudentId = student.StudentId,
                StudentName = $"Student #{student.StudentId}",
                OverallStatus = "At Risk",
                CurrentFocusMessage = "Attendance and Math performance require attention.",

                AdvisorName = "Advisor A",
                AdvisorLastAction = "Academic check-in scheduled",
                AdvisorLastContactDate = DateTime.Today.AddDays(-2),

                ActiveItems =
                {
                    new StudentMyGradPathItemViewModel
                    {
                        AlertTitle = "Low Attendance",
                        ActionNotes = "Missed 3 classes in the last 2 weeks",
                        DueDate = DateTime.Today.AddDays(5)
                    },
                    new StudentMyGradPathItemViewModel
                    {
                        AlertTitle = "Math Exam Below Passing",
                        ActionNotes = "Score: 62%. Tutoring recommended.",
                        DueDate = DateTime.Today.AddDays(7)
                    }
                },

                CompletedItems =
                {
                    new StudentMyGradPathItemViewModel
                    {
                        AlertTitle = "Advisor Intake Completed",
                        ActionNotes = "Initial onboarding meeting completed",
                        ResolvedAt = DateTime.Today.AddDays(-10)
                    }
                }
            };

            return View(vm);
        }
    }
}
