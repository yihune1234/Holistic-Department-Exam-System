using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using HolisticDepartmentExamSystem.Models;
using HolisticDepartmentExamSystem.Data;
using Microsoft.EntityFrameworkCore;

namespace HolisticDepartmentExamSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity.IsAuthenticated)
            {
                if (User.IsInRole("Admin")) return RedirectToAction("Dashboard", "Admin");
                if (User.IsInRole("Coordinator")) return RedirectToAction("Dashboard", "Coordinator");
                if (User.IsInRole("Student")) return RedirectToAction("Dashboard", "Student");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Username == username);
            
            if (user != null)
            {
                // Verify password directly
                bool isPasswordValid = user.PasswordHash == password;
                
                if (!isPasswordValid)
                {
                    ViewBag.Error = "Invalid username or password";
                    return View();
                }

                if (!user.Status)
                {
                    ViewBag.Error = "Your account is not activated. Please contact the administrator.";
                    return View();
                }

                // Update last activity timestamp
                user.LastActivity = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                var userRole = user.Role.RoleName;

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    new Claim(ClaimTypes.Role, userRole)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties {};

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                return userRole switch
                {
                    "Student" => RedirectToAction("Dashboard", "Student"),
                    "Coordinator" => RedirectToAction("Dashboard", "Coordinator"),
                    "Admin" => RedirectToAction("Dashboard", "Admin"),
                    _ => RedirectToAction("Login", "Account")
                };
            }

            ViewBag.Error = "Invalid username or password";
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
