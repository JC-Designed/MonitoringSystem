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

        // GET: Admin Users page - FIXED: Excluding declined accounts
        public async Task<IActionResult> Users()
        {
            Console.WriteLine("========== USERS ACTION HIT ==========");

            try
            {
                // Get users from database EXCLUDING declined accounts
                var users = await _userManager.Users
                    .Where(u => u.Status == "Approved") // FILTER OUT DECLINED USERS
                    .OrderBy(u => u.FullName)
                    .ToListAsync();

                Console.WriteLine($"Found {users.Count} active users in database (excluding declined)");

                return View(users ?? new List<ApplicationUser>());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in Users action: {ex.Message}");
                return View(new List<ApplicationUser>());
            }
        }

        // GET: Admin Registration page
        public async Task<IActionResult> Registration()
        {
            Console.WriteLine("========== REGISTRATION ACTION HIT ==========");

            try
            {
                // Get all users ordered by registration date
                var users = await _userManager.Users
                    .OrderByDescending(u => u.CreatedAt)
                    .ToListAsync();

                return View(users);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in Registration action: {ex.Message}");
                return View(new List<ApplicationUser>());
            }
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

        // API: Get dashboard data for charts - UPDATED WITH REAL MONTHLY DATA
        [HttpGet]
        public async Task<IActionResult> GetDashboardData(int year)
        {
            try
            {
                Console.WriteLine($"========== GET DASHBOARD DATA FOR YEAR {year} ==========");

                // Get real data from database using Status
                var totalUsers = await _userManager.Users.CountAsync();
                var totalCompanies = await _userManager.Users.CountAsync(u => u.Role == "Company");
                var totalStudents = await _userManager.Users.CountAsync(u => u.Role == "Student");
                var pendingApprovals = await _userManager.Users.CountAsync(u => u.Status == "Pending" && u.Role != "Admin");
                var approvedUsers = await _userManager.Users.CountAsync(u => u.Status == "Approved" && u.Role != "Admin");
                var declinedUsers = await _userManager.Users.CountAsync(u => u.Status == "Declined" && u.Role != "Admin");

                // Get recent registrations (last 5 users)
                var recentUsers = await _userManager.Users
                    .Where(u => u.Role != "Admin")
                    .OrderByDescending(u => u.CreatedAt)
                    .Take(5)
                    .Select(u => new
                    {
                        u.Email,
                        u.FullName,
                        u.Status,
                        CreatedAt = u.CreatedAt.ToString("yyyy-MM-dd HH:mm")
                    })
                    .ToListAsync();

                // REAL MONTHLY DATA for charts
                var pendingUsers = new List<int>();      // Pink chart - Pending users per month
                var tasksSubmitted = new List<int>();    // Blue chart - Tasks (placeholder for now)
                var monthlyUsers = new List<int>();      // Green chart - Total users per month
                var monthlyCompanies = new List<int>();  // Gold chart - Companies per month

                // For each month, get real data from database
                for (int month = 1; month <= 12; month++)
                {
                    // Create date range for the month
                    var startDate = new DateTime(year, month, 1);
                    var endDate = startDate.AddMonths(1).AddDays(-1);

                    // PENDING USERS created in this month (for pink chart)
                    var pendingCount = await _userManager.Users
                        .CountAsync(u => u.Status == "Pending"
                            && u.Role != "Admin"
                            && u.CreatedAt >= startDate
                            && u.CreatedAt <= endDate);
                    pendingUsers.Add(pendingCount);

                    // TASKS SUBMITTED (you can replace this with actual task data later)
                    // For now, using a simple calculation based on users
                    tasksSubmitted.Add(pendingCount + new Random().Next(1, 5));

                    // TOTAL USERS created in this month (for green chart)
                    var usersCount = await _userManager.Users
                        .CountAsync(u => u.Role != "Admin"
                            && u.CreatedAt >= startDate
                            && u.CreatedAt <= endDate);
                    monthlyUsers.Add(usersCount);

                    // COMPANIES created in this month (for gold chart)
                    var companiesCount = await _userManager.Users
                        .CountAsync(u => u.Role == "Company"
                            && u.CreatedAt >= startDate
                            && u.CreatedAt <= endDate);
                    monthlyCompanies.Add(companiesCount);
                }

                var data = new
                {
                    pendingUsers = pendingUsers,        // REAL pending users per month
                    tasksSubmitted = tasksSubmitted,    // Placeholder for now
                    totalUsers = monthlyUsers,          // REAL total users per month
                    totalCompanies = monthlyCompanies,  // REAL companies per month
                    year = year,
                    recentUsers = recentUsers,
                    stats = new
                    {
                        totalUsers,
                        totalCompanies,
                        totalStudents,
                        pendingApprovals,
                        approvedUsers,
                        declinedUsers
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

        // API: Update user status (Approve/Decline)
        [HttpPost]
        public async Task<IActionResult> UpdateUserStatus([FromBody] UpdateStatusModel model)
        {
            try
            {
                Console.WriteLine($"========== UPDATE USER STATUS ==========");
                Console.WriteLine($"UserId: {model.UserId}, Status: {model.Status}");

                var user = await _userManager.FindByIdAsync(model.UserId);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                // Update status
                user.Status = model.Status;
                user.UpdatedAt = DateTime.Now;

                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    Console.WriteLine($"User {user.Email} status updated to {model.Status}");
                    return Json(new
                    {
                        success = true,
                        message = $"User has been {model.Status.ToLower()} successfully",
                        status = model.Status
                    });
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return Json(new { success = false, message = $"Failed to update status: {errors}" });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in UpdateUserStatus: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // API: Get pending users
        [HttpGet]
        public async Task<IActionResult> GetPendingUsers()
        {
            try
            {
                var pendingUsers = await _userManager.Users
                    .Where(u => u.Status == "Pending" && u.Role != "Admin")
                    .OrderByDescending(u => u.CreatedAt)
                    .Select(u => new
                    {
                        u.Id,
                        u.FullName,
                        u.Email,
                        u.StudentId,
                        u.Role,
                        u.Status,
                        u.CreatedAt,
                        u.BirthDate
                    })
                    .ToListAsync();

                return Json(new { success = true, users = pendingUsers });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // API: Get user details for preview
        [HttpGet]
        public async Task<IActionResult> GetUserDetails(string id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                return Json(new
                {
                    success = true,
                    user = new
                    {
                        user.Id,
                        user.FullName,
                        user.Email,
                        user.StudentId,
                        user.Role,
                        user.Status,
                        user.BirthDate,
                        user.MobileNumber,
                        user.Address,
                        user.Program,
                        user.Year,
                        user.ProfileImage,
                        user.CreatedAt,
                        user.IsActive
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
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

    // Model for updating user status
    public class UpdateStatusModel
    {
        public string UserId { get; set; }
        public string Status { get; set; }
    }
}