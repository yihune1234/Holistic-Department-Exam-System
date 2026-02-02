using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HolisticDepartmentExamSystem.Models;
using HolisticDepartmentExamSystem.Data;

namespace HolisticDepartmentExamSystem.Controllers
{

    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        #region Dashboard & Overview

        public async Task<IActionResult> Dashboard()
        {
            var dashboard = new AdminDashboardViewModel
            {
                TotalUsers = await _context.Users.CountAsync(),
                TotalStudents = await _context.Students.CountAsync(),
                TotalExams = await _context.Exams.CountAsync(),
                ActiveExams = await _context.Exams.CountAsync(e => e.Status == "Published"),
                RecentUsers = await _context.Users
                    .Include(u => u.Role)
                    .OrderByDescending(u => u.UserId)
                    .Take(5)
                    .ToListAsync(),
                RecentExams = await _context.Exams.OrderByDescending(e => e.CreatedAt).Take(5).ToListAsync()
            };

            return View(dashboard);
        }

        #endregion

        #region Staff & User Management

        public async Task<IActionResult> ManageUsers()
        {
            var users = await _context.Users
                .Include(u => u.Role)
                .Where(u => u.Role.RoleName != "Student") // Separate Staff from Students
                .OrderBy(u => u.Role.RoleName)
                .ThenBy(u => u.Username)
                .ToListAsync();

            return View(users);
        }

        [HttpGet]
        public async Task<IActionResult> CreateUser()
        {
            ViewBag.Roles = await _context.Roles.Where(r => r.RoleName != "Student").ToListAsync();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser(User user, string password)
        {
            ModelState.Remove("PasswordHash");
            ModelState.Remove("Role");

            if (_context.Users.Any(u => u.Username == user.Username))
            {
                 ModelState.AddModelError("Username", "Username already exists.");
            }

            if (string.IsNullOrEmpty(password) || password.Length < 6)
            {
                ModelState.AddModelError("Password", "Password must be at least 6 characters long.");
            }

            if (ModelState.IsValid)
            {
                user.PasswordHash = password;
                user.Status = true; 
                user.CreatedAt = DateTime.UtcNow;
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                
                TempData["Success"] = "User created successfully";
                return RedirectToAction("ManageUsers");
            }

            ViewBag.Roles = await _context.Roles.Where(r => r.RoleName != "Student").ToListAsync();
            return View(user);
        }

        [HttpGet]
        public async Task<IActionResult> EditUser(int id)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == id);
            
            if (user == null) return NotFound();
            
            ViewBag.Roles = await _context.Roles.Where(r => r.RoleName != "Student").ToListAsync();
            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> EditUser(User user, string password)
        {
            ModelState.Remove("PasswordHash");
            ModelState.Remove("Role");

            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == user.UserId);

            if (existingUser == null) return NotFound();

