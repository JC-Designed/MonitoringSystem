using System.ComponentModel.DataAnnotations;

namespace MonitoringSystem.Models
{
    public class Company
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Email { get; set; } = string.Empty;

        public string Industry { get; set; } = string.Empty;

        // ✅ REQUIRED for your error fix
        public string Address { get; set; } = string.Empty;

        // ✅ LINK company to user
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }
    }
}
