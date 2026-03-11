using StudentTrackingCoach.Models;

namespace StudentTrackingCoach.Services.Interfaces
{
    public interface ITenantService
    {
        int CurrentTenantId { get; }
        Tenant? CurrentTenant { get; }
        bool IsMultiTenantEnabled { get; }

        Task<Tenant?> ResolveTenantAsync(HttpContext httpContext);
        void SetCurrentTenant(Tenant? tenant);
        void SetCurrentTenantId(int tenantId);

        Task<List<Tenant>> GetAllTenantsAsync();
        Task<Tenant?> GetTenantByIdAsync(int tenantId);
        Task<Tenant?> GetTenantBySlugAsync(string slug);
        Task<Tenant> CreateTenantAsync(Tenant tenant);
        Task UpdateTenantAsync(Tenant tenant);
        Task<Dictionary<string, int>> GetTenantUsageMetricsAsync(int tenantId);
    }
}
