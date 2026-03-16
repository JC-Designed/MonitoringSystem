using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MonitoringSystem.Models;
using System;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace MonitoringSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _roleManager = roleManager;
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: /Account/Login (Form submit fallback)
        [HttpPost]
        public async Task<IActionResult> Login(string email, string password, bool rememberMe = false)
        {
            Console.WriteLine($"========== LOGIN ATTEMPT ==========");
            Console.WriteLine($"Email: {email}");
            Console.WriteLine($"Remember Me: {rememberMe}");

            try
            {
                // First check if user exists
                var user = await _userManager.FindByEmailAsync(email);

                if (user == null)
                {
                    Console.WriteLine($"User not found with email: {email}");
                    ViewBag.Error = "Invalid login attempt";
                    return View();
                }

                Console.WriteLine($"User found: ID={user.Id}, Email={user.Email}, Role={user.Role}");
                Console.WriteLine($"Status: {user.Status}, IsActive: {user.IsActive}");

                // Check user status
                if (user.Status == "Pending")
                {
                    Console.WriteLine($"User status is Pending");
                    ViewBag.Error = "Your account is pending approval. Please wait for an administrator to approve your account.";
                    return View();
                }
                else if (user.Status == "Declined")
                {
                    Console.WriteLine($"User status is Declined");
                    ViewBag.Error = "Your registration has been declined. Please contact support for assistance or register with a different email.";
                    return View();
                }
                else if (user.Status != "Approved" || !user.IsActive)
                {
                    Console.WriteLine($"User not approved or not active - Status: {user.Status}, IsActive: {user.IsActive}");
                    ViewBag.Error = "Your account is not approved or has been deactivated.";
                    return View();
                }

                // Attempt to sign in
                var result = await _signInManager.PasswordSignInAsync(email, password, rememberMe, lockoutOnFailure: false);

                Console.WriteLine($"SignIn Result: Succeeded={result.Succeeded}, IsLockedOut={result.IsLockedOut}, IsNotAllowed={result.IsNotAllowed}");

                if (result.Succeeded)
                {
                    Console.WriteLine($"Login successful for: {email}");

                    // Update last login
                    user.LastLogin = DateTime.Now;
                    user.UpdatedAt = DateTime.Now;
                    await _userManager.UpdateAsync(user);

                    // Redirect based on role
                    switch (user.Role)
                    {
                        case "Admin":
                            return RedirectToAction("Dashboard", "Admin");
                        case "Company":
                            return RedirectToAction("Dashboard", "Company");
                        case "Student":
                            return RedirectToAction("Dashboard", "Student");
                        default:
                            return RedirectToAction("Index", "Home");
                    }
                }
                else if (result.IsLockedOut)
                {
                    Console.WriteLine($"Account locked out: {email}");
                    ViewBag.Error = "Account locked out. Please try again later.";
                }
                else if (result.IsNotAllowed)
                {
                    Console.WriteLine($"Login not allowed: {email}");
                    ViewBag.Error = "Login not allowed. Please confirm your email.";
                }
                else
                {
                    Console.WriteLine($"Invalid password for: {email}");
                    ViewBag.Error = "Invalid login attempt";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EXCEPTION during login: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                ViewBag.Error = "An error occurred during login. Please try again.";
            }

            Console.WriteLine($"========== LOGIN END ==========");
            return View();
        }

        // POST: /Account/LoginCredentials (AJAX call from login form)
        [HttpPost]
        public async Task<IActionResult> LoginCredentials([FromBody] LoginModel model)
        {
            Console.WriteLine($"========== LOGIN CREDENTIALS AJAX ==========");
            Console.WriteLine($"Username: {model?.Username}");
            Console.WriteLine($"Password length: {model?.Password?.Length}");
            Console.WriteLine($"RememberMe: {model?.RememberMe}");

            try
            {
                if (model == null)
                {
                    Console.WriteLine("ERROR: Model is null");
                    return Json(new { success = false, message = "Invalid login data." });
                }

                if (string.IsNullOrEmpty(model.Username))
                {
                    Console.WriteLine("ERROR: Username is empty");
                    return Json(new { success = false, message = "Username is required." });
                }

                if (string.IsNullOrEmpty(model.Password))
                {
                    Console.WriteLine("ERROR: Password is empty");
                    return Json(new { success = false, message = "Password is required." });
                }

                Console.WriteLine("Attempting to find user by username...");
                // First check if user exists
                var user = await _userManager.FindByNameAsync(model.Username);

                if (user == null)
                {
                    Console.WriteLine("User not found by username, trying by email...");
                    // Try finding by email
                    user = await _userManager.FindByEmailAsync(model.Username);
                }

                if (user == null)
                {
                    Console.WriteLine($"User not found with username/email: {model.Username}");

                    // Check if any users exist in the database at all
                    var anyUser = _userManager.Users.FirstOrDefault();
                    Console.WriteLine($"Any user in database: {(anyUser != null ? anyUser.Email : "NO USERS FOUND")}");

                    return Json(new
                    {
                        success = false,
                        message = "Invalid username or password."
                    });
                }

                Console.WriteLine($"User found: ID={user.Id}, Email={user.Email}, Role={user.Role}, UserName={user.UserName}");
                Console.WriteLine($"Status: {user.Status}, IsActive: {user.IsActive}");

                // Check status
                if (user.Status == "Pending")
                {
                    Console.WriteLine($"User status is Pending");
                    return Json(new
                    {
                        success = false,
                        message = "Your account is pending approval. Please wait for admin confirmation."
                    });
                }
                else if (user.Status == "Declined")
                {
                    Console.WriteLine($"User status is Declined");
                    return Json(new
                    {
                        success = false,
                        message = "Your registration has been declined. Please contact administrator or register with a different email."
                    });
                }
                else if (user.Status != "Approved" && user.Role != "Admin")
                {
                    Console.WriteLine($"User not approved: {model.Username}");
                    return Json(new
                    {
                        success = false,
                        message = "Your account is not approved. Please contact administrator."
                    });
                }

                // Check if active
                if (!user.IsActive)
                {
                    Console.WriteLine($"User not active: {model.Username}");
                    return Json(new
                    {
                        success = false,
                        message = "Your account has been deactivated. Please contact administrator."
                    });
                }

                Console.WriteLine("Attempting password sign in...");
                Console.WriteLine($"Using username: {user.UserName}");

                var result = await _signInManager.PasswordSignInAsync(user.UserName, model.Password, model.RememberMe, lockoutOnFailure: false);

                Console.WriteLine($"SignIn Result: Succeeded={result.Succeeded}, IsLockedOut={result.IsLockedOut}, IsNotAllowed={result.IsNotAllowed}");

                if (result.Succeeded)
                {
                    Console.WriteLine($"Login successful for: {model.Username}");

                    // Update last login
                    user.LastLogin = DateTime.Now;
                    user.UpdatedAt = DateTime.Now;
                    await _userManager.UpdateAsync(user);

                    // Determine redirect URL based on role
                    string redirectUrl = "";
                    switch (user.Role)
                    {
                        case "Admin":
                            redirectUrl = "/Admin/Dashboard";
                            break;
                        case "Company":
                            redirectUrl = "/Company/Dashboard";
                            break;
                        case "Student":
                            redirectUrl = "/Student/Dashboard";
                            break;
                        default:
                            redirectUrl = "/Home/Index";
                            break;
                    }

                    return Json(new
                    {
                        success = true,
                        redirect = redirectUrl,
                        role = user.Role
                    });
                }

                if (result.IsLockedOut)
                {
                    Console.WriteLine($"Account locked out: {model.Username}");
                    return Json(new
                    {
                        success = false,
                        message = "Account locked out. Please try again later."
                    });
                }
                else if (result.IsNotAllowed)
                {
                    Console.WriteLine($"Login not allowed: {model.Username}");
                    return Json(new
                    {
                        success = false,
                        message = "Login not allowed. Please confirm your email."
                    });
                }
                else
                {
                    Console.WriteLine($"Invalid password for: {model.Username}");

                    // Verify password manually to see if it's a hashing issue
                    var passwordValid = await _userManager.CheckPasswordAsync(user, model.Password);
                    Console.WriteLine($"Manual password check: {passwordValid}");

                    return Json(new
                    {
                        success = false,
                        message = "Invalid username or password."
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("========== EXCEPTION DETAILS ==========");
                Console.WriteLine($"Exception Type: {ex.GetType().FullName}");
                Console.WriteLine($"Message: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    Console.WriteLine("Inner Exception:");
                    Console.WriteLine($"Type: {ex.InnerException.GetType().FullName}");
                    Console.WriteLine($"Message: {ex.InnerException.Message}");
                    Console.WriteLine($"Stack Trace: {ex.InnerException.StackTrace}");
                }
                Console.WriteLine("========================================");

                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register - UPDATED with PROGRAM field
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string studentId, string email, string program, int birthMonth, int birthDay, int birthYear)
        {
            try
            {
                // Validate required fields (including program)
                if (string.IsNullOrEmpty(studentId) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(program))
                {
                    ViewBag.Error = "All fields are required";
                    return View();
                }

                // Validate program values
                if (program != "BIT" && program != "BSIT")
                {
                    ViewBag.Error = "Please select a valid program (BIT or BSIT)";
                    return View();
                }

                // Create birth date from components
                DateTime? birthDate = null;
                try
                {
                    birthDate = new DateTime(birthYear, birthMonth, birthDay);
                }
                catch
                {
                    ViewBag.Error = "Invalid birth date";
                    return View();
                }

                // Check if user already exists
                var existingUser = await _userManager.FindByEmailAsync(email);

                if (existingUser != null)
                {
                    // If user was declined, allow them to re-register by updating the existing record
                    if (existingUser.Status == "Declined")
                    {
                        // Check if Student ID is already used by another active user
                        var existingStudentId = await _userManager.Users
                            .FirstOrDefaultAsync(u => u.StudentId == studentId && u.Id != existingUser.Id);

                        if (existingStudentId != null)
                        {
                            ViewBag.Error = "Student ID is already registered";
                            return View();
                        }

                        // Update the existing user's information (INCLUDE PROGRAM)
                        existingUser.StudentId = studentId;
                        existingUser.BirthDate = birthDate;
                        existingUser.Program = program;
                        existingUser.Status = "Pending";
                        existingUser.UpdatedAt = DateTime.Now;

                        var updateResult = await _userManager.UpdateAsync(existingUser);

                        if (updateResult.Succeeded)
                        {
                            // Reset password
                            var token = await _userManager.GeneratePasswordResetTokenAsync(existingUser);
                            var passwordResult = await _userManager.ResetPasswordAsync(existingUser, token, studentId);

                            if (passwordResult.Succeeded)
                            {
                                TempData["Success"] = "Your registration has been resubmitted for approval.";
                                return RedirectToAction("Register");
                            }
                            else
                            {
                                foreach (var error in passwordResult.Errors)
                                {
                                    ViewBag.Error += error.Description + " ";
                                }
                                return View();
                            }
                        }
                        else
                        {
                            foreach (var error in updateResult.Errors)
                            {
                                ViewBag.Error += error.Description + " ";
                            }
                            return View();
                        }
                    }
                    else if (existingUser.Status == "Pending")
                    {
                        ViewBag.Error = "You already have a pending registration. Please wait for approval.";
                        return View();
                    }
                    else if (existingUser.Status == "Approved")
                    {
                        ViewBag.Error = "An account with this email already exists. Please login.";
                        return View();
                    }
                }

                // Check if Student ID already exists (for non-declined users)
                var studentIdExists = await _userManager.Users
                    .FirstOrDefaultAsync(u => u.StudentId == studentId && u.Status != "Declined");

                if (studentIdExists != null)
                {
                    ViewBag.Error = "Student ID is already registered";
                    return View();
                }

                // Create new user (INCLUDE PROGRAM)
                var user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FullName = "",
                    Role = "Student",
                    StudentId = studentId,
                    Program = program,
                    BirthDate = birthDate,
                    CreatedAt = DateTime.Now,
                    Status = "Pending",
                    IsActive = true,
                    ProfileImage = "/images/defaultpicture.jpg",
                    BannerImage = "/images/banner-placeholder.jpg"
                };

                // Use studentId as the password
                var result = await _userManager.CreateAsync(user, studentId);

                if (result.Succeeded)
                {
                    // Create student record
                    var student = new Student
                    {
                        UserId = user.Id,
                        StudentId = studentId,
                        CreatedAt = DateTime.Now
                    };

                    _context.Students.Add(student);
                    await _context.SaveChangesAsync();

                    // Assign role
                    await _userManager.AddToRoleAsync(user, "Student");

                    TempData["Success"] = "Registration successful! Your account is pending approval. You will be able to login once an administrator approves your account.";
                    return RedirectToAction("Register");
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ViewBag.Error += error.Description + " ";
                    }
                    return View();
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Registration failed: " + ex.Message;
                return View();
            }
        }

        // POST: /Account/RegisterCompany - FIXED FOREIGN KEY ISSUE
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterCompany([FromBody] RegisterCompanyViewModel model)
        {
            try
            {
                Console.WriteLine("========== REGISTER COMPANY CALLED ==========");
                Console.WriteLine($"CompanyName: {model?.CompanyName}");
                Console.WriteLine($"Email: {model?.Email}");

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors)
                                                  .Select(e => e.ErrorMessage)
                                                  .ToList();
                    return Json(new { success = false, message = string.Join(", ", errors) });
                }

                // Check if email already exists
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    return Json(new { success = false, message = "Email already registered" });
                }

                // Generate new Company ID
                int newCompanyId = await GenerateCompanyId();

                // Create new company user
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.CompanyName,
                    Role = "Company",
                    Status = "Approved",
                    MobileNumber = model.MobileNumber,
                    Address = model.Address,
                    CompanyID = newCompanyId,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    IsActive = true
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // Assign to Company role
                    if (!await _roleManager.RoleExistsAsync("Company"))
                    {
                        await _roleManager.CreateAsync(new IdentityRole("Company"));
                    }
                    await _userManager.AddToRoleAsync(user, "Company");

                    // Create Company record - FIXED FOREIGN KEY ISSUE
                    var company = new Company
                    {
                        UserId = user.Id,
                        CompanyId = newCompanyId.ToString(),
                        CompanyName = model.CompanyName,
                        CompanyDescription = model.Description,
                        Industry = model.Type,
                        Website = model.Website,
                        TaxId = model.TinId,
                        SecId = model.SecId,
                        YearEstablished = DateTime.Now.Year,
                        IsVerified = true,
                        VerifiedBy = null, // Set to null to avoid foreign key constraint
                        VerifiedDate = DateTime.Now,
                        CreatedAt = DateTime.Now
                    };

                    _context.Companies.Add(company);
                    await _context.SaveChangesAsync();

                    Console.WriteLine("Company created successfully with ID: " + company.Id);

                    return Json(new
                    {
                        success = true,
                        message = "Company created successfully",
                        companyId = company.Id,
                        userId = user.Id
                    });
                }
                else
                {
                    var errorsList = string.Join(", ", result.Errors.Select(e => e.Description));
                    return Json(new { success = false, message = errorsList });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EXCEPTION: {ex.Message}");
                Console.WriteLine($"Inner exception: {ex.InnerException?.Message}");

                var errorMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return Json(new { success = false, message = "An error occurred while creating the company: " + errorMessage });
            }
        }

        private async Task<int> GenerateCompanyId()
        {
            // Get the last company ID and increment
            var lastCompany = await _context.Companies
                .OrderByDescending(c => c.Id)
                .FirstOrDefaultAsync();

            return (lastCompany?.Id ?? 0) + 1;
        }

        // POST: /Account/ForgotPassword
        [HttpPost]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordModel model)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(model.Email);

                if (user == null)
                {
                    // Don't reveal that the user does not exist
                    return Json(new
                    {
                        success = true,
                        message = "If your email is registered, you will receive a password reset link."
                    });
                }

                // Generate password reset token
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);

                // Build reset link
                var resetLink = Url.Action("ResetPassword", "Account",
                    new { email = model.Email, token = token },
                    protocol: HttpContext.Request.Scheme);

                // Here you would send email with resetLink
                // For now, just return success

                return Json(new
                {
                    success = true,
                    message = "Password reset link has been sent to your email."
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "An error occurred. Please try again later."
                });
            }
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        // GET: /Account/PendingApproval
        [HttpGet]
        public IActionResult PendingApproval()
        {
            return View();
        }
    }

    // Model classes for API calls
    public class LoginModel
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public bool RememberMe { get; set; }
    }

    public class ForgotPasswordModel
    {
        public string Email { get; set; }
    }
}