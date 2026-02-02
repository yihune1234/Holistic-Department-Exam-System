using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HolisticDepartmentExamSystem.Models
{
    public class Question
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("question_id")]
        public int QuestionId { get; set; }

        [Column("exam_id")]
        public int ExamId { get; set; }

        [Column("question_text")]
        public string QuestionText { get; set; }

        [Column("question_type")]
        public string QuestionType { get; set; }

        [Column("marks")]
        public int Marks { get; set; }

        [Column("question_order")]
        public int QuestionOrder { get; set; }

        [ForeignKey("ExamId")]
        public virtual Exam Exam { get; set; }

        public virtual ICollection<Choice> Choices { get; set; } = new List<Choice>();
        public virtual ICollection<ExamAnswer> ExamAnswers { get; set; } = new List<ExamAnswer>();
    }
}
