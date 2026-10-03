using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartAttendanceSystem.Properties.Model
{
    public class Attendance
    {
        [Key]
        public int AttendanceID { get; set; }

        [Required]
        public int StudentID { get; set; }

        public Student? Student { get; set; }

        public int? SessionId { get; set; }

        public AttendanceSession? Session { get; set; }

        [Required]
        public DateTime AttendanceDate { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Present";

        [Required]
        [StringLength(20)]
        public string Method { get; set; } = "QR";

        [StringLength(200)]
        public string? Subject { get; set; }
    }
}
