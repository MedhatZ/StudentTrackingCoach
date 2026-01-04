using Microsoft.EntityFrameworkCore;

namespace StudentTrackingCoach.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // =========================
        // CORE TABLES
        // =========================
        public DbSet<Student> Students { get; set; }
        public DbSet<Advisor> Advisors { get; set; }
        public DbSet<DecisionAudit> DecisionAudits { get; set; }
        public DbSet<PendingAction> PendingActions { get; set; }

        // =========================
        // ANALYTICS VIEWS (READ-ONLY)
        // =========================
        public DbSet<AdvisorRiskDashboardDto> AdvisorRiskDashboard { get; set; }
        public DbSet<StudentRiskNarrativeDto> StudentRiskNarratives { get; set; }

        // =========================
        // MODEL CONFIGURATION
        // =========================
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Advisor dashboard view
            modelBuilder.Entity<AdvisorRiskDashboardDto>()
                .HasNoKey()
                .ToView("vw_AdvisorRiskDashboard", "analytics");

            // Student narrative view
            modelBuilder.Entity<StudentRiskNarrativeDto>()
                .HasNoKey()
                .ToView("vw_StudentRiskNarrative", "analytics");
        }
    }
}
