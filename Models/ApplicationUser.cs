using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace MonitoringSystem.Models
{
    public class ApplicationUser : IdentityUser
    {
        // ===================== CUSTOM FIELDS =====================
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty; // Student, Company
        public string Gender { get; set; } = string.Empty; // Female, Male, Custom

        public DateTime BirthDate { get; set; }

        // ===================== EXISTING FIELDS =====================
        public bool IsApproved { get; set; } = false;

        // Track registration date
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Temporary property for roles (not stored in DB)
        [NotMapped]
        public List<string> Roles { get; set; } = new List<string>();
    }
}
