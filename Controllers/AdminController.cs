using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using StudentTrackingCoach.Data;
using StudentTrackingCoach.Models;
using StudentTrackingCoach.Models.ViewModels;
using StudentTrackingCoach.Services.Implementations;
using StudentTrackingCoach.Services.Interfaces;

namespace StudentTrackingCoach.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _db;
        private readonly IAiUsageTrackingService _aiUsageTracking;
        private readonly IConfigurationValidationService _configurationValidationService;
        private readonly IMemoryCache _memoryCache;
        private readonly IAiRecommendationService _aiRecommendationService;
        private readonly IConfiguration _configuration;
        private readonly IRiskCalculationService _riskCalculationService;
        private readonly ITelemetryService _telemetryService;
        private readonly ICacheService _cacheService;
        private readonly ITenantService _tenantService;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext db,
            IAiUsageTrackingService aiUsageTracking,
            IConfigurationValidationService configurationValidationService,
            IMemoryCache memoryCache,
            IAiRecommendationService aiRecommendationService,
            IConfiguration configuration,
            IRiskCalculationService riskCalculationService,
            ITelemetryService telemetryService,
            ICacheService cacheService,
            ITenantService tenantService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _db = db;
            _aiUsageTracking = aiUsageTracking;
            _configurationValidationService = configurationValidationService;
            _memoryCache = memoryCache;
            _aiRecommendationService = aiRecommendationService;
            _configuration = configuration;
            _riskCalculationService = riskCalculationService;
            _telemetryService = telemetryService;
            _cacheService = cacheService;
            _tenantService = tenantService;
        }

        // =====================================================
        // GET: /Admin
        // =====================================================
        [HttpGet]
        public IActionResult Index()
        {
            return RedirectToAction(nameof(Users));
        }

        // =====================================================
        // GET: /Admin/Users
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Users(string? search = null)
        {
            var query = _userManager.Users.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                query = query.Where(u =>
                    (u.Email != null && u.Email.Contains(s)) ||
                    (u.UserName != null && u.UserName.Contains(s)));
            }

            var users = await query
                .OrderBy(u => u.Email)
                .Take(500)
                .ToListAsync();

            var vm = new AdminUserListViewModel { Search = search };

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

                vm.Users.Add(new AdminUserListItemViewModel
                {
                    Id = user.Id,
                    Email = user.Email ?? "",
                    UserName = user.UserName ?? "",
                    EmailConfirmed = user.EmailConfirmed,
                    LockoutEnabled = user.LockoutEnabled,
                    LockoutEnd = user.LockoutEnd,
                    IsDisabled = user.LockoutEnd.HasValue &&
                                 user.LockoutEnd.Value > DateTimeOffset.UtcNow,
                    StudentId = user.StudentId,
                    AdvisorId = user.AdvisorId,
                    Roles = roles.ToList()
                });
            }

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> HealthCheck()
        {
            _telemetryService.TrackEvent("AdminHealthCheckViewed");
            var vm = new AdminHealthCheckViewModel
            {
                AzureOpenAiConfigured = _configurationValidationService.IsAzureOpenAiConfigured(),
                AzureOpenAiStatus = _configurationValidationService.GetConfigurationStatus(),
                MockFallbackActive = !_configuration.GetValue<bool>("AiFeatures:Enabled") ||
                                     !_configuration.GetValue<bool>("AiFeatures:UseRealAi") ||
                                     !_configurationValidationService.IsAzureOpenAiConfigured(),
                AiServiceType = _aiRecommendationService.GetType().Name,
                LastAiTestResult = TempData["AiTestResult"] as string,
                RedisEnabled = _configuration.GetValue<bool>("Redis:Enabled"),
                RedisConfigured = !string.IsNullOrWhiteSpace(_configuration["Redis:ConnectionString"]),
                ApplicationInsightsEnabled = _configuration.GetValue<bool>("ApplicationInsights:Enabled"),
                ApplicationInsightsConfigured = !string.IsNullOrWhiteSpace(_configuration["ApplicationInsights:ConnectionString"])
            };

            vm.DatabaseConnected = await _db.Database.CanConnectAsync();

            var cacheProbeKey = "admin-health-cache-probe";
            _memoryCache.Set(cacheProbeKey, DateTime.UtcNow, TimeSpan.FromMinutes(1));
            vm.CacheAvailable = _memoryCache.TryGetValue(cacheProbeKey, out _);
            vm.CacheStatus = vm.CacheAvailable ? "In-memory cache operational" : "Cache unavailable";
            vm.RedisStatus = vm.RedisEnabled
                ? (vm.RedisConfigured ? "Redis enabled and configured" : "Redis enabled but connection string missing")
                : "Redis disabled (memory cache fallback active)";
            vm.ApplicationInsightsStatus = vm.ApplicationInsightsEnabled
                ? (vm.ApplicationInsightsConfigured ? "Application Insights configured" : "Application Insights enabled but connection string missing")
                : "Application Insights disabled (null telemetry active)";

            try
            {
                await _cacheService.SetAsync("healthcheck:cache", "ok", TimeSpan.FromMinutes(1));
                vm.CacheAvailable = vm.CacheAvailable && await _cacheService.ExistsAsync("healthcheck:cache");
            }
            catch
            {
                vm.CacheAvailable = false;
                vm.CacheStatus = "Configured cache service not available";
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TestAiConnection()
        {
            try
            {
                var testStudentId = await _db.Students
                    .AsNoTracking()
                    .Select(s => s.StudentId)
                    .FirstOrDefaultAsync();
                if (testStudentId <= 0)
                {
                    testStudentId = 100001;
                }

                var result = await _aiRecommendationService.GenerateRecommendationsAsync(testStudentId);
                TempData["AiTestResult"] = $"AI test succeeded via {_aiRecommendationService.GetType().Name}. Outcome: {result.ExpectedOutcome}";
                _telemetryService.TrackEvent("AdminAiConnectionTestSucceeded");
            }
            catch (Exception ex)
            {
                TempData["AiTestResult"] = $"AI test failed: {ex.Message}";
                _telemetryService.TrackException(ex, new Dictionary<string, string>
                {
                    ["component"] = "AdminController.TestAiConnection"
                });
            }

            return RedirectToAction(nameof(HealthCheck));
        }

        [HttpGet]
        public async Task<IActionResult> DataQuality()
        {
            _telemetryService.TrackEvent("AdminDataQualityViewed");
            var vm = new AdminDataQualityViewModel();

            try
            {
                // Get all students
                var students = await _db.Students.ToListAsync();
                vm.TotalStudents = students.Count;

                // Count students with notes
                var studentsWithNotes = await _db.AdvisorNotes
                    .Select(n => n.StudentId)
                    .Distinct()
                    .CountAsync();
                vm.StudentsWithNotes = studentsWithNotes;

                foreach (var student in students.Take(20)) // Limit for performance
                {
                    var riskLevel = await _riskCalculationService.CalculateStudentRiskLevelAsync(student.StudentId);

                    switch (riskLevel)
                    {
                        case "High": vm.HighRiskStudents++; break;
                        case "Medium": vm.MediumRiskStudents++; break;
                        default: vm.LowRiskStudents++; break;
                    }

                    // Check for data issues
                    if (student.IsFirstGen == null)
                    {
                        vm.StudentsWithIssues.Add(new StudentDataIssue
                        {
                            StudentId = student.StudentId,
                            Issue = "Missing FirstGen data"
                        });
                    }

                    if (student.IsWorking == null)
                    {
                        vm.StudentsWithIssues.Add(new StudentDataIssue
                        {
                            StudentId = student.StudentId,
                            Issue = "Missing Employment data"
                        });
                    }

                    if (string.IsNullOrEmpty(student.EnrollmentStatus))
                    {
                        vm.StudentsWithIssues.Add(new StudentDataIssue
                        {
                            StudentId = student.StudentId,
                            Issue = "Missing Enrollment Status"
                        });
                    }
                }

                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                vm.AiUsageMetrics = _aiUsageTracking.GetMetricsForDay(today);
                vm.AiCallsToday = vm.AiUsageMetrics.Sum(x => x.TotalCalls);
                vm.AiFallbacksToday = vm.AiUsageMetrics.Sum(x => x.FallbackCalls);
                _telemetryService.TrackMetric("Admin.AiCallsToday", vm.AiCallsToday);
                _telemetryService.TrackMetric("Admin.AiFallbacksToday", vm.AiFallbacksToday);

                return View(vm);
            }
            catch (SqlException ex) when (ex.Message.Contains("Invalid column name 'TenantId'", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] =
                    "Database schema is not up to date. Please apply multi-tenant migration using: dotnet ef database update --context ApplicationDbContext";
                _telemetryService.TrackException(ex, new Dictionary<string, string>
                {
                    ["component"] = "AdminController.DataQuality",
                    ["errorType"] = "MissingTenantColumn"
                });
                return View(vm);
            }
        }

        // =====================================================
        // GET: /Admin/EditRoles/{id}
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> EditRoles(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest("User id is required.");

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            var userRoles = await _userManager.GetRolesAsync(user);

            var vm = new AdminEditRolesViewModel
            {
                UserId = user.Id,
                Email = user.Email ?? ""
            };

            var allRoles = await _roleManager.Roles
                .OrderBy(r => r.Name)
                .ToListAsync();

            foreach (var role in allRoles)
            {
                vm.Roles.Add(new RoleCheckboxItem
                {
                    RoleName = role.Name ?? "",
                    IsAssigned = role.Name != null && userRoles.Contains(role.Name)
                });
            }

            return View(vm);
        }

        // =====================================================
        // POST: /Admin/EditRoles
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRoles(AdminEditRolesViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
                return NotFound();

            var isSelf = user.Id == _userManager.GetUserId(User);
            if (isSelf && model.Roles.All(r => r.RoleName != "Admin" || !r.IsAssigned))
            {
                ModelState.AddModelError("", "You cannot remove your own Admin role.");
                return View(model);
            }

            var currentRoles = await _userManager.GetRolesAsync(user);

            foreach (var role in model.Roles)
            {
                if (role.IsAssigned && !currentRoles.Contains(role.RoleName))
                    await _userManager.AddToRoleAsync(user, role.RoleName);

                if (!role.IsAssigned && currentRoles.Contains(role.RoleName))
                    await _userManager.RemoveFromRoleAsync(user, role.RoleName);
            }

            await LogAdminAction("Roles Updated", user.Id);
            return RedirectToAction(nameof(Users));
        }

        // =====================================================
        // GET: /Admin/EditProfile/{id}
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> EditProfile(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest("User id is required.");

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            var roles = await _userManager.GetRolesAsync(user);

            var vm = new AdminEditProfileViewModel
            {
                UserId = user.Id,
                Email = user.Email ?? "",
                StudentId = user.StudentId,
                AdvisorId = user.AdvisorId,
                Roles = roles,
                CanEditStudent = roles.Contains("Student"),
                CanEditAdvisor = roles.Contains("Advisor")
            };

            return View(vm);
        }

        // =====================================================
        // POST: /Admin/EditProfile
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(AdminEditProfileViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
                return NotFound();

            var roles = await _userManager.GetRolesAsync(user);

            user.StudentId = roles.Contains("Student") ? model.StudentId : null;
            user.AdvisorId = roles.Contains("Advisor") ? model.AdvisorId : null;


            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);

                model.Roles = roles;
                model.CanEditStudent = roles.Contains("Student");
                model.CanEditAdvisor = roles.Contains("Advisor");

                return View(model);
            }

            await LogAdminAction("Profile Updated", user.Id);
            TempData["SuccessMessage"] = "User profile updated successfully.";

            return RedirectToAction(nameof(Users));
        }

        // =====================================================
        // POST: /Admin/ToggleUser
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUser(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest("User id is required.");

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            await _userManager.SetLockoutEnabledAsync(user, true);

            if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow)
            {
                await _userManager.SetLockoutEndDateAsync(user, null);
                await LogAdminAction("User Enabled", user.Id);
            }
            else
            {
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
                await LogAdminAction("User Disabled", user.Id);
            }

            return RedirectToAction(nameof(Users));
        }

        // =====================================================
        // AUDIT LOG HELPER (Step 6-3)
        // =====================================================
        private async Task LogAdminAction(string action, string targetUserId)
        {
            var log = new AdminAuditLog
            {
                Action = action,
                TargetUserId = targetUserId,
                PerformedByUserId = _userManager.GetUserId(User) ?? "SYSTEM",
                TenantId = _tenantService.CurrentTenantId
            };

            _db.AdminAuditLogs.Add(log);
            await _db.SaveChangesAsync();
        }
    }
}