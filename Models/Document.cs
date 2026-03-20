using System.ComponentModel.DataAnnotations;

namespace MonitoringSystem.Models
{
    public class Document
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty; // Original filename

        [Required]
        public string FileName { get; set; } = string.Empty; // Unique filename on server

        [Required]
        public string Type { get; set; } = string.Empty; // File extension (PDF, DOCX, etc.)

        [Required]
        public string Size { get; set; } = string.Empty; // Formatted file size

        // Store file as Base64
        [Required]
        public string FileData { get; set; } = string.Empty; // Base64 encoded file

        public DateTime UploadedAt { get; set; } // When file was uploaded (changed from Uploaded)

        public string UploadedBy { get; set; } = string.Empty; // Who uploaded it

        // New properties for read/unread status
        public bool IsRead { get; set; } = false; // Default to false (Unread)

        public DateTime? ReadAt { get; set; } // When the document was marked as read (null if unread)
    }
}