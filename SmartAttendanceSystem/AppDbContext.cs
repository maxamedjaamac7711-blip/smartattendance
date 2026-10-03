using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartAttendanceSystem.Properties.Model;

namespace SmartAttendanceSystem
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Student> Students { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<Faculty> Faculties { get; set; }
        public DbSet<SchoolClass> Classes { get; set; }
        public DbSet<Teacher> Teachers { get; set; }
        public DbSet<AttendanceSession> AttendanceSessions { get; set; }
    }
}
