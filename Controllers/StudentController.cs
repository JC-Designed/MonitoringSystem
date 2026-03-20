using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonitoringSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Security.Claims;
using System;
using System.IO;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;

namespace MonitoringSystem.Controllers
{
    [Authorize]
    public class StudentController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<StudentController> _logger;

        public StudentController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            ILogger<StudentController> logger)
        {
            _userManager = userManager;
            _context = context;
            _logger = logger;
        }

        // GET: /Student/Dashboard
        public IActionResult Dashboard()
        {
            return View();
        }

        // GET: /Student/Tasks
        public IActionResult Tasks()
        {
            return View();
        }

        // GET: /Student/Report
        public IActionResult Report()
        {
            return View();
        }

        // ===================== TEST REPORT ACTION =====================
        // GET: /Student/TestReport
        public IActionResult TestReport()
        {
            return View();
        }

        // ===================== OJT REPORT ACTION =====================
        // GET: /Student/OJTReport
        public IActionResult OJTReport()
        {
            return View();
        }

        // ===================== TASK MANAGEMENT =====================
        // GET: Student/GetTasks
        [HttpGet]
        public async Task<IActionResult> GetTasks()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var tasks = await _context.StudentTasks
                    .Where(t => t.UserId == userId)
                    .OrderByDescending(t => t.DateFrom)
                    .Select(t => new
                    {
                        t.Id,
                        t.Title,
                        DateFrom = t.DateFrom.ToString("yyyy-MM-dd"),
                        DateTo = t.DateTo.ToString("yyyy-MM-dd"),
                        t.Status,
                        AdminStatus = "Unread",
                        t.TaskContent,
                        t.LearningContent
                    })
                    .ToListAsync();

                return Ok(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting tasks: {ex.Message}");
                return Ok(new List<object>());
            }
        }

        // POST: Student/CreateTask
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTask([FromBody] TaskViewModel model)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest(new { success = false, message = "User not authenticated" });
                }

                var task = new StudentTask
                {
                    Title = model.Title,
                    DateFrom = DateTime.Parse(model.DateFrom),
                    DateTo = DateTime.Parse(model.DateTo),
                    Status = model.Status,
                    TaskContent = model.TaskContent ?? "",
                    LearningContent = model.LearningContent ?? "",
                    UserId = userId,
                    CreatedAt = DateTime.Now
                };

                _context.StudentTasks.Add(task);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Task created: {task.Title} for user {userId}");

                return Ok(new { success = true, id = task.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating task: {ex.Message}");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // POST: Student/UpdateTask
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateTask([FromBody] TaskViewModel model)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest(new { success = false, message = "User not authenticated" });
                }

                var task = await _context.StudentTasks
                    .FirstOrDefaultAsync(t => t.Id == model.Id && t.UserId == userId);

                if (task == null)
                {
                    return NotFound(new { success = false, message = "Task not found" });
                }

                task.Title = model.Title;
                task.DateFrom = DateTime.Parse(model.DateFrom);
                task.DateTo = DateTime.Parse(model.DateTo);
                task.Status = model.Status;
                task.TaskContent = model.TaskContent ?? "";
                task.LearningContent = model.LearningContent ?? "";
                task.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Task updated: {task.Title} for user {userId}");

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating task: {ex.Message}");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // POST: Student/DeleteTask
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTask([FromBody] int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest(new { success = false, message = "User not authenticated" });
                }

                var task = await _context.StudentTasks
                    .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

                if (task == null)
                {
                    return NotFound(new { success = false, message = "Task not found" });
                }

                _context.StudentTasks.Remove(task);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Task deleted: {task.Title} for user {userId}");

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting task: {ex.Message}");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
        // ===================== END TASK MANAGEMENT =====================

        // ===================== DOCUMENTS MANAGEMENT =====================

        // GET: Student/GetDocuments
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
                        Uploaded = d.UploadedAt,
                        Status = d.IsRead ? "Read" : "Unread"
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

        // GET: Student/DownloadDocument/5
        [HttpGet]
        public async Task<IActionResult> DownloadDocument(int id)
        {
            try
            {
                var document = await _context.Documents.FindAsync(id);
                if (document == null)
                    return NotFound("Document not found");

                if (!document.IsRead)
                {
                    document.IsRead = true;
                    document.ReadAt = DateTime.Now;
                    _context.Documents.Update(document);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Document {id} marked as read by student");
                }

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

        // ===================== TIME LOGS MANAGEMENT =====================

        // GET: Student/GetTimeLogs
        [HttpGet]
        public async Task<IActionResult> GetTimeLogs()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var timeLogs = await _context.TimeLogs
                    .Where(t => t.UserId == userId)
                    .OrderByDescending(t => t.Date)
                    .Select(t => new
                    {
                        t.Id,
                        Date = t.Date.ToString("yyyy-MM-dd"),
                        t.AmIn,
                        t.AmOut,
                        t.PmIn,
                        t.PmOut,
                        t.OtIn,
                        t.OtOut,
                        t.TotalHours,
                        t.Type
                    })
                    .ToListAsync();

                return Ok(timeLogs);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting time logs: {ex.Message}");
                return Ok(new List<object>());
            }
        }

        // POST: Student/SaveTimeLog
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveTimeLog([FromBody] TimeLogModel model)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var user = await _userManager.FindByIdAsync(userId);

                if (user == null)
                    return BadRequest(new { success = false, message = "User not found" });

                if (!DateTime.TryParse(model.Date, out DateTime logDate))
                {
                    return BadRequest(new { success = false, message = "Invalid date format" });
                }

                var existingLog = await _context.TimeLogs
                    .FirstOrDefaultAsync(t => t.UserId == userId && t.Date.Date == logDate.Date);

                if (existingLog != null)
                {
                    existingLog.AmIn = model.AmIn;
                    existingLog.AmOut = model.AmOut;
                    existingLog.PmIn = model.PmIn;
                    existingLog.PmOut = model.PmOut;
                    existingLog.OtIn = model.OtIn;
                    existingLog.OtOut = model.OtOut;
                    existingLog.TotalHours = model.TotalHours;
                    existingLog.Type = model.Type;
                    existingLog.UpdatedAt = DateTime.Now;

                    _context.TimeLogs.Update(existingLog);
                }
                else
                {
                    var timeLog = new TimeLog
                    {
                        UserId = userId,
                        Date = logDate,
                        AmIn = model.AmIn,
                        AmOut = model.AmOut,
                        PmIn = model.PmIn,
                        PmOut = model.PmOut,
                        OtIn = model.OtIn,
                        OtOut = model.OtOut,
                        TotalHours = model.TotalHours,
                        Type = model.Type,
                        CreatedAt = DateTime.Now
                    };

                    _context.TimeLogs.Add(timeLog);
                }

                await _context.SaveChangesAsync();

                var totalRendered = await _context.TimeLogs
                    .Where(t => t.UserId == userId && t.Type == "regular")
                    .SumAsync(t => t.TotalHours);

                var updatedUser = await _userManager.FindByIdAsync(userId);

                return Ok(new
                {
                    success = true,
                    message = existingLog != null ? "Time log updated successfully" : "Time log saved successfully",
                    totalRendered = totalRendered,
                    totalAllotted = updatedUser?.TotalAllottedHours ?? 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error saving time log: {ex.Message}");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // DELETE: Student/DeleteTimeLog/5
        [HttpDelete]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTimeLog(int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var timeLog = await _context.TimeLogs
                    .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

                if (timeLog == null)
                    return NotFound(new { success = false, message = "Time log not found" });

                _context.TimeLogs.Remove(timeLog);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Time log deleted for user {userId}, id: {id}");

                var totalRendered = await _context.TimeLogs
                    .Where(t => t.UserId == userId && t.Type == "regular")
                    .SumAsync(t => t.TotalHours);

                return Ok(new
                {
                    success = true,
                    message = "Time log deleted successfully",
                    totalRendered = totalRendered
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting time log: {ex.Message}");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // ===================== Get current user data =====================
        [HttpGet]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var user = await _userManager.FindByIdAsync(userId);

                if (user == null)
                {
                    return Ok(new { success = false, message = "User not found" });
                }

                if (user.TotalAllottedHours == 0 && !string.IsNullOrEmpty(user.Program))
                {
                    var program = await _context.ProgramHours.FirstOrDefaultAsync(p => p.Code == user.Program);
                    if (program != null)
                    {
                        user.TotalAllottedHours = program.Hours;
                        await _userManager.UpdateAsync(user);
                        _logger.LogInformation($"Auto-updated TotalAllottedHours for user {user.Email} to {program.Hours} (program {user.Program})");
                    }
                }

                var totalRendered = await _context.TimeLogs
                    .Where(t => t.UserId == userId && t.Type == "regular")
                    .SumAsync(t => t.TotalHours);

                var userData = new
                {
                    success = true,
                    user = new
                    {
                        user.Id,
                        user.UserName,
                        user.Email,
                        user.FullName,
                        user.Role,
                        user.Gender,
                        birthDate = user.BirthDate?.ToString("yyyy-MM-dd"),
                        contactPerson = user.ContactPerson,
                        user.MobileNumber,
                        user.Address,
                        user.ProfileImage,
                        user.BannerImage,
                        user.IsActive,
                        user.CreatedAt,
                        user.UpdatedAt,
                        user.LastLogin,
                        user.Facebook,
                        user.Year,
                        company = user.CompanyID?.ToString(),
                        user.Program,
                        user.Status,
                        user.StudentId,
                        totalAllottedHours = user.TotalAllottedHours,
                        totalRenderedHours = totalRendered
                    }
                };

                return Ok(userData);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetCurrentUser: {ex.Message}");
                return Ok(new { success = false, message = ex.Message });
            }
        }

        // API: Update student profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileModel model)
        {
            try
            {
                Console.WriteLine("========== UPDATE PROFILE CALLED ==========");
                Console.WriteLine($"Time: {DateTime.Now}");
                Console.WriteLine($"User: {User?.Identity?.Name ?? "Unknown"}");
                Console.WriteLine($"IsAuthenticated: {User?.Identity?.IsAuthenticated}");

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                Console.WriteLine($"UserId from token: {userId}");

                if (model == null)
                {
                    Console.WriteLine("ERROR: model is null");
                    return Ok(new { success = false, message = "Model is null" });
                }

                Console.WriteLine("Received data:");
                Console.WriteLine($"  FullName: {model.FullName}");
                Console.WriteLine($"  Facebook: {model.Facebook}");
                Console.WriteLine($"  Company: {model.Company}");
                Console.WriteLine($"  MobileNumber: {model.MobileNumber}");
                Console.WriteLine($"  Address: {model.Address}");
                Console.WriteLine($"  ContactPerson: {model.ContactPerson}");
                Console.WriteLine($"  StudentId: {model.StudentId}");
                Console.WriteLine($"  Program: {model.Program}");
                Console.WriteLine($"  BirthDate: {model.BirthDate}");
                Console.WriteLine($"  Email: {model.Email}");

                var user = await _userManager.FindByIdAsync(userId);

                if (user == null)
                {
                    Console.WriteLine($"ERROR: User not found with ID: {userId}");
                    return Ok(new { success = false, message = "User not found" });
                }

                Console.WriteLine($"Found user: {user.Email} (Current FullName: {user.FullName})");

                if (!string.IsNullOrEmpty(model.FullName)) user.FullName = model.FullName;
                if (!string.IsNullOrEmpty(model.Facebook)) user.Facebook = model.Facebook;

                if (!string.IsNullOrEmpty(model.Company))
                {
                    if (int.TryParse(model.Company, out int companyId))
                    {
                        user.CompanyID = companyId;
                    }
                    else
                    {
                        user.CompanyID = null;
                    }
                }

                if (!string.IsNullOrEmpty(model.MobileNumber)) user.MobileNumber = model.MobileNumber;
                if (!string.IsNullOrEmpty(model.Address)) user.Address = model.Address;
                if (!string.IsNullOrEmpty(model.ContactPerson)) user.ContactPerson = model.ContactPerson;
                if (!string.IsNullOrEmpty(model.StudentId)) user.StudentId = model.StudentId;
                if (!string.IsNullOrEmpty(model.Program)) user.Program = model.Program;

                if (!string.IsNullOrEmpty(model.BirthDate))
                {
                    if (DateTime.TryParse(model.BirthDate, out DateTime birthDate))
                    {
                        user.BirthDate = birthDate;
                    }
                }

                if (!string.IsNullOrEmpty(model.Email))
                {
                    user.Email = model.Email;
                    user.UserName = model.Email;
                }

                user.UpdatedAt = DateTime.Now;

                var result = await _userManager.UpdateAsync(user);
                Console.WriteLine($"Update result: {(result.Succeeded ? "SUCCESS" : "FAILED")}");

                if (result.Succeeded)
                {
                    Console.WriteLine("Profile updated successfully!");
                    return Ok(new { success = true, message = "Profile updated successfully" });
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    Console.WriteLine($"Update errors: {errors}");
                    return Ok(new { success = false, message = $"Failed to update: {errors}" });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EXCEPTION: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return Ok(new { success = false, message = ex.Message });
            }
        }

        // API: Upload profile image
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadProfileImage(IFormFile profileImage)
        {
            try
            {
                if (profileImage == null || profileImage.Length == 0)
                {
                    return Ok(new { success = false, message = "No image uploaded" });
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var user = await _userManager.FindByIdAsync(userId);

                if (user == null)
                {
                    return Ok(new { success = false, message = "User not found" });
                }

                var imagesPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "profiles");
                if (!Directory.Exists(imagesPath))
                {
                    Directory.CreateDirectory(imagesPath);
                }

                var fileName = $"{userId}_{DateTime.Now.Ticks}{Path.GetExtension(profileImage.FileName)}";
                var filePath = Path.Combine(imagesPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await profileImage.CopyToAsync(stream);
                }

                if (!string.IsNullOrEmpty(user.ProfileImage) &&
                    !user.ProfileImage.Contains("defaultpicture") &&
                    !user.ProfileImage.Contains("ctu-logo"))
                {
                    var oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.ProfileImage.TrimStart('/'));
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                user.ProfileImage = $"/images/profiles/{fileName}";
                user.UpdatedAt = DateTime.Now;

                await _userManager.UpdateAsync(user);

                return Ok(new { success = true, imageUrl = user.ProfileImage });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = ex.Message });
            }
        }

        // ===== UPDATED: SEND LOGS TO ADMIN WITH READ/UNREAD STATUS =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendLogsToAdmin([FromBody] SendLogsViewModel model)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var student = await _userManager.FindByIdAsync(userId);

                if (student == null)
                {
                    return Json(new { success = false, message = "Student not found" });
                }

                _logger.LogInformation($"Saving logs for student {student.Email}, Month: {model.Month}, Total Hours: {model.TotalHours}");
                _logger.LogInformation($"Logs count: {model.Logs?.Count ?? 0}");

                if (model.Month < 1 || model.Month > 12)
                {
                    return Json(new { success = false, message = "Invalid month value" });
                }

                if (string.IsNullOrEmpty(student.Program))
                {
                    _logger.LogWarning($"Student {student.Email} has no program assigned");
                }

                var submission = new TimeLogSubmission
                {
                    Id = Guid.NewGuid().ToString(),
                    StudentId = userId,
                    StudentName = student.FullName ?? student.Email,
                    Course = student.Program ?? "N/A",
                    Year = student.Year?.ToString() ?? "N/A",
                    SubmissionDate = DateTime.Now,
                    Month = model.Month,
                    TotalHours = model.TotalHours,
                    Logs = System.Text.Json.JsonSerializer.Serialize(model.Logs),
                    Status = "Unread",
                    IsRead = false
                };

                _context.StudentTimeLogs.Add(submission);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Time logs saved to database for student {student.Email}, Month: {model.Month}, Total Hours: {model.TotalHours}");

                return Json(new
                {
                    success = true,
                    message = "Logs sent successfully to admin",
                    submissionId = submission.Id,
                    status = "Unread"
                });
            }
            catch (DbUpdateException ex)
            {
                var innerException = ex.InnerException?.Message ?? "No inner exception";

                _logger.LogError($"Database error sending logs to admin: {ex.Message}");
                _logger.LogError($"Inner exception: {innerException}");

                if (innerException.Contains("FK_") || innerException.Contains("foreign key"))
                {
                    return Json(new { success = false, message = "Foreign key constraint error. Please check your data." });
                }
                else if (innerException.Contains("PK_") || innerException.Contains("primary key"))
                {
                    return Json(new { success = false, message = "Duplicate key error. Please try again." });
                }
                else if (innerException.Contains("Cannot insert the value NULL"))
                {
                    return Json(new { success = false, message = "A required field is missing. Please check your data." });
                }

                return Json(new { success = false, message = $"Database error: {innerException}" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending logs to admin: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ===== GET STUDENT'S SUBMISSIONS WITH STATUS =====
        [HttpGet]
        public async Task<IActionResult> GetSubmissions()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var submissions = await _context.StudentTimeLogs
                    .Where(s => s.StudentId == userId)
                    .OrderByDescending(s => s.SubmissionDate)
                    .Select(s => new
                    {
                        s.Id,
                        s.Month,
                        MonthName = GetMonthName(s.Month),
                        s.TotalHours,
                        s.SubmissionDate,
                        s.Status,
                        s.IsRead
                    })
                    .ToListAsync();

                return Ok(new { success = true, data = submissions });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting submissions: {ex.Message}");
                return Ok(new { success = false, message = ex.Message, data = new List<object>() });
            }
        }

        private string GetMonthName(int month)
        {
            return month switch
            {
                1 => "January",
                2 => "February",
                3 => "March",
                4 => "April",
                5 => "May",
                6 => "June",
                7 => "July",
                8 => "August",
                9 => "September",
                10 => "October",
                11 => "November",
                12 => "December",
                _ => "Unknown"
            };
        }
    }

    public class UpdateProfileModel
    {
        public string FullName { get; set; }
        public string Facebook { get; set; }
        public string Company { get; set; }
        public string MobileNumber { get; set; }
        public string Address { get; set; }
        public string ContactPerson { get; set; }
        public string StudentId { get; set; }
        public string Program { get; set; }
        public string BirthDate { get; set; }
        public string Email { get; set; }
    }

    public class TimeLogModel
    {
        public string Date { get; set; }
        public string AmIn { get; set; }
        public string AmOut { get; set; }
        public string PmIn { get; set; }
        public string PmOut { get; set; }
        public string OtIn { get; set; }
        public string OtOut { get; set; }
        public double TotalHours { get; set; }
        public string Type { get; set; }
    }

    // ===== MODEL FOR SENDING LOGS =====
    public class SendLogsViewModel
    {
        public List<object> Logs { get; set; }
        public string StudentName { get; set; }
        public double TotalHours { get; set; }
        public int Month { get; set; }
    }

    // ===== TASK VIEW MODEL =====
    public class TaskViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string DateFrom { get; set; }
        public string DateTo { get; set; }
        public string Status { get; set; }
        public string TaskContent { get; set; }
        public string LearningContent { get; set; }
    }
}