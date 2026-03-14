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

        // GET: Student Dashboard
        public IActionResult Dashboard()
        {
            return View();
        }

        // GET: Student Tasks page
        public IActionResult Tasks()
        {
            return View();
        }

        // GET: Student Report page
        public IActionResult Report()
        {
            return View();
        }

        // API: Update student profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileModel model)
        {
            try
            {
                // Log everything for debugging
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

                // Log received data
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

                // Update user properties
                if (!string.IsNullOrEmpty(model.FullName))
                {
                    user.FullName = model.FullName;
                    Console.WriteLine($"Updated FullName to: {model.FullName}");
                }

                if (!string.IsNullOrEmpty(model.Facebook))
                {
                    user.Facebook = model.Facebook;
                    Console.WriteLine($"Updated Facebook to: {model.Facebook}");
                }

                // Handle Company conversion
                if (!string.IsNullOrEmpty(model.Company))
                {
                    if (int.TryParse(model.Company, out int companyId))
                    {
                        user.CompanyID = companyId;
                        Console.WriteLine($"Updated CompanyID to: {companyId}");
                    }
                    else
                    {
                        user.CompanyID = null;
                        Console.WriteLine("CompanyID set to null (invalid number format)");
                    }
                }

                if (!string.IsNullOrEmpty(model.MobileNumber))
                {
                    user.MobileNumber = model.MobileNumber;
                    Console.WriteLine($"Updated MobileNumber to: {model.MobileNumber}");
                }

                if (!string.IsNullOrEmpty(model.Address))
                {
                    user.Address = model.Address;
                    Console.WriteLine($"Updated Address to: {model.Address}");
                }

                if (!string.IsNullOrEmpty(model.ContactPerson))
                {
                    user.ContactPerson = model.ContactPerson;
                    Console.WriteLine($"Updated ContactPerson to: {model.ContactPerson}");
                }

                if (!string.IsNullOrEmpty(model.StudentId))
                {
                    user.StudentId = model.StudentId;
                    Console.WriteLine($"Updated StudentId to: {model.StudentId}");
                }

                if (!string.IsNullOrEmpty(model.Program))
                {
                    user.Program = model.Program;
                    Console.WriteLine($"Updated Program to: {model.Program}");
                }

                if (!string.IsNullOrEmpty(model.BirthDate))
                {
                    if (DateTime.TryParse(model.BirthDate, out DateTime birthDate))
                    {
                        user.BirthDate = birthDate;
                        Console.WriteLine($"Updated BirthDate to: {birthDate}");
                    }
                }

                if (!string.IsNullOrEmpty(model.Email))
                {
                    user.Email = model.Email;
                    user.UserName = model.Email;
                    Console.WriteLine($"Updated Email to: {model.Email}");
                }

                user.UpdatedAt = DateTime.Now;
                Console.WriteLine($"UpdatedAt set to: {user.UpdatedAt}");

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

                // Create images directory if it doesn't exist
                var imagesPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "profiles");
                if (!Directory.Exists(imagesPath))
                {
                    Directory.CreateDirectory(imagesPath);
                }

                // Generate unique filename
                var fileName = $"{userId}_{DateTime.Now.Ticks}{Path.GetExtension(profileImage.FileName)}";
                var filePath = Path.Combine(imagesPath, fileName);

                // Save the file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await profileImage.CopyToAsync(stream);
                }

                // Delete old profile image if it exists and is not the default
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

                // Update user's profile image path
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

        // ===================== UPDATED: Get current user data with auto-fix for hours =====================
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

                // --- NEW: If TotalAllottedHours is 0 but user has a program, fetch from ProgramHours and update ---
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
                        totalAllottedHours = user.TotalAllottedHours
                    }
                };

                return Ok(userData);
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = ex.Message });
            }
        }

        // ===================== ADDED: Get student's time logs =====================
        [HttpGet]
        public async Task<IActionResult> GetTimeLogs()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // For now, return empty array until TimeLogs table is created
                var timeLogs = new List<object>();

                return Ok(timeLogs);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // ===================== ADDED: Save time log =====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveTimeLog([FromBody] TimeLogModel model)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // This is a placeholder - implement when you create TimeLogs table
                return Ok(new { success = true, message = "Time log saved successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // ===================== ADDED: Delete time log =====================
        [HttpDelete]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTimeLog(int id)
        {
            try
            {
                // This is a placeholder - implement when you create TimeLogs table
                return Ok(new { success = true, message = "Time log deleted successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
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
        public DateTime Date { get; set; }
        public string AmIn { get; set; }
        public string AmOut { get; set; }
        public string PmIn { get; set; }
        public string PmOut { get; set; }
        public string OtIn { get; set; }
        public string OtOut { get; set; }
        public double TotalHours { get; set; }
        public string Type { get; set; }
    }
}