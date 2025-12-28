using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MonitoringSystem.Models
{
    public class Conversation
    {
        public int Id { get; set; }

        // Users involved in the conversation
        [Required]
        public string User1Id { get; set; } = null!;
        public ApplicationUser User1 { get; set; } = null!;

        [Required]
        public string User2Id { get; set; } = null!;
        public ApplicationUser User2 { get; set; } = null!;

        // Messages in the conversation
        public ICollection<Message> Messages { get; set; } = new List<Message>();

        // Created date for the conversation
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
