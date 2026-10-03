using System.ComponentModel.DataAnnotations;

namespace SmartAttendanceSystem.Models
{
    public class StudentCreateViewModel
    {
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

        [Required]
        [StringLength(100, MinimumLength = 3)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}
