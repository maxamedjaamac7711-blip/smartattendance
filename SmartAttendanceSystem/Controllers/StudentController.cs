using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartAttendanceSystem;
using SmartAttendanceSystem.Properties.Model;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using SmartAttendanceSystem.Models;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;
using System.IO;
using System.Collections.Generic;

namespace SmartAttendanceSystem.Controllers
{
    [Authorize]
    public class StudentController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public StudentController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var students = await _context.Students
                .Include(s => s.Faculty)
                .Include(s => s.SchoolClass)
                .OrderBy(s => s.Name)
                .ToListAsync();

            return View(students);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var faculties = await _context.Faculties.OrderBy(f => f.Name).ToListAsync();
            var classes = await _context.Classes.OrderBy(c => c.Name).ToListAsync();
            ViewBag.Faculties = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(faculties, "Id", "Name");
            ViewBag.Classes = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(classes, "Id", "Name");
            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StudentCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var faculties = await _context.Faculties.OrderBy(f => f.Name).ToListAsync();
                var classes = await _context.Classes.OrderBy(c => c.Name).ToListAsync();
                ViewBag.Faculties = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(faculties, "Id", "Name");
                ViewBag.Classes = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(classes, "Id", "Name");
                return View(model);
            }

            var existingUser = await _userManager.FindByNameAsync(model.Username);
            if (existingUser != null)
            {
                ModelState.AddModelError("Username", "Username already exists.");
                var faculties = await _context.Faculties.OrderBy(f => f.Name).ToListAsync();
                var classes = await _context.Classes.OrderBy(c => c.Name).ToListAsync();
                ViewBag.Faculties = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(faculties, "Id", "Name");
                ViewBag.Classes = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(classes, "Id", "Name");
                return View(model);
            }

            var user = new ApplicationUser { UserName = model.Username, Email = $"{model.Username}@student.edu", EmailConfirmed = true };
            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                foreach (var err in result.Errors)
                    ModelState.AddModelError(string.Empty, err.Description);

                var faculties = await _context.Faculties.OrderBy(f => f.Name).ToListAsync();
                var classes = await _context.Classes.OrderBy(c => c.Name).ToListAsync();
                ViewBag.Faculties = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(faculties, "Id", "Name");
                ViewBag.Classes = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(classes, "Id", "Name");
                return View(model);
            }

            // assign Student role
            await _userManager.AddToRoleAsync(user, "Student");

            var student = new Student
            {
                Name = model.Name,
                EnrollmentNumber = model.EnrollmentNumber,
                UserId = user.Id,
                FacultyId = model.FacultyId,
                ClassId = model.ClassId,
                SchoolClassId = model.ClassId
            };

            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            TempData["Message"] = $"Student '{student.Name}' created successfully. Now proceed with face enrollment.";
            return RedirectToAction(nameof(FaceEnrollment), new { id = student.StudentID });
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var student = await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.StudentID == id);

            if (student == null)
                return NotFound();

            var faculties = await _context.Faculties.OrderBy(f => f.Name).ToListAsync();
            var classes = await _context.Classes.OrderBy(c => c.Name).ToListAsync();
            ViewBag.Faculties = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(faculties, "Id", "Name", student.FacultyId);
            ViewBag.Classes = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(classes, "Id", "Name", student.ClassId);

            var model = new StudentEditViewModel
            {
                StudentID = student.StudentID,
                Name = student.Name,
                EnrollmentNumber = student.EnrollmentNumber,
                FacultyId = student.FacultyId,
                ClassId = student.ClassId,
                Username = student.User?.UserName ?? ""
            };

            return View(model);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(StudentEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var faculties = await _context.Faculties.OrderBy(f => f.Name).ToListAsync();
                var classes = await _context.Classes.OrderBy(c => c.Name).ToListAsync();
                ViewBag.Faculties = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(faculties, "Id", "Name", model.FacultyId);
                ViewBag.Classes = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(classes, "Id", "Name", model.ClassId);
                return View(model);
            }

            var student = await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.StudentID == model.StudentID);

            if (student == null)
                return NotFound();

            student.Name = model.Name;
            student.EnrollmentNumber = model.EnrollmentNumber;
            student.FacultyId = model.FacultyId;
            student.ClassId = model.ClassId;
            student.SchoolClassId = model.ClassId;

            // Password update if provided
            if (!string.IsNullOrWhiteSpace(model.NewPassword) && student.User != null)
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(student.User);
                await _userManager.ResetPasswordAsync(student.User, token, model.NewPassword);
            }

            await _context.SaveChangesAsync();
            TempData["Message"] = $"Student '{student.Name}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null)
                return NotFound();

            var studentName = student.Name;
            var userId = student.UserId;

            // Clean up face image if exists
            if (!string.IsNullOrEmpty(student.FaceImagePath))
            {
                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", student.FaceImagePath.TrimStart('/'));
                if (System.IO.File.Exists(fullPath))
                {
                    try { System.IO.File.Delete(fullPath); } catch { }
                }
            }

            // Remove attendances of student
            var attendances = await _context.Attendances.Where(a => a.StudentID == id).ToListAsync();
            _context.Attendances.RemoveRange(attendances);

            _context.Students.Remove(student);
            await _context.SaveChangesAsync();

            // Remove associated Identity user
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                await _userManager.DeleteAsync(user);
            }

            TempData["Message"] = $"Student '{studentName}' and associated records were deleted.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin,Student")]
        [HttpGet]
        public async Task<IActionResult> FaceEnrollment(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null)
                return NotFound();

            var userId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole("Admin");
            var currentStudent = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
            
            if (!isAdmin && (currentStudent == null || currentStudent.StudentID != id))
                return Forbid();

            ViewBag.StudentId = id;
            ViewBag.StudentName = student.Name;
            ViewBag.CurrentFaceImage = student.FaceImagePath;
            ViewBag.IsEnrolled = !string.IsNullOrEmpty(student.FaceEmbedding);
            return View();
        }

        [Authorize(Roles = "Admin,Student")]
        [HttpPost]
        public async Task<IActionResult> SaveFaceEmbedding(int studentId, [FromBody] JsonElement data)
        {
            var student = await _context.Students.FindAsync(studentId);
            if (student == null)
                return NotFound();

            var userId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole("Admin");
            var currentStudent = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
            
            if (!isAdmin && (currentStudent == null || currentStudent.StudentID != studentId))
                return Forbid();

            try
            {
                var embedding = data.GetProperty("embedding").GetRawText();
                var imageData = data.GetProperty("imageData").GetString();

                student.FaceEmbedding = embedding;
                
                if (!string.IsNullOrEmpty(imageData))
                {
                    var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "faces");
                    Directory.CreateDirectory(uploadsPath);
                    
                    var fileName = $"student_{studentId}_{DateTime.UtcNow:yyyyMMddHHmmss}.jpg";
                    var filePath = Path.Combine(uploadsPath, fileName);
                    
                    var base64Data = imageData.Split(',')[1];
                    var imageBytes = Convert.FromBase64String(base64Data);
                    await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);
                    
                    student.FaceImagePath = $"/uploads/faces/{fileName}";
                }

                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Face biometrics enrolled successfully!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [Authorize(Roles = "Student,Admin")]
        public async Task<IActionResult> Dashboard()
        {
            var userId = _userManager.GetUserId(User);
            var student = await _context.Students
                .Include(s => s.Faculty)
                .Include(s => s.SchoolClass)
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (student == null)
                return View("DashboardEmpty");

            var today = DateTime.UtcNow.Date;
            var todayRecord = await _context.Attendances
                .FirstOrDefaultAsync(a => a.StudentID == student.StudentID && a.AttendanceDate.Date == today && a.Status == "Present");

            var totalAttended = await _context.Attendances
                .CountAsync(a => a.StudentID == student.StudentID && a.Status == "Present");

            var totalClassSessions = await _context.AttendanceSessions
                .CountAsync(s => s.ClassId == student.ClassId);

            var rate = totalClassSessions > 0 ? Math.Round((double)totalAttended / totalClassSessions * 100, 1) : 100;

            var activeSessions = await _context.AttendanceSessions
                .Where(s => s.ClassId == student.ClassId && s.IsActive && s.ExpiresAt > DateTime.UtcNow)
                .Include(s => s.Class)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            var recentAttendances = await _context.Attendances
                .Where(a => a.StudentID == student.StudentID)
                .OrderByDescending(a => a.AttendanceDate)
                .Take(5)
                .ToListAsync();

            var model = new StudentDashboardViewModel
            {
                Student = student,
                IsPresentToday = todayRecord != null,
                TodayCheckInTime = todayRecord?.AttendanceDate,
                TodayCheckInMethod = todayRecord?.Method,
                TotalAttendedSessions = totalAttended,
                TotalClassSessions = totalClassSessions,
                OverallAttendanceRate = rate,
                ActiveSessionsForClass = activeSessions,
                RecentAttendances = recentAttendances
            };

            return View(model);
        }

        [Authorize(Roles = "Student,Admin")]
        public async Task<IActionResult> MyProfile()
        {
            var userId = _userManager.GetUserId(User);
            var student = await _context.Students
                .Include(s => s.Faculty)
                .Include(s => s.SchoolClass)
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (student == null)
                return View("DashboardEmpty");

            return View(student);
        }

        [Authorize(Roles = "Student,Admin")]
        public async Task<IActionResult> MyClass()
        {
            var userId = _userManager.GetUserId(User);
            var student = await _context.Students
                .Include(s => s.SchoolClass)
                .Include(s => s.Faculty)
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (student == null)
                return View("DashboardEmpty");

            var classmates = await _context.Students
                .Where(s => s.ClassId == student.ClassId)
                .OrderBy(s => s.EnrollmentNumber)
                .ToListAsync();

            ViewBag.Classmates = classmates;
            return View(student);
        }

        [Authorize(Roles = "Student,Admin")]
        public async Task<IActionResult> MyAttendance()
        {
            var userId = _userManager.GetUserId(User);
            var student = await _context.Students
                .Include(s => s.Faculty)
                .Include(s => s.SchoolClass)
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (student == null)
                return View("DashboardEmpty");

            var attendances = await _context.Attendances
                .Where(a => a.StudentID == student.StudentID)
                .Include(a => a.Session)
                    .ThenInclude(s => s.Class)
                .OrderByDescending(a => a.AttendanceDate)
                .ToListAsync();

            var totalClassSessions = await _context.AttendanceSessions
                .CountAsync(s => s.ClassId == student.ClassId);

            var totalAttended = attendances.Count(a => a.Status == "Present");
            var qrCount = attendances.Count(a => a.Method == "QR");
            var faceCount = attendances.Count(a => a.Method == "Face");
            var manualCount = attendances.Count(a => a.Method == "Manual");

            var rate = totalClassSessions > 0 ? Math.Round((double)totalAttended / totalClassSessions * 100, 1) : 100;

            var records = attendances.Select(a => new AttendanceRecordItem
            {
                AttendanceId = a.AttendanceID,
                Date = a.AttendanceDate,
                Subject = a.Subject ?? a.Session?.Subject ?? "Class Lecture",
                Method = a.Method,
                Status = a.Status,
                ClassName = a.Session?.Class?.Name ?? student.SchoolClass?.Name
            }).ToList();

            var model = new StudentAttendanceHistoryViewModel
            {
                Student = student,
                TotalAttended = totalAttended,
                TotalClassSessions = totalClassSessions > 0 ? totalClassSessions : totalAttended,
                QrCheckIns = qrCount,
                FaceCheckIns = faceCount,
                ManualCheckIns = manualCount,
                AttendancePercentage = rate,
                Records = records
            };

            return View(model);
        }

        [Authorize(Roles = "Student,Admin")]
        public IActionResult ScanQR()
        {
            return View();
        }

        [Authorize(Roles = "Student,Admin")]
        public async Task<IActionResult> FaceCheckIn()
        {
            var userId = _userManager.GetUserId(User);
            var student = await _context.Students
                .Include(s => s.SchoolClass)
                .FirstOrDefaultAsync(s => s.UserId == userId);
            
            if (student == null)
                return View("DashboardEmpty");

            if (string.IsNullOrEmpty(student.FaceEmbedding))
            {
                ViewBag.Message = "You need to enroll your face first before using biometric face check-in.";
                ViewBag.StudentId = student.StudentID;
                return View("FaceEnrollmentPrompt");
            }

            var activeSessions = await _context.AttendanceSessions
                .Where(s => s.ClassId == student.ClassId 
                    && s.IsActive 
                    && s.ExpiresAt > DateTime.UtcNow)
                .Include(s => s.Class)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            ViewBag.Sessions = activeSessions;
            ViewBag.Student = student;
            return View();
        }
    }
}
