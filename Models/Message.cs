using System;
using System.ComponentModel.DataAnnotations;

namespace MonitoringSystem.Models
{
    public class Message
    {
        public int Id { get; set; }

        // Conversation this message belongs to
        [Required]
        public int ConversationId { get; set; }
        public Conversation Conversation { get; set; } = null!;

        // Sender of the message
        [Required]
        public string SenderId { get; set; } = null!;
        public ApplicationUser Sender { get; set; } = null!;

        [Required]
        public string Text { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
