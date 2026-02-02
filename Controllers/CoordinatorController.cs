using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HolisticDepartmentExamSystem.Models;
using HolisticDepartmentExamSystem.Data;
using HolisticDepartmentExamSystem.Services;
using System.Text;

namespace HolisticDepartmentExamSystem.Controllers
{
    [Authorize(Roles = "Coordinator")]
    public class CoordinatorController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ExamMarkCalculationService _markCalculationService;

        public CoordinatorController(AppDbContext context, ExamMarkCalculationService markCalculationService)
        {
            _context = context;
            _markCalculationService = markCalculationService;
        }

        #region Dashboard & Overview

        public async Task<IActionResult> Dashboard()
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == User.Identity.Name);
            var coordinatorId = user.UserId;

            var exams = await _context.Exams.Where(e => e.CreatedBy == coordinatorId).ToListAsync();
            var fiveMinutesAgo = DateTime.UtcNow.AddMinutes(-5);

            var model = new CoordinatorDashboardViewModel
            {
                TotalExams = exams.Count,
                ActiveExams = exams.Count(e => e.Status == "Active"),
                ScheduledExams = exams.Count(e => e.Status == "Published"),
                CompletedExams = exams.Count(e => e.Status == "Closed"),
                CurrentTakingCount = await _context.ExamAttempts.CountAsync(ea => ea.Status == "In Progress"),
                TotalStudents = await _context.Students.CountAsync(),
                RecentExams = exams.OrderByDescending(e => e.CreatedAt).Take(5).ToList(),
                LiveActivity = await _context.ActivityLogs
                    .Include(al => al.User)
                    .OrderByDescending(al => al.Timestamp)
                    .Take(10)
                    .Select(al => new ActivityItemViewModel {
                        Username = al.User.Username,
                        Action = al.Action,
                        Timestamp = al.Timestamp
                    })
                    .ToListAsync()
            };
            
            return View(model);
        }

        #endregion

        #region Examination Management

        [HttpGet]
        public IActionResult CreateExam() => View();

        [HttpPost]
        public async Task<IActionResult> CreateExam(Exam exam)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == User.Identity.Name);
            exam.CreatedBy = user?.UserId ?? 0;
            exam.Status = "Draft";

            if (ModelState.IsValid)
            {
                _context.Exams.Add(exam);
                await _context.SaveChangesAsync();
                return RedirectToAction("ManageQuestions", new { examId = exam.ExamId });
            }

            return View(exam);
        }

        public async Task<IActionResult> ExamList()
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == User.Identity.Name);
            var exams = await _context.Exams
                .Where(e => e.CreatedBy == user.UserId)
                .Include(e => e.Questions)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
            return View(exams);
        }

        public async Task<IActionResult> ManageExam(int examId)
        {
            var exam = await _context.Exams
                .Include(e => e.Questions)
                .Include(e => e.ExamPasswords).ThenInclude(ep => ep.Student)
                .Include(e => e.ExamAttempts)
                .FirstOrDefaultAsync(e => e.ExamId == examId);

            if (exam == null) return NotFound();
            return View(exam);
        }

        [HttpGet]
        public async Task<IActionResult> EditExam(int examId)
        {
            var exam = await _context.Exams.FindAsync(examId);
            if (exam == null) return NotFound();
            return View(exam);
        }

        [HttpPost]
        public async Task<IActionResult> EditExam(Exam exam)
        {
            if (ModelState.IsValid)
            {
                var existing = await _context.Exams.FindAsync(exam.ExamId);
                if (existing == null) return NotFound();

                existing.Title = exam.Title;
                existing.Description = exam.Description;
                existing.DurationMinutes = exam.DurationMinutes;
                existing.TotalMarks = exam.TotalMarks;

                await _context.SaveChangesAsync();
                return RedirectToAction("ManageExam", new { examId = exam.ExamId });
            }
            return View(exam);
        }

        public async Task<IActionResult> ManageQuestions(int examId)
        {
            var exam = await _context.Exams
                .Include(e => e.Questions).ThenInclude(q => q.Choices)
                .FirstOrDefaultAsync(e => e.ExamId == examId);

            if (exam == null) return NotFound();

            // Get weighted marks summary
            var markSummary = await _markCalculationService.GetExamWeightedSummaryAsync(examId);
            ViewBag.MarkSummary = markSummary;

            return View(exam);
        }

        #endregion


        #region Student Interaction & Monitoring

        [HttpGet]
        public async Task<IActionResult> GeneratePasswords(int examId)
        {
            var exam = await _context.Exams
                .Include(e => e.ExamPasswords).ThenInclude(ep => ep.Student)
                .FirstOrDefaultAsync(e => e.ExamId == examId);

            if (exam == null) return NotFound();
            return View(exam);
        }

        [HttpPost]
        public async Task<IActionResult> GeneratePasswords(int examId, int length = 8)
        {
            var exam = await _context.Exams.FindAsync(examId);
            if (exam == null) return NotFound();

            var students = await _context.Students.ToListAsync();
            foreach (var student in students)
            {
                var existing = await _context.ExamPasswords
                    .FirstOrDefaultAsync(ep => ep.ExamId == examId && ep.StudentId == student.StudentId);
                
                if (existing == null)
                {
                    _context.ExamPasswords.Add(new ExamPassword
                    {
                        ExamId = examId, StudentId = student.StudentId,
                        PasswordHash = Guid.NewGuid().ToString().ToUpper().Substring(0, length),
                        ExpiresAt = DateTime.UtcNow.AddDays(1)
                    });
                }
                else if (!existing.IsUsed)
                {
                    existing.PasswordHash = Guid.NewGuid().ToString().ToUpper().Substring(0, length);
                    existing.ExpiresAt = DateTime.UtcNow.AddDays(1);
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("ManageExam", new { examId });
        }

        public async Task<IActionResult> MonitorExam(int examId)
        {
            var exam = await _context.Exams
                .Include(e => e.ExamAttempts).ThenInclude(ea => ea.Student)
                .FirstOrDefaultAsync(e => e.ExamId == examId);

            if (exam == null) return NotFound();

            var stats = new ExamMonitorViewModel
            {
                Exam = exam,
                TotalAssigned = await _context.ExamPasswords.CountAsync(ep => ep.ExamId == examId),
                InProgressCount = exam.ExamAttempts.Count(ea => ea.Status == "In Progress"),
                SubmittedCount = exam.ExamAttempts.Count(ea => ea.Status == "Submitted")
            };

            return View(stats);
        }

        [HttpPost]
        public async Task<IActionResult> BlockStudent(int attemptId)
        {
            var attempt = await _context.ExamAttempts.FindAsync(attemptId);
            if (attempt == null) return NotFound();
            attempt.IsBlocked = true;
            attempt.Status = "Blocked";
            await _context.SaveChangesAsync();
            return RedirectToAction("MonitorExam", new { examId = attempt.ExamId });
        }

        [HttpPost]
        public async Task<IActionResult> UnblockStudent(int attemptId)
        {
            var attempt = await _context.ExamAttempts.FindAsync(attemptId);
            if (attempt == null) return NotFound();
            attempt.IsBlocked = false;
            attempt.Status = "In Progress";
            await _context.SaveChangesAsync();
            return RedirectToAction("MonitorExam", new { examId = attempt.ExamId });
        }

        [HttpPost]
        public async Task<IActionResult> ActivateExam(int examId)
        {
            var exam = await _context.Exams.FindAsync(examId);
            if (exam == null) return NotFound();

            exam.Status = "Active";
            await _context.SaveChangesAsync();
            await LogActivity(exam.CreatedBy, $"Activated Exam: {exam.Title}");

            return RedirectToAction("MonitorExam", new { examId });
        }

        [HttpPost]
        public async Task<IActionResult> PauseExam(int examId)
        {
            var exam = await _context.Exams.FindAsync(examId);
            if (exam == null) return NotFound();

            exam.Status = "Draft"; // or "Paused" if you have that status
            await _context.SaveChangesAsync();
            await LogActivity(exam.CreatedBy, $"Paused Exam: {exam.Title}");

            return RedirectToAction("MonitorExam", new { examId });
        }

        [HttpPost]
        public async Task<IActionResult> CloseExam(int examId)
        {
            var exam = await _context.Exams.FindAsync(examId);
            if (exam == null) return NotFound();

            exam.Status = "Closed";
            // Optional: Set EndTime if you have a property for it
            await _context.SaveChangesAsync();
            await LogActivity(exam.CreatedBy, $"Closed Exam: {exam.Title}");

            return RedirectToAction("MonitorExam", new { examId });
        }

        #endregion


        [HttpGet]
        public async Task<IActionResult> GetMonitorStats(int examId)
        {
            var exam = await _context.Exams
                .Include(e => e.ExamAttempts)
                .ThenInclude(ea => ea.Student)
                .FirstOrDefaultAsync(e => e.ExamId == examId);

            if (exam == null) return NotFound();

            var assignedStudentIds = await _context.ExamPasswords
                .Where(ep => ep.ExamId == examId)
                .Select(ep => ep.StudentId)
                .ToListAsync();

            var students = await _context.Students
                .Include(s => s.User)
                .Where(s => assignedStudentIds.Contains(s.StudentId))
                .ToListAsync();

            var oneHourAgo = DateTime.Now.AddHours(-1);
            var activeSessionUserIds = await _context.ActivityLogs
                .Where(al => al.Timestamp >= oneHourAgo && al.Action.Contains("Login"))
                .Select(al => al.UserId)
                .Distinct()
                .ToListAsync();

            var stats = new
            {
                totalAssigned = assignedStudentIds.Count,
                loggedInCount = students.Count(s => activeSessionUserIds.Contains(s.UserId)),
                startedCount = exam.ExamAttempts.Count,
                inProgressCount = exam.ExamAttempts.Count(ea => ea.Status == "In Progress"),
                submittedCount = exam.ExamAttempts.Count(ea => ea.Status == "Submitted"),
                studentActivities = students.OrderBy(s => s.FullName).Select(s => {
                    var attempt = exam.ExamAttempts.FirstOrDefault(ea => ea.StudentId == s.StudentId);
                    var latestLog = _context.ActivityLogs
                        .Where(al => al.UserId == s.UserId && al.Action.Contains("Heartbeat"))
                        .OrderByDescending(al => al.Timestamp)
                        .FirstOrDefault();

                    return new
                    {
                        fullName = s.FullName,
                        isLoggedIn = activeSessionUserIds.Contains(s.UserId) || (latestLog != null && latestLog.Timestamp >= DateTime.Now.AddMinutes(-1)),
                        status = attempt?.Status ?? "Not Started",
                        latestActivity = latestLog?.Action.Replace("Exam Heartbeat: ", "") ?? "No Signal",
                        startTime = attempt != null ? attempt.StartTime.ToString("hh:mm tt") : "-",
                        endTime = attempt?.EndTime?.ToString("hh:mm tt") ?? "-",
                        durationUsed = attempt?.EndTime.HasValue == true 
                            ? (attempt.EndTime.Value - attempt.StartTime).ToString(@"hh\:mm\:ss") 
                            : (attempt != null ? (DateTime.Now - attempt.StartTime).ToString(@"hh\:mm\:ss") : "-"),
                        attemptId = attempt?.AttemptId ?? 0,
                        isBlocked = attempt?.IsBlocked ?? false
                    };
                }),
                liveFeed = await _context.ActivityLogs
                    .Include(al => al.User)
                    .Where(al => al.Timestamp >= oneHourAgo && (al.Action.Contains("Exam") || al.Action.Contains("Login")))
                    .OrderByDescending(al => al.Timestamp)
                    .Take(10)
                    .Select(al => $"{al.User.Username} {al.Action} - {al.Timestamp:hh:mm tt}")
                    .ToListAsync()
            };

            return Json(stats);
        }


        #region Result Management

        public async Task<IActionResult> ExamResults(int examId)
        {
            var exam = await _context.Exams
                .Include(e => e.ExamAttempts).ThenInclude(ea => ea.Student)
                .Include(e => e.ExamAttempts).ThenInclude(ea => ea.Result)
                .FirstOrDefaultAsync(e => e.ExamId == examId);

            if (exam == null) return NotFound();
            return View(exam);
        }

        public async Task<IActionResult> ExportResults(int examId)
        {
            var exam = await _context.Exams
                .Include(e => e.ExamAttempts).ThenInclude(ea => ea.Student)
                .Include(e => e.ExamAttempts).ThenInclude(ea => ea.Result)
                .FirstOrDefaultAsync(e => e.ExamId == examId);

            if (exam == null) return NotFound();

            var csv = new StringBuilder("Student ID,Name,Score,Grade,Status\n");
            foreach (var a in exam.ExamAttempts)
                csv.AppendLine($"{a.StudentId},{a.Student.FullName},{a.Result?.TotalScore},{a.Result?.Grade},{a.Status}");

            return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"Exam_{examId}_Results.csv");
        }

        #endregion

        #region Helper Methods (Private)

        private async Task LogActivity(int userId, string action)
        {
            _context.ActivityLogs.Add(new ActivityLog
            {
                UserId = userId, Action = action, Timestamp = DateTime.Now,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            });
            await _context.SaveChangesAsync();
        }

        #endregion

        #region Password Management & Weighted Marks

        /// <summary>
        /// Regenerate password for a specific student
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> RegeneratePassword(int passwordId)
        {
            var examPassword = await _context.ExamPasswords
                .Include(ep => ep.Exam)
                .FirstOrDefaultAsync(ep => ep.PasswordId == passwordId);

            if (examPassword == null)
            {
                return Json(new { success = false, message = "Password record not found" });
            }

            // Only regenerate if not used
            if (examPassword.IsUsed)
            {
                return Json(new { success = false, message = "Cannot regenerate used password" });
            }

            // Generate new password
            string newPassword = Guid.NewGuid().ToString().ToUpper().Substring(0, 8);
            examPassword.PasswordHash = newPassword;
            examPassword.ExpiresAt = DateTime.UtcNow.AddDays(1);

            await _context.SaveChangesAsync();

            return Json(new { 
                success = true, 
                message = "Password regenerated successfully",
                newPassword = newPassword
            });
        }

        /// <summary>
        /// Regenerate all passwords for an exam
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> RegenerateAllPasswords(int examId)
        {
            var examPasswords = await _context.ExamPasswords
                .Where(ep => ep.ExamId == examId && !ep.IsUsed)
                .ToListAsync();

            if (!examPasswords.Any())
            {
                TempData["Error"] = "No unused passwords found to regenerate";
                return RedirectToAction("GeneratePasswords", new { examId });
            }

            int regeneratedCount = 0;
            foreach (var ep in examPasswords)
            {
                ep.PasswordHash = Guid.NewGuid().ToString().ToUpper().Substring(0, 8);
                ep.ExpiresAt = DateTime.UtcNow.AddDays(1);
                regeneratedCount++;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Successfully regenerated {regeneratedCount} passwords";
            return RedirectToAction("GeneratePasswords", new { examId });
        }

        /// <summary>
        /// View weighted marks for an exam
        /// </summary>
        public async Task<IActionResult> ViewWeightedMarks(int examId)
        {
            var summary = await _markCalculationService.GetExamWeightedSummaryAsync(examId);

            if (summary == null)
            {
                TempData["Error"] = "Exam not found";
                return RedirectToAction("ExamList");
            }

            return View(summary);
        }

        /// <summary>
        /// Validate exam marks
        /// </summary>
        public async Task<IActionResult> ValidateExamMarks(int examId)
        {
            var validation = await _markCalculationService.ValidateExamMarksAsync(examId);
            return Json(validation);
        }

        #endregion

    }

    public class CoordinatorDashboardViewModel
    {
        public int TotalExams { get; set; }
        public int ActiveExams { get; set; }
        public int ScheduledExams { get; set; }
        public int CompletedExams { get; set; }
        public int CurrentTakingCount { get; set; }
        public int TotalStudents { get; set; }
        public int StudentsLoggedIn { get; set; } // HEMS
        public int StudentsReady { get; set; } // HEMS
        public List<Exam> RecentExams { get; set; } = new List<Exam>();
        public List<ActivityItemViewModel> LiveActivity { get; set; } = new List<ActivityItemViewModel>();
    }

    public class ActivityItemViewModel
    {
        public string Username { get; set; }
        public string Action { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class ExamMonitorViewModel
    {
        public Exam Exam { get; set; }
        public int TotalAssigned { get; set; }
        public int LoggedInCount { get; set; }
        public int StartedCount { get; set; }
        public int InProgressCount { get; set; }
        public int SubmittedCount { get; set; }
        public List<StudentMonitorItem> StudentActivities { get; set; } = new List<StudentMonitorItem>();
        public List<string> LiveFeed { get; set; } = new List<string>();
    }

    public class StudentMonitorItem
    {
        public string FullName { get; set; }
        public bool IsLoggedIn { get; set; }
        public string Status { get; set; }
        public string LatestActivity { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string DurationUsed { get; set; }
        public int AttemptId { get; set; }
        public bool IsBlocked { get; set; }
    }
}
