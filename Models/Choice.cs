using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HolisticDepartmentExamSystem.Models
{
    public class Choice
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("choice_id")]
        public int ChoiceId { get; set; }

        [Column("question_id")]
        public int QuestionId { get; set; }

        [Column("choice_text")]
        public string ChoiceText { get; set; }

        [Column("is_correct")]
        public bool IsCorrect { get; set; }

        [ForeignKey("QuestionId")]
        public virtual Question Question { get; set; }
    }
}
