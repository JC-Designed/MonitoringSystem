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

            // Ensure admin exists
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
            {
                // Populate Roles for Role filter
                var roles = await _userManager.GetRolesAsync(user);
                user.Roles = roles.ToList();

                // Populate Year column
                user.Year = user.CreatedAt.Year.ToString();
            }

            return View(allUsers);
        }

        // ================= REGISTRATION PAGE =================
        [HttpGet]
        public IActionResult Registration()
        {
            // Just return the view
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registration(RegisterDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            var existingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("", "Email is already registered.");
                return View(dto);
            }

            var user = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                IsApproved = true,
                CreatedAt = DateTime.Now
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(dto.Role))
                    await _userManager.AddToRoleAsync(user, dto.Role);

                TempData["SuccessMessage"] = "User registered successfully!";
                return RedirectToAction("Users");
            }
            else
            {
                foreach (var err in result.Errors)
                    ModelState.AddModelError("", err.Description);
            }

            return View(dto);
        }

        // ================= EDIT USER =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> EditUser([FromBody] EditUserDto dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.Id))
                return Json(new { success = false, message = "Invalid data" });

            var user = await _userManager.FindByIdAsync(dto.Id);
            if (user == null)
                return Json(new { success = false, message = "User not found" });

            user.FirstName = dto.FirstName;
            user.LastName = dto.LastName;
            user.Email = dto.Email;
            user.UserName = dto.Email;

            // Update Role
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Count > 0)
                await _userManager.RemoveFromRolesAsync(user, currentRoles);

            if (!string.IsNullOrEmpty(dto.Role))
                await _userManager.AddToRoleAsync(user, dto.Role);

            // Update password if provided
            if (!string.IsNullOrEmpty(dto.Password))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passResult = await _userManager.ResetPasswordAsync(user, token, dto.Password);
                if (!passResult.Succeeded)
                    return Json(new { success = false, message = "Password update failed" });
            }

            var result = await _userManager.UpdateAsync(user);
            return Json(new { success = result.Succeeded });
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

        // ================= APPROVE / REJECT =================
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

        // ================= OTHER PAGES =================
        public IActionResult Company() { return View(); }
        public IActionResult Messages() { return View(); }
        public IActionResult Reports() { return View(); }

        // ================= PROFILE =================
        [HttpGet]
        public async Task<JsonResult> GetAdminProfile() { return Json(new { success = true }); }
        [HttpPost]
        public async Task<JsonResult> UpdateProfile() { return Json(new { success = true }); }

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
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        public class IdDto
        {
            public string Id { get; set; } = string.Empty;
        }

        public class RegisterDto
        {
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Role { get; set; } = "Student";
            public string Password { get; set; } = string.Empty;
            public string ConfirmPassword { get; set; } = string.Empty;
        }
    }
}
