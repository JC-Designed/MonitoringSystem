using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace MonitoringSystem.Controllers
{
    // Removed [Authorize] so anyone can access
    public class CompanyPanelController : Controller
    {
        // ================== DASHBOARD ==================
        [AllowAnonymous] // Allows anyone to access
        public IActionResult Dashboard()
        {
            return View();
        }

        // ================== MANAGE INTERN / OJT ==================
        [AllowAnonymous] // Allows anyone to access
        public IActionResult ManageOJT()
        {
            return View();
        }
    }
}
