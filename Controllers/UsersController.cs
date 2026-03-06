using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MonitoringSystem.Models;
using System.Threading.Tasks;

// If you use areas, decorate the controller with [Area("Admin")]
[Authorize(Roles = "Admin")]  
[Route("Admin/Users")]        
public class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UsersController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [HttpPost("MakeAdmin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MakeAdmin([FromBody] MakeAdminModel model)
    {
        // Find the user by ID
        var user = await _userManager.FindByIdAsync(model.UserId);
        if (user == null)
            return NotFound();

        // Get current roles
        var currentRoles = await _userManager.GetRolesAsync(user);

        // Remove all existing roles
        if (currentRoles.Count > 0)
            await _userManager.RemoveFromRolesAsync(user, currentRoles);

        // Add the Admin role
        var result = await _userManager.AddToRoleAsync(user, "Admin");

        if (result.Succeeded)
            return Ok();
        else
            return BadRequest(result.Errors);
    }
}

// Simple model for the JSON payload
public class MakeAdminModel
{
    public string UserId { get; set; }
}