using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonitoringSystem.Models;
using MonitoringSystem.Data;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace MonitoringSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext db,
            IWebHostEnvironment env)
        {
            _userManager = userManager;
            _db = db;
            _env = env;

            // Ensure admin exists (async-safe)
            Task.Run(async () => await EnsureAdminUser()).Wait();
        }

        // ================= CREATE DEFAULT ADMIN IF NOT EXISTS =================
        private async Task EnsureAdminUser()
        {
            var adminEmail = "jonardcarmelotes09@gmail.com";
            var admin = await _userManager.FindByEmailAsync(adminEmail);

            if (admin == null)
            {
                admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "",
                    LastName = "",
                    Role = "Admin",
                    IsApproved = true,
                    CreatedAt = DateTime.Now
                };

                var result = await _userManager.CreateAsync(admin, "admin123");
                if (result.Succeeded)
                    await _userManager.AddToRoleAsync(admin, "Admin");
            }
        }

        // ================= HELPER TO GET SCHOOL YEAR =================
        private int GetSchoolYear()
        {
            if (int.TryParse(Request.Query["schoolYear"], out int year))
                return year;

            if (Request.Cookies.ContainsKey("SelectedSchoolYear") &&
                int.TryParse(Request.Cookies["SelectedSchoolYear"], out int cookieYear))
                return cookieYear;

            return DateTime.Now.Year;
        }

        // ================= DASHBOARD =================
        public async Task<IActionResult> Dashboard()
        {
            int schoolYear = GetSchoolYear();
            ViewBag.SchoolYear = schoolYear;

            var allUsers = await _userManager.Users
                .Where(u => u.CreatedAt.Year == schoolYear)
                .ToListAsync();

            foreach (var user in allUsers)
                user.Roles = (await _userManager.GetRolesAsync(user)).ToList();

            var pendingUsersList = allUsers
                .Where(u => !u.IsApproved)
                .Select(u => new PendingUserDto
                {
                    Id = u.Id,
                    Email = u.Email,
                    Role = (u.Roles != null && u.Roles.Count > 0) ? u.Roles.First() : "N/A",
                    RegisteredMonth = u.CreatedAt.Month
                })
                .ToList();

            ViewBag.PendingUsersList = pendingUsersList;
            ViewBag.PendingUsers = pendingUsersList.Count;
            ViewBag.ApprovedUsers = allUsers.Count(u => u.IsApproved);
            ViewBag.TotalUsers = allUsers.Count;

            ViewBag.TotalCompanies = _db.Companies
                .Include(c => c.User)
                .Count(c => c.User != null && c.User.CreatedAt.Year == schoolYear);

            var monthlyRegistrations = new int[12];
            var totalUsersByMonth = new int[12];

            for (int month = 1; month <= 12; month++)
            {
                monthlyRegistrations[month - 1] = allUsers.Count(u => u.CreatedAt.Month == month);
                totalUsersByMonth[month - 1] = allUsers.Count(u => u.CreatedAt.Month <= month && u.IsApproved);
            }

            ViewBag.MonthlyRegistrations = monthlyRegistrations;
            ViewBag.TotalUsersByMonth = totalUsersByMonth;

            return View(allUsers);
        }

        // ================= USERS PAGE =================
        public async Task<IActionResult> Users()
        {
            int schoolYear = GetSchoolYear();
            ViewBag.SchoolYear = schoolYear;

            var allUsers = await _userManager.Users
                .Where(u => u.CreatedAt.Year == schoolYear)
                .ToListAsync();

            foreach (var user in allUsers)
                user.Roles = (await _userManager.GetRolesAsync(user)).ToList();

            return View(allUsers);
        }

        // ================= DELETE USER =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> DeleteUser([FromBody] IdDto dto)
        {
            if (string.IsNullOrEmpty(dto?.Id))
                return Json(new { success = false });

            var user = await _userManager.FindByIdAsync(dto.Id);
            if (user == null)
                return Json(new { success = false });

            var result = await _userManager.DeleteAsync(user);
            return Json(new { success = result.Succeeded });
        }

        // ================= APPROVE PENDING USER =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> Approve([FromBody] IdDto dto)
        {
            if (string.IsNullOrEmpty(dto?.Id))
                return Json(new { success = false });

            var user = await _userManager.FindByIdAsync(dto.Id);
            if (user == null)
                return Json(new { success = false });

            user.IsApproved = true;
            var result = await _userManager.UpdateAsync(user);
            return Json(new { success = result.Succeeded });
        }

        // ================= REJECT PENDING USER =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> Reject([FromBody] IdDto dto)
        {
            if (string.IsNullOrEmpty(dto?.Id))
                return Json(new { success = false });

            var user = await _userManager.FindByIdAsync(dto.Id);
            if (user == null)
                return Json(new { success = false });

            var result = await _userManager.DeleteAsync(user);
            return Json(new { success = result.Succeeded });
        }

        // ================= COMPANY PAGE =================
        public IActionResult Company()
        {
            int schoolYear = GetSchoolYear();
            ViewBag.SchoolYear = schoolYear;

            var companies = _db.Companies
                .Include(c => c.User)
                .Where(c => c.User != null && c.User.CreatedAt.Year == schoolYear)
                .ToList();

            return View(companies);
        }

        // ================= DELETE COMPANY =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCompany(int id)
        {
            var company = await _db.Companies
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (company == null) return NotFound();

            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                if (company.User != null)
                {
                    var result = await _userManager.DeleteAsync(company.User);
                    if (!result.Succeeded)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest("Failed to delete the user associated with this company.");
                    }
                }

                _db.Companies.Remove(company);
                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                return RedirectToAction("Company");
            }
            catch
            {
                await transaction.RollbackAsync();
                return BadRequest("An error occurred while deleting the company.");
            }
        }

        // ================= MESSAGES PAGE =================
        public IActionResult Messages()
        {
            ViewBag.SchoolYear = GetSchoolYear();
            return View();
        }

        // ================= REPORTS PAGE =================
        public IActionResult Reports()
        {
            int schoolYear = GetSchoolYear();
            ViewBag.SchoolYear = schoolYear;

            var reports = new List<Report>
            {
                new Report { Id = 1, ReporterName = "John Doe", ReporterType = "Student", Status = "Unread", Details = "Demo report 1", DateSubmitted = new DateTime(schoolYear, 6, 1) },
                new Report { Id = 2, ReporterName = "Acme Corp", ReporterType = "Company", Status = "Read", Details = "Demo report 2", DateSubmitted = new DateTime(schoolYear, 5, 10) },
                new Report { Id = 3, ReporterName = "Jane Smith", ReporterType = "Student", Status = "Unread", Details = "Demo report 3", DateSubmitted = new DateTime(schoolYear, 4, 20) }
            };

            return View(reports);
        }

        // ================= GET ADMIN PROFILE =================
        [HttpGet]
        public async Task<JsonResult> GetAdminProfile()
        {
            var admin = await _userManager.GetUserAsync(User);
            if (admin == null)
                return Json(new { success = false });

            return Json(new
            {
                success = true,
                firstName = admin.FirstName ?? "",
                lastName = admin.LastName ?? "",
                email = admin.Email ?? "",
                contact = admin.Contact ?? "",
                address = admin.Address ?? "",
                profileImage = string.IsNullOrEmpty(admin.ProfileImage) ? "/images/ctu-logo.png" : admin.ProfileImage,
                bannerImage = string.IsNullOrEmpty(admin.BannerImage) ? "/images/banner-placeholder.jpg" : admin.BannerImage
            });
        }

        // ================= UPDATE PROFILE =================
        [HttpPost]
        public async Task<JsonResult> UpdateProfile()
        {
            try
            {
                var admin = await _userManager.GetUserAsync(User);
                if (admin == null)
                    return Json(new { success = false });

                var form = Request.Form;

                admin.FirstName = form["FirstName"];
                admin.LastName = form["LastName"];
                admin.Email = form["Email"];
                admin.UserName = form["Email"];
                admin.Contact = form["Contact"];
                admin.Address = form["Address"];

                string root = Path.Combine(_env.WebRootPath, "uploads");
                string profileDir = Path.Combine(root, "profiles");
                string bannerDir = Path.Combine(root, "banners");

                Directory.CreateDirectory(profileDir);
                Directory.CreateDirectory(bannerDir);

                foreach (var file in Request.Form.Files)
                {
                    if (file.Length <= 0) continue;

                    string ext = Path.GetExtension(file.FileName);
                    string fileName = $"{Guid.NewGuid()}{ext}";

                    if (file.Name == "ProfilePicture")
                    {
                        string path = Path.Combine(profileDir, fileName);
                        using var stream = new FileStream(path, FileMode.Create);
                        await file.CopyToAsync(stream);
                        admin.ProfileImage = "/uploads/profiles/" + fileName;
                    }

                    if (file.Name == "BannerPicture")
                    {
                        string path = Path.Combine(bannerDir, fileName);
                        using var stream = new FileStream(path, FileMode.Create);
                        await file.CopyToAsync(stream);
                        admin.BannerImage = "/uploads/banners/" + fileName;
                    }
                }

                await _userManager.UpdateAsync(admin);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ================= DTOs =================
        public class PendingUserDto
        {
            public string Id { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty;
            public int RegisteredMonth { get; set; }
        }

        public class EditUserDto
        {
            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty;
            public string NewPassword { get; set; } = string.Empty;
        }

        public class DeleteUserDto
        {
            public string Id { get; set; } = string.Empty;
        }

        public class IdDto
        {
            public string Id { get; set; } = string.Empty;
        }
    }
}
