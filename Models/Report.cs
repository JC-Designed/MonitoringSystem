using System;
using System.ComponentModel.DataAnnotations;

namespace MonitoringSystem.Models
{
    public class Report
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime DateFrom { get; set; }

        public string FilePath { get; set; }

        public string StudentId { get; set; }
    }
}