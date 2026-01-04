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

        public string Role { get; set; } = string.Empty; // Student, Company, Admin
        public string Gender { get; set; } = string.Empty; // Female, Male, Custom
        public DateTime BirthDate { get; set; }

        // ===================== ADMIN PROFILE FIELDS =====================
        public string FullName { get; set; } = string.Empty;      // For full name display
        public string Contact { get; set; } = string.Empty;       // Admin contact number
        public string Address { get; set; } = string.Empty;       // Admin address
        public string ProfileImage { get; set; } = string.Empty;  // Profile picture path
        public string BannerImage { get; set; } = string.Empty;   // Banner picture path

        // ===================== EXISTING FIELDS =====================
        public bool IsApproved { get; set; } = false;

        // Track registration date
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Temporary property for roles (not stored in DB)
        [NotMapped]
        public List<string> Roles { get; set; } = new List<string>();

        // ===================== NAVIGATION PROPERTIES FOR CHAT =====================
        // Conversations where this user is User1
        public ICollection<Conversation> ConversationsAsUser1 { get; set; } = new List<Conversation>();

        // Conversations where this user is User2
        public ICollection<Conversation> ConversationsAsUser2 { get; set; } = new List<Conversation>();

        // Messages sent by this user
        public ICollection<Message> MessagesSent { get; set; } = new List<Message>();
    }
}
