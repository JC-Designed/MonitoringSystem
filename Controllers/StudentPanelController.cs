using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonitoringSystem.Models;
using System;
using System.Collections.Generic;

namespace MonitoringSystem.Controllers
{
    // No [Authorize] on the controller
    public class StudentPanelController : Controller
    {
        // ======================= DASHBOARD =======================
        [AllowAnonymous] // Public
        public IActionResult Dashboard()
        {
            ViewData["Title"] = "Dashboard";
            return View();
        }

        // ======================= TASKS =======================
        [AllowAnonymous] // Public
        public IActionResult Tasks()
        {
            ViewData["Title"] = "TASK LIST";

            var tasks = new List<StudentTask>
            {
                new StudentTask
                {
                    Id = 1,
                    Title = "Design Logo",
                    Company = "ABC Corp",
                    Deadline = DateTime.Now.AddDays(5),
                    Status = "Ongoing",
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
                    AttachmentPath = "/files/report.pdf"
                },
                new StudentTask
                {
                    Id = 3,
                    Title = "Update Website",
                    Company = "DesignHub",
                    Deadline = DateTime.Now.AddDays(10),
                    Status = "Ongoing",
                    Description = "Update the website with the new product catalog.",
                    AttachmentPath = null
                }
            };

            return View(tasks);
        }

        // ======================= MESSAGES (Optional Protected) =======================
        [Authorize(Roles = "Student")]
        public IActionResult Messages()
        {
            ViewData["Title"] = "Messages";
            return View();
        }

        // ======================= REPORTS (Optional Protected) =======================
        [Authorize(Roles = "Student")]
        public IActionResult Reports()
        {
            ViewData["Title"] = "Reports";
            return View();
        }
    }
}
//hi