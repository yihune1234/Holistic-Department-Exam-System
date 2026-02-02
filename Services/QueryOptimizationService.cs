using HolisticDepartmentExamSystem.Data;
using HolisticDepartmentExamSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace HolisticDepartmentExamSystem.Services
{
    public class QueryOptimizationService
    {
        private readonly AppDbContext _context;

        public QueryOptimizationService(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get online users (active within last 5 minutes) - Optimized query
        /// </summary>
        public async Task<List<User>> GetOnlineUsersAsync(int minutesThreshold = 5)
        {
            var threshold = DateTime.UtcNow.AddMinutes(-minutesThreshold);
            
            return await _context.Users
                .Include(u => u.Role)
                .Where(u => u.LastActivity.HasValue && u.LastActivity.Value > threshold)
                .OrderByDescending(u => u.LastActivity)
                .AsNoTracking() // Read-only optimization
                .ToListAsync();
        }

        /// <summary>
        /// Get active exams with student counts - Optimized query
        /// </summary>
        public async Task<List<ExamWithStats>> GetActiveExamsWithStatsAsync()
        {
            var activeExams = await _context.Exams
                .Where(e => e.Status == "Active" || e.Status == "Published")
                .Select(e => new ExamWithStats
                {
                    Exam = e,
                    AssignedStudents = e.ExamPasswords.Count(),
                    StartedAttempts = e.ExamAttempts.Count(),
                    InProgressAttempts = e.ExamAttempts.Count(ea => ea.Status == "In Progress"),
                    CompletedAttempts = e.ExamAttempts.Count(ea => ea.Status == "Submitted" || ea.Status == "Auto-Submitted")
                })
                .AsNoTracking()
                .ToListAsync();

            return activeExams;
        }

        /// <summary>
        /// Get student performance summary - Optimized query
        /// </summary>
        public async Task<StudentPerformanceSummary> GetStudentPerformanceAsync(int studentId)
        {
            var attempts = await _context.ExamAttempts
                .Include(ea => ea.Result)
                .Include(ea => ea.Exam)
                .Where(ea => ea.StudentId == studentId && ea.Result != null)
                .AsNoTracking()
                .ToListAsync();

            if (!attempts.Any())
            {
                return new StudentPerformanceSummary
                {
                    StudentId = studentId,
                    TotalExamsTaken = 0,
                    AverageScore = 0,
                    HighestScore = 0,
                    LowestScore = 0,
                    PassRate = 0
                };
            }

            var results = attempts.Select(a => a.Result).ToList();

            return new StudentPerformanceSummary
            {
                StudentId = studentId,
                TotalExamsTaken = attempts.Count,
                AverageScore = results.Average(r => r.TotalScore),
                HighestScore = results.Max(r => r.TotalScore),
                LowestScore = results.Min(r => r.TotalScore),
                PassRate = (double)results.Count(r => r.PassStatus == "Pass") / results.Count * 100
            };
        }

        /// <summary>
        /// Get exam statistics - Optimized query
        /// </summary>
        public async Task<ExamStatistics> GetExamStatisticsAsync(int examId)
        {
            var exam = await _context.Exams
                .Include(e => e.ExamAttempts)
                    .ThenInclude(ea => ea.Result)
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.ExamId == examId);

            if (exam == null) return null;

            var completedAttempts = exam.ExamAttempts
                .Where(ea => ea.Result != null)
                .ToList();

            if (!completedAttempts.Any())
            {
                return new ExamStatistics
                {
                    ExamId = examId,
                    TotalAttempts = exam.ExamAttempts.Count,
                    CompletedAttempts = 0,
                    AverageScore = 0,
                    HighestScore = 0,
                    LowestScore = 0,
                    PassRate = 0
                };
            }

            var results = completedAttempts.Select(a => a.Result).ToList();

            return new ExamStatistics
            {
                ExamId = examId,
                TotalAttempts = exam.ExamAttempts.Count,
                CompletedAttempts = completedAttempts.Count,
                AverageScore = results.Average(r => r.TotalScore),
                HighestScore = results.Max(r => r.TotalScore),
                LowestScore = results.Min(r => r.TotalScore),
                PassRate = (double)results.Count(r => r.PassStatus == "Pass") / results.Count * 100,
                GradeDistribution = results.GroupBy(r => r.Grade)
                    .ToDictionary(g => g.Key, g => g.Count())
            };
        }

        /// <summary>
        /// Batch update last activity for multiple users - Optimized
        /// </summary>
        public async Task UpdateLastActivityBatchAsync(List<int> userIds)
        {
            var now = DateTime.UtcNow;
            
            await _context.Users
                .Where(u => userIds.Contains(u.UserId))
                .ExecuteUpdateAsync(u => u.SetProperty(x => x.LastActivity, now));
        }

        /// <summary>
        /// Clean old activity logs - Optimized bulk delete
        /// </summary>
        public async Task<int> CleanOldActivityLogsAsync(int daysToKeep = 90)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
            
            return await _context.ActivityLogs
                .Where(al => al.Timestamp < cutoffDate)
                .ExecuteDeleteAsync();
        }
    }

    // DTOs for optimized queries
    public class ExamWithStats
    {
        public Exam Exam { get; set; }
        public int AssignedStudents { get; set; }
        public int StartedAttempts { get; set; }
        public int InProgressAttempts { get; set; }
        public int CompletedAttempts { get; set; }
    }

    public class StudentPerformanceSummary
    {
        public int StudentId { get; set; }
        public int TotalExamsTaken { get; set; }
        public double AverageScore { get; set; }
        public int HighestScore { get; set; }
        public int LowestScore { get; set; }
        public double PassRate { get; set; }
    }

    public class ExamStatistics
    {
        public int ExamId { get; set; }
        public int TotalAttempts { get; set; }
        public int CompletedAttempts { get; set; }
        public double AverageScore { get; set; }
        public int HighestScore { get; set; }
        public int LowestScore { get; set; }
        public double PassRate { get; set; }
        public Dictionary<string, int> GradeDistribution { get; set; }
    }
}
