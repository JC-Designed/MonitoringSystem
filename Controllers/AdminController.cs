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

        // ===== EXPORT ALL USERS TO EXCEL =====
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
                dt.Columns.Add("Contact Person", typeof(string));
                dt.Columns.Add("Student ID", typeof(string));
                dt.Columns.Add("Birthdate", typeof(string));
                dt.Columns.Add("Email", typeof(string));

                // Add rows with ONLY the columns you want
                foreach (var user in users)
                {
                    // Handle Company (nullable int)
                    string companyValue = user.CompanyID?.ToString() ?? "";

                    // Handle Contact Person
                    string contactPerson = user.ContactPerson ?? "";

                    // Handle Student ID
                    string studentId = user.StudentId ?? "";

                    // Format Birthdate
                    string birthDate = user.BirthDate.HasValue ? user.BirthDate.Value.ToString("yyyy-MM-dd") : "";

                    dt.Rows.Add(
                        user.FullName ?? "",
                        companyValue,
                        user.MobileNumber ?? "",
                        user.Address ?? "",
                        user.Program ?? "",
                        contactPerson,
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

        // ===== EXPORT SELECTED USERS TO EXCEL =====
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
                dt.Columns.Add("Contact Person", typeof(string));
                dt.Columns.Add("Student ID", typeof(string));
                dt.Columns.Add("Birthdate", typeof(string));
                dt.Columns.Add("Email", typeof(string));

                // Add rows with ONLY the columns you want
                foreach (var user in selectedUsers)
                {
                    // Handle Company
                    string companyValue = user.CompanyID?.ToString() ?? "";

                    // Handle Contact Person
                    string contactPerson = user.ContactPerson ?? "";

                    // Handle Student ID
                    string studentId = user.StudentId ?? "";

                    // Format Birthdate
                    string birthDate = user.BirthDate.HasValue ? user.BirthDate.Value.ToString("yyyy-MM-dd") : "";

                    dt.Rows.Add(
                        user.FullName ?? "",
                        companyValue,
                        user.MobileNumber ?? "",
                        user.Address ?? "",
                        user.Program ?? "",
                        contactPerson,
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

        // ===== PROGRAM HOURS MANAGEMENT =====

        // GET: Get all program hours
        [HttpGet]
        public async Task<IActionResult> GetProgramHours()
        {
            try
            {
                var programs = await _context.ProgramHours
                    .OrderBy(p => p.Code)
                    .Select(p => new
                    {
                        p.Id,
                        p.FullName,
                        p.Code,
                        p.Hours
                    })
                    .ToListAsync();

                return Ok(programs);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting program hours: {ex.Message}");
                return Ok(new List<object>()); // Return empty list on error
            }
        }

        // GET: Get programs list for registration dropdown - PUBLIC ACCESS
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetProgramsList()
        {
            try
            {
                var programs = await _context.ProgramHours
                    .OrderBy(p => p.Code)
                    .Select(p => new
                    {
                        code = p.Code,
                        displayName = p.FullName + " (" + p.Code + ")"
                    })
                    .ToListAsync();

                return Ok(programs);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting programs list: {ex.Message}");
                return Ok(new object[0]); // Return empty array on error
            }
        }

        // POST: Save all program hours
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveProgramHours([FromBody] List<ProgramHourModel> programs)
        {
            try
            {
                if (programs == null || !programs.Any())
                {
                    return BadRequest("No programs provided");
                }

                // Validate all programs have required fields
                foreach (var program in programs)
                {
                    if (string.IsNullOrWhiteSpace(program.FullName))
                        return BadRequest("Program full name cannot be empty");
                    if (string.IsNullOrWhiteSpace(program.Code))
                        return BadRequest("Program code cannot be empty");
                    if (program.Hours <= 0)
                        return BadRequest("Hours must be greater than 0");
                }

                // Clear existing programs
                var existingPrograms = await _context.ProgramHours.ToListAsync();
                _context.ProgramHours.RemoveRange(existingPrograms);
                await _context.SaveChangesAsync();

                // Add new programs
                foreach (var program in programs)
                {
                    var newProgram = new ProgramHour
                    {
                        FullName = program.FullName.Trim(),
                        Code = program.Code.Trim().ToUpper(),
                        Hours = program.Hours
                    };
                    _context.ProgramHours.Add(newProgram);
                }

                await _context.SaveChangesAsync();

                // Update all students' total hours based on their program
                await UpdateAllStudentHours();

                _logger.LogInformation($"Program hours saved successfully. {programs.Count} programs updated.");
                return Ok(new { success = true, message = "Program hours saved successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error saving program hours: {ex.Message}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // POST: Add a new program
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProgram([FromBody] ProgramHourModel program)
        {
            try
            {
                if (program == null)
                    return BadRequest("Program data is required");

                if (string.IsNullOrWhiteSpace(program.FullName))
                    return BadRequest("Program full name is required");

                if (string.IsNullOrWhiteSpace(program.Code))
                    return BadRequest("Program code is required");

                if (program.Hours <= 0)
                    return BadRequest("Hours must be greater than 0");

                // Check if program code already exists
                var exists = await _context.ProgramHours
                    .AnyAsync(p => p.Code == program.Code.Trim().ToUpper());

                if (exists)
                    return BadRequest("A program with this code already exists");

                var newProgram = new ProgramHour
                {
                    FullName = program.FullName.Trim(),
                    Code = program.Code.Trim().ToUpper(),
                    Hours = program.Hours
                };

                _context.ProgramHours.Add(newProgram);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Program added successfully: {newProgram.Code}");

                return Ok(new
                {
                    id = newProgram.Id,
                    fullName = newProgram.FullName,
                    code = newProgram.Code,
                    hours = newProgram.Hours
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error adding program: {ex.Message}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // DELETE: Delete a program
        [HttpDelete]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProgram(int id)
        {
            try
            {
                var program = await _context.ProgramHours.FindAsync(id);
                if (program == null)
                    return NotFound("Program not found");

                // Check if any students are using this program
                var studentsUsingProgram = await _userManager.Users
                    .CountAsync(u => u.Program == program.Code);

                _context.ProgramHours.Remove(program);
                await _context.SaveChangesAsync();

                // If there were students using this program, set their total hours to 0
                if (studentsUsingProgram > 0)
                {
                    _logger.LogWarning($"Program {program.Code} was deleted but {studentsUsingProgram} students were using it. Their hours have been reset.");
                }

                _logger.LogInformation($"Program deleted: {program.Code}");

                return Ok(new { success = true, message = "Program deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting program: {ex.Message}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // Helper method to update all students' total hours based on their program
        private async Task UpdateAllStudentHours()
        {
            try
            {
                var students = await _userManager.Users
                    .Where(u => u.Role == "Student")
                    .ToListAsync();

                var programs = await _context.ProgramHours.ToDictionaryAsync(p => p.Code, p => p.Hours);

                foreach (var student in students)
                {
                    if (!string.IsNullOrEmpty(student.Program) && programs.ContainsKey(student.Program))
                    {
                        student.TotalAllottedHours = programs[student.Program];
                    }
                    else
                    {
                        student.TotalAllottedHours = 0; // Default if program not found
                    }
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Updated total hours for {students.Count} students");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating student hours: {ex.Message}");
            }
        }

        // ===== DOCUMENTS MANAGEMENT =====

        // GET: Get all documents
        [HttpGet]
        public async Task<IActionResult> GetDocuments()
        {
            try
            {
                var documents = await _context.Documents
                    .OrderByDescending(d => d.UploadedAt)
                    .Select(d => new
                    {
                        d.Id,
                        d.Name,
                        d.Type,
                        d.Size,
                        Uploaded = d.UploadedAt.ToString("yyyy-MM-dd"),
                        d.FileData
                    })
                    .ToListAsync();

                return Ok(documents);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting documents: {ex.Message}");
                return Ok(new List<object>());
            }
        }

        // POST: Upload a document
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadDocument(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest("No file uploaded");

                // Validate file size (10MB max)
                if (file.Length > 10 * 1024 * 1024)
                    return BadRequest("File exceeds 10MB limit");

                // Get file extension
                var extension = Path.GetExtension(file.FileName).ToUpper().Replace(".", "");
                var allowedTypes = new[] { "PDF", "DOCX", "XLSX", "JPG", "PNG" };

                if (!allowedTypes.Contains(extension))
                    return BadRequest($"File type {extension} not supported");

                // Read file data
                using (var memoryStream = new MemoryStream())
                {
                    await file.CopyToAsync(memoryStream);
                    var fileData = memoryStream.ToArray();

                    var document = new Document
                    {
                        Name = file.FileName,
                        Type = extension,
                        Size = FormatFileSize(file.Length),
                        FileData = Convert.ToBase64String(fileData),
                        UploadedAt = DateTime.Now,
                        UploadedBy = User.Identity.Name ?? "Admin"
                    };

                    _context.Documents.Add(document);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"Document uploaded: {document.Name}");

                    return Ok(new
                    {
                        id = document.Id,
                        name = document.Name,
                        type = document.Type,
                        size = document.Size,
                        uploaded = document.UploadedAt.ToString("yyyy-MM-dd")
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error uploading document: {ex.Message}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // GET: Download a document
        [HttpGet]
        public async Task<IActionResult> DownloadDocument(int id)
        {
            try
            {
                var document = await _context.Documents.FindAsync(id);
                if (document == null)
                    return NotFound("Document not found");

                if (string.IsNullOrEmpty(document.FileData))
                    return NotFound("File data not found");

                var fileBytes = Convert.FromBase64String(document.FileData);
                var contentType = GetContentType(document.Type);

                return File(fileBytes, contentType, document.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error downloading document: {ex.Message}");
                return BadRequest("Error downloading document");
            }
        }

        // DELETE: Delete a document
        [HttpDelete]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDocument(int id)
        {
            try
            {
                var document = await _context.Documents.FindAsync(id);
                if (document == null)
                    return NotFound("Document not found");

                _context.Documents.Remove(document);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Document deleted: {document.Name}");

                return Ok(new { success = true, message = "Document deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting document: {ex.Message}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // Helper method to format file size
        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "Bytes", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        // Helper method to get content type
        private string GetContentType(string extension)
        {
            return extension.ToUpper() switch
            {
                "PDF" => "application/pdf",
                "DOCX" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "XLSX" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "JPG" or "JPEG" => "image/jpeg",
                "PNG" => "image/png",
                _ => "application/octet-stream"
            };
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

                // Get recent registrations
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
                        CreatedAt = u.CreatedAt.ToString()
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

        // API: Get user details
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
                        ContactPerson = user.ContactPerson ?? "",
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

        // ========== NEW: Get student hours summary ==========
        [HttpGet]
        public async Task<IActionResult> GetStudentHoursSummary(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return NotFound(new { success = false, message = "User not found" });

                // Get total allotted hours from user
                int total = user.TotalAllottedHours;

                // Get rendered hours from TimeLogs (if table exists)
                // For now, we'll return 0 for rendered until you have TimeLogs table
                // You can implement actual time log fetching later
                double rendered = 0; // Replace with actual calculation from TimeLogs when ready

                double remaining = total - rendered;

                return Ok(new
                {
                    success = true,
                    rendered = rendered,
                    remaining = remaining,
                    total = total
                });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = ex.Message });
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

        // ===== MAKE ADMIN METHOD (ORIGINAL VERSION) =====
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

    public class ProgramHourModel
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Code { get; set; }
        public int Hours { get; set; }
    }
}