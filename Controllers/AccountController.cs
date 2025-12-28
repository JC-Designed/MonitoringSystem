using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MonitoringSystem.Models;
using MonitoringSystem.Data; // For ApplicationDbContext
using System.Linq;
using System.Threading.Tasks;

namespace MonitoringSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _db; // Added for Company

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext db)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _db = db;
        }

        // ======================= LOGIN =======================
        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password, string roleString)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user != null)
            {
                if (!user.IsApproved)
                {
                    ViewBag.Error = "Account not approved.";
                    return View();
                }

                var roles = await _userManager.GetRolesAsync(user);
                if (!roles.Contains(roleString))
                {
                    ViewBag.Error = "Invalid role selected.";
                    return View();
                }

                var result = await _signInManager.PasswordSignInAsync(user, password, false, false);
                if (result.Succeeded)
                {
                    // ✅ Redirect based on role automatically
                    if (await _userManager.IsInRoleAsync(user, "Admin"))
                        return RedirectToAction("Dashboard", "Admin");
                    else if (await _userManager.IsInRoleAsync(user, "Company"))
                        return RedirectToAction("Dashboard", "CompanyPanel");
                    else if (await _userManager.IsInRoleAsync(user, "Student"))
                        return RedirectToAction("Dashboard", "StudentPanel"); // Added Student redirect
                }
            }

            ViewBag.Error = "Invalid login attempt.";
            return View();
        }

        // ======================= REGISTER =======================
        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(string email, string password, string roleString, string companyName = null)
        {
            if (string.IsNullOrEmpty(roleString))
            {
                ViewBag.Error = "Please select a role.";
                return View();
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                IsApproved = false
            };

            var result = await _userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, roleString);

                // ✅ Auto-create company if role is Company
                if (roleString == "Company")
                {
                    if (string.IsNullOrEmpty(companyName))
                        companyName = email; // fallback if no company name provided

                    var company = new Company
                    {
                        Name = companyName,
                        Email = email,
                        UserId = user.Id
                    };

                    _db.Companies.Add(company);
                    await _db.SaveChangesAsync();
                }

                TempData["Success"] = "Registration successful. Wait for admin approval.";
                return View();
            }

            ViewBag.Error = result.Errors.FirstOrDefault()?.Description;
            return View();
        }

        // ======================= LOGOUT =======================
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }
    }
}
