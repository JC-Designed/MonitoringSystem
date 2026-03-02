using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MonitoringSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonitoringSystem.Controllers
{
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
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

            // Load all users from database
            var allUsers = _userManager.Users.ToList();

            // Pending / Approved counts
            var pendingUsersList = allUsers
                .Where(u => !u.IsApproved)
                .Select(u => new PendingUserDto
                {
                    Id = u.Id,
                    Email = u.Email,
                    Role = u.Roles?.FirstOrDefault() ?? "N/A",
                    RegisteredMonth = u.CreatedAt.Month
                })
                .ToList();

            ViewBag.PendingUsersList = pendingUsersList;
            ViewBag.PendingUsers = pendingUsersList.Count;
            ViewBag.ApprovedUsers = allUsers.Count(u => u.IsApproved);
            ViewBag.TotalUsers = allUsers.Count;

            // Total companies (example: users with Company role)
            ViewBag.TotalCompanies = allUsers.Count(u => u.Roles.Contains("Company"));

            // Monthly stats
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

        // ================= USERS =================
        public IActionResult Users()
        {
            int schoolYear = GetSchoolYear();
            ViewBag.SchoolYear = schoolYear;

            var allUsers = _userManager.Users.ToList();

            foreach (var u in allUsers)
                u.Year = u.CreatedAt.Year.ToString();

            return View(allUsers);
        }

        // ================= REGISTRATION =================
        public IActionResult Registration()
        {
            return View();
        }

        // ================= REPORTS =================
        public IActionResult Reports()
        {
            var allUsers = _userManager.Users.ToList();
            ViewBag.TotalUsers = allUsers.Count;
            ViewBag.PendingUsers = allUsers.Count(u => !u.IsApproved);
            ViewBag.ApprovedUsers = allUsers.Count(u => u.IsApproved);
            return View();
        }

        // ================= OTHER PAGES =================
        public IActionResult Company() => View();
        public IActionResult Messages() => View();

        // ================= ADMIN PROFILE FULL STACK =================
        [HttpGet]
        public async Task<IActionResult> GetAdminProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var model = new AdminProfileEditModel
            {
                FullName = user.FullName,
                Username = user.UserName,
                Email = user.Email,
                Address = user.Address,
                ProfileImage = user.ProfileImage
            };

            return PartialView("_AdminProfilePartial", model);
        }

        [HttpPost]
        public async Task<IActionResult> SaveAdminProfile([FromForm] AdminProfileEditModel model)
        {
            if (!ModelState.IsValid) return BadRequest("Invalid data.");

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            // ✅ Update FullName directly
            if (!string.IsNullOrEmpty(model.FullName))
            {
                user.FullName = model.FullName.Trim();
            }

            user.UserName = model.Username;
            user.Email = model.Email;
            user.Address = model.Address;

            if (!string.IsNullOrEmpty(model.ProfileImage))
                user.ProfileImage = model.ProfileImage;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
                return Json(new { success = true, message = "Profile updated successfully." });

            return Json(new { success = false, message = "Failed to update profile." });
        }

        // ================= DTOs =================
        public class PendingUserDto
        {
            public string Id { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty;
            public int RegisteredMonth { get; set; }
        }
    }

    // =================== VIEW MODEL FOR ADMIN PROFILE ===================
    public class AdminProfileEditModel
    {
        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string ProfileImage { get; set; } = string.Empty;
    }
}