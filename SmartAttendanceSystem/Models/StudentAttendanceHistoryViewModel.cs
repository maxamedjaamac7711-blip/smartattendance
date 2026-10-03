using System;
using System.Collections.Generic;
using SmartAttendanceSystem.Properties.Model;

namespace SmartAttendanceSystem.Models
{
    public class StudentAttendanceHistoryViewModel
    {
        public Student Student { get; set; } = null!;
        public int TotalAttended { get; set; }
        public int TotalClassSessions { get; set; }
        public int QrCheckIns { get; set; }
        public int FaceCheckIns { get; set; }
        public int ManualCheckIns { get; set; }
        public double AttendancePercentage { get; set; }

        public List<AttendanceRecordItem> Records { get; set; } = new();
    }

    public class AttendanceRecordItem
    {
        public int AttendanceId { get; set; }
        public DateTime Date { get; set; }
        public string Subject { get; set; } = "General";
        public string Method { get; set; } = "QR";
        public string Status { get; set; } = "Present";
        public string? ClassName { get; set; }
    }
}
