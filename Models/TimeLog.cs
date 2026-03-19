using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MonitoringSystem.Models
{
    public class TimeLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        [Required]
        public DateTime Date { get; set; }

        public string? AmIn { get; set; }
        public string? AmOut { get; set; }
        public string? PmIn { get; set; }
        public string? PmOut { get; set; }
        public string? OtIn { get; set; }
        public string? OtOut { get; set; }

        public double TotalHours { get; set; }

        [Required]
        public string Type { get; set; } = "regular";

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
    }
}