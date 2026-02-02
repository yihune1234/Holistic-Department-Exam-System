using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HolisticDepartmentExamSystem.Models
{
    public class ExamPassword
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("password_id")]
        public int PasswordId { get; set; }

        [Column("exam_id")]
        public int ExamId { get; set; }

        [Column("student_id")]
        public int StudentId { get; set; }

        [Column("password_hash")]
        [StringLength(255)]
        public string PasswordHash { get; set; }

        [Column("is_used")]
        public bool IsUsed { get; set; }

        [Column("expires_at")]
        public DateTime ExpiresAt { get; set; }

        [ForeignKey("ExamId")]
        public virtual Exam Exam { get; set; }

        [ForeignKey("StudentId")]
        public virtual Student Student { get; set; }
    }
}
