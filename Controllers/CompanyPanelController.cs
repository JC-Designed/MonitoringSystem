using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MonitoringSystem.Data;
using MonitoringSystem.Models;
using System.Linq;

namespace MonitoringSystem.Controllers
{
    [Authorize(Roles = "Company")] // Only accessible by Company users
    public class CompanyPanelController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CompanyPanelController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ================== DASHBOARD ==================
        public IActionResult Dashboard()
        {
            return View();
        }

        // ================== MESSAGES ==================
        public IActionResult Messages()
        {
            return View();
        }

        // ================== MANAGE OJT ==================
        public IActionResult ManageOJT()
        {
            return View();
        }

        // ================== REPORTS ==================
        public IActionResult Reports()
        {
            return View();
        }

        // ================== OPTIONAL CRUD ==================
        // GET: CompanyPanel/Index
        public IActionResult Index()
        {
            var companies = _context.Companies.ToList();
            return View(companies);
        }
    }
}
