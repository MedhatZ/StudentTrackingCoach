using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentTrackingCoach.Data;
using StudentTrackingCoach.Models;
using StudentTrackingCoach.Models.ViewModels;
using StudentTrackingCoach.Services.Interfaces;

namespace StudentTrackingCoach.Controllers
{
    [Authorize]
    public class AdvisorController : Controller
    {
        private readonly IAdvisorService _advisorService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _configuration;

        public AdvisorController(
            UserManager<ApplicationUser> userManager,
            IAdvisorService advisorService,
            ApplicationDbContext db,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _advisorService = advisorService;
            _db = db;
            _configuration = configuration;
        }

        // ===============================
        // GET: /Advisor
        // ===============================
        public async Task<IActionResult> Index(string? search, int pageNumber = 1, int pageSize = 20)
        {
            pageSize = NormalizePageSize(pageSize);
            pageNumber = Math.Max(1, pageNumber);

            var currentUser = await _userManager.GetUserAsync(User);
            var students = await _advisorService.GetStudentsRequiringAttentionAsync(currentUser?.Id ?? "");

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                students = students
                    .Where(s =>
                        s.StudentId.ToString().Contains(term, StringComparison.OrdinalIgnoreCase) ||
                        $"{s.FirstName} {s.LastName}".Trim().Contains(term, StringComparison.OrdinalIgnoreCase) ||
                        $"Student {s.StudentId}".Contains(term, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            var totalCount = students.Count;
            var pagedStudents = students
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var pagination = new PaginationViewModel
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            ViewBag.Search = search;
            ViewBag.PageSize = pageSize;
            ViewBag.Pagination = pagination;
            return View(pagedStudents);
        }

        // ===============================
        // GET: /Advisor/Student/{id}
        // ===============================
        public async Task<IActionResult> Student(long id)
        {
            var vm = await _advisorService.GetStudentDetailForAdvisorAsync(id);
            if (vm == null)
                return NotFound();

            var coursesVm = await _advisorService.GetStudentCoursesAsync(id);
            ViewBag.StudentCourses = coursesVm;
            ViewBag.PassingThreshold = _configuration.GetValue<decimal?>("RiskThresholds:PassingGrade") ?? 70m;
            
            return View(vm);
        }

        // ===============================
        // POST: /Advisor/LogNote
        // ===============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogNote(AdvisorNoteInputModel input)
        {
            if (!ModelState.IsValid)
                return RedirectToAction(nameof(Student), new { id = input.StudentId });

            var user = await _userManager.GetUserAsync(User);
            var success = await _advisorService.LogNoteAsync(input, user?.UserName ?? "Advisor");

            if (!success)
            {
                TempData["ErrorMessage"] = "Failed to save note.";
            }

            return RedirectToAction(nameof(Student), new { id = input.StudentId });
        }

        [HttpGet]
        public async Task<IActionResult> SeedTestData()
        {
            var notes = new List<AdvisorNote>
            {
                new AdvisorNote
                {
                    StudentId = 100001,
                    AdvisorUserId = "system",
                    ActionTaken = "Academic Alert",
                    Notes = "Student is failing multiple classes. Attendance at 60%.",
                    CreatedAt = DateTime.UtcNow
                },
                new AdvisorNote
                {
                    StudentId = 100001,
                    AdvisorUserId = "system",
                    ActionTaken = "Follow-up",
                    Notes = "Missed tutoring appointment. Grades dropping.",
                    CreatedAt = DateTime.UtcNow.AddDays(-7)
                },
                new AdvisorNote
                {
                    StudentId = 100001,
                    AdvisorUserId = "system",
                    ActionTaken = "Initial Concern",
                    Notes = "First generation student struggling with coursework.",
                    CreatedAt = DateTime.UtcNow.AddDays(-14)
                },
                new AdvisorNote
                {
                    StudentId = 100003,
                    AdvisorUserId = "system",
                    ActionTaken = "Attendance Issue",
                    Notes = "Missed 5 classes. Not responding to emails.",
                    CreatedAt = DateTime.UtcNow
                },
                new AdvisorNote
                {
                    StudentId = 100003,
                    AdvisorUserId = "system",
                    ActionTaken = "Tutoring Referral",
                    Notes = "Referred to tutoring but hasn't shown up.",
                    CreatedAt = DateTime.UtcNow.AddDays(-5)
                }
            };

            _db.AdvisorNotes.AddRange(notes);
            await _db.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> PendingReviews()
        {
            var pending = await _db.Interventions
                .Where(i => i.Status == "Pending")
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            // Get student names
            foreach (var item in pending)
            {
                var student = await _db.Students.FirstOrDefaultAsync(s => s.StudentId == item.StudentId);
                if (student != null)
                {
                    item.StudentName = $"Student {student.StudentId}";
                }
            }

            return View(pending);
        }

        [HttpGet]
        public async Task<IActionResult> ReviewStudyGuide(int id)
        {
            var intervention = await _db.Interventions.FirstOrDefaultAsync(i => i.Id == id);
            if (intervention == null)
            {
                return NotFound();
            }

            // Deserialize the content back to ViewModel
            var studyGuide = System.Text.Json.JsonSerializer.Deserialize<RecommendedStudyGuideViewModel>(intervention.Content);

            // Pass both to the view
            ViewBag.Intervention = intervention;
            return View(studyGuide);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReviewStudyGuide(int id, string action, RecommendedStudyGuideViewModel model)
        {
            var intervention = await _db.Interventions.FirstOrDefaultAsync(i => i.Id == id);
            if (intervention == null)
            {
                return NotFound();
            }

            // Update based on action
            switch (action)
            {
                case "approve":
                    intervention.Status = "Approved";
                    intervention.ApprovedAt = DateTime.UtcNow;
                    TempData["SuccessMessage"] = "Study guide approved and sent to student.";
                    break;

                case "reject":
                    intervention.Status = "Rejected";
                    TempData["SuccessMessage"] = "Study guide rejected.";
                    break;

                case "modify":
                    // Update the content with modified model
                    intervention.Content = System.Text.Json.JsonSerializer.Serialize(model);
                    intervention.Status = "Modified";
                    TempData["SuccessMessage"] = "Study guide updated and saved.";
                    break;
            }

            await _db.SaveChangesAsync();

            return RedirectToAction("PendingReviews");
        }

        [HttpGet]
        public async Task<IActionResult> DebugSession()
        {
            var interventions = await _db.Interventions
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            string result = "=== DATABASE DATA (Interventions) ===\n";
            foreach (var i in interventions)
            {
                result += $"ID: {i.Id}, Student: {i.StudentId}, Status: {i.Status}, Created: {i.CreatedAt}\n";
            }

            return Content(result, "text/plain");
        }

        private static int NormalizePageSize(int pageSize)
        {
            return pageSize switch
            {
                10 => 10,
                20 => 20,
                50 => 50,
                100 => 100,
                _ => 20
            };
        }
    }
}
