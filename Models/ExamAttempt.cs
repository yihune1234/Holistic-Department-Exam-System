using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HolisticDepartmentExamSystem.Models
{
    public class ExamAttempt
    {
        public ExamAttempt()
        {
            Answers = new HashSet<ExamAnswer>();
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("attempt_id")]
        public int AttemptId { get; set; }

        [Column("student_id")]
        public int StudentId { get; set; }

        [Column("exam_id")]
        public int ExamId { get; set; }

        [Column("start_time")]
        public DateTime StartTime { get; set; }

        [Column("end_time")]
        public DateTime? EndTime { get; set; }

        [Column("status")]
        [StringLength(50)]
        public string Status { get; set; } // In Progress / Submitted / Auto-Submitted / Blocked

        [Column("is_blocked")]
        public bool IsBlocked { get; set; } = false;

        [ForeignKey("StudentId")]
        public virtual Student Student { get; set; }

        [ForeignKey("ExamId")]
        public virtual Exam Exam { get; set; }

        public virtual ICollection<ExamAnswer> Answers { get; set; }
        public virtual Result Result { get; set; }
    }
}
