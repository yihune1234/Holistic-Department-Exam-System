using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HolisticDepartmentExamSystem.Models;
using HolisticDepartmentExamSystem.Data;

namespace HolisticDepartmentExamSystem.Controllers
{
    [Authorize(Roles = "Coordinator")]
    public class QuestionController : Controller
    {
        private readonly AppDbContext _context;

        public QuestionController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Create(int examId)
        {
            ViewBag.ExamId = examId;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Question question, List<string> optionTexts, int correctOptionIndex)
        {
            if (ModelState.IsValid && question.ExamId > 0)
            {
                // Verify the exam exists
                var exam = await _context.Exams.FindAsync(question.ExamId);
                if (exam == null)
                {
                    ModelState.AddModelError("", "Invalid exam selected");
                    ViewBag.ExamId = question.ExamId;
                    return View(question);
                }

                _context.Questions.Add(question);
                await _context.SaveChangesAsync();

                if (optionTexts != null && optionTexts.Any())
                {
                    for (int i = 0; i < optionTexts.Count; i++)
                    {
                        if (!string.IsNullOrWhiteSpace(optionTexts[i]))
                        {
                            var choice = new Choice
                            {
                                QuestionId = question.QuestionId,
                                ChoiceText = optionTexts[i],
                                IsCorrect = i == correctOptionIndex
                            };
                            _context.Choices.Add(choice);
                        }
                    }
                    await _context.SaveChangesAsync();
                }

                return RedirectToAction("ManageQuestions", "Coordinator", new { examId = question.ExamId });
            }

            if (question.ExamId <= 0)
            {
                ModelState.AddModelError("", "Invalid exam ID");
            }

            ViewBag.ExamId = question.ExamId;
            return View(question);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var question = await _context.Questions
                .Include(q => q.Choices)
                .FirstOrDefaultAsync(q => q.QuestionId == id);

            if (question == null) return NotFound();

            return View(question);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Question question, List<string> optionTexts, int correctOptionIndex)
        {
            if (ModelState.IsValid)
            {
                var existingQuestion = await _context.Questions
                    .Include(q => q.Choices)
                    .FirstOrDefaultAsync(q => q.QuestionId == question.QuestionId);

                if (existingQuestion == null) return NotFound();

                existingQuestion.QuestionText = question.QuestionText;
                existingQuestion.QuestionType = question.QuestionType;
                existingQuestion.Marks = question.Marks;
                existingQuestion.QuestionOrder = question.QuestionOrder;

                // Sync choices
                _context.Choices.RemoveRange(existingQuestion.Choices);
                if (optionTexts != null)
                {
                    for (int i = 0; i < optionTexts.Count; i++)
                    {
                        var choice = new Choice
                        {
                            QuestionId = question.QuestionId,
                            ChoiceText = optionTexts[i],
                            IsCorrect = i == correctOptionIndex
                        };
                        _context.Choices.Add(choice);
                    }
                }

                await _context.SaveChangesAsync();

                return RedirectToAction("ManageQuestions", "Coordinator", new { examId = question.ExamId });
            }

            return View(question);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var question = await _context.Questions.FindAsync(id);
            if (question == null) return NotFound();

            var examId = question.ExamId;

            _context.Questions.Remove(question);
            await _context.SaveChangesAsync();

            return RedirectToAction("ManageQuestions", "Coordinator", new { examId });
        }
    }
}
