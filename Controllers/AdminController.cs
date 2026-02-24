using Microsoft.AspNetCore.Mvc;
using MonitoringSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MonitoringSystem.Controllers
{
    // Anyone can access, no DB required
    public class AdminController : Controller
    {
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
        public IActionResult Dashboard()
        {
            int schoolYear = GetSchoolYear();
            ViewBag.SchoolYear = schoolYear;

            // Fake users
            var allUsers = new List<ApplicationUser>
            {
                new ApplicationUser { Id="1", FirstName="John", LastName="Doe", Email="john@example.com", IsApproved=true, CreatedAt=new DateTime(schoolYear,1,10), Roles=new List<string>{"Student"} },
                new ApplicationUser { Id="2", FirstName="Jane", LastName="Smith", Email="jane@example.com", IsApproved=false, CreatedAt=new DateTime(schoolYear,2,15), Roles=new List<string>{"Company"} },
                new ApplicationUser { Id="3", FirstName="Admin", LastName="User", Email="admin@example.com", IsApproved=true, CreatedAt=new DateTime(schoolYear,3,20), Roles=new List<string>{"Admin"} },
            };

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

            // Fake total companies
            ViewBag.TotalCompanies = 5;

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

            var allUsers = new List<ApplicationUser>
            {
                new ApplicationUser { Id="1", FirstName="John", LastName="Doe", Email="john@example.com", IsApproved=true, CreatedAt=new DateTime(schoolYear,1,10), Roles=new List<string>{"Student"} },
                new ApplicationUser { Id="2", FirstName="Jane", LastName="Smith", Email="jane@example.com", IsApproved=false, CreatedAt=new DateTime(schoolYear,2,15), Roles=new List<string>{"Company"} },
                new ApplicationUser { Id="3", FirstName="Admin", LastName="User", Email="admin@example.com", IsApproved=true, CreatedAt=new DateTime(schoolYear,3,20), Roles=new List<string>{"Admin"} },
            };

            // Add Year property for display
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
            // Fake report data
            ViewBag.TotalUsers = 3;
            ViewBag.PendingUsers = 1;
            ViewBag.ApprovedUsers = 2;
            return View();
        }

        // ================= OTHER PAGES PLACEHOLDERS =================
        public IActionResult Company() => View();
        public IActionResult Messages() => View();

        // ================= DTOs =================
        public class PendingUserDto
        {
            public string Id { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty;
            public int RegisteredMonth { get; set; }
        }
    }
}
