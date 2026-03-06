using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MonitoringSystem.Models
{
    [Table("Students")]
    public class Student
    {
        [Key]
        public int Id { get; set; }

        // Foreign key to AspNetUsers
        public string UserId { get; set; } = string.Empty;

        // Navigation property back to ApplicationUser
        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        // Student-specific fields
        public string StudentId { get; set; } = string.Empty;
        public string? Year { get; set; }
        public string? Program { get; set; }
        public string? EmergencyContact { get; set; }
        public string? EmergencyPhone { get; set; }
        public string? GuardianName { get; set; }
        public string? GuardianPhone { get; set; }
        public DateTime? EnrollmentDate { get; set; }
        public DateTime? ExpectedGraduation { get; set; }
        public string? PreviousSchool { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? GPA { get; set; }

        // System fields
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}