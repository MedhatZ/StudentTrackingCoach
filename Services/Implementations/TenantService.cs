using Microsoft.EntityFrameworkCore;
using StudentTrackingCoach.Data;
using StudentTrackingCoach.Models;
using StudentTrackingCoach.Services.Interfaces;

namespace StudentTrackingCoach.Services.Implementations
{
    public class TenantService : ITenantService
    {
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _configuration;
        private Tenant? _currentTenant;
        private int? _currentTenantId;

        public TenantService(ApplicationDbContext db, IConfiguration configuration)
        {
            _db = db;
            _configuration = configuration;
        }

        public int CurrentTenantId
            => _currentTenantId
               ?? _currentTenant?.TenantId
               ?? _configuration.GetValue<int?>("MultiTenant:DefaultTenantId")
               ?? 1;

        public Tenant? CurrentTenant => _currentTenant;

        public bool IsMultiTenantEnabled => _configuration.GetValue<bool>("MultiTenant:Enabled");

        public async Task<Tenant?> ResolveTenantAsync(HttpContext httpContext)
        {
            if (!IsMultiTenantEnabled)
            {
                SetCurrentTenantId(_configuration.GetValue<int?>("MultiTenant:DefaultTenantId") ?? 1);
                return null;
            }

            var mode = _configuration["MultiTenant:ResolutionMode"] ?? "Subdomain";
            var slug = mode.Equals("Header", StringComparison.OrdinalIgnoreCase)
                ? httpContext.Request.Headers["X-Tenant-ID"].FirstOrDefault()
                : ExtractSubdomain(httpContext.Request.Host.Host);

            if (httpContext.User.IsInRole("SuperAdmin") &&
                httpContext.Session.TryGetValue("SelectedTenantId", out var selectedTenantBytes) &&
                int.TryParse(System.Text.Encoding.UTF8.GetString(selectedTenantBytes), out var selectedTenantId))
            {
                var selectedTenant = await _db.Tenants.FirstOrDefaultAsync(t => t.TenantId == selectedTenantId && t.IsActive);
                if (selectedTenant != null)
                {
                    SetCurrentTenant(selectedTenant);
                    return selectedTenant;
                }
            }

            if (string.IsNullOrWhiteSpace(slug))
            {
                SetCurrentTenantId(_configuration.GetValue<int?>("MultiTenant:DefaultTenantId") ?? 1);
                return null;
            }

            var tenant = await _db.Tenants
                .Include(t => t.Features)
                .FirstOrDefaultAsync(t => t.Slug == slug && t.IsActive);

            SetCurrentTenant(tenant);
            return tenant;
        }

        public void SetCurrentTenant(Tenant? tenant)
        {
            _currentTenant = tenant;
            _currentTenantId = tenant?.TenantId;
        }

        public void SetCurrentTenantId(int tenantId)
        {
            _currentTenantId = tenantId;
            if (_currentTenant?.TenantId != tenantId)
            {
                _currentTenant = null;
            }
        }

        public async Task<List<Tenant>> GetAllTenantsAsync()
            => await _db.Tenants
                .Include(t => t.Features)
                .AsNoTracking()
                .OrderBy(t => t.Name)
                .ToListAsync();

        public async Task<Tenant?> GetTenantByIdAsync(int tenantId)
            => await _db.Tenants
                .Include(t => t.Features)
                .FirstOrDefaultAsync(t => t.TenantId == tenantId);

        public async Task<Tenant?> GetTenantBySlugAsync(string slug)
            => await _db.Tenants
                .Include(t => t.Features)
                .FirstOrDefaultAsync(t => t.Slug == slug);

        public async Task<Tenant> CreateTenantAsync(Tenant tenant)
        {
            _db.Tenants.Add(tenant);
            await _db.SaveChangesAsync();
            return tenant;
        }

        public async Task UpdateTenantAsync(Tenant tenant)
        {
            _db.Tenants.Update(tenant);
            await _db.SaveChangesAsync();
        }

        public async Task<Dictionary<string, int>> GetTenantUsageMetricsAsync(int tenantId)
        {
            var students = await _db.Students
                .IgnoreQueryFilters()
                .CountAsync(s => s.TenantId == tenantId);
            var interventions = await _db.Interventions
                .IgnoreQueryFilters()
                .CountAsync(i => i.TenantId == tenantId);
            var notes = await _db.AdvisorNotes
                .IgnoreQueryFilters()
                .CountAsync(n => n.TenantId == tenantId);

            return new Dictionary<string, int>
            {
                ["Students"] = students,
                ["Interventions"] = interventions,
                ["AdvisorNotes"] = notes
            };
        }

        private static string? ExtractSubdomain(string host)
        {
            if (string.IsNullOrWhiteSpace(host))
            {
                return null;
            }

            var parts = host.Split('.', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 3 ? parts[0] : null;
        }
    }
}
