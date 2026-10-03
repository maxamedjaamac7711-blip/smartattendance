using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SmartAttendanceSystem.Properties.Model;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace SmartAttendanceSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class FacultiesController : Controller
    {
        private readonly AppDbContext _context;

        public FacultiesController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var items = await _context.Faculties.OrderBy(f => f.Name).ToListAsync();

            ViewBag.ClassCounts = await _context.Classes
                .GroupBy(c => c.FacultyId)
                .Select(g => new { FacultyId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.FacultyId, x => x.Count);

            ViewBag.StudentCounts = await _context.Students
                .GroupBy(s => s.FacultyId)
                .Select(g => new { FacultyId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.FacultyId, x => x.Count);

            return View(items);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Faculty faculty)
        {
            if (!ModelState.IsValid)
                return View(faculty);

            _context.Faculties.Add(faculty);
            await _context.SaveChangesAsync();

            TempData["Message"] = $"Faculty '{faculty.Name}' created successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var faculty = await _context.Faculties.FindAsync(id);
            if (faculty == null)
                return NotFound();

            return View(faculty);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Faculty faculty)
        {
            if (!ModelState.IsValid)
                return View(faculty);

            _context.Faculties.Update(faculty);
            await _context.SaveChangesAsync();

            TempData["Message"] = $"Faculty '{faculty.Name}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var faculty = await _context.Faculties.FindAsync(id);
            if (faculty == null)
                return NotFound();

            var hasClasses = await _context.Classes.AnyAsync(c => c.FacultyId == id);
            var hasStudents = await _context.Students.AnyAsync(s => s.FacultyId == id);
            if (hasClasses || hasStudents)
            {
                TempData["Error"] = $"Cannot delete faculty '{faculty.Name}' because there are classes or students assigned to it. Please reassign them first.";
                return RedirectToAction(nameof(Index));
            }

            _context.Faculties.Remove(faculty);
            await _context.SaveChangesAsync();

            TempData["Message"] = $"Faculty '{faculty.Name}' was deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
