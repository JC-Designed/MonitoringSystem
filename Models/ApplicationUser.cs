using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace MonitoringSystem.Models
{
    public class ApplicationUser : IdentityUser
    {
        // ===================== BASIC USER INFO =====================
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        // Computed full name (optional helper)
        [NotMapped]
        public string DisplayName => $"{FirstName} {LastName}".Trim();

        // ===================== ROLE & PERSONAL INFO =====================
        public string Role { get; set; } = string.Empty;   // Student, Company, Admin
        public string Gender { get; set; } = string.Empty; // Female, Male, Custom
        public DateTime BirthDate { get; set; }

        // ===================== PROFILE (ALL ROLES) =====================
        // Used by Admin, Company, and Student
        public string Contact { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;

        // NEW: Profile images
        public string ProfileImage { get; set; } = "/images/ctu-logo.png"; // default
        public string BannerImage { get; set; } = "/images/banner-placeholder.jpg"; // default

        // ===================== ADMIN / COMPANY EXTRA INFO =====================
        // Optional – only filled if role needs it
        public string CompanyName { get; set; } = string.Empty;
        public string CompanyDescription { get; set; } = string.Empty;

        // ===================== SYSTEM FIELDS =====================
        public bool IsApproved { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // ===================== TEMP / VIEW HELPERS =====================
        [NotMapped]
        public List<string> Roles { get; set; } = new List<string>();

        // ===================== CHAT NAVIGATION =====================
        public ICollection<Conversation> ConversationsAsUser1 { get; set; } = new List<Conversation>();
        public ICollection<Conversation> ConversationsAsUser2 { get; set; } = new List<Conversation>();
        public ICollection<Message> MessagesSent { get; set; } = new List<Message>();
    }
}
