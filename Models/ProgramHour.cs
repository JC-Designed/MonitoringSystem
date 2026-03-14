using System.ComponentModel.DataAnnotations;

namespace MonitoringSystem.Models
{
    public class ProgramHour
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Full Program Name")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Program Code")]
        public string Code { get; set; } = string.Empty;

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Hours must be greater than 0")]
        public int Hours { get; set; }
    }
}