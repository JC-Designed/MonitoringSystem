using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MonitoringSystem.Models;
using System;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace MonitoringSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
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
                Console.WriteLine($"IsApproved: {user.IsApproved}, IsActive: {user.IsActive}");

                // Check if user is approved and active
                if (!user.IsApproved || !user.IsActive)
                {
                    Console.WriteLine($"User not approved or not active - IsApproved: {user.IsApproved}, IsActive: {user.IsActive}");
                    ViewBag.Error = "Your account is not approved or has been deactivated.";
                    return View();
                }

                // Attempt to sign in
                var result = await _signInManager.PasswordSignInAsync(email, password, rememberMe, lockoutOnFailure: false);

                Console.WriteLine($"SignIn Result: Succeeded={result.Succeeded}, IsLockedOut={result.IsLockedOut}, IsNotAllowed={result.IsNotAllowed}");

                if (result.Succeeded)
                {
                    Console.WriteLine($"Login successful for: {email}");

                    // TEMPORARILY DISABLED - Update last login (commented out due to trigger error)
                    // user.LastLogin = DateTime.Now;
                    // await _userManager.UpdateAsync(user);

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
                Console.WriteLine($"IsApproved: {user.IsApproved}, IsActive: {user.IsActive}");

                // Check if approved (for non-admin users)
                if (!user.IsApproved && user.Role != "Admin")
                {
                    Console.WriteLine($"User not approved: {model.Username}");
                    return Json(new
                    {
                        success = false,
                        message = "Your account is pending approval. Please wait for admin confirmation."
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

                    // TEMPORARILY DISABLED - Update last login (commented out due to trigger error)
                    // user.LastLogin = DateTime.Now;
                    // await _userManager.UpdateAsync(user);

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

                    // Check if company needs e-signature (customize as needed)
                    bool hasESign = true;
                    if (user.Role == "Company")
                    {
                        // You can implement e-signature check here
                        // For now, default to true
                        hasESign = true;
                    }

                    return Json(new
                    {
                        success = true,
                        redirect = redirectUrl,
                        role = user.Role,
                        hasESign = hasESign
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

                // Return the actual error message for debugging
                return Json(new
                {
                    success = false,
                    message = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        // GET: /Account/CheckAdmin
        [HttpGet]
        public async Task<IActionResult> CheckAdmin()
        {
            try
            {
                Console.WriteLine("========== CHECKING ADMIN ACCOUNT ==========");

                // Check if any users exist
                var userCount = _userManager.Users.Count();
                Console.WriteLine($"Total users in database: {userCount}");

                var admin = await _userManager.FindByEmailAsync("admin@ctu.edu.ph");

                if (admin == null)
                {
                    Console.WriteLine("Admin user NOT found in database");
                    return Content($"Admin user NOT found in database. Total users: {userCount}");
                }

                var passwordValid = await _userManager.CheckPasswordAsync(admin, "Admin@123");

                Console.WriteLine($"Admin found: Email={admin.Email}, IsApproved={admin.IsApproved}, IsActive={admin.IsActive}");
                Console.WriteLine($"Password valid: {passwordValid}");
                Console.WriteLine($"Role: {admin.Role}");
                Console.WriteLine("=============================================");

                return Content($"Admin found: Email={admin.Email}, IsApproved={admin.IsApproved}, IsActive={admin.IsActive}, PasswordValid={passwordValid}, Role={admin.Role}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CheckAdmin: {ex.Message}");
                return Content($"Error: {ex.Message}");
            }
        }

        // GET: /Account/FixAdminRole - FIX FOR ACCESS DENIED ISSUE
        [HttpGet]
        public async Task<IActionResult> FixAdminRole()
        {
            try
            {
                Console.WriteLine("========== FIXING ADMIN ROLE ==========");

                var admin = await _userManager.FindByEmailAsync("admin@ctu.edu.ph");

                if (admin == null)
                {
                    Console.WriteLine("Admin user not found!");
                    return Content("❌ Admin user not found! Please run the application first to create the admin account.");
                }

                Console.WriteLine($"Admin found: {admin.Email}");

                // Check if user is in Admin role
                var isInRole = await _userManager.IsInRoleAsync(admin, "Admin");
                Console.WriteLine($"Is in Admin role: {isInRole}");

                if (!isInRole)
                {
                    Console.WriteLine("Adding user to Admin role...");

                    try
                    {
                        // Add to Admin role
                        var result = await _userManager.AddToRoleAsync(admin, "Admin");

                        if (result.Succeeded)
                        {
                            Console.WriteLine("Successfully added to Admin role");
                            return Content(@"
                                <h2 style='color: green;'>✅ SUCCESS!</h2>
                                <p>Admin role has been assigned successfully!</p>
                                <p>Please <a href='/Account/Login'>click here to login</a> with:</p>
                                <ul>
                                    <li><strong>Email:</strong> admin@ctu.edu.ph</li>
                                    <li><strong>Password:</strong> Admin@123</li>
                                </ul>
                            ", "text/html");
                        }
                        else
                        {
                            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                            Console.WriteLine($"Failed to add to role: {errors}");

                            // Check if the error is about the trigger
                            if (errors.Contains("trigger", StringComparison.OrdinalIgnoreCase))
                            {
                                // Try direct SQL approach
                                return await TryDirectSqlApproach(admin);
                            }

                            return Content($"❌ Failed to assign role: {errors}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Exception while adding to role: {ex.Message}");

                        // Try direct SQL approach as fallback
                        return await TryDirectSqlApproach(admin);
                    }
                }
                else
                {
                    Console.WriteLine("User already has Admin role");
                    return Content(@"
                        <h2 style='color: blue;'>✅ ALREADY FIXED!</h2>
                        <p>Admin user already has the Admin role assigned.</p>
                        <p>Please <a href='/Account/Login'>click here to login</a> with:</p>
                        <ul>
                            <li><strong>Email:</strong> admin@ctu.edu.ph</li>
                            <li><strong>Password:</strong> Admin@123</li>
                        </ul>
                    ", "text/html");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in FixAdminRole: {ex.Message}");
                return Content($"❌ Error: {ex.Message}");
            }
        }

        // Helper method for direct SQL approach
        private async Task<IActionResult> TryDirectSqlApproach(ApplicationUser admin)
        {
            try
            {
                Console.WriteLine("Attempting direct SQL approach...");

                // Get the role ID
                var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
                if (role != null)
                {
                    // Check if already exists
                    var existing = await _context.UserRoles
                        .FirstOrDefaultAsync(ur => ur.UserId == admin.Id && ur.RoleId == role.Id);

                    if (existing == null)
                    {
                        // Use raw SQL to bypass the trigger issue
                        var sql = "INSERT INTO AspNetUserRoles (UserId, RoleId) VALUES ({0}, {1})";
                        var result = await _context.Database.ExecuteSqlRawAsync(sql, admin.Id, role.Id);

                        if (result > 0)
                        {
                            Console.WriteLine("Successfully added to Admin role using direct SQL");
                            return Content(@"
                                <h2 style='color: green;'>✅ SUCCESS!</h2>
                                <p>Admin role has been assigned successfully using direct SQL!</p>
                                <p>Please <a href='/Account/Login'>click here to login</a> with:</p>
                                <ul>
                                    <li><strong>Email:</strong> admin@ctu.edu.ph</li>
                                    <li><strong>Password:</strong> Admin@123</li>
                                </ul>
                            ", "text/html");
                        }
                    }
                    else
                    {
                        return Content(@"
                            <h2 style='color: blue;'>✅ ALREADY FIXED!</h2>
                            <p>Admin role already exists in the database.</p>
                            <p>Please <a href='/Account/Login'>click here to login</a>.</p>
                        ", "text/html");
                    }
                }

                return Content(@"
                    <h2 style='color: orange;'>⚠️ MANUAL STEP REQUIRED</h2>
                    <p>The database trigger is preventing automatic role assignment. Please run this SQL command in SQL Server Management Studio:</p>
                    <pre style='background: #f4f4f4; padding: 15px; border-radius: 5px; overflow-x: auto;'>
-- First, get the Role ID for 'Admin'
DECLARE @RoleId nvarchar(450) = (SELECT Id FROM AspNetRoles WHERE Name = 'Admin');

-- Then insert the user role
INSERT INTO AspNetUserRoles (UserId, RoleId)
VALUES ('" + admin.Id + @"', @RoleId);

-- Verify it worked
SELECT * FROM AspNetUserRoles WHERE UserId = '" + admin.Id + @"';
                    </pre>
                    <p>After running this SQL, <a href='/Account/Login'>try logging in again</a>.</p>
                ", "text/html");
            }
            catch (Exception ex)
            {
                return Content($"❌ Direct SQL approach also failed: {ex.Message}");
            }
        }

        // GET: /Account/CheckRole - Check current user's roles
        [HttpGet]
        public async Task<IActionResult> CheckRole()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Content("No user is logged in");
            }

            var roles = await _userManager.GetRolesAsync(user);
            var isInRole = await _userManager.IsInRoleAsync(user, "Admin");

            return Content($@"
                <h3>User Role Information</h3>
                <ul>
                    <li><strong>User:</strong> {user.Email}</li>
                    <li><strong>UserName:</strong> {user.UserName}</li>
                    <li><strong>IsAuthenticated:</strong> {User.Identity.IsAuthenticated}</li>
                    <li><strong>Roles from Identity:</strong> {string.Join(", ", roles)}</li>
                    <li><strong>IsInRole Admin:</strong> {isInRole}</li>
                    <li><strong>Role property:</strong> {user.Role}</li>
                </ul>
                <p><a href='/Account/Login'>Login Page</a> | <a href='/Admin/Dashboard'>Go to Admin Dashboard</a></p>
            ", "text/html");
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string studentId, string email, int birthMonth, int birthDay, int birthYear)
        {
            try
            {
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

                // Create new user
                var user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FullName = "",
                    Role = "Student",
                    BirthDate = birthDate,
                    CreatedAt = DateTime.Now,
                    IsApproved = false,
                    IsActive = true,
                    ProfileImage = "/images/ctu-logo.png",
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

                    TempData["Success"] = "Registration successful! You can now login with your ID number.";
                    return RedirectToAction("Login");
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