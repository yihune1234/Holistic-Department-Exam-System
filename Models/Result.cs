using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HolisticDepartmentExamSystem.Models
{
    public class Result
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("result_id")]
        public int ResultId { get; set; }

        [Column("attempt_id")]
        public int AttemptId { get; set; }

        [Column("total_score")]
        public int TotalScore { get; set; }

        [Column("grade")]
        [StringLength(10)]
        public string Grade { get; set; }

        [Column("pass_status")]
        [StringLength(20)]
        public string PassStatus { get; set; }

        [Column("published_at")]
        public DateTime PublishedAt { get; set; }

        [ForeignKey("AttemptId")]
        public virtual ExamAttempt Attempt { get; set; }
    }
}
