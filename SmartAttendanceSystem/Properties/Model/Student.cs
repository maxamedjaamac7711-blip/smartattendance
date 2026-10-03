using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartAttendanceSystem.Properties.Model
{
    public class Student
    {
        public int StudentID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string EnrollmentNumber { get; set; } = string.Empty;

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

        public string? FaceEmbedding { get; set; }

        public string? FaceImagePath { get; set; }
    }
}
