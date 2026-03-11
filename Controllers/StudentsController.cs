using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using StudentTrackingCoach.Data;
using StudentTrackingCoach.Models.Ai;
using StudentTrackingCoach.Models;
using StudentTrackingCoach.Models.ViewModels;
using StudentTrackingCoach.Services.Interfaces;
using System.Text.Json;

namespace StudentTrackingCoach.Controllers
{
    [Authorize]
    public class StudentsController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _db;
        private readonly IStudentService _studentService;
        private readonly IRiskCalculationService _riskService;
        private readonly IAiRecommendationService _aiService;
        private readonly IConfiguration _configuration;
        private readonly ITelemetryService _telemetryService;
        private readonly ITenantService _tenantService;
        private readonly ILogger<StudentsController> _logger;

        public StudentsController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext db,
            IStudentService studentService,
            IRiskCalculationService riskService,
            IAiRecommendationService aiService,
            IConfiguration configuration,
            ITelemetryService telemetryService,
            ITenantService tenantService,
            ILogger<StudentsController> logger)
        {
            _userManager = userManager;
            _db = db;
            _studentService = studentService;
            _riskService = riskService;
            _aiService = aiService;
            _configuration = configuration;
            _telemetryService = telemetryService;
            _tenantService = tenantService;
            _logger = logger;
        }

        // ===============================
        // GET: /Students
        // ===============================
        public async Task<IActionResult> Index(bool highRiskOnly = false, int pageNumber = 1, int pageSize = 20)
        {
            _telemetryService.TrackEvent("StudentsIndexViewed", new Dictionary<string, string>
            {
                ["highRiskOnly"] = highRiskOnly.ToString(),
                ["pageNumber"] = pageNumber.ToString(),
                ["pageSize"] = pageSize.ToString()
            });
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

            var enableAiRecommendations = _configuration.GetValue<bool>("AiFeatures:Enabled");
            var aiResult = enableAiRecommendations
                ? await _aiService.GenerateRecommendationsAsync(id)
                : null;
            _telemetryService.TrackEvent("RecommendedStudyGuideGenerated", new Dictionary<string, string>
            {
                ["studentId"] = id.ToString(),
                ["usedAi"] = (aiResult != null).ToString()
            });

            var vm = new RecommendedStudyGuideViewModel
            {
                StudentId = id,
                StudentName = $"Student {id}",
                CourseName = "Current Term Courses",
                CurrentGrade = simulatedGrade.HasValue ? $"{simulatedGrade.Value:F1}%" : "N/A",
                FocusAreas = aiResult?.FocusAreas ?? GenerateFocusAreas(riskLevel, riskDrivers, recentNotes),
                StudySchedule = aiResult?.StudySchedule ?? GenerateStudySchedule(riskLevel),
                StudyTechniques = aiResult?.StudyTechniques ?? GenerateStudyTechniques(riskLevel, riskDrivers),
                Resources = aiResult?.Resources ?? GenerateResources(riskLevel),
                AdvisorNotes = recentNotes.Any()
                    ? string.Join("\n", recentNotes.Select(n => $"- {n.Notes}"))
                    : "No recent advisor notes",
                FollowUpDate = aiResult?.FollowUpDate ?? DateTime.Today.AddDays(14),
                ExpectedOutcome = aiResult?.ExpectedOutcome ?? (riskLevel == "High"
                    ? "Improve grade to passing (70%+)"
                    : riskLevel == "Medium"
                        ? "Improve grade to 75%+"
                        : "Maintain current performance"),
                GeneratedBy = aiResult != null ? "AI-Mock" : "Advisor",
                GeneratedAt = DateTime.UtcNow,
                ConfidenceScore = aiResult?.ConfidenceScore,
                IsAIGenerated = aiResult != null,
                AdvisorModified = false
            };

            ViewBag.AIGenerated = aiResult != null;
            ViewBag.ShowConfidenceScore = _configuration.GetValue<bool>("AiFeatures:ShowConfidenceScore");

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

            var originalAi = model.IsAIGenerated
                ? await _aiService.GenerateRecommendationsAsync(model.StudentId)
                : null;
            var advisorChanges = BuildAdvisorChanges(model, originalAi);
            model.AdvisorModified = advisorChanges.Any();
            model.GeneratedBy = model.IsAIGenerated
                ? (model.AdvisorModified ? "Advisor-Modified (AI-Mock)" : "AI-Mock")
                : "Advisor";
            model.GeneratedAt ??= DateTime.UtcNow;

            var payload = new StudyGuideInterventionContent
            {
                StudyGuide = model,
                OriginalAI = originalAi,
                AdvisorChanges = advisorChanges
            };

            try
            {
                var newIntervention = new Intervention
                {
                    StudentId = model.StudentId,
                    TenantId = _tenantService.CurrentTenantId,
                    AdvisorId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier) ?? "system",
                    Type = "Study Guide",
                    Content = System.Text.Json.JsonSerializer.Serialize(payload),
                    CreatedAt = DateTime.UtcNow,
                    Status = "Pending",
                    StudentName = model.StudentName,
                    IsAIGenerated = model.IsAIGenerated
                };

                _db.Interventions.Add(newIntervention);
                await _db.SaveChangesAsync();
                _telemetryService.TrackEvent("StudyGuideSaved", new Dictionary<string, string>
                {
                    ["studentId"] = model.StudentId.ToString(),
                    ["isAiGenerated"] = model.IsAIGenerated.ToString(),
                    ["advisorModified"] = model.AdvisorModified.ToString()
                });

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
            _telemetryService.TrackEvent("MyStudyGuidesViewed", new Dictionary<string, string>
            {
                ["isAdmin"] = User.IsInRole("Admin").ToString(),
                ["isStudent"] = User.IsInRole("Student").ToString()
            });
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
            var guideDebug = new List<StudyGuideDebugInfoViewModel>();
            foreach (var guide in approvedGuides)
            {
                var contentLength = guide.Content?.Length ?? 0;
                var content = guide.Content ?? string.Empty;
                var preview = content.Length > 200
                    ? content[..200]
                    : content;

                _logger.LogInformation(
                    "MyStudyGuides guide encountered. studentId={StudentId}, guideId={GuideId}, contentLength={ContentLength}",
                    studentId,
                    guide.Id,
                    contentLength);

                var vm = ExtractStudyGuideFromContent(
                    guide.Content,
                    guide.Id,
                    studentId,
                    out var formatUsed,
                    out var deserializationError);

                var success = vm != null;
                if (success && vm != null)
                {
                    viewModels.Add(vm);
                    _logger.LogInformation(
                        "MyStudyGuides deserialization succeeded. studentId={StudentId}, guideId={GuideId}, format={FormatUsed}",
                        studentId,
                        guide.Id,
                        formatUsed);
                }
                else
                {
                    _logger.LogWarning(
                        "MyStudyGuides deserialization failed. studentId={StudentId}, guideId={GuideId}, format={FormatUsed}, error={Error}",
                        studentId,
                        guide.Id,
                        formatUsed,
                        deserializationError ?? "Unknown error");
                }

                guideDebug.Add(new StudyGuideDebugInfoViewModel
                {
                    GuideId = guide.Id,
                    StudentId = guide.StudentId,
                    ContentLength = contentLength,
                    FormatUsed = formatUsed,
                    DeserializationSucceeded = success,
                    ErrorMessage = deserializationError,
                    RawContentPreview = preview
                });

                if (!success)
                {
                    // Always show a fallback card so users can still see the guide slot.
                    var fallback = BuildFallbackGuide(studentId, guide.Id);
                    if (fallback != null)
                    {
                        viewModels.Add(fallback);
                    }
                }
            }

            // Pass debug info to ViewBag
            ViewBag.TotalInterventions = interventions.Count;
            ViewBag.StudentId = studentId;
            ViewBag.AllStatuses = string.Join(", ", interventions.Select(i => i.Status));
            ViewBag.ApprovedCount = approvedGuides.Count;
            ViewBag.GuideDebug = guideDebug;

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

        private RecommendedStudyGuideViewModel? ExtractStudyGuideFromContent(
            string? content,
            int guideId,
            long studentId,
            out string formatUsed,
            out string? errorMessage)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                formatUsed = "empty";
                errorMessage = "Content is null or empty.";
                _logger.LogWarning(
                    "MyStudyGuides guide content empty. studentId={StudentId}, guideId={GuideId}",
                    studentId,
                    guideId);
                return BuildFallbackGuide(studentId, guideId);
            }

            try
            {
                var wrapped = JsonSerializer.Deserialize<StudyGuideInterventionContent>(content);
                if (wrapped?.StudyGuide != null)
                {
                    formatUsed = "wrapped";
                    errorMessage = null;
                    return EnsureGuideDefaults(wrapped.StudyGuide, studentId, guideId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(
                    ex,
                    "MyStudyGuides wrapped deserialization failed. studentId={StudentId}, guideId={GuideId}",
                    studentId,
                    guideId);
            }

            try
            {
                var legacy = JsonSerializer.Deserialize<RecommendedStudyGuideViewModel>(content);
                if (legacy != null)
                {
                    formatUsed = "legacy";
                    errorMessage = null;
                    return EnsureGuideDefaults(legacy, studentId, guideId);
                }
            }
            catch (Exception ex)
            {
                formatUsed = "legacy-failed";
                errorMessage = ex.Message;
                _logger.LogWarning(
                    ex,
                    "MyStudyGuides legacy deserialization failed. studentId={StudentId}, guideId={GuideId}",
                    studentId,
                    guideId);
                return BuildFallbackGuide(studentId, guideId);
            }

            formatUsed = "unknown-json-shape";
            errorMessage = "JSON payload does not match wrapped or legacy study-guide schema.";
            _logger.LogWarning(
                "MyStudyGuides unknown content shape. studentId={StudentId}, guideId={GuideId}",
                studentId,
                guideId);
            return BuildFallbackGuide(studentId, guideId);
        }

        private static RecommendedStudyGuideViewModel EnsureGuideDefaults(
            RecommendedStudyGuideViewModel vm,
            long studentId,
            int guideId)
        {
            vm.StudentId = vm.StudentId <= 0 ? studentId : vm.StudentId;
            vm.StudentName = string.IsNullOrWhiteSpace(vm.StudentName) ? $"Student {studentId}" : vm.StudentName;
            vm.CourseName = string.IsNullOrWhiteSpace(vm.CourseName) ? "General Coursework" : vm.CourseName;
            vm.CurrentGrade = string.IsNullOrWhiteSpace(vm.CurrentGrade) ? "N/A" : vm.CurrentGrade;
            vm.FocusAreas = string.IsNullOrWhiteSpace(vm.FocusAreas) ? "No focus areas provided." : vm.FocusAreas;
            vm.StudySchedule = string.IsNullOrWhiteSpace(vm.StudySchedule) ? "No schedule provided." : vm.StudySchedule;
            vm.StudyTechniques = string.IsNullOrWhiteSpace(vm.StudyTechniques) ? "No techniques provided." : vm.StudyTechniques;
            vm.Resources = string.IsNullOrWhiteSpace(vm.Resources) ? "No resources listed." : vm.Resources;
            vm.ExpectedOutcome = string.IsNullOrWhiteSpace(vm.ExpectedOutcome)
                ? "Expected outcome not available."
                : vm.ExpectedOutcome;
            vm.GeneratedAt ??= DateTime.UtcNow;
            vm.GeneratedBy = string.IsNullOrWhiteSpace(vm.GeneratedBy) ? $"Recovered Guide #{guideId}" : vm.GeneratedBy;
            return vm;
        }

        private static RecommendedStudyGuideViewModel BuildFallbackGuide(long studentId, int guideId)
        {
            return new RecommendedStudyGuideViewModel
            {
                StudentId = studentId,
                StudentName = $"Student {studentId}",
                CourseName = "General Coursework",
                CurrentGrade = "N/A",
                FocusAreas = "Study guide content is unavailable.",
                StudySchedule = "Unable to load schedule from saved content.",
                StudyTechniques = "Unable to load study techniques from saved content.",
                Resources = "No resources available.",
                ExpectedOutcome = "Please ask your advisor to regenerate this guide.",
                GeneratedBy = $"Fallback for Guide #{guideId}",
                GeneratedAt = DateTime.UtcNow,
                AdvisorModified = false,
                IsAIGenerated = false
            };
        }

        private static List<string> BuildAdvisorChanges(RecommendedStudyGuideViewModel model, AiRecommendationResult? originalAi)
        {
            var changes = new List<string>();
            if (originalAi == null)
            {
                return changes;
            }

            if (!string.Equals(model.FocusAreas?.Trim(), originalAi.FocusAreas.Trim(), StringComparison.Ordinal))
                changes.Add("FocusAreas");
            if (!string.Equals(model.StudySchedule?.Trim(), originalAi.StudySchedule.Trim(), StringComparison.Ordinal))
                changes.Add("StudySchedule");
            if (!string.Equals(model.StudyTechniques?.Trim(), originalAi.StudyTechniques.Trim(), StringComparison.Ordinal))
                changes.Add("StudyTechniques");
            if (!string.Equals((model.Resources ?? string.Empty).Trim(), originalAi.Resources.Trim(), StringComparison.Ordinal))
                changes.Add("Resources");
            if (model.FollowUpDate != originalAi.FollowUpDate)
                changes.Add("FollowUpDate");
            if (!string.Equals((model.ExpectedOutcome ?? string.Empty).Trim(), originalAi.ExpectedOutcome.Trim(), StringComparison.Ordinal))
                changes.Add("ExpectedOutcome");

            return changes;
        }

    }
}
