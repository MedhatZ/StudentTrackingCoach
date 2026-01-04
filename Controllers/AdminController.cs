using Microsoft.AspNetCore.Mvc;

namespace StudentTrackingCoach.Controllers
{
    public class AdminController : Controller
    {
        // This handles /Admin
        public IActionResult Index()
        {
            // Redirect /Admin → /Admin/Metrics
            return RedirectToAction(nameof(Metrics));
        }

        // This handles /Admin/Metrics
        public IActionResult Metrics()
        {
            return View();
        }
    }
}
