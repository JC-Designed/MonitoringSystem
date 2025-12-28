using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace MonitoringSystem.Models
{
    public class ApplicationUser : IdentityUser
    {
        public bool IsApproved { get; set; }

        // Track registration date
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Temporary property for roles (not stored in DB)
        [NotMapped]
        public List<string> Roles { get; set; } = new List<string>();
    }
}
