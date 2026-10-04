using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartAttendanceSystem.Properties.Model;
using SmartAttendanceSystem.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SmartAttendanceSystem.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AppDbContext _context;

        public HomeController(UserManager<ApplicationUser> userManager, AppDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var roles = await _userManager.GetRolesAsync(user);

            if (roles.Contains("Teacher"))
                return RedirectToAction("Dashboard", "Teachers");
            if (roles.Contains("Student"))
                return RedirectToAction("Dashboard", "Student");

            // Admin Dashboard Data
            var today = DateTime.UtcNow.Date;
            var totalStudents = await _context.Students.CountAsync();
            var totalTeachers = await _context.Teachers.CountAsync();
            var totalClasses = await _context.Classes.CountAsync();
            var totalFaculties = await _context.Faculties.CountAsync();

            var faceEnrolled = await _context.Students.CountAsync(s => !string.IsNullOrEmpty(s.FaceEmbedding));
            var todayPresent = await _context.Attendances
                .CountAsync(a => a.AttendanceDate.Date == today && a.Status == "Present");

            var todayRate = totalStudents > 0 ? Math.Round((double)todayPresent / totalStudents * 100, 1) : 0;

            var activeSessions = await _context.AttendanceSessions
                .Include(s => s.Class)
                .Where(s => s.IsActive && s.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            var recentRecords = await _context.Attendances
                .Include(a => a.Student)
                    .ThenInclude(s => s.SchoolClass)
                .OrderByDescending(a => a.AttendanceDate)
                .Take(8)
                .Select(a => new RecentAttendanceItem
                {
                    AttendanceId = a.AttendanceID,
                    StudentId = a.Student != null ? a.Student.StudentID : 0,
                    StudentName = a.Student != null ? a.Student.Name : "Unknown",
                    EnrollmentNumber = a.Student != null ? a.Student.EnrollmentNumber : "",
                    ClassName = a.Student != null && a.Student.SchoolClass != null ? a.Student.SchoolClass.Name : "N/A",
                    Subject = a.Subject ?? "Class Session",
                    Method = a.Method,
                    Status = a.Status,
                    AttendanceDate = a.AttendanceDate,
                    FaceImagePath = a.Student != null ? a.Student.FaceImagePath : null
                })
                .ToListAsync();

            var model = new AdminDashboardViewModel
            {
                TotalStudents = totalStudents,
                TotalTeachers = totalTeachers,
                TotalClasses = totalClasses,
                TotalFaculties = totalFaculties,
                TodayAttendanceCount = todayPresent,
                FaceEnrolledStudentsCount = faceEnrolled,
                TodayAttendanceRate = todayRate,
                ActiveSessionsCount = activeSessions.Count,
                ActiveSessions = activeSessions,
                RecentAttendances = recentRecords
            };

            return View(model);
        }

        public async Task<IActionResult> Recognition()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var roles = await _userManager.GetRolesAsync(user);

            if (roles.Contains("Teacher"))
                return RedirectToAction("Index", "Attendance");
            if (roles.Contains("Student"))
                return RedirectToAction("FaceCheckIn", "Student");

            return RedirectToAction("Index");
        }
    }
}
