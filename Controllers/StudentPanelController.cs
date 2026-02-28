using Microsoft.AspNetCore.Mvc;
using MonitoringSystem.Models;
using System;
using System.Collections.Generic;

namespace MonitoringSystem.Controllers
{
    // No [Authorize] attributes, all pages accessible
    public class StudentPanelController : Controller
    {
        // ======================= DASHBOARD =======================
        public IActionResult Dashboard()
        {
            ViewData["Title"] = "Dashboard";
            return View(); // Views/StudentPanel/Dashboard.cshtml
        }

        // ======================= TASKS =======================
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

            return View(tasks); // Views/StudentPanel/Tasks.cshtml
        }

        // ======================= MESSAGES =======================
        public IActionResult Messages()
        {
            ViewData["Title"] = "Messages";
            return View(); // Views/StudentPanel/Messages.cshtml
        }

        // ======================= REPORT =======================
        public IActionResult Report()
        {
            ViewData["Title"] = "Report";

            var reports = new List<StudentTask>
            {
                new StudentTask
                {
                    Id = 1,
                    Title = "Monthly Progress",
                    Company = "ABC Corp",
                    Deadline = DateTime.Now.AddDays(-1),
                    Status = "Completed",
                    Description = "Detailed report for the monthly progress of tasks.",
                    AttachmentPath = "/files/monthly-progress.pdf"
                },
                new StudentTask
                {
                    Id = 2,
                    Title = "Website Update Summary",
                    Company = "DesignHub",
                    Deadline = DateTime.Now.AddDays(3),
                    Status = "Ongoing",
                    Description = "Summary report of website updates and changes.",
                    AttachmentPath = null
                }
            };

            return View(reports); // Views/StudentPanel/Report.cshtml
        }
    }
}