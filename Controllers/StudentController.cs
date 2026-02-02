using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HolisticDepartmentExamSystem.Models;
using HolisticDepartmentExamSystem.Data;
using HolisticDepartmentExamSystem.Services;
using System.Security.Claims;

namespace HolisticDepartmentExamSystem.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ExamMarkCalculationService _markCalculationService;

        public StudentController(AppDbContext context, ExamMarkCalculationService markCalculationService)
        {
            _context = context;
            _markCalculationService = markCalculationService;
        }

        #region Student Dashboard & Academic Sessions

        public async Task<IActionResult> Dashboard()
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == User.Identity.Name);
            if (user == null) return RedirectToAction("Login", "Account");

            var student = await _context.Students
                .Include(s => s.User)
                .Include(s => s.ExamAttempts).ThenInclude(ea => ea.Exam)
                .Include(s => s.ExamAttempts).ThenInclude(ea => ea.Result)
                .Include(s => s.ExamPasswords).ThenInclude(ep => ep.Exam)
                .FirstOrDefaultAsync(s => s.UserId == user.UserId);
            
            if (student == null) return NotFound();

            // Update last activity
            user.LastActivity = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Get upcoming exams (assigned but not taken)
            var upcomingExams = await _context.ExamPasswords
                .Include(ep => ep.Exam)
                .Where(ep => ep.StudentId == student.StudentId && 
                            !ep.IsUsed && 
                            (ep.Exam.Status == "Published" || ep.Exam.Status == "Active"))
                .Select(ep => ep.Exam)
                .OrderBy(e => e.CreatedAt)
                .Take(5)
                .ToListAsync();

            // Get recent results (only published results)
            var recentResults = await _context.Results
                .Include(r => r.Attempt)
                    .ThenInclude(a => a.Exam)
                .Where(r => r.Attempt.StudentId == student.StudentId && 
                           r.Attempt.Exam.ResultsPublished)
                .OrderByDescending(r => r.PublishedAt)
                .Take(5)
                .ToListAsync();

            // Calculate statistics
            var totalExamsTaken = student.ExamAttempts.Count(ea => ea.Status == "Submitted");
            var averageScore = recentResults.Any() 
                ? recentResults.Average(r => r.TotalScore) 
                : 0;

            var viewModel = new StudentDashboardViewModel
            {
                Student = student,
                UpcomingExams = upcomingExams,
                RecentResults = recentResults,
                TotalExamsTaken = totalExamsTaken,
                AverageScore = Math.Round(averageScore, 2)
            };
            
            return View(viewModel);
        }

        public async Task<IActionResult> ExamList()
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == User.Identity.Name);
            if (user == null) return RedirectToAction("Login", "Account");

            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == user.UserId);
            if (student == null) return RedirectToAction("Dashboard", "Student");
            
            var exams = await _context.Exams
                .Where(e => e.Status == "Published" || e.Status == "Active")
                .Include(e => e.ExamPasswords.Where(ep => ep.StudentId == student.StudentId))
                .ToListAsync();
            
            return View(exams);
        }

        #endregion


        #region Examination Execution & Submission

        [HttpGet]
        public async Task<IActionResult> EnterPassword(int examId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == User.Identity.Name);
            if (user == null) return RedirectToAction("Login", "Account");

            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == user.UserId);
            if (student == null) return RedirectToAction("Dashboard", "Student");
            
            var existingAttempt = await _context.ExamAttempts
                .FirstOrDefaultAsync(ea => ea.ExamId == examId && ea.StudentId == student.StudentId && ea.Status == "Submitted");

            if (existingAttempt != null) return RedirectToAction("Result", new { attemptId = existingAttempt.AttemptId });

            // Get exam details and password
            var exam = await _context.Exams.FirstOrDefaultAsync(e => e.ExamId == examId);
            var studentPassword = await _context.ExamPasswords
                .FirstOrDefaultAsync(ep => ep.ExamId == examId && ep.StudentId == student.StudentId && !ep.IsUsed);

            ViewBag.ExamId = examId;
            ViewBag.ExamTitle = exam?.Title ?? "Examination";
            ViewBag.Password = studentPassword?.PasswordHash ?? "";
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> EnterPassword(int examId, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == User.Identity.Name);
            if (user == null) return RedirectToAction("Login", "Account");
            
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == user.UserId);
            if (student == null) return RedirectToAction("Dashboard", "Student");

            var ep = await _context.ExamPasswords
                .FirstOrDefaultAsync(e => e.ExamId == examId && e.StudentId == student.StudentId && e.PasswordHash == password && !e.IsUsed);

            if (ep != null)
            {
                ep.IsUsed = true;
                var attempt = new ExamAttempt { ExamId = examId, StudentId = student.StudentId, StartTime = DateTime.Now, Status = "In Progress" };
                _context.ExamAttempts.Add(attempt);
                await _context.SaveChangesAsync();
                return RedirectToAction("TakeExam", new { attemptId = attempt.AttemptId });
            }

            ViewBag.Error = "Invalid or used password";
            ViewBag.ExamId = examId;
            
            // Get exam details and password for display
            var exam = await _context.Exams.FirstOrDefaultAsync(e => e.ExamId == examId);
            var studentPassword = await _context.ExamPasswords
                .FirstOrDefaultAsync(ep => ep.ExamId == examId && ep.StudentId == student.StudentId && !ep.IsUsed);

            ViewBag.ExamTitle = exam?.Title ?? "Examination";
            ViewBag.Password = studentPassword?.PasswordHash ?? "";
            
            return View();
        }

        public async Task<IActionResult> TakeExam(int attemptId)
        {
            var attempt = await _context.ExamAttempts
                .Include(ea => ea.Exam).ThenInclude(e => e.Questions).ThenInclude(q => q.Choices)
                .Include(ea => ea.Answers)
                .FirstOrDefaultAsync(ea => ea.AttemptId == attemptId);

            if (attempt == null) return NotFound();
            if (attempt.Status == "Submitted") return RedirectToAction("Result", new { attemptId });

            return View(attempt);
        }

        [HttpPost]
        public async Task<IActionResult> SaveAnswer(int attemptId, int questionId, int? selectedOptionId, bool isFlagged)
        {
            var answer = await _context.ExamAnswers.FirstOrDefaultAsync(sa => sa.AttemptId == attemptId && sa.QuestionId == questionId);
            if (answer == null)
            {
                answer = new ExamAnswer { AttemptId = attemptId, QuestionId = questionId };
                _context.ExamAnswers.Add(answer);
            }
            answer.SelectedChoiceId = selectedOptionId;
            answer.IsFlagged = isFlagged;
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> SubmitExam(int attemptId)
        {
            var attempt = await _context.ExamAttempts
                .Include(ea => ea.Exam)
                .Include(ea => ea.Answers)
                    .ThenInclude(sa => sa.Question)
                        .ThenInclude(q => q.Choices)
                .FirstOrDefaultAsync(ea => ea.AttemptId == attemptId);

            if (attempt == null) return NotFound();

            attempt.EndTime = DateTime.Now;
            attempt.Status = "Submitted";
            await _context.SaveChangesAsync();

            // Use weighted mark calculation service
            var result = await _markCalculationService.CalculateAndSaveResult(attemptId);

            if (result == null)
            {
                TempData["Error"] = "Failed to calculate result";
                return RedirectToAction("Dashboard");
            }

            return RedirectToAction("Result", new { attemptId });
        }

        #endregion


        #region Result Viewing & Performance Recommendations

        public async Task<IActionResult> Result(int attemptId)
        {
            var attempt = await _context.ExamAttempts
                .Include(ea => ea.Exam).ThenInclude(e => e.Questions)
                .Include(ea => ea.Answers).ThenInclude(sa => sa.Question) 
                .FirstOrDefaultAsync(ea => ea.AttemptId == attemptId);

            if (attempt == null) return NotFound();

            var result = await _context.Results
                .Include(r => r.Attempt).ThenInclude(ea => ea.Exam)
                .FirstOrDefaultAsync(r => r.AttemptId == attemptId);

            if (result == null) return NotFound();

            var recommendations = new List<string>();
            var allCount = attempt.Exam.Questions.Count;
            var answeredCount = attempt.Answers.Count;
            
            if (allCount > answeredCount) recommendations.Add($"You skipped {allCount - answeredCount} questions. Time management is key.");

            return View(new ResultViewModel { Result = result, Recommendations = recommendations });
        }

        public async Task<IActionResult> ReviewAnswers(int attemptId)
        {
            var attempt = await _context.ExamAttempts.Include(ea => ea.Exam).FirstOrDefaultAsync(ea => ea.AttemptId == attemptId);
            if (attempt == null || !attempt.Exam.ResultsPublished) return RedirectToAction("Result", new { attemptId });

            var answers = await _context.ExamAnswers
                .Include(sa => sa.Question).ThenInclude(q => q.Choices).Include(sa => sa.SelectedChoice)
                .Where(sa => sa.AttemptId == attemptId).ToListAsync();

            return View(new ReviewAnswersViewModel { Attempt = attempt, Answers = answers });
        }

        private string CalculateGrade(double pc) => pc switch { >= 90 => "A", >= 80 => "B", >= 70 => "C", >= 60 => "D", >= 50 => "E", _ => "F" };

        #endregion
    }

    #region View Models

    public class StudentDashboardViewModel
    {
        public Student Student { get; set; }
        public List<Exam> UpcomingExams { get; set; }
        public List<Result> RecentResults { get; set; }
        public int TotalExamsTaken { get; set; }
        public double AverageScore { get; set; }
    }

    public class ReviewAnswersViewModel
    {
        public ExamAttempt Attempt { get; set; }
        public List<ExamAnswer> Answers { get; set; }
    }

    public class ResultViewModel
    {
        public Result Result { get; set; }
        public List<string> Recommendations { get; set; }
    }

    #endregion

}
