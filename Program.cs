using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StudentTrackingCoach.Data;
using StudentTrackingCoach.Middleware;
using StudentTrackingCoach.Models;
using StudentTrackingCoach.Services.Implementations;
using StudentTrackingCoach.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// ================================
// DATABASE
// ================================
var connectionString =
    builder.Configuration.GetConnectionString("StudentTrackingDB")
    ?? throw new InvalidOperationException("StudentTrackingDB connection string missing.");

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
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<IAdvisorService, AdvisorService>();
builder.Services.AddScoped<IRiskCalculationService, RiskCalculationService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<IRUMService, ApplicationInsightsRUMService>();
builder.Services.AddSingleton<IAiUsageTrackingService, AiUsageTrackingService>();
builder.Services.AddSingleton<IConfigurationValidationService, ConfigurationValidationService>();
if (builder.Configuration.GetValue<bool>("ApplicationInsights:Enabled") &&
    !string.IsNullOrWhiteSpace(builder.Configuration["ApplicationInsights:ConnectionString"]))
{
    builder.Services.AddApplicationInsightsTelemetry(options =>
    {
        options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
    });
    builder.Services.AddSingleton<ITelemetryService, ApplicationInsightsTelemetryService>();
}
else
{
    builder.Services.AddSingleton<ITelemetryService, NullTelemetryService>();
}

if (builder.Configuration.GetValue<bool>("Redis:Enabled") &&
    !string.IsNullOrWhiteSpace(builder.Configuration["Redis:ConnectionString"]))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = builder.Configuration["Redis:ConnectionString"];
        options.InstanceName = builder.Configuration["Redis:InstanceName"] ?? "GradPath";
    });
    builder.Services.AddScoped<ICacheService, RedisCacheService>();
}
else
{
    builder.Services.AddDistributedMemoryCache();
    builder.Services.AddScoped<ICacheService, MemoryCacheFallbackService>();
}

builder.Services.AddScoped<MockAiRecommendationService>();
builder.Services.AddScoped<AzureOpenAiRecommendationService>();
builder.Services.AddScoped<IAiRecommendationService>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("AiServiceFactory");
    var validator = sp.GetRequiredService<IConfigurationValidationService>();
    var aiEnabled = config.GetValue<bool>("AiFeatures:Enabled");

    if (!aiEnabled)
    {
        logger.LogInformation("AI features disabled. Using Mock AI service.");
        return sp.GetRequiredService<MockAiRecommendationService>();
    }

    var useRealAi = config.GetValue<bool>("AiFeatures:UseRealAi");
    if (!useRealAi)
    {
        logger.LogInformation("UseRealAi disabled. Using Mock AI service.");
        return sp.GetRequiredService<MockAiRecommendationService>();
    }

    var endpoint = config["AzureOpenAI:Endpoint"];
    var apiKey = config["AzureOpenAI:ApiKey"];
    if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(apiKey) || !validator.IsAzureOpenAiConfigured())
    {
        logger.LogWarning("Azure OpenAI Endpoint/ApiKey missing. Using Mock AI service.");
        return sp.GetRequiredService<MockAiRecommendationService>();
    }

    logger.LogInformation("Azure OpenAI configuration valid. Using AzureOpenAiRecommendationService.");
    return sp.GetRequiredService<AzureOpenAiRecommendationService>();
});
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

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
app.UseSession();

app.UseAuthentication();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseAuthorization();

// ================================
// ROUTES
// ================================
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

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
        var defaultTenantId = builder.Configuration.GetValue<int?>("MultiTenant:DefaultTenantId") ?? 1;

        if (!context.Tenants.Any())
        {
            context.Tenants.Add(new Tenant
            {
                TenantId = defaultTenantId,
                Name = "Default Institution",
                Slug = "default",
                PassingGrade = builder.Configuration.GetValue<int?>("RiskThresholds:PassingGrade") ?? 70,
                IsActive = true
            });
            context.SaveChanges();
        }

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
                    TenantId = defaultTenantId,
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
                    TenantId = defaultTenantId,
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
