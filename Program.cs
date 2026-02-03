using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MonitoringSystem.Data;
using MonitoringSystem.Models;

var builder = WebApplication.CreateBuilder(args);

// ===================== SERVICES =====================
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddSession();

var app = builder.Build();


// ===================== DEMO ACCOUNT SEEDING =====================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

    // Ensure roles exist
    string[] roles = { "Student", "Company" };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    // ===== STUDENT DEMO =====
    var studentEmail = "jonarcarmelotes123@gmail.com";
    if (await userManager.FindByEmailAsync(studentEmail) == null)
    {
        var student = new ApplicationUser
        {
            UserName = studentEmail,
            Email = studentEmail,
            EmailConfirmed = true
        };

        await userManager.CreateAsync(student, "paolo123!");
        await userManager.AddToRoleAsync(student, "Student");
    }

    // ===== COMPANY DEMO =====
    var companyEmail = "proyanfromyt@gmail.com";
    if (await userManager.FindByEmailAsync(companyEmail) == null)
    {
        var company = new ApplicationUser
        {
            UserName = companyEmail,
            Email = companyEmail,
            EmailConfirmed = true
        };

        await userManager.CreateAsync(company, "company123!");
        await userManager.AddToRoleAsync(company, "Company");
    }
}


// ===================== MIDDLEWARE =====================
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

app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
