using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MonitoringSystem.Models;
using MonitoringSystem.Models.ParamModel;

namespace MonitoringSystem.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // ===================== CUSTOM TABLES =====================
        public DbSet<UserAccount> UserAccounts { get; set; } = null!;
        public DbSet<AccountType> AccountTypes { get; set; } = null!;

        // ===================== COMPANY =====================
        public DbSet<Company> Companies { get; set; } = null!;

        // ===================== MESSAGES =====================
        public DbSet<Conversation> Conversations { get; set; } = null!;
        public DbSet<Message> Messages { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ===================== ACCOUNT TYPE =====================
            builder.Entity<AccountType>()
                   .HasKey(a => a.TypeId); // Primary key

            builder.Entity<AccountType>()
                   .HasMany(a => a.UserAccounts) // Navigation property
                   .WithOne(u => u.AccountType)
                   .HasForeignKey(u => u.AccountTypeID)
                   .OnDelete(DeleteBehavior.Restrict);

            // ===================== USER ACCOUNT =====================
            builder.Entity<UserAccount>()
                   .HasKey(u => u.UserID); // Primary key

            //// Explicitly configure Status column
            //builder.Entity<UserAccount>()
            //       .Property(u => u.Status)
            //       .HasMaxLength(50)
            //       .HasDefaultValue("Active");

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
        }
    }
}