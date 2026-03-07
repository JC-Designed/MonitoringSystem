using Microsoft.EntityFrameworkCore;
using MonitoringSystem.Models;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register DbContext with SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register Identity with ApplicationUser
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password settings - CHANGED for Student ID compatibility
    options.Password.RequireDigit = true;           // Keep (Student IDs have numbers)
    options.Password.RequiredLength = 6;            // Keep minimum length
    options.Password.RequireNonAlphanumeric = false; // Don't need special chars
    options.Password.RequireUppercase = false;       // CHANGED to false
    options.Password.RequireLowercase = false;       // CHANGED to false

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure cookie authentication to redirect to login
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// ==================== CREATE HARDCODED ADMIN ACCOUNT ====================
using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    try
    {
        // Create Admin role if it doesn't exist
        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new IdentityRole("Admin"));
            Console.WriteLine("Admin role created.");
        }

        // Create Student role if it doesn't exist
        if (!await roleManager.RoleExistsAsync("Student"))
        {
            await roleManager.CreateAsync(new IdentityRole("Student"));
            Console.WriteLine("Student role created.");
        }

        // Create Company role if it doesn't exist
        if (!await roleManager.RoleExistsAsync("Company"))
        {
            await roleManager.CreateAsync(new IdentityRole("Company"));
            Console.WriteLine("Company role created.");
        }

        // Check if admin already exists
        string adminEmail = "admin@ctu.edu.ph";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            Console.WriteLine("Admin user not found. Creating new admin...");

            // Create new admin user
            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "System Administrator",
                Role = "Admin",
                Status = "Approved",
                IsActive = true,
                CreatedAt = DateTime.Now,
                ProfileImage = "/images/ctu-logo.png",
                BannerImage = "/images/banner-placeholder.jpg"
            };

            // Create with password: Admin@123
            var result = await userManager.CreateAsync(admin, "Admin@123");

            if (result.Succeeded)
            {
                // Add to Admin role
                await userManager.AddToRoleAsync(admin, "Admin");
                Console.WriteLine("Admin user created successfully with email: admin@ctu.edu.ph and password: Admin@123");
            }
            else
            {
                Console.WriteLine("Failed to create admin user:");
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($"- {error.Description}");
                }
            }
        }
        else
        {
            Console.WriteLine("Admin user already exists.");

            // Update existing admin to use Status if it's not set
            if (string.IsNullOrEmpty(adminUser.Status))
            {
                adminUser.Status = "Approved";
                await userManager.UpdateAsync(adminUser);
                Console.WriteLine("Updated existing admin with Status = Approved");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error creating admin: {ex.Message}");
    }
}
// =========================================================================

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// IMPORTANT: Authentication must be before Authorization
app.UseAuthentication();
app.UseAuthorization();

// Default route now goes to Account/Login
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();