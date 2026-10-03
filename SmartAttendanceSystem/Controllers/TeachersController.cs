using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SmartAttendanceSystem.Properties.Model;
using System;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using SmartAttendanceSystem.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace SmartAttendanceSystem.Controllers
{
    [Authorize]
    public class TeachersController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TeachersController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var items = await _context.Teachers
                .Include(t => t.Faculty)
                .Include(t => t.SchoolClass)
                .Include(t => t.User)
                .OrderBy(t => t.Name)
                .ToListAsync();
            return View(items);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var faculties = await _context.Faculties.OrderBy(f => f.Name).ToListAsync();
            var classes = await _context.Classes.OrderBy(c => c.Name).ToListAsync();
            ViewBag.Faculties = new SelectList(faculties, "Id", "Name");
            ViewBag.Classes = new SelectList(classes, "Id", "Name");
            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TeacherCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var faculties = await _context.Faculties.OrderBy(f => f.Name).ToListAsync();
                var classes = await _context.Classes.OrderBy(c => c.Name).ToListAsync();
                ViewBag.Faculties = new SelectList(faculties, "Id", "Name");
                ViewBag.Classes = new SelectList(classes, "Id", "Name");
                return View(model);
            }

            var existingUser = await _userManager.FindByNameAsync(model.Username);
            if (existingUser != null)
            {
                ModelState.AddModelError("Username", "Username already exists.");
                var faculties = await _context.Faculties.OrderBy(f => f.Name).ToListAsync();
                var classes = await _context.Classes.OrderBy(c => c.Name).ToListAsync();
                ViewBag.Faculties = new SelectList(faculties, "Id", "Name");
                ViewBag.Classes = new SelectList(classes, "Id", "Name");
                return View(model);
            }

            var user = new ApplicationUser { UserName = model.Username, Email = model.Email, EmailConfirmed = true };
            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                foreach (var err in result.Errors)
                    ModelState.AddModelError(string.Empty, err.Description);

                var faculties = await _context.Faculties.OrderBy(f => f.Name).ToListAsync();
                var classes = await _context.Classes.OrderBy(c => c.Name).ToListAsync();
                ViewBag.Faculties = new SelectList(faculties, "Id", "Name");
                ViewBag.Classes = new SelectList(classes, "Id", "Name");
                return View(model);
            }

            // assign Teacher role
            await _userManager.AddToRoleAsync(user, "Teacher");

            var teacher = new Teacher
            {
                Name = model.Name,
                UserId = user.Id,
                FacultyId = model.FacultyId,
                ClassId = model.ClassId,
                SchoolClassId = model.ClassId
            };

            _context.Teachers.Add(teacher);
            await _context.SaveChangesAsync();

            TempData["Message"] = $"Teacher '{teacher.Name}' created successfully.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var teacher = await _context.Teachers
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (teacher == null)
                return NotFound();

            var faculties = await _context.Faculties.OrderBy(f => f.Name).ToListAsync();
            var classes = await _context.Classes.OrderBy(c => c.Name).ToListAsync();
            ViewBag.Faculties = new SelectList(faculties, "Id", "Name", teacher.FacultyId);
            ViewBag.Classes = new SelectList(classes, "Id", "Name", teacher.ClassId);

            var model = new TeacherEditViewModel
            {
                Id = teacher.Id,
                Name = teacher.Name,
                Email = teacher.User?.Email ?? "",
                FacultyId = teacher.FacultyId,
                ClassId = teacher.ClassId,
                Username = teacher.User?.UserName ?? ""
            };

            return View(model);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(TeacherEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var faculties = await _context.Faculties.OrderBy(f => f.Name).ToListAsync();
                var classes = await _context.Classes.OrderBy(c => c.Name).ToListAsync();
                ViewBag.Faculties = new SelectList(faculties, "Id", "Name", model.FacultyId);
                ViewBag.Classes = new SelectList(classes, "Id", "Name", model.ClassId);
                return View(model);
            }

            var teacher = await _context.Teachers
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Id == model.Id);

            if (teacher == null)
                return NotFound();

            teacher.Name = model.Name;
            teacher.FacultyId = model.FacultyId;
            teacher.ClassId = model.ClassId;
            teacher.SchoolClassId = model.ClassId;

            if (teacher.User != null)
            {
                teacher.User.Email = model.Email;
                if (!string.IsNullOrWhiteSpace(model.NewPassword))
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(teacher.User);
                    await _userManager.ResetPasswordAsync(teacher.User, token, model.NewPassword);
                }
            }

            await _context.SaveChangesAsync();
            TempData["Message"] = $"Teacher '{teacher.Name}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var teacher = await _context.Teachers.FindAsync(id);
            if (teacher == null)
                return NotFound();

            var teacherName = teacher.Name;
            var userId = teacher.UserId;

            _context.Teachers.Remove(teacher);
            await _context.SaveChangesAsync();

            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                await _userManager.DeleteAsync(user);
            }

            TempData["Message"] = $"Teacher '{teacherName}' was deleted.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> Dashboard()
        {
            var userId = _userManager.GetUserId(User);
            var teacher = await _context.Teachers
                .Include(t => t.Faculty)
                .Include(t => t.SchoolClass)
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.UserId == userId);

            if (teacher == null)
            {
                return View("DashboardEmpty");
            }

            var today = DateTime.UtcNow.Date;
            var totalStudents = await _context.Students.CountAsync(s => s.ClassId == teacher.ClassId);
            var faceEnrolled = await _context.Students.CountAsync(s => s.ClassId == teacher.ClassId && !string.IsNullOrEmpty(s.FaceEmbedding));

            var todayPresent = await _context.Attendances
                .Include(a => a.Student)
                .CountAsync(a => a.Student != null && a.Student.ClassId == teacher.ClassId && a.AttendanceDate.Date == today && a.Status == "Present");

            var rate = totalStudents > 0 ? Math.Round((double)todayPresent / totalStudents * 100, 1) : 0;

            var activeSessions = await _context.AttendanceSessions
                .Where(s => s.ClassId == teacher.ClassId && s.IsActive && s.ExpiresAt > DateTime.UtcNow)
                .Include(s => s.Class)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            var recentSessions = await _context.AttendanceSessions
                .Where(s => s.ClassId == teacher.ClassId)
                .Include(s => s.Class)
                .OrderByDescending(s => s.CreatedAt)
                .Take(6)
                .ToListAsync();

            var model = new TeacherDashboardViewModel
            {
                Teacher = teacher,
                TotalStudentsInClass = totalStudents,
                TodayPresentCount = todayPresent,
                TodayAttendanceRate = rate,
                FaceEnrolledCount = faceEnrolled,
                ActiveSessions = activeSessions,
                RecentSessions = recentSessions
            };

            return View(model);
        }

        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> MyStudents()
        {
            var userId = _userManager.GetUserId(User);
            var teacher = await _context.Teachers
                .Include(t => t.SchoolClass)
                .FirstOrDefaultAsync(t => t.UserId == userId);
            
            if (teacher == null)
            {
                return View("DashboardEmpty");
            }

            var students = await _context.Students
                .Include(s => s.Faculty)
                .Include(s => s.SchoolClass)
                .Where(s => s.ClassId == teacher.ClassId)
                .OrderBy(s => s.EnrollmentNumber)
                .ToListAsync();

            ViewBag.ClassName = teacher.SchoolClass?.Name ?? "My Class";
            return View(students);
        }
    }
}
