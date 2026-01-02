using Microsoft.AspNetCore.Mvc;

namespace StudentTrackingCoach.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Metrics()
        {
            return View();
        }
    }
}
