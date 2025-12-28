using Microsoft.AspNetCore.Identity;

namespace MonitoringSystem.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<MonitoringSystem.Models.ApplicationUser>>();

            // Create roles
            string[] roles = { "Admin", "Student", "Company" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // Create master Admin
            var adminEmail = "Jonardcarmelotes09@gmail.com";
            var adminPassword = "admin123";

            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var adminUser = new MonitoringSystem.Models.ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    IsApproved = true
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }
        }
    }
}
