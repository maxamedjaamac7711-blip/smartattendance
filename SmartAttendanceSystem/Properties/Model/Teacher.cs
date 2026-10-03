using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartAttendanceSystem.Properties.Model
{
    public class Teacher
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        [Required]
        public int FacultyId { get; set; }

        public Faculty? Faculty { get; set; }

        [Required]
        public int ClassId { get; set; }

        public int SchoolClassId { get; set; }

        public SchoolClass? SchoolClass { get; set; }
    }
}
