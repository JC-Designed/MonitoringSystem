using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MonitoringSystem.Models
{
    [Table("Admins")]
    public class Admin
    {
        [Key]
        public int Id { get; set; }

        // Foreign key to AspNetUsers
        public string UserId { get; set; } = string.Empty;

        // Navigation property back to ApplicationUser
        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        // Admin-specific fields (from your Admins table)
        public string EmployeeId { get; set; } = string.Empty;
        public string? Department { get; set; }
        public string? Position { get; set; }
        public DateTime? HireDate { get; set; }

        // Permissions
        public string PermissionsLevel { get; set; } = "Basic";
        public bool CanManageUsers { get; set; }
        public bool CanManageCompanies { get; set; }
        public bool CanManageStudents { get; set; }
        public bool CanViewReports { get; set; } = true;
        public bool CanApproveAccounts { get; set; }

        // Office info
        public string? OfficeLocation { get; set; }
        public string? OfficePhone { get; set; }

        // System fields
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}