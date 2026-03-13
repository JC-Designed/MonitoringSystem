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
using ClosedXML.Excel;
using System.Data;
using System.IO;

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

        // ===== EXPORT ALL USERS TO EXCEL - WITH YOUR SPECIFIC COLUMNS =====
        public async Task<IActionResult> ExportUsersToExcel()
        {
            try
            {
                _logger.LogInformation("========== EXPORT ALL USERS TO EXCEL ==========");

                // Get all approved users
                var users = await _userManager.Users
                    .Where(u => u.Status == "Approved")
                    .OrderBy(u => u.FullName)
                    .ToListAsync();

                // Create a DataTable with ONLY the columns you want
                DataTable dt = new DataTable();
                dt.Columns.Add("Name", typeof(string));
                dt.Columns.Add("Company", typeof(string));
                dt.Columns.Add("Mobile Number", typeof(string));
                dt.Columns.Add("Address", typeof(string));
                dt.Columns.Add("Program", typeof(string));
                dt.Columns.Add("Contact Person", typeof(string));  // CHANGED: from "Emergency Contact" to "Contact Person"
                dt.Columns.Add("Student ID", typeof(string));
                dt.Columns.Add("Birthdate", typeof(string));
                dt.Columns.Add("Email", typeof(string));

                // Add rows with ONLY the columns you want
                foreach (var user in users)
                {
                    // Handle Company (nullable int)
                    string companyValue = user.CompanyID?.ToString() ?? "";

                    // Handle Contact Person - UPDATED: Use ContactPerson property
                    string contactPerson = user.ContactPerson ?? "";  // CHANGED: from Contact to ContactPerson

                    // Handle Student ID
                    string studentId = user.StudentId ?? "";

                    // Format Birthdate - FIXED: Handle nullable DateTime properly
                    string birthDate = user.BirthDate.HasValue ? user.BirthDate.Value.ToString("yyyy-MM-dd") : "";

                    dt.Rows.Add(
                        user.FullName ?? "",
                        companyValue,
                        user.MobileNumber ?? "",
                        user.Address ?? "",
                        user.Program ?? "",
                        contactPerson,  // UPDATED: variable name changed
                        studentId,
                        birthDate,
                        user.Email ?? ""
                    );
                }

                // Create Excel file using ClosedXML
                using (XLWorkbook wb = new XLWorkbook())
                {
                    var worksheet = wb.Worksheets.Add(dt, "Users");

                    // Format the worksheet
                    worksheet.Columns().AdjustToContents();

                    // Style the header row
                    var headerRow = worksheet.Row(1);
                    headerRow.Style.Font.Bold = true;
                    headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;

                    // Prepare the file for download
                    using (MemoryStream stream = new MemoryStream())
                    {
                        wb.SaveAs(stream);
                        string fileName = $"Users_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                        _logger.LogInformation($"Export completed: {users.Count} users exported to {fileName}");

                        return File(stream.ToArray(),
                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                            fileName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"ERROR in ExportUsersToExcel: {ex.Message}");
                return BadRequest("An error occurred while exporting users.");
            }
        }

        // ===== EXPORT SELECTED USERS TO EXCEL - WITH YOUR SPECIFIC COLUMNS =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExportSelectedUsers(List<string> userIds)
        {
            try
            {
                _logger.LogInformation("========== EXPORT SELECTED USERS TO EXCEL ==========");

                if (userIds == null || userIds.Count == 0)
                {
                    _logger.LogWarning("No users selected for export");
                    return BadRequest("No users selected");
                }

                _logger.LogInformation($"Selected {userIds.Count} users for export");

                // Get selected users from database
                var selectedUsers = await _userManager.Users
                    .Where(u => userIds.Contains(u.Id))
                    .OrderBy(u => u.FullName)
                    .ToListAsync();

                // Create DataTable with ONLY the columns you want
                DataTable dt = new DataTable();
                dt.Columns.Add("Name", typeof(string));
                dt.Columns.Add("Company", typeof(string));
                dt.Columns.Add("Mobile Number", typeof(string));
                dt.Columns.Add("Address", typeof(string));
                dt.Columns.Add("Program", typeof(string));
                dt.Columns.Add("Contact Person", typeof(string));  // CHANGED: from "Emergency Contact" to "Contact Person"
                dt.Columns.Add("Student ID", typeof(string));
                dt.Columns.Add("Birthdate", typeof(string));
                dt.Columns.Add("Email", typeof(string));

                // Add rows with ONLY the columns you want
                foreach (var user in selectedUsers)
                {
                    // Handle Company
                    string companyValue = user.CompanyID?.ToString() ?? "";

                    // Handle Contact Person - UPDATED: Use ContactPerson property
                    string contactPerson = user.ContactPerson ?? "";  // CHANGED: from Contact to ContactPerson

                    // Handle Student ID
                    string studentId = user.StudentId ?? "";

                    // Format Birthdate - FIXED: Handle nullable DateTime properly
                    string birthDate = user.BirthDate.HasValue ? user.BirthDate.Value.ToString("yyyy-MM-dd") : "";

                    dt.Rows.Add(
                        user.FullName ?? "",
                        companyValue,
                        user.MobileNumber ?? "",
                        user.Address ?? "",
                        user.Program ?? "",
                        contactPerson,  // UPDATED: variable name changed
                        studentId,
                        birthDate,
                        user.Email ?? ""
                    );
                }

                // Create Excel file
                using (XLWorkbook wb = new XLWorkbook())
                {
                    var worksheet = wb.Worksheets.Add(dt, "Selected Users");

                    // Format the worksheet
                    worksheet.Columns().AdjustToContents();

                    // Style the header row
                    var headerRow = worksheet.Row(1);
                    headerRow.Style.Font.Bold = true;
                    headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;

                    using (MemoryStream stream = new MemoryStream())
                    {
                        wb.SaveAs(stream);
                        string fileName = $"SelectedUsers_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                        _logger.LogInformation($"Export completed: {selectedUsers.Count} users exported to {fileName}");

                        return File(stream.ToArray(),
                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                            fileName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"ERROR in ExportSelectedUsers: {ex.Message}");
                return BadRequest(new { success = false, message = "An error occurred while exporting users." });
            }
        }

        // GET: Admin Registration page
        public async Task<IActionResult> Registration()
        {
            Console.WriteLine("========== REGISTRATION ACTION HIT ==========");

            try
            {
                // Get all users ordered by Status (Pending first) then by registration date
                var users = await _userManager.Users
                    .OrderBy(u => u.Status == "Pending" ? 0 : 1)
                    .ThenByDescending(u => u.CreatedAt)
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

        // API: Get dashboard data for charts
        [HttpGet]
        public async Task<IActionResult> GetDashboardData(int year)
        {
            try
            {
                Console.WriteLine($"========== GET DASHBOARD DATA FOR YEAR {year} ==========");

                var totalUsers = await _userManager.Users.CountAsync();
                var totalCompanies = await _userManager.Users.CountAsync(u => u.Role == "Company");
                var totalStudents = await _userManager.Users.CountAsync(u => u.Role == "Student");
                var pendingApprovals = await _userManager.Users.CountAsync(u => u.Status == "Pending" && u.Role != "Admin");
                var approvedUsers = await _userManager.Users.CountAsync(u => u.Status == "Approved" && u.Role != "Admin");
                var declinedUsers = await _userManager.Users.CountAsync(u => u.Status == "Declined" && u.Role != "Admin");

                // Get recent registrations - FIXED: Handle nullable DateTime
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
                        CreatedAt = u.CreatedAt.ToString()  // This works
                    })
                    .ToListAsync();

                var pendingUsers = new List<int>();
                var tasksSubmitted = new List<int>();
                var monthlyUsers = new List<int>();
                var monthlyCompanies = new List<int>();

                for (int month = 1; month <= 12; month++)
                {
                    var startDate = new DateTime(year, month, 1);
                    var endDate = startDate.AddMonths(1).AddDays(-1);

                    var pendingCount = await _userManager.Users
                        .CountAsync(u => u.Status == "Pending"
                            && u.Role != "Admin"
                            && u.CreatedAt >= startDate
                            && u.CreatedAt <= endDate);
                    pendingUsers.Add(pendingCount);

                    tasksSubmitted.Add(pendingCount + new Random().Next(1, 5));

                    var usersCount = await _userManager.Users
                        .CountAsync(u => u.Role != "Admin"
                            && u.CreatedAt >= startDate
                            && u.CreatedAt <= endDate);
                    monthlyUsers.Add(usersCount);

                    var companiesCount = await _userManager.Users
                        .CountAsync(u => u.Role == "Company"
                            && u.CreatedAt >= startDate
                            && u.CreatedAt <= endDate);
                    monthlyCompanies.Add(companiesCount);
                }

                var data = new
                {
                    pendingUsers = pendingUsers,
                    tasksSubmitted = tasksSubmitted,
                    totalUsers = monthlyUsers,
                    totalCompanies = monthlyCompanies,
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

        // API: Update user status
        [HttpPost]
        public async Task<IActionResult> UpdateUserStatus([FromBody] UpdateStatusModel model)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(model.UserId);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                user.Status = model.Status;
                user.UpdatedAt = DateTime.Now;

                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
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
                return Json(new { success = false, message = ex.Message });
            }
        }

        // API: Get pending users - FIXED: Handle nullable DateTime
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
                        CreatedAt = u.CreatedAt.ToString(),
                        BirthDate = u.BirthDate.ToString()
                    })
                    .ToListAsync();

                return Json(new { success = true, users = pendingUsers });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // API: Get user details - UPDATED: Use ContactPerson property
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
                        BirthDate = user.BirthDate.ToString(),
                        user.MobileNumber,
                        user.Address,
                        user.Program,
                        Year = user.Year?.ToString() ?? "",
                        CompanyID = user.CompanyID?.ToString() ?? "",
                        ContactPerson = user.ContactPerson ?? "",  // CHANGED: from Contact to ContactPerson
                        user.ProfileImage,
                        CreatedAt = user.CreatedAt.ToString(),
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

            var admin = await _context.Admins
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.UserId == userId);

            if (admin == null)
            {
                return Json(new { success = false, message = "Admin not found" });
            }

            return Json(new
            {
                success = true,
                fullName = admin.User?.FullName ?? "",
                employeeId = admin.EmployeeId ?? "",
                department = admin.Department ?? "",
                position = admin.Position ?? "",
                hireDate = admin.HireDate?.ToString() ?? "",
                permissionsLevel = admin.PermissionsLevel ?? "Basic",
                officeLocation = admin.OfficeLocation ?? "",
                officePhone = admin.OfficePhone ?? "",
                createdAt = admin.CreatedAt.ToString(),
                username = admin.User?.UserName ?? "",
                email = admin.User?.Email ?? "",
                address = admin.User?.Address ?? "",
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
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Invalid data" });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var admin = await _context.Admins
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.UserId == userId);

            if (admin == null)
            {
                return NotFound(new { success = false, message = "Admin not found" });
            }

            if (admin.User != null)
            {
                admin.User.FullName = model.FullName;
                admin.User.Address = model.Address;
            }

            admin.Department = model.Department;
            admin.Position = model.Position;
            admin.OfficeLocation = model.OfficeLocation;
            admin.OfficePhone = model.OfficePhone;
            admin.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

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

        // ===== MAKE ADMIN METHOD =====
        [HttpPost]
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

                var user = await _userManager.FindByIdAsync(request.UserId);
                if (user == null)
                {
                    return NotFound(new { success = false, message = "User not found" });
                }

                if (user.Role == "Admin")
                {
                    return BadRequest(new { success = false, message = "User is already an Admin" });
                }

                var oldRole = user.Role;

                user.Role = "Admin";
                user.UpdatedAt = DateTime.Now;

                var updateResult = await _userManager.UpdateAsync(user);

                if (!updateResult.Succeeded)
                {
                    var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
                    return BadRequest(new { success = false, message = $"Failed to update role: {errors}" });
                }

                if (!await _roleManager.RoleExistsAsync("Admin"))
                {
                    await _roleManager.CreateAsync(new IdentityRole("Admin"));
                }

                var currentRoles = await _userManager.GetRolesAsync(user);
                if (currentRoles.Any())
                {
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);
                }

                var addToRoleResult = await _userManager.AddToRoleAsync(user, "Admin");

                if (!addToRoleResult.Succeeded)
                {
                    var errors = string.Join(", ", addToRoleResult.Errors.Select(e => e.Description));
                    user.Role = oldRole;
                    await _userManager.UpdateAsync(user);
                    return BadRequest(new { success = false, message = $"Failed to assign Admin role: {errors}" });
                }

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
                return StatusCode(500, new { success = false, message = "An error occurred" });
            }
        }
    }

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

    public class UpdateStatusModel
    {
        public string UserId { get; set; }
        public string Status { get; set; }
    }
}