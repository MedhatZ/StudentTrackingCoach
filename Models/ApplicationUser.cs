using Microsoft.AspNetCore.Identity;

namespace StudentTrackingCoach.Models
{
    public class ApplicationUser : IdentityUser
    {
        // ==============================
        // DOMAIN LINKING (MATCH DB TYPES)
        // ==============================

        // These MUST be int? because SQL columns are INT
        public int? StudentId { get; set; }
        public int? AdvisorId { get; set; }

        // Optional future expansion
        public int? InstitutionId { get; set; }
    }
}
