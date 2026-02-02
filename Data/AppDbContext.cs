using Microsoft.EntityFrameworkCore;
using HolisticDepartmentExamSystem.Models;
using System;

namespace HolisticDepartmentExamSystem.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Role> Roles { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Exam> Exams { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<Choice> Choices { get; set; }
        public DbSet<ExamPassword> ExamPasswords { get; set; }
        public DbSet<ExamAttempt> ExamAttempts { get; set; }
        public DbSet<ExamAnswer> ExamAnswers { get; set; }
        public DbSet<Result> Results { get; set; }
        public DbSet<Feedback> Feedbacks { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Role
            modelBuilder.Entity<Role>()
                .Property(r => r.RoleName)
                .IsRequired();

            // Configure User
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.LastActivity);

            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Student
            modelBuilder.Entity<Student>()
                .HasOne(s => s.User)
                .WithOne(u => u.Student)
                .HasForeignKey<Student>("UserId")
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Student>()
                .HasIndex(s => s.Email);

            // Configure Exam
            modelBuilder.Entity<Exam>()
                .HasOne(e => e.Creator)
                .WithMany(u => u.ExamsCreated)
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Exam>()
                .HasIndex(e => e.Status);

            modelBuilder.Entity<Exam>()
                .HasIndex(e => e.ResultsPublished);

            // Configure Question
            modelBuilder.Entity<Question>()
                .HasOne(q => q.Exam)
                .WithMany(e => e.Questions)
                .HasForeignKey(q => q.ExamId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Choice
            modelBuilder.Entity<Choice>()
                .HasOne(c => c.Question)
                .WithMany(q => q.Choices)
                .HasForeignKey(c => c.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

             // Configure ExamPassword
            modelBuilder.Entity<ExamPassword>()
                .HasOne(ep => ep.Exam)
                .WithMany(e => e.ExamPasswords)
                .HasForeignKey(ep => ep.ExamId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ExamPassword>()
                .HasIndex(ep => new { ep.ExamId, ep.StudentId });

             // Configure ExamAttempt
            modelBuilder.Entity<ExamAttempt>()
                .HasOne(ea => ea.Exam)
                .WithMany(e => e.ExamAttempts)
                .HasForeignKey(ea => ea.ExamId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ExamAttempt>()
                .HasIndex(ea => ea.Status);

            modelBuilder.Entity<ExamAttempt>()
                .HasIndex(ea => ea.IsBlocked);

            // Configure ExamAnswer
            modelBuilder.Entity<ExamAnswer>()
                .HasOne(ea => ea.Attempt)
                .WithMany(a => a.Answers)
                .HasForeignKey(ea => ea.AttemptId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ExamAnswer>()
                .HasOne(ea => ea.Question)
                .WithMany(q => q.ExamAnswers)
                .HasForeignKey(ea => ea.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Feedback
            modelBuilder.Entity<Feedback>()
                .HasOne(f => f.Exam)
                .WithMany(e => e.Feedbacks)
                .HasForeignKey(f => f.ExamId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure ActivityLog
            modelBuilder.Entity<ActivityLog>()
                .HasIndex(al => al.Timestamp);

            modelBuilder.Entity<ActivityLog>()
                .HasIndex(al => al.UserId);

            // Seed Roles
            modelBuilder.Entity<Role>().HasData(
                new Role { RoleId = 1, RoleName = "Admin" },
                new Role { RoleId = 2, RoleName = "Coordinator" },
                new Role { RoleId = 3, RoleName = "Student" }
            );
        }
    }
}
