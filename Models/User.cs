using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HolisticDepartmentExamSystem.Models
{
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("user_id")]
        public int UserId { get; set; }

        [Required]
        [Column("username")]
        [StringLength(100)]
        public string Username { get; set; }

        [Required]
        [Column("password_hash")]
        public string PasswordHash { get; set; }
        [Column("role_id")]
        public int RoleId { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("status")]
        public bool Status { get; set; }

        [Column("last_activity")]
        public DateTime? LastActivity { get; set; }

        [ForeignKey("RoleId")]
        public virtual Role Role { get; set; }

        public virtual Student Student { get; set; }
        public virtual ICollection<Exam> ExamsCreated { get; set; }
        public virtual ICollection<Feedback> Feedbacks { get; set; }
        public virtual ICollection<ActivityLog> ActivityLogs { get; set; }
    }
}
