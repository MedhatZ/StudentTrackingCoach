using Microsoft.AspNetCore.Mvc;

namespace StudentTrackingCoach.Controllers
{
    public class TasksController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
