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
            return DateTime.Now.Year;
        }

        // ================= DASHBOARD =================
        public async Task<IActionResult> Dashboard()
        {
            int schoolYear = GetSchoolYear();
            var allUsers = await _userManager.Users.ToListAsync();

            foreach (var user in allUsers)
                user.Roles = (await _userManager.GetRolesAsync(user)).ToList();

            ViewBag.PendingUsers = allUsers.Count(u => !u.IsApproved);
            ViewBag.ApprovedUsers = allUsers.Count(u => u.IsApproved);
            ViewBag.TotalUsers = allUsers.Count;
            ViewBag.TotalCompanies = _db.Companies.Include(c => c.User).Count(c => c.User != null);

            return View(allUsers);
        }

        // ================= USERS PAGE =================
        public async Task<IActionResult> Users()
        {
            var allUsers = await _userManager.Users.ToListAsync();
            foreach (var user in allUsers)
                user.Roles = (await _userManager.GetRolesAsync(user)).ToList();

            return View(allUsers);
        }

        // ================= COMPANY PAGE =================
        public IActionResult Company()
        {
            var companies = _db.Companies.Include(c => c.User).ToList();
            return View(companies);
        }

        // ================= MESSAGES PAGE =================
        public IActionResult Messages()
        {
            return View();
        }

        // ================= REPORTS PAGE =================
        public IActionResult Reports()
        {
            // FRONT-END ONLY demo data
            var demoData = new List<ReportDemo>
            {
                new ReportDemo { Id = 1, ReporterName = "John Doe", ReporterType = "Student", Status = "Unread", Details = "Demo report 1", DateSubmitted = DateTime.Now },
                new ReportDemo { Id = 2, ReporterName = "Acme Corp", ReporterType = "Company", Status = "Read", Details = "Demo report 2", DateSubmitted = DateTime.Now.AddDays(-1) },
                new ReportDemo { Id = 3, ReporterName = "Jane Smith", ReporterType = "Student", Status = "Unread", Details = "Demo report 3", DateSubmitted = DateTime.Now.AddDays(-2) }
            };

            // MAP demo → real view model
            var reports = demoData.Select(r => new MonitoringSystem.Models.Report
            {
                Id = r.Id,
                ReporterName = r.ReporterName,
                ReporterType = r.ReporterType,
                Status = r.Status,
                Details = r.Details,
                DateSubmitted = r.DateSubmitted
            }).ToList();

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

        // ================= DEMO REPORT (KEEPED, RENAMED) =================
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
