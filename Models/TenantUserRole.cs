using Microsoft.AspNetCore.Identity;

namespace StudentTrackingCoach.Models
{
    public class TenantUserRole
    {
        public int Id { get; set; }
        public int TenantId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;

        public Tenant? Tenant { get; set; }
        public ApplicationUser? User { get; set; }
    }
}
