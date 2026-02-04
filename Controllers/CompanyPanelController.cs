using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace MonitoringSystem.Controllers
{
    [Authorize(Roles = "Company")] // Only accessible by Company users
    public class CompanyPanelController : Controller
    {
        // ================== DASHBOARD ==================
        public IActionResult Dashboard()
        {
            return View();
        }

        // ================== MANAGE INTERN / OJT ==================
        public IActionResult ManageOJT()
        {
            return View();
        }
    }
}
