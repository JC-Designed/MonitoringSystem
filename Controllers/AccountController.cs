using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MonitoringSystem.Models;
using MonitoringSystem.Models.ParamModel;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MonitoringSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
        }

        // ================== LOGIN PAGE ==================
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // ================== LOGIN AJAX / JSON ==================
        [HttpPost]
        public async Task<IActionResult> LoginCredentials([FromBody] LoginParamModel model)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Invalid input." });

            var result = await _signInManager.PasswordSignInAsync(
                model.Username,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: false
            );

            if (!result.Succeeded)
                return Json(new { success = false, message = "Invalid username or password." });

            var user = await _userManager.FindByNameAsync(model.Username);
            var roles = await _userManager.GetRolesAsync(user);
            string role = roles.FirstOrDefault() ?? "";

            // Redirect based on role
            string redirectUrl = "/";
            if (role == "Admin")
                redirectUrl = Url.Action("Dashboard", "Admin");
            else if (role == "Company")
                redirectUrl = Url.Action("Dashboard", "Company");
            else if (role == "Student")
                redirectUrl = Url.Action("Dashboard", "Student");

            return Json(new
            {
                success = true,
                redirect = redirectUrl,
                role = role
            });
        }

        // ================== REGISTER PAGE (GET) ==================
        [HttpGet]
        public IActionResult Register()
        {
            return View(); // Opens Register.cshtml
        }

        // ================== REGISTER POST ==================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterPost() // ✅ renamed from Register() to RegisterPost()
        {
            try
            {
                // Pull form values from Register.cshtml
                var fullName = Request.Form["fullName"].ToString();
                var studentId = Request.Form["studentId"].ToString();
                var mobileNumber = Request.Form["mobileNumber"].ToString();
                var facebook = Request.Form["Facebook"].ToString();
                var emergencyContact = Request.Form["emergencyContact"].ToString();
                var address = Request.Form["address"].ToString();
                var program = Request.Form["program"].ToString();
                var birthMonth = Request.Form["birthMonth"].ToString();
                var birthDay = Request.Form["birthDay"].ToString();
                var birthYear = Request.Form["birthYear"].ToString();
                var gender = Request.Form["gender"].ToString();
                var email = Request.Form["email"].ToString();
                var password = Request.Form["password"].ToString();

                // Check if user exists
                var existingUser = await _userManager.FindByNameAsync(email);
                if (existingUser != null)
                {
                    ViewBag.Error = "Email/Username already exists.";
                    return View("Register");
                }

                // Split full name
                var firstName = fullName.Split(' ').FirstOrDefault() ?? "";
                var lastName = string.Join(" ", fullName.Split(' ').Skip(1));

                // Create user
                var user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    FirstName = firstName,
                    LastName = lastName,
                    Role = "Student",
                    Contact = mobileNumber,
                    Address = address,
                    Gender = gender,
                    BirthDate = new DateTime(int.Parse(birthYear), int.Parse(birthMonth), int.Parse(birthDay)),
                    CreatedAt = DateTime.Now
                };

                var result = await _userManager.CreateAsync(user, password);
                if (!result.Succeeded)
                {
                    ViewBag.Error = string.Join(", ", result.Errors.Select(e => e.Description));
                    return View("Register");
                }

                // Ensure Student role exists
                if (!await _roleManager.RoleExistsAsync("Student"))
                    await _roleManager.CreateAsync(new IdentityRole("Student"));

                await _userManager.AddToRoleAsync(user, "Student");

                TempData["Success"] = "Account created successfully! You can now login.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View("Register");
            }
        }

        // ================== RESET PASSWORD ==================
        [HttpPost]
        public async Task<IActionResult> ResetPassword([FromBody] LoginParamModel model)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Invalid input." });

            var user = await _userManager.FindByNameAsync(model.Username);
            if (user == null)
                return Json(new { success = false, message = "User not found." });

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, model.Password);

            if (!result.Succeeded)
                return Json(new { success = false, message = string.Join(", ", result.Errors.Select(e => e.Description)) });

            return Json(new { success = true, message = $"Password for {model.Username} has been reset successfully." });
        }

        // ================== LOGOUT ==================
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }
    }
}