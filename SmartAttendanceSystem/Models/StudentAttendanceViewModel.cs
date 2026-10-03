using System;

namespace SmartAttendanceSystem.Models
{
    public class StudentAttendanceViewModel
    {
        public int StudentID { get; set; }
        public string EnrollmentNumber { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool Present { get; set; }
        public string? Status { get; set; }
        public DateTime? RecordedAt { get; set; }
    }
}
