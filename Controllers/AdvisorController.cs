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
    public class AdvisorController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _db;

        public AdvisorController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext db)
        {
            _userManager = userManager;
            _db = db;
        }

        // ===============================
        // GET: /Advisor
        // ===============================
        public async Task<IActionResult> Index()
        {
            var students = await _db.Students
                .AsNoTracking()
                .Take(10)
                .Select(s => new AdvisorRiskDashboardDto
                {
                    StudentId = s.StudentId,
                    FirstName = "Student",
                    LastName = s.StudentId.ToString(),
                    RiskLevel = "High",
                    AverageScore = 62,
                    PrimaryRiskDriver = "Failing one or more assessments",
                    SecondaryRiskDriver = "Low engagement"
                })
                .ToListAsync();

            return View(students);
        }

        // ===============================
        // GET: /Advisor/Student/{id}
        // ===============================
        public async Task<IActionResult> Student(long id)
        {
            var notes = await _db.AdvisorNotes
                .AsNoTracking()
                .Where(n => n.StudentId == id)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            var vm = new AdvisorStudentDetailViewModel
            {
                StudentId = id,
                StudentName = $"Student {id}",
                EnrollmentStatus = "Enrolled",

                RiskLevel = "High",
                AverageScore = 62,
                PrimaryRiskDriver = "Failing one or more assessments",
                SecondaryRiskDriver = "Low engagement",

                // ✅ CORRECT + SAFE MAPPING
                Notes = notes.Select(n => new AdvisorNoteViewModel
                {
                    Title = n.ActionTaken,          // map action → title
                    Severity = "Medium",            // demo-safe default
                    Category = "Advising",
                    Notes = n.Notes,
                    CreatedBy = n.AdvisorUserId,
                    CreatedAt = n.CreatedAt
                }).ToList()


            };

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

            var note = new AdvisorNote
            {
                StudentId = input.StudentId,
                AdvisorUserId = user?.UserName ?? "Advisor",
                ActionTaken = input.ActionTaken,
                Notes = input.Notes,
                CreatedAt = DateTime.UtcNow
            };

            _db.AdvisorNotes.Add(note);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Student), new { id = input.StudentId });
        }
    }
}
