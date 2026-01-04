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

namespace MonitoringSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _db;

        public AdminController(UserManager<ApplicationUser> userManager, ApplicationDbContext db)
        {
            _userManager = userManager;
            _db = db;
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
                .Include(c => c.User) // include the registered user
                .FirstOrDefaultAsync(c => c.Id == id);

            if (company == null)
            {
                return NotFound();
            }

            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                // Delete the user
                if (company.User != null)
                {
                    var result = await _userManager.DeleteAsync(company.User);
                    if (!result.Succeeded)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest("Failed to delete the user associated with this company.");
                    }
                }

                // Delete the company
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

        // ================= EDIT USER =================
        [HttpPost]
        public async Task<JsonResult> EditUser([FromBody] EditUserDto dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.Id))
                return Json(new { success = false });

            var user = await _userManager.FindByIdAsync(dto.Id);
            if (user == null)
                return Json(new { success = false });

            var nameParts = dto.Name.Trim().Split(' ', 2);
            user.FirstName = nameParts[0];
            user.LastName = nameParts.Length > 1 ? nameParts[1] : "";
            user.Email = dto.Email;
            user.UserName = dto.Email;

            await _userManager.UpdateAsync(user);

            var currentRoles = await _userManager.GetRolesAsync(user);
            if (!currentRoles.Contains(dto.Role))
            {
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                await _userManager.AddToRoleAsync(user, dto.Role);
            }

            if (!string.IsNullOrWhiteSpace(dto.NewPassword))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                await _userManager.ResetPasswordAsync(user, token, dto.NewPassword);
            }

            return Json(new { success = true });
        }

        // ================= DELETE USER =================
        [HttpPost]
        public async Task<JsonResult> DeleteUser([FromBody] DeleteUserDto dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.Id))
                return Json(new { success = false });

            var user = await _userManager.FindByIdAsync(dto.Id);
            if (user == null)
                return Json(new { success = false });

            var result = await _userManager.DeleteAsync(user);
            return Json(new { success = result.Succeeded });
        }

        // ================= APPROVE USER =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> Approve([FromBody] IdDto dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.Id))
                return Json(new { success = false });

            var user = await _userManager.FindByIdAsync(dto.Id);
            if (user == null)
                return Json(new { success = false });

            user.IsApproved = true;
            var result = await _userManager.UpdateAsync(user);

            return Json(new { success = result.Succeeded });
        }

        // ================= REJECT USER =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> Reject([FromBody] IdDto dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.Id))
                return Json(new { success = false });

            var user = await _userManager.FindByIdAsync(dto.Id);
            if (user == null)
                return Json(new { success = false });

            var result = await _userManager.DeleteAsync(user);
            return Json(new { success = result.Succeeded });
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
