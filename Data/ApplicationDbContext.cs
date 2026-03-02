using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MonitoringSystem.Models;

namespace MonitoringSystem.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // ===================== COMPANY =====================
        public DbSet<Company> Companies { get; set; } = null!;

        // ===================== MESSAGES =====================
        public DbSet<Conversation> Conversations { get; set; } = null!;
        public DbSet<Message> Messages { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ===================== CONVERSATION =====================
            builder.Entity<Conversation>()
                   .HasOne(c => c.User1)
                   .WithMany(u => u.ConversationsAsUser1)
                   .HasForeignKey(c => c.User1Id)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Conversation>()
                   .HasOne(c => c.User2)
                   .WithMany(u => u.ConversationsAsUser2)
                   .HasForeignKey(c => c.User2Id)
                   .OnDelete(DeleteBehavior.Restrict);

            // ===================== MESSAGE =====================
            builder.Entity<Message>()
                   .HasOne(m => m.Conversation)
                   .WithMany(c => c.Messages)
                   .HasForeignKey(m => m.ConversationId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Message>()
                   .HasOne(m => m.Sender)
                   .WithMany(u => u.MessagesSent)
                   .HasForeignKey(m => m.SenderId)
                   .OnDelete(DeleteBehavior.Restrict);

            // ===================== APPLICATIONUSER CONFIG =====================
            builder.Entity<ApplicationUser>()
                   .Property(u => u.FullName)
                   .HasMaxLength(150)
                   .IsRequired(); // FullName mandatory

            // Ignore non-mapped helper properties
            builder.Entity<ApplicationUser>()
                   .Ignore(u => u.DisplayName)
                   .Ignore(u => u.Role)
                   .Ignore(u => u.Year);
        }
    }
}