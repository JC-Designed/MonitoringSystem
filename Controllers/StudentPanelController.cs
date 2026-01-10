using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonitoringSystem.Models;
using System;
using System.Collections.Generic;

namespace MonitoringSystem.Controllers
{
    [Authorize(Roles = "Student")] // Only users in Student role can access
    public class StudentPanelController : Controller
    {
        // ======================= DASHBOARD =======================
        public IActionResult Dashboard()
        {
            ViewData["Title"] = "Dashboard";
            return View();
        }

        // ======================= MESSAGES =======================
        public IActionResult Messages()
        {
            ViewData["Title"] = "Messages";
            return View();
        }

        // ======================= TASK =======================
        public IActionResult Tasks()
        {
            ViewData["Title"] = "Tasks";

            // ===== SAMPLE TASK DATA =====
            var tasks = new List<StudentTask>
            {
                new StudentTask
                {
                    Id = 1,
                    Title = "Design Logo",
                    Company = "ABC Corp",
                    Deadline = DateTime.Now.AddDays(5),
                    Status = "Pending",
                    Description = "Create a modern logo for ABC Corp's new product.",
                    AttachmentPath = null
                },
                new StudentTask
                {
                    Id = 2,
                    Title = "Submit Report",
                    Company = "XYZ Ltd",
                    Deadline = DateTime.Now.AddDays(-2),
                    Status = "Completed",
                    Description = "Submit the monthly monitoring report.",
                    AttachmentPath = "/files/report.pdf" // example
                },
                new StudentTask
                {
                    Id = 3,
                    Title = "Update Website",
                    Company = "DesignHub",
                    Deadline = DateTime.Now.AddDays(10),
                    Status = "Overdue",
                    Description = "Update the website with the new product catalog.",
                    AttachmentPath = null
                }
            };

            return View(tasks);
        }

        // ======================= REPORTS =======================
        public IActionResult Reports()
        {
            ViewData["Title"] = "Reports";
            return View();
        }
    }
}
