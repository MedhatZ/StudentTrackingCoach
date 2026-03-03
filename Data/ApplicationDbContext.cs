using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StudentTrackingCoach.Models;

namespace StudentTrackingCoach.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public DbSet<AdvisorNote> AdvisorNotes { get; set; } = null!;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // ================================
        // CORE TABLES
        // ================================
        public DbSet<Student> Students { get; set; } = null!;
        public DbSet<Advisor> Advisor { get; set; } = null!;
        public DbSet<AdminAuditLog> AdminAuditLogs { get; set; } = null!;
        public DbSet<Intervention> Interventions { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ================================
            // STUDENT (dbo.Student)
            // ================================
            builder.Entity<Student>()
                .ToTable("Student", "dbo")
                .HasKey(s => s.StudentId);

            // ================================
            // ADVISOR (dbo.Advisor)  🔥 FIX
            // ================================
            builder.Entity<Advisor>()
                .ToTable("Advisor", "dbo")   // ⬅️ SINGULAR TABLE NAME
                .HasKey(a => a.AdvisorId);

            // ================================
            // ADVISOR NOTES - FIX FOR ADVISORID ERROR
            // ================================
            builder.Entity<AdvisorNote>()
                .ToTable("AdvisorNotes")
                .HasKey(a => a.AdvisorNoteId);

            // Ignore the Advisor navigation property completely
            // This prevents EF Core from looking for an AdvisorId column
            builder.Entity<AdvisorNote>()
                .Ignore(a => a.Advisor);

            // Configure the string column that actually exists
            builder.Entity<AdvisorNote>()
                .Property(a => a.AdvisorUserId)
                .HasColumnName("AdvisorUserId")
                .HasMaxLength(450);

            builder.Entity<Intervention>()
                .ToTable("Interventions", "dbo")
                .HasKey(i => i.Id);

            builder.Entity<Intervention>()
                .Property(i => i.Id)
                .ValueGeneratedOnAdd();

            builder.Entity<Intervention>()
                .Property(i => i.Content)
                .HasColumnType("nvarchar(max)");

            // ================================
            // ADMIN AUDIT LOGS
            // ================================
            builder.Entity<AdminAuditLog>()
                .ToTable("AdminAuditLogs")
                .HasKey(a => a.Id);
        }

    }
}
