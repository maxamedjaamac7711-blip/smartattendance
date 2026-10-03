using System;
using System.Collections.Generic;
using SmartAttendanceSystem.Properties.Model;

namespace SmartAttendanceSystem.Models
{
    public class StudentDashboardViewModel
    {
        public Student Student { get; set; } = null!;
        public bool HasFaceEnrollment => !string.IsNullOrEmpty(Student.FaceEmbedding);
        public bool IsPresentToday { get; set; }
        public DateTime? TodayCheckInTime { get; set; }
        public string? TodayCheckInMethod { get; set; }
        public int TotalAttendedSessions { get; set; }
        public int TotalClassSessions { get; set; }
        public double OverallAttendanceRate { get; set; }

        public List<AttendanceSession> ActiveSessionsForClass { get; set; } = new();
        public List<Attendance> RecentAttendances { get; set; } = new();
    }
}
