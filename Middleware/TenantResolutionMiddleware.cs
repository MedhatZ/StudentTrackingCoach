using StudentTrackingCoach.Services.Interfaces;

namespace StudentTrackingCoach.Middleware
{
    public class TenantResolutionMiddleware
    {
        private readonly RequestDelegate _next;

        public TenantResolutionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ITenantService tenantService)
        {
            var tenant = await tenantService.ResolveTenantAsync(context);

            if (tenant != null)
            {
                context.Items["CurrentTenant"] = tenant;
                context.Items["CurrentTenantId"] = tenant.TenantId;
            }
            else
            {
                context.Items["CurrentTenantId"] = tenantService.CurrentTenantId;
            }

            await _next(context);
        }
    }
}
