using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HolisticDepartmentExamSystem.Models
{
    public class Student
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("student_id")]
        public int StudentId { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("full_name")]
        [StringLength(255)]
        public string FullName { get; set; }

        [Column("email")]
        [StringLength(255)]
        public string Email { get; set; }

        [Column("department")]
        [StringLength(100)]
        public string Department { get; set; }

        [Column("year_of_study")]
        public int YearOfStudy { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }
        
        public virtual ICollection<ExamPassword> ExamPasswords { get; set; }
        public virtual ICollection<ExamAttempt> ExamAttempts { get; set; }
    }
}
