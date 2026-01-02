using Microsoft.EntityFrameworkCore;

namespace StudentTrackingCoach.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Student> Students { get; set; }
        public DbSet<Advisor> Advisors { get; set; }
        public DbSet<DecisionAudit> DecisionAudits { get; set; }
        public DbSet<PendingAction> PendingActions { get; set; }
    }
}
