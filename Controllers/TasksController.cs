using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using StudentTrackingCoach.Models;
using StudentTrackingCoach.Models.ViewModels;
using StudentTrackingCoach.Services.Interfaces;

namespace StudentTrackingCoach.Controllers
{
    [Authorize]
    public class TasksController : Controller
    {
        private readonly ITaskService _taskService;
        private readonly UserManager<ApplicationUser> _userManager;

        public TasksController(ITaskService taskService, UserManager<ApplicationUser> userManager)
        {
            _taskService = taskService;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            int pendingPageNumber = 1,
            int completedPageNumber = 1,
            int pageSize = 20,
            string activeTab = "pending")
        {
            pageSize = NormalizePageSize(pageSize);
            pendingPageNumber = Math.Max(1, pendingPageNumber);
            completedPageNumber = Math.Max(1, completedPageNumber);
            activeTab = string.Equals(activeTab, "completed", StringComparison.OrdinalIgnoreCase)
                ? "completed"
                : "pending";

            var user = await _userManager.GetUserAsync(User);
            var advisorId = user?.Id ?? string.Empty;

            var allPending = await _taskService.GetPendingTasksAsync(advisorId);
            var allCompleted = await _taskService.GetCompletedTasksAsync(advisorId);

            var vm = new TasksIndexViewModel
            {
                PendingTasks = allPending
                    .Skip((pendingPageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList(),
                CompletedTasks = allCompleted
                    .Skip((completedPageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList()
            };

            ViewBag.ActiveTab = activeTab;
            ViewBag.PageSize = pageSize;
            ViewBag.PendingPagination = new PaginationViewModel
            {
                PageNumber = pendingPageNumber,
                PageSize = pageSize,
                TotalCount = allPending.Count
            };
            ViewBag.CompletedPagination = new PaginationViewModel
            {
                PageNumber = completedPageNumber,
                PageSize = pageSize,
                TotalCount = allCompleted.Count
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteTask(
            int taskId,
            int pendingPageNumber = 1,
            int completedPageNumber = 1,
            int pageSize = 20,
            string activeTab = "pending")
        {
            var success = await _taskService.CompleteTaskAsync(taskId);
            if (!success)
            {
                TempData["ErrorMessage"] = "Unable to complete the selected task.";
            }
            else
            {
                TempData["SuccessMessage"] = "Task marked as completed.";
            }

            return RedirectToAction(nameof(Index), new
            {
                pendingPageNumber,
                completedPageNumber,
                pageSize,
                activeTab
            });
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
