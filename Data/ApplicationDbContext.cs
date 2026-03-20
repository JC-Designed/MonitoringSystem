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

        // ===== ADD THESE NEW DbSets FOR PROGRAM HOURS AND DOCUMENTS =====
        public DbSet<ProgramHour> ProgramHours { get; set; }
        public DbSet<Document> Documents { get; set; }

        // ===== ADD TIMELOG DbSet =====
        public DbSet<TimeLog> TimeLogs { get; set; }

        // ===== ADD TASKS DbSet =====
        public DbSet<Task> Tasks { get; set; }

        // ===== ADD STUDENT TASKS DbSet =====
        public DbSet<StudentTask> StudentTasks { get; set; }

        // ===== ADD STUDENT TIME LOGS DbSet (RENAMED) =====
        public DbSet<TimeLogSubmission> StudentTimeLogs { get; set; }

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

            // ===== ADD TRIGGER CONFIGURATION FOR NEW TABLES =====
            builder.Entity<ProgramHour>()
                .ToTable(tb => tb.HasTrigger("ProgramHours_Trigger"));

            builder.Entity<Document>()
                .ToTable(tb => tb.HasTrigger("Documents_Trigger"));

            // ===== ADD TRIGGER CONFIGURATION FOR TIMELOGS TABLE =====
            builder.Entity<TimeLog>()
                .ToTable(tb => tb.HasTrigger("TimeLogs_Trigger"));

            // ===== ADD TRIGGER CONFIGURATION FOR TASKS TABLE =====
            builder.Entity<Task>()
                .ToTable(tb => tb.HasTrigger("Tasks_Trigger"));

            // ===== ADD TRIGGER CONFIGURATION FOR STUDENT TASKS TABLE =====
            builder.Entity<StudentTask>()
                .ToTable(tb => tb.HasTrigger("StudentTasks_Trigger"));

            // ===== ADD TRIGGER CONFIGURATION FOR STUDENT TIME LOGS TABLE =====
            builder.Entity<TimeLogSubmission>()
                .ToTable(tb => tb.HasTrigger("StudentTimeLogs_Trigger"));

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

            // ===== CONFIGURE TABLE NAMES FOR NEW TABLES =====
            builder.Entity<ProgramHour>().ToTable("ProgramHours");
            builder.Entity<Document>().ToTable("Documents");
            builder.Entity<TimeLog>().ToTable("TimeLogs");
            builder.Entity<Task>().ToTable("Tasks");
            builder.Entity<StudentTask>().ToTable("StudentTasks");

            // ===== RENAMED TABLE: TimeLogSubmissions -> StudentTimeLogs =====
            builder.Entity<TimeLogSubmission>().ToTable("StudentTimeLogs");

            // ===================== CONFIGURE INDEXES =====================
            builder.Entity<Student>()
                .HasIndex(s => s.StudentId)
                .HasDatabaseName("IX_Students_StudentId");

            builder.Entity<Company>()
                .HasIndex(c => c.CompanyName)
                .HasDatabaseName("IX_Companies_CompanyName");

            // ===== ADD INDEXES FOR NEW TABLES =====
            builder.Entity<ProgramHour>()
                .HasIndex(p => p.Code)
                .IsUnique()
                .HasDatabaseName("IX_ProgramHours_Code");

            // ===== INDEX FOR DOCUMENTS =====
            builder.Entity<Document>()
                .HasIndex(d => d.UploadedAt)
                .HasDatabaseName("IX_Documents_UploadedAt");

            // ===== ADD INDEX FOR TIMELOGS =====
            builder.Entity<TimeLog>()
                .HasIndex(t => new { t.UserId, t.Date })
                .HasDatabaseName("IX_TimeLogs_UserId_Date");

            // ===== ADD INDEX FOR TASKS =====
            builder.Entity<Task>()
                .HasIndex(t => t.StudentId)
                .HasDatabaseName("IX_Tasks_StudentId");

            // ===== ADD INDEX FOR STUDENT TASKS (UPDATED FROM Deadline TO DateFrom) =====
            builder.Entity<StudentTask>()
                .HasIndex(t => t.UserId)
                .HasDatabaseName("IX_StudentTasks_UserId");

            builder.Entity<StudentTask>()
                .HasIndex(t => t.Status)
                .HasDatabaseName("IX_StudentTasks_Status");

            builder.Entity<StudentTask>()
                .HasIndex(t => t.DateFrom)
                .HasDatabaseName("IX_StudentTasks_DateFrom");

            // ===== ADD INDEX FOR DATE TO =====
            builder.Entity<StudentTask>()
                .HasIndex(t => t.DateTo)
                .HasDatabaseName("IX_StudentTasks_DateTo");

            // ===== ADD INDEXES FOR STUDENT TIME LOGS =====
            builder.Entity<TimeLogSubmission>()
                .HasIndex(t => t.StudentId)
                .HasDatabaseName("IX_StudentTimeLogs_StudentId");

            builder.Entity<TimeLogSubmission>()
                .HasIndex(t => t.Status)
                .HasDatabaseName("IX_StudentTimeLogs_Status");

            builder.Entity<TimeLogSubmission>()
                .HasIndex(t => t.SubmissionDate)
                .HasDatabaseName("IX_StudentTimeLogs_SubmissionDate");

            // ===== CONFIGURE STUDENT TASK PROPERTIES (UPDATED) =====
            builder.Entity<StudentTask>()
                .Property(t => t.Title)
                .IsRequired()
                .HasMaxLength(200);

            builder.Entity<StudentTask>()
                .Property(t => t.Status)
                .IsRequired()
                .HasMaxLength(50);

            builder.Entity<StudentTask>()
                .Property(t => t.DateFrom)
                .IsRequired();

            builder.Entity<StudentTask>()
                .Property(t => t.DateTo)
                .IsRequired();

            builder.Entity<StudentTask>()
                .Property(t => t.TaskContent)
                .HasColumnType("nvarchar(max)");

            builder.Entity<StudentTask>()
                .Property(t => t.LearningContent)
                .HasColumnType("nvarchar(max)");

            // ===== CONFIGURE STUDENT TIME LOG PROPERTIES =====
            builder.Entity<TimeLogSubmission>()
                .Property(t => t.Logs)
                .HasColumnType("nvarchar(max)"); // For storing JSON string

            builder.Entity<TimeLogSubmission>()
                .Property(t => t.Status)
                .HasDefaultValue("Pending");

            builder.Entity<TimeLogSubmission>()
                .Property(t => t.IsRead)
                .HasDefaultValue(false);
        }
    }
}