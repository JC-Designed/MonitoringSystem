using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace MonitoringSystem.Models
{
    public class ApplicationUser : IdentityUser
    {
        // ===================== EXACTLY MATCHING YOUR DB COLUMNS =====================

        // BASIC USER INFO (from your AspNetUsers table)
        public string? FullName { get; set; } = string.Empty;
        public string? Role { get; set; } = string.Empty;
        public string? Gender { get; set; } = string.Empty;
        public DateTime? BirthDate { get; set; }
        public string? ContactPerson { get; set; } = string.Empty;  // CHANGED: from Contact to ContactPerson
        public string? MobileNumber { get; set; } = string.Empty;
        public string? Address { get; set; } = string.Empty;
        public string? ProfileImage { get; set; } = "/images/ctu-logo.png";
        public string? BannerImage { get; set; } = "/images/banner-placeholder.jpg";
        public bool IsActive { get; set; } = true;

        // Status can be "Pending", "Approved", or "Declined"
        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
        public string? Facebook { get; set; } = string.Empty;

        // ============== NEWLY ADDED COLUMNS ==============
        public int? Year { get; set; }                    // For student year level
        public int? CompanyID { get; set; }               // Foreign key to Company
        public string? Program { get; set; } = string.Empty; // For student program/course

        // Student ID field for registration
        public string? StudentId { get; set; } = string.Empty;

        // ===================== NAVIGATION PROPERTIES =====================
        public Student? Student { get; set; }
        public Company? Company { get; set; }
        public Admin? Admin { get; set; }

        // ===================== DISPLAY HELPERS =====================
        [NotMapped]
        public string DisplayName => FullName ?? string.Empty;

        // ===================== HELPER METHODS =====================
        public string GetFacebookUrl()
        {
            if (string.IsNullOrEmpty(Facebook))
                return string.Empty;

            if (Facebook.StartsWith("http"))
                return Facebook;

            return $"https://facebook.com/{Facebook}";
        }

        public void UpdateLastLogin()
        {
            LastLogin = DateTime.Now;
            UpdatedAt = DateTime.Now;
        }

        // Helper to check if user can login
        public bool CanLogin()
        {
            return Status == "Approved" && IsActive;
        }
    }
}