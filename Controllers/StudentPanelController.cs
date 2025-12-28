using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
        public IActionResult Task()
        {
            ViewData["Title"] = "Task";
            return View();
        }

        // ======================= REPORTS =======================
        public IActionResult Reports()
        {
            ViewData["Title"] = "Generate Reports";
            return View();
        }
    }
}
