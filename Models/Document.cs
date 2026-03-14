using System.ComponentModel.DataAnnotations;

namespace MonitoringSystem.Models
{
    public class Document
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Type { get; set; } = string.Empty;

        [Required]
        public string Size { get; set; } = string.Empty;

        [Required]
        public string FileData { get; set; } = string.Empty; // Base64 encoded file

        public DateTime UploadedAt { get; set; }

        public string UploadedBy { get; set; } = string.Empty;
    }
}