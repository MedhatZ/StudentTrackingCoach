using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace StudentTrackingCoach.Models
{
    public class ApplicationDbContext
        : IdentityDbContext<ApplicationUser>
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
        public DbSet<AdvisorStudent> AdvisorStudents { get; set; }
        public DbSet<DecisionAudit> DecisionAudits { get; set; }
        public DbSet<PendingAction> PendingActions { get; set; }

        // =========================
        // ANALYTICS VIEWS (READ-ONLY)
        // =========================
        public DbSet<AdvisorRiskDashboardDto> AdvisorRiskDashboard { get; set; }
        public DbSet<StudentRiskNarrativeDto> StudentRiskNarratives { get; set; }
        public DbSet<SuccessStudentMyGradPath> SuccessStudentMyGradPath { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // -------------------------
            // TABLE MAPPINGS
            // -------------------------
            modelBuilder.Entity<Student>()
                .ToTable("Student", "dbo");

            modelBuilder.Entity<Advisor>()
                .ToTable("Advisor", "dbo");

            modelBuilder.Entity<AdvisorStudent>()
                .ToTable("AdvisorStudent", "dbo");

            modelBuilder.Entity<DecisionAudit>()
                .ToTable("DecisionAudits", "dbo");

            modelBuilder.Entity<PendingAction>()
                .ToTable("PendingAction", "dbo");

            // -------------------------
            // RELATIONSHIPS
            // -------------------------
            modelBuilder.Entity<AdvisorStudent>()
                .HasOne(x => x.Advisor)
                .WithMany()
                .HasForeignKey(x => x.AdvisorId);

            modelBuilder.Entity<AdvisorStudent>()
                .HasOne(x => x.Student)
                .WithMany()
                .HasForeignKey(x => x.StudentId);

            // -------------------------
            // SQL VIEWS (READ-ONLY)
            // -------------------------
            modelBuilder.Entity<SuccessStudentMyGradPath>()
                .HasNoKey()
                .ToView("vw_StudentMyGradPath", "success");

            modelBuilder.Entity<AdvisorRiskDashboardDto>()
                .HasNoKey()
                .ToView("vw_AdvisorRiskDashboard", "analytics");

            modelBuilder.Entity<StudentRiskNarrativeDto>()
                .HasNoKey()
                .ToView("vw_StudentRiskNarrative", "analytics");
        }
    }
}
