using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MonitoringSystem.Models;
using MonitoringSystem.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MonitoringSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _db;

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
        public async Task<IActionResult> Login(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Please enter email and password.";
                return View();
            }

            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                ViewBag.Error = "Invalid login attempt.";
                return View();
            }

            // Check approval
            if (!user.IsApproved)
            {
                ViewBag.Error = "Account not approved.";
                return View();
            }

            var result = await _signInManager.PasswordSignInAsync(user, password, false, false);

            if (!result.Succeeded)
            {
                ViewBag.Error = "Invalid login attempt.";
                return View();
            }

            // 🔥 AUTO ROLE DETECTION
            var roles = await _userManager.GetRolesAsync(user);

            if (roles.Contains("Admin"))
                return RedirectToAction("Dashboard", "Admin");

            if (roles.Contains("Company"))
                return RedirectToAction("Dashboard", "CompanyPanel");

            if (roles.Contains("Student"))
                return RedirectToAction("Dashboard", "StudentPanel");

            // fallback
            return RedirectToAction("Login");
        }

        // ======================= REGISTER =======================
        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(
            string firstName,
            string lastName,
            string roleString,
            string gender,
            int birthDay,
            int birthMonth,
            int birthYear,
            string email,
            string password,
            string companyName = null)
        {
            if (string.IsNullOrEmpty(roleString))
            {
                ViewBag.Error = "Please select a role.";
                return View();
            }

            DateTime birthDate;
            try
            {
                birthDate = new DateTime(birthYear, birthMonth, birthDay);
            }
            catch
            {
                ViewBag.Error = "Invalid birthdate.";
                return View();
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                CreatedAt = DateTime.Now,
                IsApproved = false,
                FirstName = firstName,
                LastName = lastName,
                Gender = gender,
                BirthDate = birthDate
            };

            var result = await _userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, roleString);

                if (roleString == "Company")
                {
                    if (string.IsNullOrEmpty(companyName))
                        companyName = email;

                    var company = new Company
                    {
                        Name = companyName,
                        Email = email,
                        UserId = user.Id
                    };

                    _db.Companies.Add(company);
                    await _db.SaveChangesAsync();
                }

                TempData["Success"] = "Registration successful! Please wait for admin approval.";
                return View();
            }

            ViewBag.Error = string.Join(", ", result.Errors.Select(e => e.Description));
            return View();
        }

        // ======================= LOGOUT =======================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }
    }
}
