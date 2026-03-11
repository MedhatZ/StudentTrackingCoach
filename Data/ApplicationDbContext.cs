using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StudentTrackingCoach.Models;

namespace StudentTrackingCoach.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        private readonly IHttpContextAccessor? _httpContextAccessor;
        private readonly IConfiguration? _configuration;

        public DbSet<AdvisorNote> AdvisorNotes { get; set; } = null!;

        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options,
            IHttpContextAccessor? httpContextAccessor = null,
            IConfiguration? configuration = null)
            : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
        }

        // ================================
        // CORE TABLES
        // ================================
        public DbSet<Student> Students { get; set; } = null!;
        public DbSet<Advisor> Advisor { get; set; } = null!;
        public DbSet<AdminAuditLog> AdminAuditLogs { get; set; } = null!;
        public DbSet<Intervention> Interventions { get; set; } = null!;
        public DbSet<Tenant> Tenants { get; set; } = null!;
        public DbSet<TenantFeature> TenantFeatures { get; set; } = null!;
        public DbSet<TenantUserRole> TenantUserRoles { get; set; } = null!;

        private bool IsMultiTenantEnabled
            => _configuration?.GetValue<bool>("MultiTenant:Enabled") == true;

        private int CurrentTenantId
        {
            get
            {
                var tenantFromContext = _httpContextAccessor?.HttpContext?.Items["CurrentTenantId"];
                if (tenantFromContext is int tenantIdFromItems)
                {
                    return tenantIdFromItems;
                }

                if (tenantFromContext is string tenantIdString && int.TryParse(tenantIdString, out var parsed))
                {
                    return parsed;
                }

                return _configuration?.GetValue<int?>("MultiTenant:DefaultTenantId") ?? 1;
            }
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ================================
            // STUDENT (dbo.Student)
            // ================================
            builder.Entity<Student>()
                .ToTable("Student", "dbo")
                .HasKey(s => s.StudentId);
            builder.Entity<Student>()
                .HasIndex(s => new { s.TenantId, s.StudentId });
            builder.Entity<Student>()
                .HasQueryFilter(s => !IsMultiTenantEnabled || s.TenantId == CurrentTenantId);

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
            builder.Entity<AdvisorNote>()
                .HasIndex(a => new { a.TenantId, a.StudentId, a.CreatedAt });
            builder.Entity<AdvisorNote>()
                .HasQueryFilter(a => !IsMultiTenantEnabled || a.TenantId == CurrentTenantId);

            builder.Entity<Intervention>()
                .ToTable("Interventions", "dbo")
                .HasKey(i => i.Id);

            builder.Entity<Intervention>()
                .Property(i => i.Id)
                .ValueGeneratedOnAdd();

            builder.Entity<Intervention>()
                .Property(i => i.Content)
                .HasColumnType("nvarchar(max)");
            builder.Entity<Intervention>()
                .HasIndex(i => new { i.TenantId, i.StudentId, i.Status });
            builder.Entity<Intervention>()
                .HasQueryFilter(i => !IsMultiTenantEnabled || i.TenantId == CurrentTenantId);

            // ================================
            // ADMIN AUDIT LOGS
            // ================================
            builder.Entity<AdminAuditLog>()
                .ToTable("AdminAuditLogs")
                .HasKey(a => a.Id);
            builder.Entity<AdminAuditLog>()
                .HasIndex(a => new { a.TenantId, a.CreatedAt });
            builder.Entity<AdminAuditLog>()
                .HasQueryFilter(a => !IsMultiTenantEnabled || a.TenantId == CurrentTenantId);

            // ================================
            // TENANTS
            // ================================
            builder.Entity<Tenant>()
                .ToTable("Tenants")
                .HasKey(t => t.TenantId);
            builder.Entity<Tenant>()
                .HasIndex(t => t.Slug)
                .IsUnique();

            builder.Entity<TenantFeature>()
                .ToTable("TenantFeatures")
                .HasKey(tf => tf.Id);
            builder.Entity<TenantFeature>()
                .HasIndex(tf => new { tf.TenantId, tf.FeatureKey })
                .IsUnique();
            builder.Entity<TenantFeature>()
                .HasOne(tf => tf.Tenant)
                .WithMany(t => t.Features)
                .HasForeignKey(tf => tf.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<TenantUserRole>()
                .ToTable("TenantUserRoles")
                .HasKey(tur => tur.Id);
            builder.Entity<TenantUserRole>()
                .HasIndex(tur => new { tur.TenantId, tur.UserId, tur.RoleName })
                .IsUnique();
            builder.Entity<TenantUserRole>()
                .HasOne(tur => tur.Tenant)
                .WithMany(t => t.TenantUsers)
                .HasForeignKey(tur => tur.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Entity<TenantUserRole>()
                .HasOne(tur => tur.User)
                .WithMany(u => u.TenantRoles)
                .HasForeignKey(tur => tur.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
