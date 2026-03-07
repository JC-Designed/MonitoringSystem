using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MonitoringSystem.Models
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSets for your custom tables
        public DbSet<Student> Students { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<Admin> Admins { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ===================== FIX FOR DATABASE TRIGGERS =====================
            // Tell EF Core that AspNetUsers table has triggers
            builder.Entity<ApplicationUser>()
                .ToTable(tb => tb.HasTrigger("AspNetUsers_Trigger"));

            // Also configure other tables that might have triggers
            builder.Entity<Student>()
                .ToTable(tb => tb.HasTrigger("Students_Trigger"));

            builder.Entity<Company>()
                .ToTable(tb => tb.HasTrigger("Companies_Trigger"));

            builder.Entity<Admin>()
                .ToTable(tb => tb.HasTrigger("Admins_Trigger"));

            // ===================== CONFIGURE ONE-TO-ONE RELATIONSHIPS =====================

            // Student - User (one-to-one)
            builder.Entity<Student>()
                .HasOne(s => s.User)
                .WithOne(u => u.Student)
                .HasForeignKey<Student>(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Company - User (one-to-one)
            builder.Entity<Company>()
                .HasOne(c => c.User)
                .WithOne(u => u.Company)
                .HasForeignKey<Company>(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Admin - User (one-to-one)
            builder.Entity<Admin>()
                .HasOne(a => a.User)
                .WithOne(u => u.Admin)
                .HasForeignKey<Admin>(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // ===================== CONFIGURE TABLE NAMES =====================
            builder.Entity<Student>().ToTable("Students");
            builder.Entity<Company>().ToTable("Companies");
            builder.Entity<Admin>().ToTable("Admins");

            // ===================== CONFIGURE INDEXES =====================
            builder.Entity<Student>()
                .HasIndex(s => s.StudentId)
                .HasDatabaseName("IX_Students_StudentId");

            builder.Entity<Company>()
                .HasIndex(c => c.CompanyName)
                .HasDatabaseName("IX_Companies_CompanyName");
        }
    }
}