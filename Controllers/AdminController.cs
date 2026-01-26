using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentTrackingCoach.Data;
using StudentTrackingCoach.Models;
using StudentTrackingCoach.Models.ViewModels;

namespace StudentTrackingCoach.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _db;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext db)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _db = db;
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
                PerformedByUserId = _userManager.GetUserId(User) ?? "SYSTEM"
            };

            _db.AdminAuditLogs.Add(log);
            await _db.SaveChangesAsync();
        }
    }
}