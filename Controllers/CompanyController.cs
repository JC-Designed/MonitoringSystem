using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonitoringSystem.Models;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;

namespace MonitoringSystem.Controllers
{
    [Authorize(Roles = "Company")]
    public class CompanyController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<CompanyController> _logger;

        public CompanyController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILogger<CompanyController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Dashboard()
        {
            var userEmail = User.Identity.Name;
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == userEmail);

            if (user == null)
                return NotFound();

            var company = await _context.Companies
                .FirstOrDefaultAsync(c => c.UserId == user.Id);

            ViewBag.CompanyName = company?.CompanyName ?? user.FullName;
            ViewBag.CompanyId = company?.CompanyId;

            return View();
        }

        // GET: /Company/GetCurrentUser
        [HttpGet]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                var userEmail = User.Identity.Name;
                var user = await _userManager.FindByEmailAsync(userEmail);

                if (user == null)
                    return NotFound();

                var company = await _context.Companies
                    .FirstOrDefaultAsync(c => c.UserId == user.Id);

                return Json(new
                {
                    success = true,
                    fullName = user.FullName,
                    companyName = company?.CompanyName,
                    profileImage = user.ProfileImage ?? "/images/defaultpicture.jpg",
                    bannerImage = user.BannerImage ?? "/images/banner-placeholder.jpg",
                    email = user.Email,
                    mobileNumber = user.MobileNumber,
                    address = user.Address
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: /Company/GetProfile
        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var userEmail = User.Identity.Name;
                var user = await _userManager.FindByEmailAsync(userEmail);

                if (user == null)
                    return NotFound();

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
                    profileImage = user.ProfileImage ?? "/images/defaultpicture.jpg",
                    bannerImage = user.BannerImage ?? "/images/banner-placeholder.jpg",

                    // Company info
                    companyName = company?.CompanyName,
                    companyId = company?.CompanyId,
                    companyDescription = company?.CompanyDescription,
                    industry = company?.Industry,
                    companySize = company?.CompanySize,
                    website = company?.Website,
                    businessPermit = company?.BusinessPermit,
                    taxId = company?.TaxId,
                    secId = company?.SecId,
                    yearEstablished = company?.YearEstablished,
                    contactPersonName = company?.ContactPersonName,
                    contactPersonPosition = company?.ContactPersonPosition,
                    isVerified = company?.IsVerified ?? false,
                    verifiedBy = company?.VerifiedBy,
                    verifiedDate = company?.VerifiedDate,
                    createdAt = company?.CreatedAt,
                    updatedAt = company?.UpdatedAt
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: /Company/UpdateProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile([FromBody] CompanyProfileModel model)
        {
            try
            {
                var userEmail = User.Identity.Name;
                var user = await _userManager.FindByEmailAsync(userEmail);

                if (user == null)
                    return Json(new { success = false, message = "User not found" });

                // Update user information
                user.FullName = model.FullName ?? user.FullName;
                user.MobileNumber = model.MobileNumber ?? user.MobileNumber;
                user.Address = model.Address ?? user.Address;
                user.UpdatedAt = DateTime.Now;

                var updateResult = await _userManager.UpdateAsync(user);

                if (!updateResult.Succeeded)
                {
                    return Json(new { success = false, message = "Failed to update user" });
                }

                // Update company information
                var company = await _context.Companies
                    .FirstOrDefaultAsync(c => c.UserId == user.Id);

                if (company != null)
                {
                    company.CompanyName = model.CompanyName ?? company.CompanyName;
                    company.Industry = model.Industry ?? company.Industry;
                    company.Website = model.Website ?? company.Website;
                    company.CompanyDescription = model.CompanyDescription ?? company.CompanyDescription;
                    company.ContactPersonName = model.ContactPersonName ?? company.ContactPersonName;
                    company.TaxId = model.TaxId ?? company.TaxId;
                    company.SecId = model.SecId ?? company.SecId;
                    company.UpdatedAt = DateTime.Now;

                    await _context.SaveChangesAsync();
                }

                return Json(new { success = true, message = "Profile updated successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: /Company/Profile
        public IActionResult Profile()
        {
            return View();
        }

        // GET: /Company/Trainees
        public IActionResult Trainees()
        {
            return View("ManageOJT");
        }

        // GET: /Company/ManageOJT
        public IActionResult ManageOJT()
        {
            return View();
        }

        // ===== FIXED: GET TRAINEES ASSIGNED TO THIS COMPANY USING INT COMPANY ID =====
        [HttpGet]
        public async Task<IActionResult> GetTrainees()
        {
            try
            {
                var userEmail = User.Identity.Name;
                var currentUser = await _userManager.FindByEmailAsync(userEmail);

                if (currentUser == null)
                    return Json(new { success = false, message = "User not found", data = new List<object>() });

                _logger?.LogInformation($"Fetching trainees for company: {currentUser.Email}");

                // Get the company record to get the integer ID
                var companyRecord = await _context.Companies.FirstOrDefaultAsync(c => c.UserId == currentUser.Id);
                if (companyRecord == null)
                {
                    return Json(new { success = false, message = "Company record not found", data = new List<object>() });
                }

                int companyIdValue = companyRecord.Id;

                // Get all students assigned to this company using the integer CompanyID
                var trainees = await _userManager.Users
                    .Where(u => u.CompanyID.HasValue && u.CompanyID.Value == companyIdValue && u.Role == "Student" && u.Status == "Approved")
                    .Select(u => new
                    {
                        id = u.Id,
                        studentId = u.StudentId ?? "",
                        fullName = u.FullName ?? "",
                        email = u.Email ?? "",
                        program = u.Program ?? "",
                        year = u.Year ?? 0,
                        mobileNumber = u.MobileNumber ?? "",
                        address = u.Address ?? "",
                        contactPerson = u.ContactPerson ?? "",
                        birthDate = u.BirthDate.HasValue ? u.BirthDate.Value.ToString("yyyy-MM-dd") : "",
                        facebook = u.Facebook ?? "",
                        status = u.Status ?? "Active"
                    })
                    .OrderBy(u => u.fullName)
                    .ToListAsync();

                _logger?.LogInformation($"Found {trainees.Count} trainees for company");

                return Json(new { success = true, data = trainees });
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Error getting trainees: {ex.Message}");
                return Json(new { success = false, message = ex.Message, data = new List<object>() });
            }
        }

        // ===== FIXED: GET SINGLE TRAINEE DETAILS USING INT COMPANY ID =====
        [HttpGet]
        public async Task<IActionResult> GetTraineeDetails(string studentId)
        {
            try
            {
                var userEmail = User.Identity.Name;
                var currentUser = await _userManager.FindByEmailAsync(userEmail);

                if (currentUser == null)
                    return Json(new { success = false, message = "User not found" });

                // Get the company record to get the integer ID
                var companyRecord = await _context.Companies.FirstOrDefaultAsync(c => c.UserId == currentUser.Id);
                if (companyRecord == null)
                {
                    return Json(new { success = false, message = "Company record not found" });
                }

                int companyIdValue = companyRecord.Id;

                var student = await _userManager.Users
                    .Where(u => u.Id == studentId && u.CompanyID.HasValue && u.CompanyID.Value == companyIdValue && u.Role == "Student")
                    .Select(u => new
                    {
                        id = u.Id,
                        studentId = u.StudentId ?? "",
                        fullName = u.FullName ?? "",
                        email = u.Email ?? "",
                        program = u.Program ?? "",
                        year = u.Year ?? 0,
                        mobileNumber = u.MobileNumber ?? "",
                        address = u.Address ?? "",
                        contactPerson = u.ContactPerson ?? "",
                        birthDate = u.BirthDate.HasValue ? u.BirthDate.Value.ToString("yyyy-MM-dd") : "",
                        facebook = u.Facebook ?? "",
                        companyId = u.CompanyID.HasValue ? u.CompanyID.Value.ToString() : ""
                    })
                    .FirstOrDefaultAsync();

                if (student == null)
                {
                    return Json(new { success = false, message = "Student not found or not assigned to your company" });
                }

                return Json(new { success = true, data = student });
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Error getting trainee details: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ===== FIXED: CREATE REPORT ABOUT A TRAINEE USING INT COMPANY ID =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateReport([FromBody] CreateReportModel model)
        {
            try
            {
                if (model == null || string.IsNullOrEmpty(model.StudentId) || string.IsNullOrEmpty(model.Incident))
                {
                    return Json(new { success = false, message = "Invalid report data" });
                }

                var userEmail = User.Identity.Name;
                var currentUser = await _userManager.FindByEmailAsync(userEmail);

                if (currentUser == null)
                    return Json(new { success = false, message = "User not found" });

                // Get the company record to get the integer ID
                var companyRecord = await _context.Companies.FirstOrDefaultAsync(c => c.UserId == currentUser.Id);
                if (companyRecord == null)
                {
                    return Json(new { success = false, message = "Company record not found" });
                }

                int companyIdValue = companyRecord.Id;

                // Verify the student is assigned to this company
                var student = await _userManager.Users
                    .FirstOrDefaultAsync(u => u.Id == model.StudentId && u.CompanyID.HasValue && u.CompanyID.Value == companyIdValue && u.Role == "Student");

                if (student == null)
                {
                    return Json(new { success = false, message = "Student not found or not assigned to your company" });
                }

                // Create the report using CompanyReport table
                var report = new CompanyReport
                {
                    StudentId = model.StudentId,
                    CompanyId = currentUser.Id,
                    StudentName = student.FullName,
                    Incident = model.Incident,
                    Status = "Pending",
                    CreatedAt = DateTime.Now
                };

                _context.CompanyReports.Add(report);
                await _context.SaveChangesAsync();

                _logger?.LogInformation($"Report created for student {student.Email} by company {currentUser.Email}");

                return Json(new { success = true, message = "Report created successfully" });
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Error creating report: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ===== FIXED: TERMINATE TRAINEE USING INT COMPANY ID =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TerminateTrainee([FromBody] TerminateTraineeModel model)
        {
            try
            {
                if (model == null || string.IsNullOrEmpty(model.StudentId))
                {
                    return Json(new { success = false, message = "Invalid termination data" });
                }

                var userEmail = User.Identity.Name;
                var currentUser = await _userManager.FindByEmailAsync(userEmail);

                if (currentUser == null)
                    return Json(new { success = false, message = "User not found" });

                // Get the company record to get the integer ID
                var companyRecord = await _context.Companies.FirstOrDefaultAsync(c => c.UserId == currentUser.Id);
                if (companyRecord == null)
                {
                    return Json(new { success = false, message = "Company record not found" });
                }

                int companyIdValue = companyRecord.Id;

                // Verify the student is assigned to this company
                var student = await _userManager.Users
                    .FirstOrDefaultAsync(u => u.Id == model.StudentId && u.CompanyID.HasValue && u.CompanyID.Value == companyIdValue && u.Role == "Student");

                if (student == null)
                {
                    return Json(new { success = false, message = "Student not found or not assigned to your company" });
                }

                // Remove the company assignment
                student.CompanyID = null;
                student.Status = "Terminated";
                student.UpdatedAt = DateTime.Now;

                await _userManager.UpdateAsync(student);

                // Create a termination report using CompanyReport table
                var report = new CompanyReport
                {
                    StudentId = model.StudentId,
                    CompanyId = currentUser.Id,
                    StudentName = student.FullName,
                    Incident = "Student Terminated",
                    Remarks = model.Remarks ?? "Student terminated by company",
                    Status = "Resolved",
                    CreatedAt = DateTime.Now,
                    ResolvedAt = DateTime.Now
                };

                _context.CompanyReports.Add(report);
                await _context.SaveChangesAsync();

                _logger?.LogInformation($"Trainee {student.Email} terminated by company {currentUser.Email}");

                return Json(new { success = true, message = "Student terminated successfully" });
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Error terminating trainee: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: /Company/Reports
        public IActionResult Reports()
        {
            return View();
        }

        // GET: /Company/Reports/GetReports
        [HttpGet]
        public async Task<IActionResult> GetReports()
        {
            try
            {
                var userEmail = User.Identity.Name;
                var user = await _userManager.FindByEmailAsync(userEmail);

                if (user == null)
                    return Json(new { success = false, message = "User not found" });

                // Get all reports created by this company from CompanyReports table
                var reports = await _context.CompanyReports
                    .Where(r => r.CompanyId == user.Id)
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(r => new
                    {
                        r.Id,
                        r.StudentName,
                        r.Incident,
                        r.Status,
                        r.CreatedAt,
                        r.ResolvedAt
                    })
                    .ToListAsync();

                return Json(new { success = true, data = reports });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}

// CompanyProfileModel
public class CompanyProfileModel
{
    public string FullName { get; set; }
    public string MobileNumber { get; set; }
    public string Address { get; set; }
    public string CompanyName { get; set; }
    public string Industry { get; set; }
    public string Website { get; set; }
    public string CompanyDescription { get; set; }
    public string ContactPersonName { get; set; }
    public string TaxId { get; set; }
    public string SecId { get; set; }
}

// Models for API requests
public class CreateReportModel
{
    public string StudentId { get; set; }
    public string Incident { get; set; }
}

public class TerminateTraineeModel
{
    public string StudentId { get; set; }
    public string Remarks { get; set; }
}