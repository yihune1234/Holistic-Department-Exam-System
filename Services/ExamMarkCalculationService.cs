using HolisticDepartmentExamSystem.Data;
using HolisticDepartmentExamSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace HolisticDepartmentExamSystem.Services
{
    public class ExamMarkCalculationService
    {
        private readonly AppDbContext _context;

        public ExamMarkCalculationService(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Calculate weighted marks for all questions in an exam
        /// Formula: weighted_mark = (question_point / sum_of_question_points) * total_exam_weight
        /// </summary>
        public async Task<Dictionary<int, double>> CalculateWeightedMarksAsync(int examId)
        {
            var exam = await _context.Exams
                .Include(e => e.Questions)
                .FirstOrDefaultAsync(e => e.ExamId == examId);

            if (exam == null || !exam.Questions.Any())
                return new Dictionary<int, double>();

            // Calculate total raw points
            int totalRawPoints = exam.Questions.Sum(q => q.Marks);

            // If total raw points equals total marks, no scaling needed
            if (totalRawPoints == exam.TotalMarks)
            {
                return exam.Questions.ToDictionary(
                    q => q.QuestionId,
                    q => (double)q.Marks
                );
            }

            // Calculate weighted marks for each question
            var weightedMarks = new Dictionary<int, double>();
            
            foreach (var question in exam.Questions)
            {
                double weightedMark = ((double)question.Marks / totalRawPoints) * exam.TotalMarks;
                weightedMarks[question.QuestionId] = Math.Round(weightedMark, 2);
            }

            return weightedMarks;
        }

        /// <summary>
        /// Calculate student's weighted score for an exam attempt
        /// </summary>
        public async Task<double> CalculateStudentWeightedScoreAsync(int attemptId)
        {
            var attempt = await _context.ExamAttempts
                .Include(a => a.Exam)
                    .ThenInclude(e => e.Questions)
                        .ThenInclude(q => q.Choices)
                .Include(a => a.Answers)
                    .ThenInclude(ans => ans.SelectedChoice)
                .FirstOrDefaultAsync(a => a.AttemptId == attemptId);

            if (attempt == null)
                return 0;

            // Get weighted marks for all questions
            var weightedMarks = await CalculateWeightedMarksAsync(attempt.ExamId);

            // Calculate student's score
            double totalScore = 0;

            foreach (var answer in attempt.Answers)
            {
                // Check if answer is correct
                if (answer.SelectedChoice != null && answer.SelectedChoice.IsCorrect)
                {
                    // Add weighted mark for this question
                    if (weightedMarks.ContainsKey(answer.QuestionId))
                    {
                        totalScore += weightedMarks[answer.QuestionId];
                    }
                }
            }

            return Math.Round(totalScore, 2);
        }

        /// <summary>
        /// Get exam summary with weighted marks
        /// </summary>
        public async Task<ExamWeightedSummary> GetExamWeightedSummaryAsync(int examId)
        {
            var exam = await _context.Exams
                .Include(e => e.Questions)
                .FirstOrDefaultAsync(e => e.ExamId == examId);

            if (exam == null)
                return null;

            int totalRawPoints = exam.Questions.Sum(q => q.Marks);
            var weightedMarks = await CalculateWeightedMarksAsync(examId);

            return new ExamWeightedSummary
            {
                ExamId = examId,
                ExamTitle = exam.Title,
                TotalExamMarks = exam.TotalMarks,
                TotalRawPoints = totalRawPoints,
                IsScalingNeeded = totalRawPoints != exam.TotalMarks,
                QuestionWeights = exam.Questions.Select(q => new QuestionWeight
                {
                    QuestionId = q.QuestionId,
                    QuestionText = q.QuestionText,
                    RawPoints = q.Marks,
                    WeightedMarks = weightedMarks.ContainsKey(q.QuestionId) 
                        ? weightedMarks[q.QuestionId] 
                        : q.Marks,
                    QuestionOrder = q.QuestionOrder
                }).OrderBy(qw => qw.QuestionOrder).ToList()
            };
        }

        /// <summary>
        /// Validate if exam questions total matches exam total marks
        /// </summary>
        public async Task<ExamValidationResult> ValidateExamMarksAsync(int examId)
        {
            var exam = await _context.Exams
                .Include(e => e.Questions)
                .FirstOrDefaultAsync(e => e.ExamId == examId);

            if (exam == null)
            {
                return new ExamValidationResult
                {
                    IsValid = false,
                    Message = "Exam not found"
                };
            }

            if (!exam.Questions.Any())
            {
                return new ExamValidationResult
                {
                    IsValid = false,
                    Message = "Exam has no questions"
                };
            }

            int totalRawPoints = exam.Questions.Sum(q => q.Marks);

            if (totalRawPoints == exam.TotalMarks)
            {
                return new ExamValidationResult
                {
                    IsValid = true,
                    Message = "Exam marks are balanced. No scaling needed.",
                    TotalRawPoints = totalRawPoints,
                    TotalExamMarks = exam.TotalMarks,
                    RequiresScaling = false
                };
            }

            return new ExamValidationResult
            {
                IsValid = true,
                Message = $"Exam marks will be automatically scaled. Raw points: {totalRawPoints}, Exam total: {exam.TotalMarks}",
                TotalRawPoints = totalRawPoints,
                TotalExamMarks = exam.TotalMarks,
                RequiresScaling = true
            };
        }

        /// <summary>
        /// Calculate and save result for a student's exam attempt
        /// </summary>
        public async Task<Result> CalculateAndSaveResult(int attemptId)
        {
            var attempt = await _context.ExamAttempts
                .Include(a => a.Exam)
                .Include(a => a.Answers)
                    .ThenInclude(ans => ans.Question)
                        .ThenInclude(q => q.Choices)
                .FirstOrDefaultAsync(a => a.AttemptId == attemptId);

            if (attempt == null)
                return null;

            // Calculate weighted score
            double totalScore = await CalculateStudentWeightedScoreAsync(attemptId);

            // Calculate percentage
            double percentage = (totalScore / attempt.Exam.TotalMarks) * 100;

            // Determine grade
            string grade = percentage switch
            {
                >= 90 => "A",
                >= 80 => "B",
                >= 70 => "C",
                >= 60 => "D",
                >= 50 => "E",
                _ => "F"
            };

            // Determine pass status
            string passStatus = percentage >= 50 ? "Pass" : "Fail";

            // Create or update result
            var existingResult = await _context.Results
                .FirstOrDefaultAsync(r => r.AttemptId == attemptId);

            if (existingResult != null)
            {
                existingResult.TotalScore = (int)Math.Round(totalScore);
                existingResult.Grade = grade;
                existingResult.PassStatus = passStatus;
                existingResult.PublishedAt = DateTime.UtcNow;
            }
            else
            {
                var result = new Result
                {
                    AttemptId = attemptId,
                    TotalScore = (int)Math.Round(totalScore),
                    Grade = grade,
                    PassStatus = passStatus,
                    PublishedAt = DateTime.UtcNow
                };
                _context.Results.Add(result);
                existingResult = result;
            }

            await _context.SaveChangesAsync();
            return existingResult;
        }
    }

    // DTOs
    public class ExamWeightedSummary
    {
        public int ExamId { get; set; }
        public string ExamTitle { get; set; }
        public int TotalExamMarks { get; set; }
        public int TotalRawPoints { get; set; }
        public bool IsScalingNeeded { get; set; }
        public List<QuestionWeight> QuestionWeights { get; set; }
    }

    public class QuestionWeight
    {
        public int QuestionId { get; set; }
        public string QuestionText { get; set; }
        public int RawPoints { get; set; }
        public double WeightedMarks { get; set; }
        public int QuestionOrder { get; set; }
    }

    public class ExamValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; }
        public int TotalRawPoints { get; set; }
        public int TotalExamMarks { get; set; }
        public bool RequiresScaling { get; set; }
    }
}
