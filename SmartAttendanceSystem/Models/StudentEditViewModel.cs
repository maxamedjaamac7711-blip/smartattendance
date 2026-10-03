using System.ComponentModel.DataAnnotations;

namespace SmartAttendanceSystem.Models
{
    public class StudentEditViewModel
    {
        public int StudentID { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string EnrollmentNumber { get; set; } = string.Empty;

        [Required]
        public int FacultyId { get; set; }

        [Required]
        public int ClassId { get; set; }

        public string Username { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        public string? NewPassword { get; set; }
    }
}
