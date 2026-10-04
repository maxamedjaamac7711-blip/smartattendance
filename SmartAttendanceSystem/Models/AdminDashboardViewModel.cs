using System;
using System.Collections.Generic;
using SmartAttendanceSystem.Properties.Model;

namespace SmartAttendanceSystem.Models
{
    public class AdminDashboardViewModel
    {
        public int TotalStudents { get; set; }
        public int TotalTeachers { get; set; }
        public int TotalClasses { get; set; }
        public int TotalFaculties { get; set; }
        public int TodayAttendanceCount { get; set; }
        public int FaceEnrolledStudentsCount { get; set; }
        public double TodayAttendanceRate { get; set; }
        public int ActiveSessionsCount { get; set; }

        public List<AttendanceSession> ActiveSessions { get; set; } = new();
        public List<RecentAttendanceItem> RecentAttendances { get; set; } = new();
    }

    public class RecentAttendanceItem
    {
        public int AttendanceId { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string EnrollmentNumber { get; set; } = string.Empty;
        public string? ClassName { get; set; }
        public string? Subject { get; set; }
        public string Method { get; set; } = "QR";
        public string Status { get; set; } = "Present";
        public DateTime AttendanceDate { get; set; }
        public string? FaceImagePath { get; set; }
    }
}
