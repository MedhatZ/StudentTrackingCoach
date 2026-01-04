using Microsoft.EntityFrameworkCore;
using StudentTrackingCoach.Models;

var builder = WebApplication.CreateBuilder(args);

// ✅ Standard, expected connection string key
var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "DefaultConnection connection string is missing or invalid.");
}

// ✅ EF Core SQL Server registration
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(
        connectionString,
        sql => sql.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null));
});

builder.Services.AddControllersWithViews();

var app = builder.Build();

// ✅ Environment-specific pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// ✅ Default MVC route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
