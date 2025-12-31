using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MonitoringSystem.Models
{
    public class Report
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string ReporterId { get; set; } = string.Empty; // FK to ApplicationUser or Company

        [NotMapped]
        public string ReporterName { get; set; } = string.Empty; // Loaded in controller

        [Required]
        public string ReporterType { get; set; } = "Student"; // or Company

        [Required]
        public string Details { get; set; } = string.Empty;

        [Required]
        public string Status { get; set; } = "Unread"; // Read/Unread

        [Required]
        public DateTime DateSubmitted { get; set; } = DateTime.Now;
    }
}
