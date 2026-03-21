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
using Task = System.Threading.Tasks.Task; // Add this alias to fix ambiguity

namespace MonitoringSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<AdminController> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<AdminController> logger,
            IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
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

        // ===== EXPORT ALL STUDENTS TO EXCEL - GROUPED BY PROGRAM WITH HOURS =====
        public async Task<IActionResult> ExportUsersToExcel()
        {
            try
            {
                _logger.LogInformation("========== EXPORT STUDENTS GROUPED BY PROGRAM WITH HOURS ==========");

                // Get ONLY approved students
                var students = await _userManager.Users
                    .Where(u => u.Status == "Approved" && u.Role == "Student")
                    .OrderBy(u => u.Program)
                    .ThenBy(u => u.FullName)
                    .ToListAsync();

                _logger.LogInformation($"Found {students.Count} students to export");

                // Group students by program
                var studentsByProgram = students
                    .GroupBy(s => s.Program ?? "No Program")
                    .OrderBy(g => g.Key)
                    .ToDictionary(g => g.Key, g => g.ToList());

                // Create Excel file
                using (XLWorkbook wb = new XLWorkbook())
                {
                    // Create a worksheet for each program
                    foreach (var programGroup in studentsByProgram)
                    {
                        string programName = string.IsNullOrEmpty(programGroup.Key) ? "No Program" : programGroup.Key;

                        // Clean worksheet name (remove invalid characters for Excel sheet names)
                        programName = string.Join("", programName.Split(Path.GetInvalidFileNameChars()));
                        if (programName.Length > 31) programName = programName.Substring(0, 31); // Excel sheet name max length

                        var worksheet = wb.Worksheets.Add(programName);

                        // Add headers - NOW INCLUDING HOURS COLUMNS
                        worksheet.Cell(1, 1).Value = "Name";
                        worksheet.Cell(1, 2).Value = "Mobile Number";
                        worksheet.Cell(1, 3).Value = "Address";
                        worksheet.Cell(1, 4).Value = "Program";
                        worksheet.Cell(1, 5).Value = "Contact Person";
                        worksheet.Cell(1, 6).Value = "Student ID";
                        worksheet.Cell(1, 7).Value = "Birthdate";
                        worksheet.Cell(1, 8).Value = "Email";
                        worksheet.Cell(1, 9).Value = "Total Allotted Hours";
                        worksheet.Cell(1, 10).Value = "Rendered Hours";
                        worksheet.Cell(1, 11).Value = "Remaining Hours";

                        // Style header row
                        var headerRow = worksheet.Row(1);
                        headerRow.Style.Font.Bold = true;
                        headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;

                        // Add data rows
                        int row = 2;

                        foreach (var student in programGroup.Value)
                        {
                            // For now, rendered hours is 0 until you have TimeLogs table
                            // You can replace this with actual calculation when ready
                            double renderedHours = 0;
                            double remainingHours = student.TotalAllottedHours - renderedHours;

                            worksheet.Cell(row, 1).Value = student.FullName ?? "";
                            worksheet.Cell(row, 2).Value = student.MobileNumber ?? "";
                            worksheet.Cell(row, 3).Value = student.Address ?? "";
                            worksheet.Cell(row, 4).Value = student.Program ?? "";
                            worksheet.Cell(row, 5).Value = student.ContactPerson ?? "";
                            worksheet.Cell(row, 6).Value = student.StudentId ?? "";
                            worksheet.Cell(row, 7).Value = student.BirthDate.HasValue ? student.BirthDate.Value.ToString("yyyy-MM-dd") : "";
                            worksheet.Cell(row, 8).Value = student.Email ?? "";
                            worksheet.Cell(row, 9).Value = student.TotalAllottedHours;
                            worksheet.Cell(row, 10).Value = renderedHours;
                            worksheet.Cell(row, 11).Value = remainingHours;

                            // Format hours columns as numbers
                            worksheet.Cell(row, 9).Style.NumberFormat.Format = "#,##0";
                            worksheet.Cell(row, 10).Style.NumberFormat.Format = "#,##0.0";
                            worksheet.Cell(row, 11).Style.NumberFormat.Format = "#,##0.0";

                            row++;
                        }

                        // Add ONLY total students count at the bottom (no other summary)
                        row += 1; // Add a blank row
                        worksheet.Cell(row, 1).Value = $"Total Students: {programGroup.Value.Count}";
                        worksheet.Cell(row, 1).Style.Font.Bold = true;
                        worksheet.Cell(row, 1).Style.Font.FontColor = XLColor.Blue;

                        // Auto-fit columns
                        worksheet.Columns().AdjustToContents();
                    }

                    // Prepare the file for download
                    using (MemoryStream stream = new MemoryStream())
                    {
                        wb.SaveAs(stream);
                        string fileName = $"Students_With_Hours_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                        _logger.LogInformation($"Export completed: {students.Count} students exported to {fileName}");

                        return File(stream.ToArray(),
                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                            fileName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"ERROR in ExportUsersToExcel: {ex.Message}");
                return BadRequest("An error occurred while exporting students.");
            }
        }

        // ===== EXPORT SELECTED USERS TO EXCEL =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExportSelectedUsers(List<string> userIds)
        {
            try
            {
                _logger.LogInformation("========== EXPORT SELECTED STUDENTS TO EXCEL ==========");

                if (userIds == null || userIds.Count == 0)
                {
                    _logger.LogWarning("No students selected for export");
                    return BadRequest("No students selected");
                }

                _logger.LogInformation($"Selected {userIds.Count} students for export");

                // Get selected students from database (only students)
                var selectedStudents = await _userManager.Users
                    .Where(u => userIds.Contains(u.Id) && u.Role == "Student") // Only students
                    .OrderBy(u => u.FullName)
                    .ToListAsync();

                // Create DataTable with ONLY the columns you want
                DataTable dt = new DataTable();
                dt.Columns.Add("Name", typeof(string));
                dt.Columns.Add("Mobile Number", typeof(string));
                dt.Columns.Add("Address", typeof(string));
                dt.Columns.Add("Program", typeof(string));
                dt.Columns.Add("Contact Person", typeof(string));
                dt.Columns.Add("Student ID", typeof(string));
                dt.Columns.Add("Birthdate", typeof(string));
                dt.Columns.Add("Email", typeof(string));
                dt.Columns.Add("Total Allotted Hours", typeof(int));
                dt.Columns.Add("Rendered Hours", typeof(double));
                dt.Columns.Add("Remaining Hours", typeof(double));

                // Add rows with ONLY the columns you want
                foreach (var student in selectedStudents)
                {
                    // Handle Contact Person
                    string contactPerson = student.ContactPerson ?? "";

                    // Handle Student ID
                    string studentId = student.StudentId ?? "";

                    // Format Birthdate
                    string birthDate = student.BirthDate.HasValue ? student.BirthDate.Value.ToString("yyyy-MM-dd") : "";

                    // Hours (rendered is 0 for now)
                    double renderedHours = 0;
                    double remainingHours = student.TotalAllottedHours - renderedHours;

                    dt.Rows.Add(
                        student.FullName ?? "",
                        student.MobileNumber ?? "",
                        student.Address ?? "",
                        student.Program ?? "",
                        contactPerson,
                        studentId,
                        birthDate,
                        student.Email ?? "",
                        student.TotalAllottedHours,
                        renderedHours,
                        remainingHours
                    );
                }

                // Create Excel file
                using (XLWorkbook wb = new XLWorkbook())
                {
                    var worksheet = wb.Worksheets.Add(dt, "Selected Students");

                    // Format the worksheet
                    worksheet.Columns().AdjustToContents();

                    // Style the header row
                    var headerRow = worksheet.Row(1);
                    headerRow.Style.Font.Bold = true;
                    headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;

                    using (MemoryStream stream = new MemoryStream())
                    {
                        wb.SaveAs(stream);
                        string fileName = $"SelectedStudents_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                        _logger.LogInformation($"Export completed: {selectedStudents.Count} students exported to {fileName}");

                        return File(stream.ToArray(),
                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                            fileName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"ERROR in ExportSelectedUsers: {ex.Message}");
                return BadRequest(new { success = false, message = "An error occurred while exporting students." });
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

        // ===== TASKS MANAGEMENT =====

        // GET: Get all tasks for a student
        [HttpGet]
        public async Task<IActionResult> GetStudentTasks(string studentId)
        {
            try
            {
                var tasks = await _context.Tasks
                    .Where(t => t.StudentId == studentId)
                    .OrderByDescending(t => t.CreatedAt)
                    .Select(t => new
                    {
                        t.Id,
                        t.Title,
                        t.Description,
                        t.Status,
                        t.CreatedAt,
                        t.StudentId
                    })
                    .ToListAsync();

                return Ok(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting student tasks: {ex.Message}");
                return Ok(new List<object>());
            }
        }

        // POST: Upload a task
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadTask(IFormFile file, string title, string studentId)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest("No file uploaded");

                if (string.IsNullOrEmpty(title))
                    return BadRequest("Title is required");

                // Validate file size (10MB max)
                if (file.Length > 10 * 1024 * 1024)
                    return BadRequest("File exceeds 10MB limit");

                // Read file data and convert to Base64
                using (var memoryStream = new MemoryStream())
                {
                    await file.CopyToAsync(memoryStream);
                    var fileBytes = memoryStream.ToArray();
                    var fileData = Convert.ToBase64String(fileBytes);

                    // Generate unique filename
                    var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);

                    var task = new MonitoringSystem.Models.Task
                    {
                        Title = title,
                        FileName = uniqueFileName,
                        FileData = fileData,
                        Status = "Unread",
                        CreatedAt = DateTime.Now,
                        StudentId = studentId
                    };

                    _context.Tasks.Add(task);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"Task uploaded for student {studentId}: {title}");

                    return Ok(new
                    {
                        task.Id,
                        task.Title,
                        task.Status,
                        task.CreatedAt,
                        task.StudentId
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error uploading task: {ex.Message}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
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
                        Uploaded = d.UploadedAt
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

                // Get file extension and validate
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                var allowedExtensions = new[] { ".pdf", ".docx", ".xlsx", ".jpg", ".jpeg", ".png" };

                if (!allowedExtensions.Contains(extension))
                {
                    return BadRequest("File type not allowed. Allowed types: PDF, DOCX, XLSX, JPG, PNG");
                }

                // Read file data and convert to Base64
                using (var memoryStream = new MemoryStream())
                {
                    await file.CopyToAsync(memoryStream);
                    var fileBytes = memoryStream.ToArray();
                    var fileData = Convert.ToBase64String(fileBytes);

                    // Format file size
                    string formattedSize = FormatFileSize(file.Length);

                    // Generate unique filename
                    var uniqueFileName = Guid.NewGuid().ToString() + extension;

                    // Save to database - using the model properties
                    var document = new Document
                    {
                        Name = file.FileName,
                        FileName = uniqueFileName,
                        Type = extension.TrimStart('.').ToUpper(),
                        Size = formattedSize,
                        FileData = fileData,
                        UploadedAt = DateTime.Now,
                        UploadedBy = User.Identity?.Name ?? "Admin"
                    };

                    _context.Documents.Add(document);

                    try
                    {
                        await _context.SaveChangesAsync();
                    }
                    catch (DbUpdateException ex)
                    {
                        // Get the inner exception details
                        var innerException = ex.InnerException?.Message ?? "No inner exception";
                        var errorMessage = $"Database error: {ex.Message}. Inner: {innerException}";
                        _logger.LogError(errorMessage);
                        return StatusCode(500, errorMessage);
                    }

                    _logger.LogInformation($"Document uploaded: {document.Name}");

                    return Ok(new
                    {
                        document.Id,
                        document.Name,
                        document.Type,
                        document.Size,
                        Uploaded = document.UploadedAt
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

                // Convert Base64 string back to bytes
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

        // ===== DELETE DOCUMENT =====
        [HttpDelete]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDocument(int id)
        {
            try
            {
                var document = await _context.Documents.FindAsync(id);
                if (document == null)
                {
                    return Json(new { success = false, message = "Document not found." });
                }

                _context.Documents.Remove(document);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Document deleted: {document.Name}");

                return Json(new { success = true, message = "Document deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting document: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ===== FIXED: DELETE STUDENT TIME LOG WITH IMPROVED ERROR HANDLING =====
        [HttpDelete]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTimeLogSubmission(string id)
        {
            try
            {
                _logger.LogInformation($"Attempting to delete time log submission with ID: {id}");

                if (string.IsNullOrEmpty(id))
                {
                    _logger.LogWarning("Delete attempted with null or empty ID");
                    return Json(new { success = false, message = "Invalid ID provided" });
                }

                var submission = await _context.StudentTimeLogs.FindAsync(id);

                if (submission == null)
                {
                    _logger.LogWarning($"Time log submission not found with ID: {id}");
                    return Json(new { success = false, message = "Time log submission not found." });
                }

                _context.StudentTimeLogs.Remove(submission);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Time log submission deleted successfully: ID {id}");

                return Json(new { success = true, message = "Time log submission deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting time log submission ID {id}: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");

                // Check for inner exception
                if (ex.InnerException != null)
                {
                    _logger.LogError($"Inner exception: {ex.InnerException.Message}");
                }

                return Json(new { success = false, message = $"Server error: {ex.Message}" });
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

        // GET: Admin Reports page - UPDATED WITH CACHE HEADERS
        public IActionResult Reports()
        {
            Console.WriteLine("========== REPORTS ACTION HIT ==========");

            // Add cache headers to prevent caching
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";

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

        // API: Get dashboard data for charts - UPDATED WITH REPORTS DATA
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
                var reportsSubmitted = new List<int>(); // NEW: For reports data
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

                    // Tasks submitted count
                    var tasksCount = await _context.StudentTasks
                        .CountAsync(t => t.DateFrom >= startDate && t.DateFrom <= endDate);
                    tasksSubmitted.Add(tasksCount);

                    // NEW: Reports submitted count
                    var reportsCount = await _context.Reports
                        .CountAsync(r => r.DateFrom >= startDate && r.DateFrom <= endDate);
                    reportsSubmitted.Add(reportsCount);

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
                    reportsSubmitted = reportsSubmitted, // NEW: Include reports data
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

        // ===== GET COMPANY DETAILS FOR EDITING =====
        [HttpGet]
        public async Task<IActionResult> GetCompanyDetails(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return Json(new { success = false, message = "User not found" });

                var company = await _context.Companies
                    .FirstOrDefaultAsync(c => c.UserId == user.Id);

                return Json(new
                {
                    success = true,
                    // User info
                    fullName = user.FullName,
                    email = user.Email,
                    mobileNumber = user.MobileNumber,
                    address = user.Address,
                    profileImage = user.ProfileImage,
                    bannerImage = user.BannerImage,

                    // Company info
                    companyName = company?.CompanyName,
                    companyId = company?.CompanyId,
                    companyDescription = company?.CompanyDescription,
                    industry = company?.Industry,
                    website = company?.Website,

                    // Contact person
                    contactPersonName = company?.ContactPersonName,
                    contactPersonPosition = company?.ContactPersonPosition
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ===== UPDATE COMPANY DETAILS =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCompany([FromBody] UpdateCompanyModel model)
        {
            try
            {
                // Find the user by email
                var user = await _userManager.FindByEmailAsync(model.email);
                if (user == null)
                    return Json(new { success = false, message = "User not found" });

                // Update user information
                user.FullName = model.fullName ?? user.FullName;
                user.MobileNumber = model.userMobile ?? user.MobileNumber;
                user.Address = model.address ?? user.Address;
                user.UpdatedAt = DateTime.Now;

                await _userManager.UpdateAsync(user);

                // Update company information
                var company = await _context.Companies
                    .FirstOrDefaultAsync(c => c.UserId == user.Id);

                if (company != null)
                {
                    company.CompanyName = model.companyName ?? company.CompanyName;
                    company.Industry = model.industry ?? company.Industry;
                    company.Website = model.website ?? company.Website;
                    company.CompanyDescription = model.companyDescription ?? company.CompanyDescription;
                    company.ContactPersonName = model.contactPersonName ?? company.ContactPersonName;
                    company.ContactPersonPosition = model.contactPersonPosition ?? company.ContactPersonPosition;
                    company.UpdatedAt = DateTime.Now;

                    await _context.SaveChangesAsync();
                }

                return Json(new { success = true, message = "Company updated successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ========== Get student hours summary ==========
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

        // API: Delete user - UPDATED WITH CASCADING DELETE
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

                // First, delete all related data for this user
                try
                {
                    // Delete Reports and their physical files
                    var reports = await _context.Reports.Where(r => r.StudentId == user.Id).ToListAsync();
                    if (reports.Any())
                    {
                        foreach (var report in reports)
                        {
                            if (!string.IsNullOrEmpty(report.FilePath))
                            {
                                var filePath = Path.Combine(_webHostEnvironment.WebRootPath, report.FilePath.TrimStart('/'));
                                if (System.IO.File.Exists(filePath))
                                {
                                    System.IO.File.Delete(filePath);
                                }
                            }
                        }
                        _context.Reports.RemoveRange(reports);
                    }

                    // Delete StudentTasks
                    var studentTasks = await _context.StudentTasks.Where(t => t.UserId == user.Id).ToListAsync();
                    if (studentTasks.Any())
                    {
                        _context.StudentTasks.RemoveRange(studentTasks);
                    }

                    // Delete TimeLogs
                    var timeLogs = await _context.TimeLogs.Where(t => t.UserId == user.Id).ToListAsync();
                    if (timeLogs.Any())
                    {
                        _context.TimeLogs.RemoveRange(timeLogs);
                    }

                    // Delete TimeLogSubmissions
                    var timeLogSubmissions = await _context.StudentTimeLogs.Where(t => t.StudentId == user.Id).ToListAsync();
                    if (timeLogSubmissions.Any())
                    {
                        _context.StudentTimeLogs.RemoveRange(timeLogSubmissions);
                    }

                    // Delete Tasks (assigned tasks)
                    var tasks = await _context.Tasks.Where(t => t.StudentId == user.Id).ToListAsync();
                    if (tasks.Any())
                    {
                        _context.Tasks.RemoveRange(tasks);
                    }

                    // Delete Documents uploaded by this user
                    var documents = await _context.Documents.Where(d => d.UploadedBy == user.Id).ToListAsync();
                    if (documents.Any())
                    {
                        _context.Documents.RemoveRange(documents);
                    }

                    // If user is a Company, delete company record
                    if (user.Role == "Company")
                    {
                        var company = await _context.Companies.FirstOrDefaultAsync(c => c.UserId == user.Id);
                        if (company != null)
                        {
                            _context.Companies.Remove(company);
                        }
                    }

                    // If user is a Student, delete student record
                    if (user.Role == "Student")
                    {
                        var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == user.Id);
                        if (student != null)
                        {
                            _context.Students.Remove(student);
                        }
                    }

                    // If user is an Admin, delete admin record
                    if (user.Role == "Admin")
                    {
                        var admin = await _context.Admins.FirstOrDefaultAsync(a => a.UserId == user.Id);
                        if (admin != null)
                        {
                            _context.Admins.Remove(admin);
                        }
                    }

                    // Save all changes before deleting the user
                    await _context.SaveChangesAsync();

                    // Finally, delete the user
                    var result = await _userManager.DeleteAsync(user);
                    if (result.Succeeded)
                    {
                        _logger.LogInformation($"User deleted successfully: {user.Email}");
                        return Json(new { success = true, message = "User deleted successfully" });
                    }

                    return Json(new { success = false, message = "Failed to delete user: " + string.Join(", ", result.Errors.Select(e => e.Description)) });
                }
                catch (DbUpdateException ex)
                {
                    var innerException = ex.InnerException?.Message ?? "No inner exception";
                    _logger.LogError($"Database error while deleting user {user.Email}: {ex.Message}. Inner: {innerException}");
                    return Json(new { success = false, message = $"Database error: {innerException}" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting user: {ex.Message}");
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

        // ===== UPDATED: METHOD FOR UPDATING STUDENT TIME LOG STATUS (using StudentTimeLogs) =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSubmissionStatus([FromBody] UpdateSubmissionStatusModel model)
        {
            try
            {
                var submission = await _context.StudentTimeLogs
                    .FirstOrDefaultAsync(s => s.Id == model.Id);

                if (submission == null)
                {
                    return Json(new { success = false, message = "Submission not found" });
                }

                submission.Status = model.Status;
                submission.AdminRemarks = model.Remarks;
                submission.ProcessedDate = DateTime.Now;
                submission.IsRead = true;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = $"Submission {model.Status.ToLower()} successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating submission status: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
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

    // Model for updating company details - WITHOUT ContactPersonMobile
    public class UpdateCompanyModel
    {
        public string companyName { get; set; }
        public string mobileNumber { get; set; }
        public string address { get; set; }
        public string companyId { get; set; }
        public string industry { get; set; }
        public string website { get; set; }
        public string companyDescription { get; set; }
        public string contactPersonName { get; set; }
        public string contactPersonPosition { get; set; }
        public string fullName { get; set; }
        public string email { get; set; }
        public string userMobile { get; set; }
    }

    // ===== MODEL FOR SUBMISSION STATUS UPDATE =====
    public class UpdateSubmissionStatusModel
    {
        public string Id { get; set; }
        public string Status { get; set; }
        public string Remarks { get; set; }
    }
}