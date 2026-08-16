using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using FacultyManagementSystem.Models;

namespace FacultyManagementSystem.Controllers
{
    public class SemestersController : BaseController
    {
        public SemestersController(AppDbContext db) : base(db) { }

        // Auth guard - Admin only
        private IActionResult? Guard()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(role))
            {
                return RedirectToAction("Login", "Account");
            }
            if (role != "Admin")
            {
                return RedirectToAction("Index", "Home");
            }
            return null;
        }

        // GET: /Semesters/Index
        public async Task<IActionResult> Index(string? search)
        {
            var guard = Guard();
            if (guard != null)
            {
                return guard;
            }
            ViewData["Title"] = "Semesters";

            var query = _db.Semesters.Where(s => !s.IsDeleted).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(s => s.SemName.Contains(search) ||s.AcadYear.Contains(search));

            var semesters = await query.OrderByDescending(s => s.StartDate).Select(s => new SemesterListRow
                {
                    SemId = s.SemId,
                    SemName = s.SemName,
                    AcadYear = s.AcadYear,
                    StartDate = s.StartDate,
                    EndDate = s.EndDate,
                    IsCurrent = s.IsCurrent,
                    CourseCount = s.WorkloadAssignments
                        .Where(w => !w.IsDeleted)
                        .Select(w => w.CourseId)
                        .Distinct()
                        .Count(),
                    FacultyCount = s.WorkloadAssignments
                        .Where(w => !w.IsDeleted)
                        .Select(w => w.EmpId)
                        .Distinct()
                        .Count(),
                })
                .ToListAsync();

            ViewData["Search"] = search;
            return View(semesters);
        }

        // GET: /Semesters/Create
        [HttpGet]
        public IActionResult Create()
        {
            var guard = Guard();
            if (guard != null)
            {
                return guard;
            }
            ViewData["Title"] = "Semesters";
            return View(new Semester());
        }

        // POST: /Semesters/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Semester model)
        {
            var guard = Guard();
            if (guard != null)
            {
                return guard;
            }
            ViewData["Title"] = "Semesters";

            if (string.IsNullOrWhiteSpace(model.SemName))
            {
                ModelState.AddModelError("SemName", "Semester name is required.");
                return View(model);
            }

            if (string.IsNullOrWhiteSpace(model.AcadYear))
            {
                ModelState.AddModelError("AcadYear", "Academic year is required.");
                return View(model);
            }

            if (model.EndDate <= model.StartDate)
            {
                ModelState.AddModelError("EndDate", "End date must be after start date.");
                return View(model);
            }

            // Duplicate name check
            var exists = await _db.Semesters.AnyAsync(s => s.SemName == model.SemName && !s.IsDeleted);
            if (exists)
            {
                ModelState.AddModelError("SemName", "A semester with this name already exists.");
                return View(model);
            }

            // If setting as current, unset all others first
            if (model.IsCurrent)
            {
                var currentSems = await _db.Semesters.Where(s => s.IsCurrent && !s.IsDeleted).ToListAsync();
                foreach (var s in currentSems)
                    s.IsCurrent = false;
            }

            model.IsDeleted = false;
            _db.Semesters.Add(model);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Semester '{model.SemName}' created successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Semesters/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var guard = Guard();
            if (guard != null)
            {
                return guard;
            }
            ViewData["Title"] = "Semesters";
            var sem = await _db.Semesters.FirstOrDefaultAsync(s => s.SemId == id && !s.IsDeleted);
            if (sem == null)
            {
                TempData["Error"] = "Semester not found.";
                return RedirectToAction(nameof(Index));
            }
            return View(sem);
        }

        // POST: /Semesters/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Semester model)
        {
            var guard = Guard();
            if (guard != null)
            {
                return guard;
            }
            ViewData["Title"] = "Semesters";
            if (string.IsNullOrWhiteSpace(model.SemName))
            {
                ModelState.AddModelError("SemName", "Semester name is required.");
                return View(model);
            }
            if (model.EndDate <= model.StartDate)
            {
                ModelState.AddModelError("EndDate", "End date must be after start date.");
                return View(model);
            }
            var sem = await _db.Semesters.FirstOrDefaultAsync(s => s.SemId == id && !s.IsDeleted);

            if (sem == null)
            {
                TempData["Error"] = "Semester not found.";
                return RedirectToAction(nameof(Index));
            }

            // Duplicate name check (exclude self)
            var exists = await _db.Semesters.AnyAsync(s => s.SemName == model.SemName && s.SemId != id && !s.IsDeleted);
            if (exists)
            {
                ModelState.AddModelError("SemName", "A semester with this name already exists.");
                return View(model);
            }
            sem.SemName = model.SemName;
            sem.AcadYear = model.AcadYear;
            sem.StartDate = model.StartDate;
            sem.EndDate = model.EndDate;
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Semester '{sem.SemName}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Semesters/SetCurrent/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetCurrent(int id)
        {
            var guard = Guard();
            if (guard != null)
            {
                return guard;
            }
            var sem = await _db.Semesters.FirstOrDefaultAsync(s => s.SemId == id && !s.IsDeleted);

            if (sem == null)
            {
                TempData["Error"] = "Semester not found.";
                return RedirectToAction(nameof(Index));
            }

            // Transaction
            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                // Reset all semesters
                await _db.Semesters.Where(s => s.IsCurrent).ExecuteUpdateAsync(s => s.SetProperty(x => x.IsCurrent, false));
                // Set selected as current
                sem.IsCurrent = true;
                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
                TempData["Success"] = $"'{sem.SemName}' is now the active semester.";
            }
            catch
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "Failed to update active semester. Please try again.";
            }
            return RedirectToAction(nameof(Index));
        }
        // POST: /Semesters/Delete/5 (soft delete)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var guard = Guard();
            if (guard != null)
            {
                return guard;
            }
            var sem = await _db.Semesters.FirstOrDefaultAsync(s => s.SemId == id && !s.IsDeleted);
            if (sem == null)
            {
                TempData["Error"] = "Semester not found.";
                return RedirectToAction(nameof(Index));
            }
            // Prevent delete if it is the current semester
            if (sem.IsCurrent)
            {
                TempData["Error"] = $"Cannot delete '{sem.SemName}' — it is the active semester. Set another semester as current first.";
                return RedirectToAction(nameof(Index));
            }
            // Prevent delete if it has workload assignments
            var hasAssignments = await _db.WorkloadAssignments.AnyAsync(w => w.SemId == id);
            if (hasAssignments)
            {
                TempData["Error"] = $"Cannot delete '{sem.SemName}' — it has workload assignments. Remove them first.";
                return RedirectToAction(nameof(Index));
            }
            sem.IsDeleted = true;
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Semester '{sem.SemName}' deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }

    // View Model
    public class SemesterListRow
    {
        public int SemId { get; set; }
        public string SemName { get; set; } = "";
        public string AcadYear { get; set; } = "";
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public bool IsCurrent { get; set; }
        public int CourseCount { get; set; }
        public int FacultyCount { get; set; }
    }
}