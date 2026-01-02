using System;

namespace MonitoringSystem.Models
{
    public class Report
    {
        public int Id { get; set; }
        public string ReporterName { get; set; } = string.Empty;
        public string ReporterType { get; set; } = string.Empty;
        public string Status { get; set; } = "Unread";
        public string Details { get; set; } = string.Empty;
        public DateTime DateSubmitted { get; set; }
    }
}
