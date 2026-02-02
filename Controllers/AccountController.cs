using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MonitoringSystem.Models;
using MonitoringSystem.Data;
using System;
using System.Linq;
using System.Threading.Tasks;
using MonitoringSystem.Models.ParamModel;

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
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> LoginCredentials([FromBody] UserAccountParamModel userModel)
        {
            if (userModel == null || string.IsNullOrEmpty(userModel.Username))
                return Json(new { success = false, message = "Invalid request format." });

            var user = await _userManager.FindByNameAsync(userModel.Username);
            if (user == null)
                return Json(new { success = false, message = "User not found." });

            var passwordValid = await _userManager.CheckPasswordAsync(user, userModel.Password);
            if (!passwordValid)
                return Json(new { success = false, message = "Invalid password." });

            await _signInManager.SignInAsync(user, isPersistent: userModel.RememberMe);

            var roles = await _userManager.GetRolesAsync(user);
            string redirectUrl = "/";
            if (roles.Contains("Admin")) redirectUrl = "/Admin/Dashboard";
            else if (roles.Contains("Company")) redirectUrl = "/CompanyPanel/Dashboard";
            else if (roles.Contains("Student")) redirectUrl = "/StudentPanel/Dashboard";

            return Json(new { success = true, message = "Login successful", redirect = redirectUrl });
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
