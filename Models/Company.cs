using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MonitoringSystem.Models
{
    [Table("Companies")]
    public class Company
    {
        [Key]
        public int Id { get; set; }

        // Foreign key to AspNetUsers
        public string UserId { get; set; } = string.Empty;

        // Navigation property back to ApplicationUser
        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        // Company-specific fields (from your Companies table)
        public string CompanyId { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string? CompanyDescription { get; set; }
        public string? Industry { get; set; }
        public string? CompanySize { get; set; }
        public string? Website { get; set; }
        public string? BusinessPermit { get; set; }
        public string? TaxId { get; set; }
        public int? YearEstablished { get; set; }

        // Contact person (if different from main user)
        public string? ContactPersonName { get; set; }
        public string? ContactPersonPosition { get; set; }

        // Verification status
        public bool IsVerified { get; set; }
        public string? VerifiedBy { get; set; }
        public DateTime? VerifiedDate { get; set; }

        // System fields
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}