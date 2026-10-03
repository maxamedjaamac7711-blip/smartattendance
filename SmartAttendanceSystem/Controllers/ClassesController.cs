using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SmartAttendanceSystem.Properties.Model;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SmartAttendanceSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ClassesController : Controller
    {
        private readonly AppDbContext _context;

        public ClassesController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var items = await _context.Classes
                .Include(c => c.Faculty)
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewBag.StudentCounts = await _context.Students
                .GroupBy(s => s.ClassId)
                .Select(g => new { ClassId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ClassId, x => x.Count);

            return View(items);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var faculties = await _context.Faculties.OrderBy(f => f.Name).ToListAsync();
            ViewBag.Faculties = new SelectList(faculties, "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SchoolClass model)
        {
            if (!ModelState.IsValid)
            {
                var faculties = await _context.Faculties.OrderBy(f => f.Name).ToListAsync();
                ViewBag.Faculties = new SelectList(faculties, "Id", "Name");
                return View(model);
            }

            _context.Classes.Add(model);
            await _context.SaveChangesAsync();

            TempData["Message"] = $"Class '{model.Name}' created successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var item = await _context.Classes.FindAsync(id);
            if (item == null)
                return NotFound();

            var faculties = await _context.Faculties.OrderBy(f => f.Name).ToListAsync();
            ViewBag.Faculties = new SelectList(faculties, "Id", "Name", item.FacultyId);
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(SchoolClass model)
        {
            if (!ModelState.IsValid)
            {
                var faculties = await _context.Faculties.OrderBy(f => f.Name).ToListAsync();
                ViewBag.Faculties = new SelectList(faculties, "Id", "Name", model.FacultyId);
                return View(model);
            }

            _context.Classes.Update(model);
            await _context.SaveChangesAsync();

            TempData["Message"] = $"Class '{model.Name}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.Classes.FindAsync(id);
            if (item == null)
                return NotFound();

            var hasStudents = await _context.Students.AnyAsync(s => s.ClassId == id);
            var hasTeachers = await _context.Teachers.AnyAsync(t => t.ClassId == id);
            if (hasStudents || hasTeachers)
            {
                TempData["Error"] = $"Cannot delete class '{item.Name}' because there are students or instructors assigned to it. Please reassign them first.";
                return RedirectToAction(nameof(Index));
            }

            _context.Classes.Remove(item);
            await _context.SaveChangesAsync();

            TempData["Message"] = $"Class '{item.Name}' was deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
