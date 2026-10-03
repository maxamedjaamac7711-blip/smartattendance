using System.ComponentModel.DataAnnotations;

namespace SmartAttendanceSystem.Properties.Model
{
    public class SchoolClass
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public int FacultyId { get; set; }

        public Faculty? Faculty { get; set; }
    }
}
