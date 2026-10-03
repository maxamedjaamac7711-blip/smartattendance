using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using SmartAttendanceSystem.Properties.Model;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using SmartAttendanceSystem.Models;
using System.Text.Json;
using System.Numerics;

namespace SmartAttendanceSystem.Controllers
{
    [Authorize]
    public class AttendanceController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AttendanceController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole("Admin");

            var query = _context.AttendanceSessions
                .Include(s => s.Class)
                .Include(s => s.Teacher)
                .AsQueryable();

            if (!isAdmin)
            {
                var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == userId);
                if (teacher != null)
                {
                    query = query.Where(s => s.ClassId == teacher.ClassId);
                }
            }

            var sessions = await query
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            return View(sessions);
        }

        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> Report(DateTime? startDate, DateTime? endDate, int? classId, string? subject, string? method)
        {
            var userId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole("Admin");

            // Base query
            var query = _context.Attendances
                .Include(a => a.Student)
                    .ThenInclude(s => s.SchoolClass)
                .Include(a => a.Session)
                .AsQueryable();

            // Filter by teacher (non-admins only see their own)
            if (!isAdmin)
            {
                var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == userId);
                if (teacher != null)
                {
                    query = query.Where(a => a.Session.ClassId == teacher.ClassId);
                }
            }

            // Apply filters
            if (startDate.HasValue)
                query = query.Where(a => a.AttendanceDate.Date >= startDate.Value.Date);
            
            if (endDate.HasValue)
                query = query.Where(a => a.AttendanceDate.Date <= endDate.Value.Date);

            if (classId.HasValue)
                query = query.Where(a => a.Student.ClassId == classId.Value);

            if (!string.IsNullOrEmpty(subject))
                query = query.Where(a => a.Subject == subject);

            if (!string.IsNullOrEmpty(method))
                query = query.Where(a => a.Method == method);

            var attendances = await query
                .OrderByDescending(a => a.AttendanceDate)
                .ToListAsync();

            // Statistics
            var totalRecords = attendances.Count;
            var presentCount = attendances.Count(a => a.Status == "Present");
            var qrCount = attendances.Count(a => a.Method == "QR");
            var faceCount = attendances.Count(a => a.Method == "Face");

            // Group by date for chart
            var dailyStats = attendances
                .GroupBy(a => a.AttendanceDate.Date)
                .Select(g => new {
                    Date = g.Key,
                    Present = g.Count(a => a.Status == "Present"),
                    Total = g.Count()
                })
                .OrderBy(d => d.Date)
                .ToList();

            // Group by student for individual stats
            var studentStats = attendances
                .GroupBy(a => new { a.StudentID, StudentName = a.Student.Name, a.Student.EnrollmentNumber, ClassName = a.Student.SchoolClass.Name })
                .Select(g => new {
                    StudentID = g.Key.StudentID,
                    Name = g.Key.StudentName,
                    EnrollmentNumber = g.Key.EnrollmentNumber,
                    ClassName = g.Key.ClassName,
                    TotalSessions = g.Count(),
                    PresentCount = g.Count(a => a.Status == "Present"),
                    QRCount = g.Count(a => a.Method == "QR"),
                    FaceCount = g.Count(a => a.Method == "Face")
                })
                .OrderByDescending(s => s.PresentCount)
                .ToList();

            // Dropdown data
            ViewBag.Classes = await _context.Classes.OrderBy(c => c.Name).ToListAsync();
            ViewBag.Subjects = await _context.AttendanceSessions
                .Where(s => s.Subject != null)
                .Select(s => s.Subject!)
                .Distinct()
                .ToListAsync();

            ViewBag.Filters = new { startDate, endDate, classId, subject, method };
            ViewBag.Stats = new { totalRecords, presentCount, qrCount, faceCount, dailyStats, studentStats };

            return View();
        }

        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> ExportCsv(DateTime? startDate, DateTime? endDate, int? classId, string? subject, string? method)
        {
            var userId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole("Admin");

            var query = _context.Attendances
                .Include(a => a.Student)
                    .ThenInclude(s => s.SchoolClass)
                .Include(a => a.Session)
                .AsQueryable();

            if (!isAdmin)
            {
                var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == userId);
                if (teacher != null)
                {
                    query = query.Where(a => a.Session.ClassId == teacher.ClassId);
                }
            }

            if (startDate.HasValue)
                query = query.Where(a => a.AttendanceDate.Date >= startDate.Value.Date);
            if (endDate.HasValue)
                query = query.Where(a => a.AttendanceDate.Date <= endDate.Value.Date);
            if (classId.HasValue)
                query = query.Where(a => a.Student.ClassId == classId.Value);
            if (!string.IsNullOrEmpty(subject))
                query = query.Where(a => a.Subject == subject);
            if (!string.IsNullOrEmpty(method))
                query = query.Where(a => a.Method == method);

            var attendances = await query
                .OrderByDescending(a => a.AttendanceDate)
                .ToListAsync();

            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Date,Time,Student Name,Enrollment Number,Class,Subject,Method,Status");
            
            foreach (var a in attendances)
            {
                csv.AppendLine($"{a.AttendanceDate:yyyy-MM-dd},{a.AttendanceDate:HH:mm},{a.Student.Name},{a.Student.EnrollmentNumber},{a.Student.SchoolClass?.Name},{a.Subject},{a.Method},{a.Status}");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            var fileName = $"Attendance_Report_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            return File(bytes, "text/csv", fileName);
        }

        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> ClassAttendance(int classId)
        {
            var today = DateTime.UtcNow.Date;

            var students = await _context.Students
                .Where(s => s.ClassId == classId)
                .OrderBy(s => s.EnrollmentNumber)
                .ToListAsync();

            var results = new List<StudentAttendanceViewModel>();
            foreach (var st in students)
            {
                var att = await _context.Attendances
                    .Where(a => a.StudentID == st.StudentID && a.AttendanceDate.Date == today)
                    .FirstOrDefaultAsync();

                results.Add(new StudentAttendanceViewModel
                {
                    StudentID = st.StudentID,
                    EnrollmentNumber = st.EnrollmentNumber,
                    Name = st.Name,
                    Present = att != null && att.Status == "Present",
                    Status = att?.Status,
                    RecordedAt = att?.AttendanceDate
                });
            }

            ViewBag.ClassName = (await _context.Classes.FindAsync(classId))?.Name;
            return View(results);
        }

        [Authorize(Roles = "Teacher,Admin")]
        [HttpGet]
        public async Task<IActionResult> Start(string? method)
        {
            var userId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole("Admin");
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == userId);

            if (!isAdmin && teacher == null)
                return View("/Views/Teachers/DashboardEmpty.cshtml");

            var classes = await _context.Classes
                .Include(c => c.Faculty)
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewBag.Classes = classes;
            ViewBag.ClassId = teacher?.ClassId ?? (classes.FirstOrDefault()?.Id ?? 0);
            ViewBag.SelectedMethod = string.Equals(method, "Face", StringComparison.OrdinalIgnoreCase) ? "Face" : "QR";
            return View();
        }

        [Authorize(Roles = "Teacher,Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Start(int classId, string subject, string method = "QR")
        {
            var userId = _userManager.GetUserId(User);
            var token = Guid.NewGuid().ToString("N");
            var session = new AttendanceSession
            {
                ClassId = classId,
                TeacherId = userId!,
                Token = token,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                IsActive = true,
                Subject = string.IsNullOrWhiteSpace(subject) ? "Class Lecture" : subject.Trim(),
                Method = method
            };

            _context.AttendanceSessions.Add(session);
            await _context.SaveChangesAsync();

            if (method == "Face")
            {
                return RedirectToAction(nameof(FaceRecognition), new { id = session.Id });
            }
            
            return RedirectToAction(nameof(QR), new { id = session.Id });
        }

        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> QR(int id)
        {
            var session = await _context.AttendanceSessions
                .Include(s => s.Class)
                .Include(s => s.Teacher)
                .FirstOrDefaultAsync(s => s.Id == id);
            if (session == null)
                return NotFound();

            var checkUrl = Url.Action("Check", "Attendance", new { token = session.Token }, Request.Scheme);
            ViewBag.CheckUrl = checkUrl;
            return View(session);
        }

        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> FaceRecognition(int id)
        {
            var session = await _context.AttendanceSessions
                .Include(s => s.Class)
                .Include(s => s.Teacher)
                .FirstOrDefaultAsync(s => s.Id == id);
            if (session == null)
                return NotFound();

            var students = await _context.Students
                .Where(s => s.ClassId == session.ClassId && !string.IsNullOrEmpty(s.FaceEmbedding))
                .ToListAsync();

            ViewBag.Session = session;
            ViewBag.StudentsJson = JsonSerializer.Serialize(students.Select(s => new {
                id = s.StudentID,
                name = s.Name,
                enrollment = s.EnrollmentNumber,
                embedding = s.FaceEmbedding,
                imagePath = s.FaceImagePath
            }));
            
            return View();
        }

        [Authorize(Roles = "Student")]
        [HttpGet]
        public async Task<IActionResult> Check(string token)
        {
            var session = await _context.AttendanceSessions
                .Include(s => s.Class)
                .FirstOrDefaultAsync(s => s.Token == token && s.IsActive);

            if (session == null || session.ExpiresAt < DateTime.UtcNow)
            {
                ViewBag.Status = "Expired";
                ViewBag.Message = "This attendance session was not found or has already expired.";
                return View("CheckResult");
            }

            var userId = _userManager.GetUserId(User);
            var student = await _context.Students
                .Include(s => s.SchoolClass)
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (student == null)
            {
                ViewBag.Status = "Error";
                ViewBag.Message = "Student profile was not found. Please contact administration.";
                return View("CheckResult");
            }

            var already = await _context.Attendances.FirstOrDefaultAsync(a => a.StudentID == student.StudentID && a.AttendanceDate.Date == DateTime.UtcNow.Date);
            if (already != null)
            {
                ViewBag.Status = "AlreadyRecorded";
                ViewBag.Message = "Your attendance has already been recorded for today!";
                ViewBag.Student = student;
                ViewBag.Session = session;
                ViewBag.RecordedAt = already.AttendanceDate;
                return View("CheckResult");
            }

            var record = new Attendance
            {
                StudentID = student.StudentID,
                SessionId = session.Id,
                AttendanceDate = DateTime.UtcNow,
                Status = "Present",
                Method = "QR",
                Subject = session.Subject
            };
            _context.Attendances.Add(record);
            await _context.SaveChangesAsync();

            ViewBag.Status = "Success";
            ViewBag.Message = "Your attendance has been successfully recorded!";
            ViewBag.Student = student;
            ViewBag.Session = session;
            ViewBag.RecordedAt = record.AttendanceDate;
            return View("CheckResult");
        }

        [Authorize(Roles = "Student,Teacher,Admin")]
        [HttpPost]
        public async Task<IActionResult> CheckFace([FromBody] JsonElement data)
        {
            try
            {
                if (!data.TryGetProperty("token", out var tokenProp))
                    return Json(new { success = false, message = "Missing session token." });

                var token = tokenProp.GetString();
                var session = await _context.AttendanceSessions
                    .Include(s => s.Class)
                    .FirstOrDefaultAsync(s => s.Token == token && s.IsActive);

                if (session == null || session.ExpiresAt < DateTime.UtcNow)
                    return Json(new { success = false, message = "Session not found or expired." });

                var userId = _userManager.GetUserId(User);
                var isStudent = User.IsInRole("Student");

                Student? targetStudent = null;

                if (isStudent)
                {
                    // Case 1: Student is scanning on their own device
                    var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
                    if (student == null)
                        return Json(new { success = false, message = "Student profile not found." });

                    if (student.ClassId != session.ClassId)
                        return Json(new { success = false, message = "You are not enrolled in this session's class." });

                    targetStudent = student;
                }
                else
                {
                    // Case 2: Teacher or Admin running classroom scanner
                    if (data.TryGetProperty("studentId", out var sIdProp) && sIdProp.TryGetInt32(out var sId))
                    {
                        targetStudent = await _context.Students.FirstOrDefaultAsync(s => s.StudentID == sId && s.ClassId == session.ClassId);
                    }
                }

                // If not identified yet, match by embedding against class roster
                if (targetStudent == null && data.TryGetProperty("embedding", out var embProp))
                {
                    var embedding = embProp.EnumerateArray().Select(x => x.GetSingle()).ToArray();
                    var enrolledStudents = await _context.Students
                        .Where(s => s.ClassId == session.ClassId && !string.IsNullOrEmpty(s.FaceEmbedding))
                        .ToListAsync();

                    float bestDistance = float.MaxValue;
                    foreach (var enrolled in enrolledStudents)
                    {
                        try
                        {
                            var enrolledEmbedding = JsonSerializer.Deserialize<float[]>(enrolled.FaceEmbedding!);
                            if (enrolledEmbedding != null && enrolledEmbedding.Length == embedding.Length)
                            {
                                var distance = CalculateEuclideanDistance(embedding, enrolledEmbedding);
                                if (distance < bestDistance)
                                {
                                    bestDistance = distance;
                                    targetStudent = enrolled;
                                }
                            }
                        }
                        catch { }
                    }

                    const float THRESHOLD = 0.6f;
                    if (bestDistance > THRESHOLD)
                    {
                        targetStudent = null;
                    }
                }

                if (targetStudent == null)
                {
                    return Json(new { success = false, message = "Face not recognized. Please ensure student is enrolled." });
                }

                // Check if already checked in today
                var today = DateTime.UtcNow.Date;
                var already = await _context.Attendances.FirstOrDefaultAsync(a => a.StudentID == targetStudent.StudentID && a.AttendanceDate.Date == today);
                if (already != null)
                {
                    return Json(new { 
                        success = true, 
                        alreadyRecorded = true, 
                        studentName = targetStudent.Name, 
                        studentId = targetStudent.StudentID,
                        message = $"{targetStudent.Name} is already recorded present today." 
                    });
                }

                var record = new Attendance
                {
                    StudentID = targetStudent.StudentID,
                    SessionId = session.Id,
                    AttendanceDate = DateTime.UtcNow,
                    Status = "Present",
                    Method = "Face",
                    Subject = session.Subject
                };
                _context.Attendances.Add(record);
                await _context.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    studentName = targetStudent.Name, 
                    studentId = targetStudent.StudentID,
                    message = $"Attendance verified for {targetStudent.Name}!" 
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Recognition error: " + ex.Message });
            }
        }

        private float CalculateEuclideanDistance(float[] a, float[] b)
        {
            float sum = 0;
            for (int i = 0; i < a.Length; i++)
            {
                float diff = a[i] - b[i];
                sum += diff * diff;
            }
            return (float)Math.Sqrt(sum);
        }
    }
}
