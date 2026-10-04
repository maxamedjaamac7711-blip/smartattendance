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
using SmartAttendanceSystem.Services;

namespace SmartAttendanceSystem.Controllers
{
    [Authorize]
    public class StudentController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IFaceImageStorage _faceImageStorage;
        private readonly ILogger<StudentController> _logger;

        public StudentController(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            IFaceImageStorage faceImageStorage,
            ILogger<StudentController> logger)
        {
            _context = context;
            _userManager = userManager;
            _faceImageStorage = faceImageStorage;
            _logger = logger;
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

            var imageFileName = FaceImageFileName.FromStoredPath(student.FaceImagePath);
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

            var imageCleanupFailed = false;
            if (imageFileName != null)
            {
                try
                {
                    await _faceImageStorage.DeleteAsync(imageFileName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Student {StudentId} was deleted, but its face image could not be removed.", id);
                    imageCleanupFailed = true;
                }
            }

            TempData["Message"] = imageCleanupFailed
                ? $"Student '{studentName}' and associated records were deleted, but face image cleanup failed."
                : $"Student '{studentName}' and associated records were deleted.";
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
            ViewBag.CurrentFaceImage = string.IsNullOrEmpty(student.FaceImagePath)
                ? null
                : Url.Action(nameof(FaceImage), new { id = student.StudentID });
            ViewBag.IsEnrolled = !string.IsNullOrEmpty(student.FaceEmbedding);
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> FaceImage(int id, CancellationToken cancellationToken)
        {
            var student = await _context.Students
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.StudentID == id, cancellationToken);
            if (student == null)
                return NotFound();

            if (!User.IsInRole("Admin"))
            {
                var userId = _userManager.GetUserId(User);
                if (User.IsInRole("Teacher"))
                {
                    var teacher = await _context.Teachers
                        .AsNoTracking()
                        .FirstOrDefaultAsync(t => t.UserId == userId, cancellationToken);
                    if (teacher == null || teacher.ClassId != student.ClassId)
                        return Forbid();
                }
                else if (User.IsInRole("Student"))
                {
                    var currentStudent = await _context.Students
                        .AsNoTracking()
                        .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);
                    if (currentStudent == null || currentStudent.ClassId != student.ClassId)
                        return Forbid();
                }
                else
                {
                    return Forbid();
                }
            }

            var fileName = FaceImageFileName.FromStoredPath(student.FaceImagePath);
            if (fileName == null)
                return NotFound();

            var image = await _faceImageStorage.OpenReadAsync(fileName, cancellationToken);
            if (image == null)
                return NotFound();

            Response.Headers.CacheControl = "private, no-store";
            return File(image, "image/jpeg");
        }

        [Authorize(Roles = "Admin,Student")]
        [HttpPost]
        [RequestSizeLimit(7_500_000)]
        public async Task<IActionResult> SaveFaceEmbedding(
            int studentId,
            [FromBody] JsonElement data,
            CancellationToken cancellationToken)
        {
            var student = await _context.Students.FindAsync(studentId);
            if (student == null)
                return NotFound();

            var userId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole("Admin");
            var currentStudent = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
            
            if (!isAdmin && (currentStudent == null || currentStudent.StudentID != studentId))
                return Forbid();

            if (data.ValueKind != JsonValueKind.Object ||
                !data.TryGetProperty("embedding", out var embeddingElement) ||
                embeddingElement.ValueKind != JsonValueKind.Array ||
                embeddingElement.GetArrayLength() == 0)
            {
                return BadRequest(new { success = false, message = "A valid face embedding is required." });
            }

            byte[]? imageBytes = null;
            if (data.TryGetProperty("imageData", out var imageElement) &&
                imageElement.ValueKind == JsonValueKind.String)
            {
                var imageData = imageElement.GetString();
                if (!string.IsNullOrEmpty(imageData))
                {
                    const string dataUriPrefix = "data:image/jpeg;base64,";
                    if (!imageData.StartsWith(dataUriPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        return BadRequest(new { success = false, message = "The face image must be a JPEG data URL." });
                    }

                    try
                    {
                        imageBytes = Convert.FromBase64String(imageData[dataUriPrefix.Length..]);
                    }
                    catch (FormatException)
                    {
                        return BadRequest(new { success = false, message = "The face image data is invalid." });
                    }

                    if (imageBytes.Length == 0 ||
                        imageBytes.Length > 5 * 1024 * 1024 ||
                        imageBytes.Length < 3 ||
                        imageBytes[0] != 0xFF ||
                        imageBytes[1] != 0xD8 ||
                        imageBytes[2] != 0xFF)
                    {
                        return BadRequest(new { success = false, message = "The JPEG face image is invalid or exceeds 5 MB." });
                    }
                }
            }

            var previousImageFileName = FaceImageFileName.FromStoredPath(student.FaceImagePath);
            var newImageFileName = imageBytes == null
                ? null
                : $"student_{studentId}_{Guid.NewGuid():N}.jpg";

            try
            {
                if (imageBytes != null && newImageFileName != null)
                {
                    await _faceImageStorage.SaveAsync(newImageFileName, imageBytes, cancellationToken);
                    student.FaceImagePath = $"/uploads/faces/{newImageFileName}";
                }

                student.FaceEmbedding = embeddingElement.GetRawText();
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                if (newImageFileName != null)
                {
                    try
                    {
                        await _faceImageStorage.DeleteAsync(newImageFileName, cancellationToken);
                    }
                    catch (Exception cleanupException)
                    {
                        _logger.LogError(cleanupException, "Could not clean up an incomplete face image for student {StudentId}.", studentId);
                    }
                }

                _logger.LogError(ex, "Could not save face enrollment for student {StudentId}.", studentId);
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { success = false, message = "Face enrollment could not be saved. Please try again." });
            }

            if (previousImageFileName != null &&
                !string.Equals(previousImageFileName, newImageFileName, StringComparison.Ordinal))
            {
                try
                {
                    await _faceImageStorage.DeleteAsync(previousImageFileName, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not remove the previous face image for student {StudentId}.", studentId);
                }
            }

            return Ok(new { success = true, message = "Face biometrics enrolled successfully!" });
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
