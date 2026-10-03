using System.ComponentModel.DataAnnotations;

namespace SmartAttendanceSystem.Properties.Model
{
    public class Faculty
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }
    }
}
