using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HolisticDepartmentExamSystem.Models
{
    public class ExamAnswer
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("answer_id")]
        public int AnswerId { get; set; }

        [Column("attempt_id")]
        public int AttemptId { get; set; }

        [Column("question_id")]
        public int QuestionId { get; set; }

        [Column("selected_choice_id")]
        public int? SelectedChoiceId { get; set; }

        [Column("is_flagged")]
        public bool IsFlagged { get; set; }

        [ForeignKey("AttemptId")]
        public virtual ExamAttempt Attempt { get; set; }

        [ForeignKey("QuestionId")]
        public virtual Question Question { get; set; }

        [ForeignKey("SelectedChoiceId")]
        public virtual Choice SelectedChoice { get; set; }
    }
}
