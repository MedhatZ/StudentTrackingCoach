using Microsoft.AspNetCore.Identity;

namespace StudentTrackingCoach.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Link Identity user → Student
        public long? StudentId { get; set; }

        // Link Identity user → Advisor
        public int? AdvisorId { get; set; }

        public int? InstitutionId { get; set; }
    }
}