            if (ModelState.IsValid)
            {
                existingUser.Username = user.Username;
                existingUser.RoleId = user.RoleId;
                existingUser.Status = user.Status;

                if (!string.IsNullOrEmpty(password))
                {
                    if (password.Length < 6)
                    {
                        ModelState.AddModelError("Password", "Password must be at least 6 characters long.");
                        ViewBag.Roles = await _context.Roles.Where(r => r.RoleName != "Student").ToListAsync();
                        return View(user);
                    }
                    existingUser.PasswordHash = password;
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = "User updated successfully";
                return RedirectToAction("ManageUsers");
            }
            
            ViewBag.Roles = await _context.Roles.Where(r => r.RoleName != "Student").ToListAsync();
            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null && user.Username != "admin")
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("ManageUsers");
        }

        public async Task<IActionResult> ManageCoordinators()
        {
            var coordinators = await _context.Users
                .Include(u => u.Role)
                .Where(u => u.Role.RoleName == "Coordinator")
                .OrderBy(u => u.Username)
                .ToListAsync();

            return View(coordinators);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleUserStatus(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            user.Status = !user.Status;
            await _context.SaveChangesAsync();

            var action = user.Status ? "activated" : "deactivated";
            TempData["Success"] = $"User account {action} successfully";

            var role = await _context.Roles.FindAsync(user.RoleId);
            return RedirectToAction(role?.RoleName == "Student" ? "ManageStudents" : "ManageUsers");
        }

        #endregion


        #region Student Management

        public async Task<IActionResult> ManageStudents(string searchString = "", int page = 1)
        {
            var students = _context.Students
                .Include(s => s.User)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                students = students.Where(s => 
                    s.FullName.Contains(searchString) ||
                    s.Email.Contains(searchString) ||
                    (s.Department != null && s.Department.Contains(searchString)));
            }

            int pageSize = 12;
            var totalStudents = await students.CountAsync();
            var pagedStudents = await students
                .OrderBy(s => s.FullName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentSearch = searchString;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalStudents / pageSize);
            ViewBag.TotalStudents = totalStudents;

            return View(pagedStudents);
        }

        [HttpGet]
        public IActionResult CreateStudent() => View();

        [HttpPost]
        public async Task<IActionResult> CreateStudent(Student student, string username, string password)
        {
            ModelState.Remove("User");
            ModelState.Remove("ExamPasswords");
            ModelState.Remove("ExamAttempts");

            if (_context.Users.Any(u => u.Username == username))
                ModelState.AddModelError("Username", "Username already exists");

            if (string.IsNullOrEmpty(password) || password.Length < 6)
                ModelState.AddModelError("Password", "Password must be at least 6 characters long");

            if (ModelState.IsValid)
            {
                var studentRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Student");
                if (studentRole == null) return View(student);

                var user = new User
                {
                    Username = username,
                    PasswordHash = password,
                    RoleId = studentRole.RoleId,
                    Status = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                student.UserId = user.UserId;
                _context.Students.Add(student);
                await _context.SaveChangesAsync();
                
                TempData["Success"] = "Student created successfully";
                return RedirectToAction("ManageStudents");
            }

            return View(student);
        }

        [HttpGet]
        public async Task<IActionResult> EditStudent(int id)
        {
            var student = await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.StudentId == id);

            if (student == null) return NotFound();
            return View(student);
        }

        [HttpPost]
        public async Task<IActionResult> EditStudent(Student student)
        {
            var existingStudent = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentId == student.StudentId);

            if (existingStudent == null) return NotFound();

            if (ModelState.IsValid)
            {
                existingStudent.FullName = student.FullName;
                existingStudent.Email = student.Email;
                existingStudent.Department = student.Department;
                existingStudent.YearOfStudy = student.YearOfStudy;

                await _context.SaveChangesAsync();
                return RedirectToAction("ManageStudents");
            }

            return View(student);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteStudent(int id)
        {
            var student = await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.StudentId == id);

            if (student != null)
            {
                _context.Students.Remove(student);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("ManageStudents");
        }

        public async Task<IActionResult> StudentDetails(int id)
        {
            var student = await _context.Students
                .Include(s => s.User)
                .Include(s => s.ExamAttempts)
                .ThenInclude(ea => ea.Exam)
                .FirstOrDefaultAsync(s => s.StudentId == id);

            if (student == null) return NotFound();
            return View(student);
        }

        public async Task<IActionResult> ExportStudents()
        {
            var students = await _context.Students.OrderBy(s => s.FullName).ToListAsync();
            var csv = new System.Text.StringBuilder("Full Name,Email,Department,Year of Study\n");

            foreach (var s in students)
                csv.AppendLine($"{s.FullName},{s.Email},{s.Department},{s.YearOfStudy}");

            return File(System.Text.Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"Students_{DateTime.Now:yyyyMMdd}.csv");
        }

        #endregion


        #region Examination & System Monitoring

        public async Task<IActionResult> ViewAllExams()
        {
            var exams = await _context.Exams
                .Include(e => e.Questions)
                .Include(e => e.ExamAttempts)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();

            var coordinatorIds = exams.Select(e => e.CreatedBy).Distinct().ToList();
            var coordinators = await _context.Users
                .Where(u => coordinatorIds.Contains(u.UserId))
                .ToDictionaryAsync(u => u.UserId, u => u.Username);

            ViewBag.Coordinators = coordinators;
            return View(exams);
        }

        [HttpGet]
        public async Task<IActionResult> AssignStudentToExam(int examId)
        {
            var exam = await _context.Exams.FindAsync(examId);
            if (exam == null) return NotFound();

            var assignedStudentIds = await _context.ExamPasswords
                .Where(ep => ep.ExamId == examId)
                .Select(ep => ep.StudentId)
                .ToListAsync();

            ViewBag.AvailableStudents = await _context.Students
                .Where(s => !assignedStudentIds.Contains(s.StudentId))
                .OrderBy(s => s.FullName)
                .ToListAsync();

            ViewBag.AssignedStudents = await _context.ExamPasswords
                .Include(ep => ep.Student)
                .Where(ep => ep.ExamId == examId)
                .ToListAsync();
            
            return View(exam);
        }

        [HttpPost]
        public async Task<IActionResult> AssignStudentToExam(int examId, int studentId)
        {
            var existing = await _context.ExamPasswords
                .AnyAsync(ep => ep.ExamId == examId && ep.StudentId == studentId);

            if (!existing)
            {
                _context.ExamPasswords.Add(new ExamPassword
                {
                    ExamId = examId,
                    StudentId = studentId,
                    PasswordHash = "",
                    IsUsed = false,
                    ExpiresAt = DateTime.UtcNow.AddDays(30)
                });
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("AssignStudentToExam", new { examId });
        }

        public async Task<IActionResult> ViewExamAssignments(int examId)
        {
            var exam = await _context.Exams
                .Include(e => e.ExamPasswords)
                    .ThenInclude(ep => ep.Student)
                .FirstOrDefaultAsync(e => e.ExamId == examId);

            if (exam == null) return NotFound();
            return View(exam);
        }

        [HttpPost]
        public async Task<IActionResult> RemoveStudentFromExam(int examId, int studentId)
        {
            var assignment = await _context.ExamPasswords
                .FirstOrDefaultAsync(ep => ep.ExamId == examId && ep.StudentId == studentId);

            if (assignment != null)
            {
                _context.ExamPasswords.Remove(assignment);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("ViewExamAssignments", new { examId });
        }

        [HttpPost]
        public async Task<IActionResult> PublishResults(int examId)
        {
            var exam = await _context.Exams.FindAsync(examId);
            if (exam != null)
            {
                exam.ResultsPublished = true;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("ViewAllExams");
        }

        [HttpPost]
        public async Task<IActionResult> UnpublishResults(int examId)
        {
            var exam = await _context.Exams.FindAsync(examId);
            if (exam != null)
            {
                exam.ResultsPublished = false;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("ViewAllExams");
        }

        public async Task<IActionResult> ExamMonitor()
        {
            var activeExams = await _context.Exams
                .Include(e => e.ExamAttempts)
                .Where(e => e.Status == "Published")
                .ToListAsync();

            return View(activeExams);
        }

        public async Task<IActionResult> LoggedInStudents()
        {
            var fiveMinutesAgo = DateTime.UtcNow.AddMinutes(-5);
            var online = await _context.Students
                .Include(s => s.User)
                .Where(s => s.User.LastActivity > fiveMinutesAgo)
                .ToListAsync();

            ViewBag.TotalLoggedIn = online.Count;
            ViewBag.TotalStudents = await _context.Students.CountAsync();
            return View(online);
        }

        #endregion

        #region System Logs & Reports

        public async Task<IActionResult> ActivityLogs(int page = 1, string searchString = "", DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.ActivityLogs
                .Include(l => l.User)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(l => 
                    l.Action.Contains(searchString) || 
                    l.User.Username.Contains(searchString) ||
                    l.IpAddress.Contains(searchString));
            }

            if (startDate.HasValue)
            {
                query = query.Where(l => l.Timestamp >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(l => l.Timestamp <= endDate.Value.AddDays(1));
            }

            // Pagination
            int pageSize = 50;
            var totalLogs = await query.CountAsync();
            var logs = await query
                .OrderByDescending(l => l.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalLogs / pageSize);
            ViewBag.TotalLogs = totalLogs;
            ViewBag.SearchString = searchString;
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;

            return View(logs);
        }

        [HttpPost]
        public async Task<IActionResult> ClearActivityLogs(DateTime? beforeDate = null)
        {
            try
            {
                if (beforeDate.HasValue)
                {
                    // Clear logs before specific date
                    var logsToDelete = await _context.ActivityLogs
                        .Where(l => l.Timestamp < beforeDate.Value)
                        .ToListAsync();
                    
                    _context.ActivityLogs.RemoveRange(logsToDelete);
                    await _context.SaveChangesAsync();
                    
                    TempData["Success"] = $"Successfully cleared {logsToDelete.Count} activity logs before {beforeDate.Value:yyyy-MM-dd}";
                }
                else
                {
                    // Clear all logs
                    var allLogs = await _context.ActivityLogs.ToListAsync();
                    _context.ActivityLogs.RemoveRange(allLogs);
                    await _context.SaveChangesAsync();
                    
                    TempData["Success"] = $"Successfully cleared all {allLogs.Count} activity logs";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to clear activity logs: {ex.Message}";
            }

            return RedirectToAction("ActivityLogs");
        }

        [HttpPost]
        public async Task<IActionResult> ArchiveActivityLogs(DateTime beforeDate)
        {
            try
            {
                var logsToArchive = await _context.ActivityLogs
                    .Where(l => l.Timestamp < beforeDate)
                    .ToListAsync();

                if (logsToArchive.Any())
                {
                    // Export to CSV before deletion
                    var csv = new System.Text.StringBuilder();
                    csv.AppendLine("LogId,UserId,Username,Action,Timestamp,IpAddress,DeviceInfo");
                    
                    foreach (var log in logsToArchive)
                    {
                        var user = await _context.Users.FindAsync(log.UserId);
                        csv.AppendLine($"{log.LogId},{log.UserId},{user?.Username},{log.Action},{log.Timestamp:yyyy-MM-dd HH:mm:ss},{log.IpAddress},{log.DeviceInfo}");
                    }

                    // Save archive file
                    var archivePath = Path.Combine("Archives", $"ActivityLogs_{beforeDate:yyyyMMdd}.csv");
                    Directory.CreateDirectory("Archives");
                    await System.IO.File.WriteAllTextAsync(archivePath, csv.ToString());

                    // Delete archived logs
                    _context.ActivityLogs.RemoveRange(logsToArchive);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = $"Successfully archived and cleared {logsToArchive.Count} logs. Archive saved to {archivePath}";
                }
                else
                {
                    TempData["Info"] = "No logs found to archive for the specified date range";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to archive logs: {ex.Message}";
            }

            return RedirectToAction("ActivityLogs");
        }

        public async Task<IActionResult> SystemReports()
        {
            var model = new SystemReportsViewModel
            {
                TotalStudents = await _context.Students.CountAsync(),
                TotalExams = await _context.Exams.CountAsync(),
                TotalAttempts = await _context.ExamAttempts.CountAsync(),
                CompletedAttempts = await _context.ExamAttempts.CountAsync(ea => ea.Status == "Completed"),
                AverageScore = await _context.Results.AnyAsync() ? await _context.Results.AverageAsync(r => r.TotalScore) : 0,
                PassRate = await _context.Results.AnyAsync() ? (double)await _context.Results.CountAsync(r => r.Grade != "F") / await _context.Results.CountAsync() * 100 : 0
            };

            return View(model);
        }

        public async Task<IActionResult> SystemHealth()
        {
            var model = new SystemHealthViewModel
            {
                TotalUsers = await _context.Users.CountAsync(),
                ActiveUsers = await _context.Users.CountAsync(u => u.Status),
                TotalExams = await _context.Exams.CountAsync(),
                ActiveExams = await _context.Exams.CountAsync(e => e.Status == "Published")
            };
            return View(model);
        }

        public IActionResult SystemSettings() => View();

        #endregion

    }

    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalStudents { get; set; }
        public int TotalExams { get; set; }
        public int ActiveExams { get; set; }
        public List<User> RecentUsers { get; set; } = new List<User>();
        public List<Exam> RecentExams { get; set; } = new List<Exam>();
    }

    public class SecurityMonitorViewModel
    {
        public List<ActivityLog> RecentLogins { get; set; } = new List<ActivityLog>();
        public List<ActivityLog> SuspiciousActivity { get; set; } = new List<ActivityLog>();
    }

    public class SystemHealthViewModel
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int TotalExams { get; set; }
        public int ActiveExams { get; set; }
        public int TotalAttempts { get; set; }
        public int RecentActivity { get; set; }
        public string DatabaseSize { get; set; }
        public string SystemUptime { get; set; }
    }

    public class SystemReportsViewModel
    {
        public int TotalStudents { get; set; }
        public int TotalExams { get; set; }
        public int TotalAttempts { get; set; }
        public int CompletedAttempts { get; set; }
        public double AverageScore { get; set; }
        public double PassRate { get; set; }
        
        // Grade distribution
        public int GradeA { get; set; }
        public int GradeB { get; set; }
        public int GradeC { get; set; }
        public int GradeD { get; set; }
        public int GradeF { get; set; }

        public List<Exam> RecentExams { get; set; } = new List<Exam>();
        public List<Result> TopStudents { get; set; } = new List<Result>();
    }
}

