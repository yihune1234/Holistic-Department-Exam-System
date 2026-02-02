using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HolisticDepartmentExamSystem.Models
{
    public class Exam
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("exam_id")]
        public int ExamId { get; set; }

        [Required]
        [Column("title")]
        [StringLength(255)]
        public string Title { get; set; }

        [Column("description")]
        public string Description { get; set; }

        [Column("duration_minutes")]
        public int DurationMinutes { get; set; }

        [Column("total_marks")]
        public int TotalMarks { get; set; }

        [Column("status")]
        [StringLength(50)]
        public string Status { get; set; } // Draft / Published / Closed

        [Column("created_by")]
        public int CreatedBy { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("results_published")]
        public bool ResultsPublished { get; set; } = false;

        [ForeignKey("CreatedBy")]
        public virtual User Creator { get; set; }

        public virtual ICollection<Question> Questions { get; set; }
        public virtual ICollection<ExamPassword> ExamPasswords { get; set; }
        public virtual ICollection<ExamAttempt> ExamAttempts { get; set; }
        public virtual ICollection<Feedback> Feedbacks { get; set; }
    }
}
