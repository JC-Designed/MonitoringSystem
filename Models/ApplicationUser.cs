using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace MonitoringSystem.Models
{
    public class ApplicationUser : IdentityUser
    {
        // ===================== BASIC USER INFO =====================
        public string? FullName { get; set; } = string.Empty;

        [NotMapped]
        public string DisplayName => FullName ?? string.Empty;

        // ===================== ROLE & PERSONAL INFO =====================
        [NotMapped]
        public string? Role { get; set; } = string.Empty; // display only

        public string? Gender { get; set; } = string.Empty;
        public DateTime BirthDate { get; set; }

        // ===================== PROGRAM (e.g., Bachelor of Industrial Technology) =====================
        public string? Program { get; set; } = string.Empty;

        // ===================== PROFILE (ALL ROLES) =====================
        public string? Contact { get; set; } = string.Empty;
        public string? Address { get; set; } = string.Empty;
        public string? ProfileImage { get; set; } = "/images/ctu-logo.png";
        public string? BannerImage { get; set; } = "/images/banner-placeholder.jpg";

        // ===================== ADMIN / COMPANY EXTRA INFO =====================
        public string? CompanyName { get; set; } = string.Empty;
        public string? CompanyDescription { get; set; } = string.Empty;

        // ===================== PROPERTIES FOR COMPANY EDIT =====================
        public string? MobileNumber { get; set; } = string.Empty;
        public int? CompanyID { get; set; } = 0;

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

        // ===================== YEAR (NOT MAPPED) =====================
        [NotMapped]
        public string? Year { get; set; } = string.Empty;
    }
}