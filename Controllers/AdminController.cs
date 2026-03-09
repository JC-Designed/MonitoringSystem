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
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<AdminController> logger)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
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

        // GET: Admin Registration page - UPDATED to show Pending first and include Program
        public async Task<IActionResult> Registration()
        {
            Console.WriteLine("========== REGISTRATION ACTION HIT ==========");

            try
            {
                // Get all users ordered by Status (Pending first) then by registration date
                var users = await _userManager.Users
                    .OrderBy(u => u.Status == "Pending" ? 0 : 1)  // Pending users first
                    .ThenByDescending(u => u.CreatedAt)           // Then by newest first
                    .ToListAsync();

                // Log for debugging
                Console.WriteLine($"Total users found: {users.Count}");
                Console.WriteLine($"Pending users: {users.Count(u => u.Status == "Pending")}");
                Console.WriteLine($"Approved users: {users.Count(u => u.Status == "Approved")}");
                Console.WriteLine($"Declined users: {users.Count(u => u.Status == "Declined")}");

                // Log a sample user to verify Program is included
                var sampleUser = users.FirstOrDefault();
                if (sampleUser != null)
                {
                    Console.WriteLine($"Sample User - Email: {sampleUser.Email}, Program: '{sampleUser.Program}', Status: {sampleUser.Status}");
                }

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
                        u.Program,
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
                        u.Program,
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

        // ==================== IMPROVED MAKE ADMIN FUNCTIONALITY ====================
        /// <summary>
        /// Makes a user an Admin by updating both custom Role field and Identity Role
        /// </summary>
        [HttpPost]
        [Route("Admin/Users/MakeAdmin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MakeAdmin([FromBody] MakeAdminRequest request)
        {
            try
            {
                _logger.LogInformation($"========== MAKE ADMIN ATTEMPT ==========");
                _logger.LogInformation($"UserId: {request?.UserId}");

                if (request == null || string.IsNullOrEmpty(request.UserId))
                {
                    return BadRequest(new { success = false, message = "Invalid request" });
                }

                // Find the user by ID
                var user = await _userManager.FindByIdAsync(request.UserId);
                if (user == null)
                {
                    _logger.LogWarning($"User not found with ID: {request.UserId}");
                    return NotFound(new { success = false, message = "User not found" });
                }

                _logger.LogInformation($"Found user: {user.Email}, Current Role: {user.Role}");

                // Check if user is already an Admin
                if (user.Role == "Admin")
                {
                    return BadRequest(new { success = false, message = "User is already an Admin" });
                }

                // Store the old role for logging
                var oldRole = user.Role;

                // ===== STEP 1: Update the custom Role field =====
                user.Role = "Admin";
                user.UpdatedAt = DateTime.Now;

                var updateResult = await _userManager.UpdateAsync(user);

                if (!updateResult.Succeeded)
                {
                    var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
                    _logger.LogError($"Failed to update user Role field: {errors}");
                    return BadRequest(new { success = false, message = $"Failed to update role: {errors}" });
                }

                // ===== STEP 2: Ensure Admin Identity Role exists =====
                if (!await _roleManager.RoleExistsAsync("Admin"))
                {
                    _logger.LogInformation("Creating Admin role as it doesn't exist");
                    await _roleManager.CreateAsync(new IdentityRole("Admin"));
                }

                // ===== STEP 3: Remove from any existing Identity Roles =====
                var currentRoles = await _userManager.GetRolesAsync(user);
                if (currentRoles.Any())
                {
                    _logger.LogInformation($"Removing user from existing roles: {string.Join(", ", currentRoles)}");
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);
                }

                // ===== STEP 4: Add to Admin Identity Role =====
                var addToRoleResult = await _userManager.AddToRoleAsync(user, "Admin");

                if (!addToRoleResult.Succeeded)
                {
                    var errors = string.Join(", ", addToRoleResult.Errors.Select(e => e.Description));
                    _logger.LogError($"Failed to add user to Admin role: {errors}");

                    // Try to revert the custom Role change if Identity role assignment fails
                    user.Role = oldRole;
                    await _userManager.UpdateAsync(user);

                    return BadRequest(new { success = false, message = $"Failed to assign Admin role: {errors}" });
                }

                // ===== STEP 5: Log the successful promotion =====
                var currentAdminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var currentAdmin = await _userManager.FindByIdAsync(currentAdminId);

                _logger.LogInformation($"USER PROMOTED TO ADMIN - User: {user.Email} ({user.Id}) was promoted by Admin: {currentAdmin?.Email} ({currentAdminId})");

                Console.WriteLine($"========== MAKE ADMIN SUCCESS ==========");
                Console.WriteLine($"User: {user.Email} is now an Admin");
                Console.WriteLine($"Previous Role: {oldRole}");
                Console.WriteLine($"Promoted by: {currentAdmin?.Email}");
                Console.WriteLine($"Time: {DateTime.Now}");
                Console.WriteLine($"========================================");

                return Ok(new
                {
                    success = true,
                    message = "User is now an Admin",
                    userEmail = user.Email,
                    newRole = "Admin"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"ERROR in MakeAdmin: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");

                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while processing your request",
                    error = ex.Message
                });
            }
        }

        // ===== Alternative simpler version if you only want to update the custom Role field =====
        [HttpPost]
        [Route("Admin/Users/MakeAdminSimple")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MakeAdminSimple([FromBody] string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                // Update just the custom Role field
                user.Role = "Admin";
                user.UpdatedAt = DateTime.Now;

                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    _logger.LogInformation($"User {user.Email} promoted to Admin (simple method)");
                    return Json(new { success = true, message = "User is now an Admin" });
                }

                return Json(new { success = false, message = "Failed to make user admin" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in MakeAdminSimple: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }
    }

    // Model for the Make Admin request
    public class MakeAdminRequest
    {
        public string UserId { get; set; }
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