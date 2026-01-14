using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentTrackingCoach.Models;

namespace StudentTrackingCoach.Controllers
{
    [Authorize(Roles = "Advisor,Admin")]
    public class AdvisorController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdvisorController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            // ✅ ADMIN OVERRIDE
            if (User.IsInRole("Admin"))
            {
                var dashboard = await _context.AdvisorRiskDashboard
                    .OrderBy(d => d.StudentId)
                    .ToListAsync();

                return View(dashboard);
            }

            // 🔐 Advisor-only logic
            if (!user.AdvisorId.HasValue)
                return Forbid();

            int advisorId = user.AdvisorId.Value;

            var assignedStudentIds = await _context.AdvisorStudents
                .Where(a => a.AdvisorId == advisorId)
                .Select(a => a.StudentId)
                .ToListAsync();

            var advisorDashboard = await _context.AdvisorRiskDashboard
                .Where(d => assignedStudentIds.Contains(d.StudentId))
                .ToListAsync();

            return View(advisorDashboard);
        }
    }
}
