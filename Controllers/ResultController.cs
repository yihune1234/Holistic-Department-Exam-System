using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HolisticDepartmentExamSystem.Models;
using HolisticDepartmentExamSystem.Data;

namespace HolisticDepartmentExamSystem.Controllers
{
    public class ResultController : Controller
    {
        private readonly AppDbContext _context;

        public ResultController(AppDbContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "Student")]
        public IActionResult MyResults()
        {
            var user = _context.Users.FirstOrDefault(u => u.Username == User.Identity.Name);
            var student = _context.Students.FirstOrDefault(s => s.UserId == user.UserId);

            var results = _context.Results
                .Where(r => r.Attempt.StudentId == student.StudentId)
                .Include(r => r.Attempt)
                .Include(r => r.Attempt.Exam)
                .OrderByDescending(r => r.Attempt.EndTime)
                .ToList();

            return View(results);
        }

        [Authorize(Roles = "Coordinator")]
        public IActionResult ExamResults(int examId)
        {
            var results = _context.Results
                .Where(r => r.Attempt.ExamId == examId)
                .Include(r => r.Attempt)
                .Include(r => r.Attempt.Student)
                .Include(r => r.Attempt.Student.User)
                .OrderByDescending(r => r.TotalScore)
                .ToList();

            var exam = _context.Exams.Find(examId);

            ViewBag.Exam = exam;
            return View(results);
        }

        [Authorize(Roles = "Coordinator")]
        public IActionResult GenerateResults(int examId)
        {
            var exam = _context.Exams.Find(examId);
            var attempts = _context.ExamAttempts
                .Where(ea => ea.ExamId == examId && ea.Status == "Submitted" && !_context.Results.Any(r => r.AttemptId == ea.AttemptId))
                .Include(ea => ea.Student)
                .Include(ea => ea.Answers)
                .ThenInclude(sa => sa.SelectedChoice)
                .ToList();

            var generatedResults = new List<Result>();

            foreach (var attempt in attempts)
            {
                var totalPoints = _context.Questions
                    .Where(q => q.ExamId == examId)
                    .Sum(q => q.Marks);

                // Assuming simple marking for now: if Choice is correct, award full marks
                // In new schema, we need to join with Question to get marks
                var earnedPoints = 0;
                
                foreach(var answer in attempt.Answers)
                {
                     if(answer.SelectedChoiceId.HasValue)
                     {
                         var choice = _context.Choices.Find(answer.SelectedChoiceId);
                         if(choice != null && choice.IsCorrect)
                         {
                             var question = _context.Questions.Find(answer.QuestionId);
                             earnedPoints += question.Marks;
                         }
                     }
                }

                var result = new Result
                {
                    AttemptId = attempt.AttemptId,
                    TotalScore = earnedPoints,
                    Grade = CalculateGrade((double)earnedPoints / (totalPoints > 0 ? totalPoints : 1) * 100),
                    PassStatus = earnedPoints >= (totalPoints * 0.5) ? "Pass" : "Fail",
                    PublishedAt = DateTime.UtcNow
                };

                generatedResults.Add(result);
            }

            _context.Results.AddRange(generatedResults);
            _context.SaveChanges();

            return RedirectToAction("ExamResults", new { examId });
        }

        private string CalculateGrade(double percentage)
        {
            return percentage switch
            {
                >= 90 => "A",
                >= 80 => "B",
                >= 70 => "C",
                >= 60 => "D",
                >= 50 => "E",
                _ => "F"
            };
        }
    }
}
