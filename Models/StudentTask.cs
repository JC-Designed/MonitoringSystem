using System;

namespace MonitoringSystem.Models
{
    public class StudentTask
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty; // optional
        public DateTime Deadline { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Completed, Overdue
        public string Description { get; set; } = string.Empty;
        public string AssignedTo { get; set; } = string.Empty; // optional
        public string? AttachmentPath { get; set; } // optional, nullable for no attachment
    }
}
