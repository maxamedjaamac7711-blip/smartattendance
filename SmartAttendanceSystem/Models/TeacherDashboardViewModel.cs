using System;
using System.Collections.Generic;
using SmartAttendanceSystem.Properties.Model;

namespace SmartAttendanceSystem.Models
{
    public class TeacherDashboardViewModel
    {
        public Teacher Teacher { get; set; } = null!;
        public int TotalStudentsInClass { get; set; }
        public int TodayPresentCount { get; set; }
        public double TodayAttendanceRate { get; set; }
        public int FaceEnrolledCount { get; set; }
        public List<AttendanceSession> ActiveSessions { get; set; } = new();
        public List<AttendanceSession> RecentSessions { get; set; } = new();
    }
}
