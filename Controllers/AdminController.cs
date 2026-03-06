using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonitoringSystem.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace MonitoringSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Admin Dashboard
        public IActionResult Dashboard()
        {
            Console.WriteLine("========== DASHBOARD ACTION HIT ==========");
            Console.WriteLine($"Time: {DateTime.Now}");
            Console.WriteLine($"User: {User?.Identity?.Name ?? "No User"}");
            Console.WriteLine($"IsAuthenticated: {User?.Identity?.IsAuthenticated}");
            Console.WriteLine($"IsInRole Admin: {User?.IsInRole("Admin")}");
            Console.WriteLine("==========================================");

            return View();
        }

        // GET: Admin Users page - CHANGED: Now showing ALL users including admin
        public async Task<IActionResult> Users()
        {
            Console.WriteLine("========== USERS ACTION HIT ==========");

            try
            {
                // Get ALL users from database (including admin)
                var users = await _userManager.Users
                    .OrderBy(u => u.FullName)
                    .ToListAsync();

                // Log count for debugging
                Console.WriteLine($"Found {users.Count} users in database (including admin)");

                // Return the list to the view
                return View(users ?? new List<ApplicationUser>());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in Users action: {ex.Message}");
                // Return empty list if there's an error
                return View(new List<ApplicationUser>());
            }
        }

        // GET: Admin Registration page
        public IActionResult Registration()
        {
            Console.WriteLine("========== REGISTRATION ACTION HIT ==========");
            return View();
        }

        // GET: Admin Reports page
        public IActionResult Reports()
        {
            Console.WriteLine("========== REPORTS ACTION HIT ==========");
            return View();
        }

        // GET: Admin/Test - Simple test endpoint
        public IActionResult Test()
        {
            Console.WriteLine("========== TEST ACTION HIT ==========");
            return Content($@"
                Admin Controller is working!<br><br>
                User: {User?.Identity?.Name ?? "Not logged in"}<br>
                Authenticated: {User?.Identity?.IsAuthenticated}<br>
                Is Admin: {User?.IsInRole("Admin")}<br>
                Dashboard URL: /Admin/Dashboard
            ", "text/html");
        }

        // API: Get dashboard data for charts
        [HttpGet]
        public async Task<IActionResult> GetDashboardData(int year)
        {
            try
            {
                Console.WriteLine($"========== GET DASHBOARD DATA FOR YEAR {year} ==========");

                // Get real data from database
                var totalUsers = await _userManager.Users.CountAsync();
                var totalCompanies = await _userManager.Users.CountAsync(u => u.Role == "Company");
                var totalStudents = await _userManager.Users.CountAsync(u => u.Role == "Student");
                var pendingApprovals = await _userManager.Users.CountAsync(u => !u.IsApproved && u.Role != "Admin");

                // Generate monthly data
                var pendingUsers = new List<int>();
                var tasksSubmitted = new List<int>();
                var monthlyUsers = new List<int>();
                var monthlyCompanies = new List<int>();

                var random = new Random();
                for (int month = 1; month <= 12; month++)
                {
                    pendingUsers.Add(random.Next(5, 30));
                    tasksSubmitted.Add(random.Next(10, 50));
                    monthlyUsers.Add(random.Next(100, 200));
                    monthlyCompanies.Add(random.Next(20, 40));
                }

                var data = new
                {
                    pendingUsers = pendingUsers,
                    tasksSubmitted = tasksSubmitted,
                    totalUsers = monthlyUsers,
                    totalCompanies = monthlyCompanies,
                    year = year,
                    stats = new
                    {
                        totalUsers,
                        totalCompanies,
                        totalStudents,
                        pendingApprovals
                    }
                };

                return Json(data);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in GetDashboardData: {ex.Message}");
                return Json(new { error = ex.Message });
            }
        }

        // API: Get admin profile data
        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Console.WriteLine($"========== GET PROFILE FOR USER: {userId} ==========");

            var admin = await _context.Admins
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.UserId == userId);

            if (admin == null)
            {
                Console.WriteLine("Admin not found in database");
                return Json(new
                {
                    success = false,
                    message = "Admin not found"
                });
            }

            Console.WriteLine($"Admin found: {admin.EmployeeId}");
            return Json(new
            {
                success = true,
                fullName = admin.User?.FullName ?? "",
                employeeId = admin.EmployeeId ?? "",
                department = admin.Department ?? "",
                position = admin.Position ?? "",
                hireDate = admin.HireDate?.ToString("yyyy-MM-dd") ?? "",
                permissionsLevel = admin.PermissionsLevel ?? "Basic",
                officeLocation = admin.OfficeLocation ?? "",
                officePhone = admin.OfficePhone ?? "",
                createdAt = admin.CreatedAt.ToString("yyyy-MM-dd HH:mm tt"),
                username = admin.User?.UserName ?? "",
                email = admin.User?.Email ?? "",
                address = admin.User?.Address ?? "",
                // Permissions
                canManageUsers = admin.CanManageUsers,
                canManageCompanies = admin.CanManageCompanies,
                canManageStudents = admin.CanManageStudents,
                canViewReports = admin.CanViewReports,
                canApproveAccounts = admin.CanApproveAccounts
            });
        }

        // API: Save profile changes
        [HttpPost]
        public async Task<IActionResult> SaveProfile([FromBody] AdminProfileModel model)
        {
            Console.WriteLine("========== SAVE PROFILE CALLED ==========");

            if (!ModelState.IsValid)
            {
                Console.WriteLine("Model state invalid");
                return BadRequest(new { success = false, message = "Invalid data" });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Console.WriteLine($"User ID: {userId}");

            var admin = await _context.Admins
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.UserId == userId);

            if (admin == null)
            {
                Console.WriteLine("Admin not found");
                return NotFound(new { success = false, message = "Admin not found" });
            }

            // Update fields
            if (admin.User != null)
            {
                admin.User.FullName = model.FullName;
                admin.User.Address = model.Address;
                Console.WriteLine($"Updated user: {model.FullName}");
            }

            admin.Department = model.Department;
            admin.Position = model.Position;
            admin.OfficeLocation = model.OfficeLocation;
            admin.OfficePhone = model.OfficePhone;
            admin.UpdatedAt = System.DateTime.Now;

            await _context.SaveChangesAsync();
            Console.WriteLine("Profile saved successfully");

            return Ok(new { success = true, message = "Profile updated successfully" });
        }

        // API: Delete user
        [HttpPost]
        public async Task<IActionResult> DeleteUser([FromBody] string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                // Don't allow deleting yourself
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (user.Id == currentUserId)
                {
                    return Json(new { success = false, message = "You cannot delete your own account" });
                }

                var result = await _userManager.DeleteAsync(user);
                if (result.Succeeded)
                {
                    return Json(new { success = true, message = "User deleted successfully" });
                }

                return Json(new { success = false, message = "Failed to delete user" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // API: Make user admin
        [HttpPost]
        public async Task<IActionResult> MakeAdmin([FromBody] string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                // Add to Admin role
                var result = await _userManager.AddToRoleAsync(user, "Admin");
                if (result.Succeeded)
                {
                    user.Role = "Admin";
                    await _userManager.UpdateAsync(user);
                    return Json(new { success = true, message = "User is now an Admin" });
                }

                return Json(new { success = false, message = "Failed to make user admin" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }

    public class AdminProfileModel
    {
        public string FullName { get; set; } = "";
        public string Department { get; set; } = "";
        public string Position { get; set; } = "";
        public string OfficeLocation { get; set; } = "";
        public string OfficePhone { get; set; } = "";
        public string Address { get; set; } = "";
    }
}