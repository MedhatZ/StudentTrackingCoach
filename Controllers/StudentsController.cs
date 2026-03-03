using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using StudentTrackingCoach.Data;
using StudentTrackingCoach.Models;
using StudentTrackingCoach.Models.ViewModels;
using StudentTrackingCoach.Services.Interfaces;

namespace StudentTrackingCoach.Controllers
{
    [Authorize]
    public class StudentsController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _db;
        private readonly IStudentService _studentService;
        private readonly IRiskCalculationService _riskService;

        public StudentsController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext db,
            IStudentService studentService,
            IRiskCalculationService riskService)
        {
            _userManager = userManager;
            _db = db;
            _studentService = studentService;
            _riskService = riskService;
        }

        // ===============================
        // GET: /Students
        // ===============================
        public async Task<IActionResult> Index(bool highRiskOnly = false, int pageNumber = 1, int pageSize = 20)
        {
            pageSize = NormalizePageSize(pageSize);
            pageNumber = Math.Max(1, pageNumber);

            var students = await _studentService.GetAllStudentsAsync();
            var totalCount = students.Count;
            var pagedStudents = students
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var studentsWithRisk = new List<StudentListViewModel>();
            var riskLevels = new Dictionary<long, string>();
            foreach (var student in pagedStudents)
            {
                var riskLevel = await _riskService.CalculateStudentRiskLevelAsync(student.StudentId);
                var riskPriority = riskLevel switch
                {
                    "High" => 1,
                    "Medium" => 2,
                    _ => 3
                };

                if (highRiskOnly && !string.Equals(riskLevel, "High", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                studentsWithRisk.Add(new StudentListViewModel
                {
                    StudentId = student.StudentId,
                    InstitutionId = student.InstitutionId,
                    EnrollmentStatus = student.EnrollmentStatus,
                    IsFirstGen = student.IsFirstGen,
                    IsWorking = student.IsWorking,
                    PreferredModality = student.PreferredModality,
                    CreatedAt = student.CreatedAt,
                    RiskPriority = riskPriority,
                    HasOpenPendingActions =
                        student.IsFirstGen == true && student.IsWorking == false
                });

                riskLevels[student.StudentId] = riskLevel;
            }

            var pagination = new PaginationViewModel
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            ViewBag.HighRiskOnly = highRiskOnly;
            ViewBag.PageSize = pageSize;
            ViewBag.Pagination = pagination;
            ViewBag.RiskLevels = riskLevels;

            return View(studentsWithRisk);
        }

        // ===============================
        // GET: /Students/Details/{id}
        // ===============================
        public async Task<IActionResult> Details(long id)
        {
            var student = await _studentService.GetStudentByIdAsync(id);

            if (student == null)
                return NotFound();

            var riskLevel = await _riskService.CalculateStudentRiskLevelAsync(id);
            ViewBag.RiskLevel = riskLevel;

            return View(student);
        }

        // ===============================
        // GET: /Students/RecommendedStudyGuide/{id}
        // ===============================
        public async Task<IActionResult> RecommendedStudyGuide(long id)
        {
            var student = await _studentService.GetStudentByIdAsync(id);
            if (student == null)
                return NotFound();

            var riskLevel = await _riskService.CalculateStudentRiskLevelAsync(id);
            var riskDrivers = await _riskService.GetRiskDriversAsync(id);
            var simulatedGrade = await _riskService.GetSimulatedAverageGradeAsync(id);
            ViewBag.RiskLevel = riskLevel;
            ViewBag.RiskDrivers = riskDrivers;

            var recentNotes = await _db.AdvisorNotes
                .Where(n => n.StudentId == id)
                .OrderByDescending(n => n.CreatedAt)
                .Take(5)
                .ToListAsync();

            var vm = new RecommendedStudyGuideViewModel
            {
                StudentId = id,
                StudentName = $"Student {id}",
                CourseName = "Current Term Courses",
                CurrentGrade = simulatedGrade.HasValue ? $"{simulatedGrade.Value:F1}%" : "N/A",
                FocusAreas = GenerateFocusAreas(riskLevel, riskDrivers, recentNotes),
                StudySchedule = GenerateStudySchedule(riskLevel),
                StudyTechniques = GenerateStudyTechniques(riskLevel, riskDrivers),
                Resources = GenerateResources(riskLevel),
                AdvisorNotes = recentNotes.Any()
                    ? string.Join("\n", recentNotes.Select(n => $"- {n.Notes}"))
                    : "No recent advisor notes",
                FollowUpDate = DateTime.Today.AddDays(14),
                ExpectedOutcome = riskLevel == "High"
                    ? "Improve grade to passing (70%+)"
                    : riskLevel == "Medium"
                        ? "Improve grade to 75%+"
                        : "Maintain current performance"
            };

            return View(vm);
        }

        // ===============================
        // POST: /Students/SaveStudyGuide
        // ===============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveStudyGuide(RecommendedStudyGuideViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("RecommendedStudyGuide", model);
            }

            try
            {
                var newIntervention = new Intervention
                {
                    StudentId = model.StudentId,
                    AdvisorId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier) ?? "system",
                    Type = "Study Guide",
                    Content = System.Text.Json.JsonSerializer.Serialize(model),
                    CreatedAt = DateTime.UtcNow,
                    Status = "Pending",
                    StudentName = model.StudentName
                };

                _db.Interventions.Add(newIntervention);
                await _db.SaveChangesAsync();

                TempData["SuccessMessage"] = "Study guide saved successfully and is pending review.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error saving study guide: {ex.Message}";
                return View("RecommendedStudyGuide", model);
            }

            return RedirectToAction("Details", new { id = model.StudentId });
        }

        [HttpGet]
        [Authorize(Roles = "Student,Admin")]
        public async Task<IActionResult> MyStudyGuides()
        {
            // Get current logged-in user
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            // Admin users do not have personal study guides unless linked to a StudentId.
            if (User.IsInRole("Admin") && !user.StudentId.HasValue)
            {
                ViewBag.ErrorMessage = "Admins don't have personal study guides. Use Advisor dashboard to review student guides.";
                ViewBag.ApprovedCount = 0;
                return View(new List<RecommendedStudyGuideViewModel>());
            }

            // Check if user has StudentId
            if (!user.StudentId.HasValue)
            {
                // Log warning - student user without StudentId
                Console.WriteLine($"WARNING: User {user.Id} ({user.Email}) has Student role but no StudentId");

                // For demo/development, we can show a friendly message
                ViewBag.ErrorMessage = "Your account is not linked to a student record. Please contact an administrator.";
                return View(new List<RecommendedStudyGuideViewModel>());
            }

            long studentId = user.StudentId.Value;

            var interventions = await _db.Interventions
                .Where(i => i.StudentId == studentId)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            // Filter approved guides
            var approvedGuides = interventions
                .Where(i => string.Equals(i.Status?.Trim(), "Approved", StringComparison.OrdinalIgnoreCase))
                .ToList();

            // Deserialize content for display
            var viewModels = new List<RecommendedStudyGuideViewModel>();
            foreach (var guide in approvedGuides)
            {
                try
                {
                    var vm = System.Text.Json.JsonSerializer.Deserialize<RecommendedStudyGuideViewModel>(guide.Content);
                    if (vm != null)
                    {
                        viewModels.Add(vm);
                    }
                }
                catch (Exception ex)
                {
                    // Log error but continue processing other guides
                    Console.WriteLine($"Error deserializing guide {guide.Id}: {ex.Message}");
                }
            }

            // Pass debug info to ViewBag
            ViewBag.TotalInterventions = interventions.Count;
            ViewBag.StudentId = studentId;
            ViewBag.AllStatuses = string.Join(", ", interventions.Select(i => i.Status));
            ViewBag.ApprovedCount = approvedGuides.Count;

            return View(viewModels);
        }

        private string GenerateFocusAreas(string riskLevel, List<string> riskDrivers, List<AdvisorNote> recentNotes)
        {
            var areas = new List<string>();

            areas.AddRange(riskDrivers);

            foreach (var note in recentNotes.Where(n => !string.IsNullOrEmpty(n.Notes)))
            {
                if (note.Notes.Contains("math", StringComparison.OrdinalIgnoreCase) ||
                    note.Notes.Contains("algebra", StringComparison.OrdinalIgnoreCase))
                    areas.Add("Mathematics - focus on problem-solving");

                if (note.Notes.Contains("writing", StringComparison.OrdinalIgnoreCase) ||
                    note.Notes.Contains("essay", StringComparison.OrdinalIgnoreCase))
                    areas.Add("Writing skills - essay structure and grammar");

                if (note.Notes.Contains("attend", StringComparison.OrdinalIgnoreCase))
                    areas.Add("Attendance - prioritize class participation");
            }

            if (riskLevel == "High")
                areas.Add("Immediate intervention needed - schedule meeting with advisor");
            else if (riskLevel == "Medium")
                areas.Add("Weekly check-ins recommended");

            return areas.Any()
                ? string.Join("\n", areas.Distinct().Take(5))
                : "General academic improvement";
        }

        private string GenerateStudySchedule(string riskLevel)
        {
            return riskLevel switch
            {
                "High" => @"• Daily: 2 hours focused study
• Weekly: 3 tutoring sessions
• Weekend: Review all materials from the week
• Daily check-ins with study group",

                "Medium" => @"• Daily: 1.5 hours study
• Weekly: 1 tutoring session
• Weekend: Practice problems
• Bi-weekly advisor check-in",

                _ => @"• Daily: 1 hour study
• Weekly: Review notes
• Weekend: Preview next week's materials
• Monthly advisor check-in"
            };
        }

        private string GenerateStudyTechniques(string riskLevel, List<string> riskDrivers)
        {
            var techniques = new List<string>();

            if (riskLevel == "High")
            {
                techniques.Add("Active recall - practice tests");
                techniques.Add("Pomodoro technique - 25 min focused sessions");
                techniques.Add("Peer tutoring - explain concepts to others");
            }
            else if (riskLevel == "Medium")
            {
                techniques.Add("Cornell note-taking system");
                techniques.Add("Spaced repetition");
                techniques.Add("Concept mapping");
            }
            else
            {
                techniques.Add("Preview-Question-Read-Recite-Review (PQ3R)");
                techniques.Add("Self-testing");
                techniques.Add("Teaching others");
            }

            if (riskDrivers.Any(d => d.Contains("math", StringComparison.OrdinalIgnoreCase)))
                techniques.Add("Practice problems daily - start with easier ones");

            if (riskDrivers.Any(d => d.Contains("attend", StringComparison.OrdinalIgnoreCase)))
                techniques.Add("Set phone reminders for all classes");

            return string.Join("\n", techniques.Take(5));
        }

        private string GenerateResources(string riskLevel)
        {
            var resources = new List<string>();

            resources.Add("• Tutoring Center - Schedule online or in-person");
            resources.Add("• Writing Center - For essay help");

            if (riskLevel == "High")
            {
                resources.Add("• Academic Coach - Request weekly meetings");
                resources.Add("• Student Success Workshop Series");
                resources.Add("• Peer Study Groups - Join a group");
            }
            else if (riskLevel == "Medium")
            {
                resources.Add("• Online tutoring - Khan Academy");
                resources.Add("• Library study rooms");
            }

            return string.Join("\n", resources);
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
