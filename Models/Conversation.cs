using System;
using System.Collections.Generic;

namespace MonitoringSystem.Models
{
    public class Conversation
    {
        public int Id { get; set; }

        // 1-on-1 users
        public string User1Id { get; set; } = string.Empty;
        public ApplicationUser User1 { get; set; } = null!;

        public string User2Id { get; set; } = string.Empty;
        public ApplicationUser User2 { get; set; } = null!;

        // Messages in this conversation
        public ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}
