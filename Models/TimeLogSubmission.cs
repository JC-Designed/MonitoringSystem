using System;
using System.ComponentModel.DataAnnotations;

namespace MonitoringSystem.Models
{
    public class TimeLogSubmission
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string StudentId { get; set; }

        public string StudentName { get; set; }
        public string Course { get; set; }
        public string Year { get; set; }
        public string Section { get; set; }

        public DateTime SubmissionDate { get; set; } = DateTime.Now;
        public int Month { get; set; }
        public double TotalHours { get; set; }

        // Store logs as JSON string
        public string Logs { get; set; }

        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
        public bool IsRead { get; set; } = false;
        public string AdminRemarks { get; set; }
        public DateTime? ProcessedDate { get; set; }
    }
}