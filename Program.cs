using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StudentTrackingCoach.Data;          // ✅ REQUIRED
using StudentTrackingCoach.Models;

var builder = WebApplication.CreateBuilder(args);

// ================================
// DATABASE
// ================================
var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("DefaultConnection missing.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// ================================
// IDENTITY (WITH UI)
// ================================
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders()
    .AddDefaultUI();

// 🔐 COOKIE CONFIG (FIXES LOGOUT + ACCESS DENIED)
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.SlidingExpiration = true;
});

// ================================
// MVC + RAZOR
// ================================
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

// ================================
// PIPELINE
// ================================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// ================================
// ROUTES
// ================================
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages(); // REQUIRED for Identity UI

// ======================================================
// 🔥 DEMO DATA SEEDING (SAFE — DEV ONLY)
// ======================================================
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;

    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();

        // -------------------------------
        // SEED ADVISORS
        // -------------------------------
        if (!context.Advisor.Any())
        {
            context.Advisor.AddRange(
                new Advisor { Name = "Dr. Angela Morris" },
                new Advisor { Name = "Prof. James Carter" }
            );
            context.SaveChanges();
        }

        // -------------------------------
        // SEED STUDENTS
        // -------------------------------
        if (!context.Students.Any())
        {
            context.Students.AddRange(
                new Student
                {
                    StudentId = 100001,
                    InstitutionId = 1,
                    EnrollmentStatus = "Active",
                    IsFirstGen = true,
                    IsWorking = false,
                    PreferredModality = "In-Person",
                    CreatedAt = DateTime.UtcNow.AddMonths(-6)
                },
                new Student
                {
                    StudentId = 100002,
                    InstitutionId = 1,
                    EnrollmentStatus = "Probation",
                    IsFirstGen = false,
                    IsWorking = true,
                    PreferredModality = "Online",
                    CreatedAt = DateTime.UtcNow.AddMonths(-3)
                }
            );
            context.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Demo data seeding failed: {ex.Message}");
    }
}

app.Run();
