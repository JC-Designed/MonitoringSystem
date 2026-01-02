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

            // Get all users up to the current school year
            var allUsers = await _userManager.Users
                .Where(u => u.CreatedAt.Year <= schoolYear)
                .ToListAsync();

            foreach (var user in allUsers)
                user.Roles = (await _userManager.GetRolesAsync(user)).ToList();

            // ================== PENDING USERS LIST ==================
            var pendingUsersList = allUsers
                .Where(u => !u.IsApproved && u.CreatedAt.Year == schoolYear)
                .Select(u => new PendingUserDto
                {
                    Id = u.Id,
                    Email = u.Email,
                    Role = (u.Roles != null && u.Roles.Count > 0) ? u.Roles.First() : "N/A"
                })
                .ToList();

            ViewBag.PendingUsersList = pendingUsersList;

            // ================== DASHBOARD COUNTERS ==================
            ViewBag.PendingUsers = pendingUsersList.Count;
            ViewBag.ApprovedUsers = allUsers.Count(u => u.IsApproved && u.CreatedAt.Year == schoolYear);
            ViewBag.TotalUsers = allUsers.Count(u => u.CreatedAt.Year == schoolYear);

            ViewBag.TotalCompanies = _db.Companies
                .Include(c => c.User)
                .Count(c => c.User != null && c.User.CreatedAt.Year == schoolYear);

            // ================= CHART DATA =================
            var monthlyRegistrations = new int[12]; // 0 index = Jan
            var totalUsersByMonth = new int[12];

            for (int month = 1; month <= 12; month++)
            {
                monthlyRegistrations[month - 1] = allUsers
                    .Count(u => u.CreatedAt.Year == schoolYear && u.CreatedAt.Month == month);

                totalUsersByMonth[month - 1] = allUsers
                    .Count(u => u.CreatedAt.Year < schoolYear || (u.CreatedAt.Year == schoolYear && u.CreatedAt.Month <= month));
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

            // For now, we will use demo data to test the front-end
            var reports = new List<Report>
    {
        new Report { Id = 1, ReporterName = "John Doe", ReporterType = "Student", Status = "Unread", Details = "Demo report 1", DateSubmitted = new DateTime(schoolYear, 6, 1) },
        new Report { Id = 2, ReporterName = "Acme Corp", ReporterType = "Company", Status = "Read", Details = "Demo report 2", DateSubmitted = new DateTime(schoolYear, 5, 10) },
        new Report { Id = 3, ReporterName = "Jane Smith", ReporterType = "Student", Status = "Unread", Details = "Demo report 3", DateSubmitted = new DateTime(schoolYear, 4, 20) }
    };

            return View(reports);
        }


        // ================= DTOs =================
        public class PendingUserDto
        {
            public string Id { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty;
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

        // ================= DEMO REPORT =================
        public class ReportDemo
        {
            public int Id { get; set; }
            public string ReporterName { get; set; } = string.Empty;
            public string ReporterType { get; set; } = string.Empty;
            public string Status { get; set; } = "Unread";
            public string Details { get; set; } = string.Empty;
            public DateTime DateSubmitted { get; set; }
        }
    }
}
