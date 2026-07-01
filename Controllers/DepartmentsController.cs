using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using FacultyManagementSystem.Models;

namespace FacultyManagementSystem.Controllers
{
    public class DepartmentsController : BaseController
    {
        public DepartmentsController(AppDbContext db) : base(db) { }

        // Auth + role guard helper
        private IActionResult? Guard(params string[] allowedRoles)
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(role))
                return RedirectToAction("Login", "Account");
            if (!allowedRoles.Contains(role))
                return RedirectToAction("Index", "Home");
            SetViewData(role);
            return null;
        }

        private void SetViewData(string? role = null)
        {
            role ??= HttpContext.Session.GetString("UserRole") ?? "Admin";
            ViewData["UserRole"] = role;
            ViewData["UserName"] = HttpContext.Session.GetString("UserName");
            ViewData["Title"] = "Departments";
        }

        // GET: /Departments/Index
        public async Task<IActionResult> Index(string? search)
        {
            var guard = Guard("Admin", "HOD");
            if (guard != null) return guard;

            var query = _db.Departments
                .Where(d => !d.IsDeleted)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(d => d.DeptName.Contains(search) ||
                                         (d.HodName != null && d.HodName.Contains(search)));

            var departments = await query
                .OrderBy(d => d.DeptName)
                .Select(d => new DepartmentListRow
                {
                    DeptId = d.DeptId,
                    DeptName = d.DeptName,
                    HodName = d.HodName ?? "—",
                    Contact = d.Contact ?? "—",
                    Email = d.Email ?? "—",
                    IsActive = d.IsActive ?? true,
                    FacultyCount = d.Faculties.Count(f => !f.IsDeleted),
                    CourseCount = d.Courses.Count(c => !c.IsDeleted),
                })
                .ToListAsync();

            ViewData["Search"] = search;
            return View(departments);
        }

        // GET: /Departments/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var guard = Guard("Admin", "HOD");
            if (guard != null) return guard;

            await LoadFacultyDropdown();
            return View(new Department());
        }

        // POST: /Departments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Department model)
        {
            var guard = Guard("Admin", "HOD");
            if (guard != null) return guard;

            if (string.IsNullOrWhiteSpace(model.DeptName))
            {
                ModelState.AddModelError("DeptName", "Department name is required.");
                await LoadFacultyDropdown();
                return View(model);
            }

            // Check duplicate
            var exists = await _db.Departments
                .AnyAsync(d => d.DeptName == model.DeptName && !d.IsDeleted);
            if (exists)
            {
                ModelState.AddModelError("DeptName", "A department with this name already exists.");
                await LoadFacultyDropdown();
                return View(model);
            }

            model.IsActive = model.IsActive ?? true;
            model.IsDeleted = false;

            _db.Departments.Add(model);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Department '{model.DeptName}' created successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Departments/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var guard = Guard("Admin", "HOD");
            if (guard != null) return guard;

            var dept = await _db.Departments
                .FirstOrDefaultAsync(d => d.DeptId == id && !d.IsDeleted);

            if (dept == null)
            {
                TempData["Error"] = "Department not found.";
                return RedirectToAction(nameof(Index));
            }

            await LoadFacultyDropdown(dept.HodName);
            return View(dept);
        }

        // POST: /Departments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Department model)
        {
            var guard = Guard("Admin", "HOD");
            if (guard != null) return guard;

            if (string.IsNullOrWhiteSpace(model.DeptName))
            {
                ModelState.AddModelError("DeptName", "Department name is required.");
                await LoadFacultyDropdown(model.HodName);
                return View(model);
            }

            var dept = await _db.Departments
                .FirstOrDefaultAsync(d => d.DeptId == id && !d.IsDeleted);

            if (dept == null)
            {
                TempData["Error"] = "Department not found.";
                return RedirectToAction(nameof(Index));
            }

            // Check duplicate (exclude self)
            var exists = await _db.Departments
                .AnyAsync(d => d.DeptName == model.DeptName && d.DeptId != id && !d.IsDeleted);
            if (exists)
            {
                ModelState.AddModelError("DeptName", "A department with this name already exists.");
                await LoadFacultyDropdown(model.HodName);
                return View(model);
            }

            dept.DeptName = model.DeptName;
            dept.HodName = model.HodName;
            dept.Contact = model.Contact;
            dept.Email = model.Email;
            dept.IsActive = Request.Form["IsActive"].ToString().Contains("true");

            await _db.SaveChangesAsync();

            TempData["Success"] = $"Department '{dept.DeptName}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // Helper — loads faculty list into ViewBag for dropdown
        private async Task LoadFacultyDropdown(string? selectedName = null)
        {
            var faculty = await _db.Faculties
                .Where(f => !f.IsDeleted && f.IsActive == true)
                .OrderBy(f => f.Name)
                .Select(f => f.Name)
                .ToListAsync();

            ViewBag.FacultyList = faculty;
            ViewBag.SelectedHod = selectedName;
        }

        // POST: /Departments/Delete/5  (soft delete)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var guard = Guard("Admin", "HOD");
            if (guard != null) return guard;

            var dept = await _db.Departments
                .FirstOrDefaultAsync(d => d.DeptId == id && !d.IsDeleted);

            if (dept == null)
            {
                TempData["Error"] = "Department not found.";
                return RedirectToAction(nameof(Index));
            }

            // Prevent delete if faculty are assigned
            var hasFaculty = await _db.Faculties
                .AnyAsync(f => f.DeptId == id && !f.IsDeleted);
            if (hasFaculty)
            {
                TempData["Error"] = $"Cannot delete '{dept.DeptName}' — it has faculty members assigned. Reassign them first.";
                return RedirectToAction(nameof(Index));
            }

            dept.IsDeleted = true;
            dept.IsActive = false;
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Department '{dept.DeptName}' deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }

    // ── View Model ─────────────────────────────────────────────
    public class DepartmentListRow
    {
        public int DeptId { get; set; }
        public string DeptName { get; set; } = "";
        public string HodName { get; set; } = "";
        public string Contact { get; set; } = "";
        public string Email { get; set; } = "";
        public bool IsActive { get; set; }
        public int FacultyCount { get; set; }
        public int CourseCount { get; set; }
    }
}