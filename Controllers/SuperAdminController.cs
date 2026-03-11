using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentTrackingCoach.Models;
using StudentTrackingCoach.Services.Interfaces;

namespace StudentTrackingCoach.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    public class SuperAdminController : Controller
    {
        private readonly ITenantService _tenantService;

        public SuperAdminController(ITenantService tenantService)
        {
            _tenantService = tenantService;
        }

        [HttpGet]
        public async Task<IActionResult> Tenants()
        {
            var tenants = await _tenantService.GetAllTenantsAsync();
            return View(tenants);
        }

        [HttpGet]
        public IActionResult CreateTenant()
        {
            return View(new Tenant());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTenant(Tenant model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (await _tenantService.GetTenantBySlugAsync(model.Slug) != null)
            {
                ModelState.AddModelError(nameof(model.Slug), "Slug is already used by another tenant.");
                return View(model);
            }

            await _tenantService.CreateTenantAsync(model);
            TempData["SuccessMessage"] = "Tenant created successfully.";
            return RedirectToAction(nameof(Tenants));
        }

        [HttpGet]
        public async Task<IActionResult> TenantDetails(int id)
        {
            var tenant = await _tenantService.GetTenantByIdAsync(id);
            if (tenant == null)
            {
                return NotFound();
            }

            ViewBag.Usage = await _tenantService.GetTenantUsageMetricsAsync(id);
            return View(tenant);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TenantDetails(Tenant model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Usage = await _tenantService.GetTenantUsageMetricsAsync(model.TenantId);
                return View(model);
            }

            await _tenantService.UpdateTenantAsync(model);
            TempData["SuccessMessage"] = "Tenant updated.";
            return RedirectToAction(nameof(TenantDetails), new { id = model.TenantId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SwitchTenant(int tenantId)
        {
            HttpContext.Session.Set("SelectedTenantId", Encoding.UTF8.GetBytes(tenantId.ToString()));
            TempData["SuccessMessage"] = "Tenant context switched.";
            return RedirectToAction(nameof(Tenants));
        }
    }
}
