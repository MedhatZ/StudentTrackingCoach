using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentTrackingCoach.Models.RUM;
using StudentTrackingCoach.Services.Interfaces;

namespace StudentTrackingCoach.Controllers
{
    [ApiController]
    [Route("rum")]
    [IgnoreAntiforgeryToken]
    [AllowAnonymous]
    public class RUMController : ControllerBase
    {
        private readonly IRUMService _rumService;

        public RUMController(IRUMService rumService)
        {
            _rumService = rumService;
        }

        [HttpPost("page-view")]
        public async Task<IActionResult> TrackPageView([FromBody] PageViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            await _rumService.TrackPageViewAsync(model, ResolveRole());
            return Ok(new { tracked = true });
        }

        [HttpPost("action")]
        public async Task<IActionResult> TrackAction([FromBody] UserActionModel model)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            await _rumService.TrackUserActionAsync(model, ResolveRole());
            return Ok(new { tracked = true });
        }

        private string ResolveRole()
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return "Anonymous";
            }

            if (User.IsInRole("SuperAdmin")) return "SuperAdmin";
            if (User.IsInRole("Admin")) return "Admin";
            if (User.IsInRole("Advisor")) return "Advisor";
            if (User.IsInRole("Student")) return "Student";
            return "Authenticated";
        }
    }
}
