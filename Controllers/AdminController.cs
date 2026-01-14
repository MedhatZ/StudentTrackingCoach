using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace StudentTrackingCoach.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return RedirectToAction(nameof(Metrics));
        }

        public IActionResult Metrics()
        {
            return View();
        }
    }
}
