using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonitoringSystem.Models;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System;

namespace MonitoringSystem.Controllers
{
    [Authorize(Roles = "Company")]
    public class CompanyController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CompanyController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
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

                    // Company info - matching your database columns exactly
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

                // Update company information - ONLY using columns that exist in your database
                var company = await _context.Companies
                    .FirstOrDefaultAsync(c => c.UserId == user.Id);

                if (company != null)
                {
                    // Update only the columns that exist in your database
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

        // GET: /Company/Trainees - FIXED to use ManageOJT.cshtml
        public IActionResult Trainees()
        {
            return View("ManageOJT"); // This will look for ManageOJT.cshtml instead of Trainees.cshtml
        }

        // GET: /Company/ManageOJT - Alternative route if needed
        public IActionResult ManageOJT()
        {
            return View();
        }

        // GET: /Company/Trainees/GetTrainees
        [HttpGet]
        public async Task<IActionResult> GetTrainees()
        {
            try
            {
                var userEmail = User.Identity.Name;
                var user = await _userManager.FindByEmailAsync(userEmail);

                if (user == null)
                    return Json(new { success = false, message = "User not found" });

                var company = await _context.Companies
                    .FirstOrDefaultAsync(c => c.UserId == user.Id);

                if (company == null)
                    return Json(new { success = false, message = "Company not found" });

                var trainees = await _context.Users
                    .Where(u => u.CompanyID.ToString() == company.CompanyId && u.Role == "Student")
                    .Select(u => new
                    {
                        u.Id,
                        u.StudentId,
                        u.FullName,
                        u.Email,
                        u.Program,
                        u.MobileNumber,
                        u.Status
                    })
                    .ToListAsync();

                return Json(new { success = true, data = trainees });
            }
            catch (Exception ex)
            {
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

                var reports = new
                {
                    totalTrainees = 0,
                    activeTasks = 0,
                    completedHours = 0,
                    pendingEvaluations = 0
                };

                return Json(new { success = true, data = reports });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}

// CompanyProfileModel - WITHOUT ContactPersonMobile
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