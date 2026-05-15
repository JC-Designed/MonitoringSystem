using System;
using System.ComponentModel.DataAnnotations;

namespace MonitoringSystem.Models
{
    public class CompanyReport
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string StudentId { get; set; }

        [Required]
        public string CompanyId { get; set; }

        [Required]
        public string StudentName { get; set; }

        [Required]
        public string Incident { get; set; }

        public string Remarks { get; set; }

        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? ResolvedAt { get; set; }

        // Navigation properties
        public virtual ApplicationUser Student { get; set; }
        public virtual ApplicationUser Company { get; set; }
    }
}