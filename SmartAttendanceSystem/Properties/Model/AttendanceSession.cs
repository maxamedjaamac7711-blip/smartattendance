using System;
using System.ComponentModel.DataAnnotations;

namespace SmartAttendanceSystem.Properties.Model
{
    public class AttendanceSession
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ClassId { get; set; }

        public SchoolClass? Class { get; set; }

        [Required]
        public string TeacherId { get; set; } = string.Empty;

        public ApplicationUser? Teacher { get; set; }

        [Required]
        public string Token { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime ExpiresAt { get; set; }

        public bool IsActive { get; set; }

        [StringLength(200)]
        public string? Subject { get; set; }

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public string? Method { get; set; } = "QR";
    }
}
