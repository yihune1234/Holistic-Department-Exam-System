using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using HolisticDepartmentExamSystem.Models;

namespace HolisticDepartmentExamSystem.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        if (User.Identity.IsAuthenticated)
        {
            if (User.IsInRole("Admin")) return RedirectToAction("Dashboard", "Admin");
            if (User.IsInRole("Coordinator")) return RedirectToAction("Dashboard", "Coordinator");
            if (User.IsInRole("Student")) return RedirectToAction("Dashboard", "Student");
        }
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
