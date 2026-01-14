using Microsoft.AspNetCore.Identity;

namespace StudentTrackingCoach.Data
{
    public static class RoleSeeder
    {
        private static readonly string[] Roles =
        {
            "Student",
            "Advisor",
            "Admin"
        };

        public static async Task SeedRolesAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            foreach (var role in Roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }
    }
}

