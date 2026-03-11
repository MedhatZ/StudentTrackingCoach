using Microsoft.EntityFrameworkCore;

namespace StudentTrackingCoach.Data
{
    public class TenantAwareDbContext : ApplicationDbContext
    {
        public TenantAwareDbContext(
            DbContextOptions<ApplicationDbContext> options,
            IHttpContextAccessor httpContextAccessor,
            IConfiguration configuration)
            : base(options, httpContextAccessor, configuration)
        {
        }
    }
}
