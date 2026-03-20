using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MonitoringSystem.Models
{
    public class StudentTask
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [Required]
        public DateTime DateFrom { get; set; }  // Change from Deadline to DateFrom

        [Required]
        public DateTime DateTo { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; }

        public string TaskContent { get; set; }

        public string LearningContent { get; set; }

        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}