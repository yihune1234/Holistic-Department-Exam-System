using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HolisticDepartmentExamSystem.Models;
using HolisticDepartmentExamSystem.Data;

namespace HolisticDepartmentExamSystem.Controllers
{
    public class ExamController : Controller
    {
        private readonly AppDbContext _context;

        public ExamController(AppDbContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "Coordinator,Student")]
        public IActionResult GetExamDetails(int examId)
        {
            var exam = _context.Exams.Find(examId);
            if (exam == null) return NotFound();

            return Json(new
            {
                exam.ExamId,
                exam.Title,
                exam.Description,
                exam.DurationMinutes,
                exam.TotalMarks,
                exam.Status
            });
        }

        [Authorize(Roles = "Coordinator")]
        [HttpPost]
        public IActionResult ActivateExam(int examId)
        {
            var exam = _context.Exams.Find(examId);
            if (exam == null) return NotFound();

            if (exam.Status == "Draft")
            {
                exam.Status = "Active";
                _context.SaveChanges();
            }

            return Json(new { success = true, message = "Exam activated successfully" });
        }

        [Authorize(Roles = "Coordinator")]
        [HttpPost]
        public IActionResult DeactivateExam(int examId)
        {
            var exam = _context.Exams.Find(examId);
            if (exam == null) return NotFound();

            exam.Status = "Closed";
            _context.SaveChanges();

            return Json(new { success = true, message = "Exam closed successfully" });
        }
    }
}
