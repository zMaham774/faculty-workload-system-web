using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using FacultyManagementSystem.Models;

namespace FacultyManagementSystem.Controllers
{
    public class CoursesController : BaseController
    {
        public CoursesController(AppDbContext db) : base(db) { }

        private IActionResult? Guard()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(role))
            {
                return RedirectToAction("Login", "Account");
            }
            if (role != "Admin" && role != "HOD")
            {
                return RedirectToAction("Index", "Home");
            }
            return null;
        }

        // Returns the HOD's own department id, or null for Admin (sees all)
        private async Task<int?> GetHodDeptIdIfApplicable()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "HOD")
            {
                return null;
            }
            var empId = HttpContext.Session.GetInt32("FacultyId");
            if (!empId.HasValue)
            {
                return null;
            }
            var deptId = await _db.Faculties
                .Where(f => f.EmpId == empId && !f.IsDeleted)
                .Select(f => (int?)f.DeptId)
                .FirstOrDefaultAsync();

            return deptId;
        }

        // GET: /Courses/Index
        public async Task<IActionResult> Index(string? search, int? deptId, string? courseType)
        {
            var guard = Guard();
            if (guard != null)
            {
                return guard;
            }
            ViewData["Title"] = "Courses";
            var hodDeptId = await GetHodDeptIdIfApplicable();
            var query = _db.VwCourseDetails.Where(c => c.IsDeleted == false).AsQueryable();
            // HOD is locked to their own department regardless of filter selection
            if (hodDeptId.HasValue)
            {
                query = query.Where(c => c.DeptId == hodDeptId);
            }
            else if (deptId.HasValue)
            {
                query = query.Where(c => c.DeptId == deptId);
            }
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(c => c.CourseCode.Contains(search) ||
                                          c.Title.Contains(search) ||
                                          c.DeptName.Contains(search));

            if (!string.IsNullOrWhiteSpace(courseType))
            {
                query = query.Where(c => c.CourseType == courseType);
            }
            var courses = await query.OrderBy(c => c.CourseCode).ToListAsync();
            await LoadDropdowns(hodDeptId);
            ViewData["Search"] = search;
            ViewData["HodLocked"] = hodDeptId.HasValue;
            return View(courses);
        }

        // GET: /Courses/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var guard = Guard();
            if (guard != null)
            {
                return guard;
            }
            ViewData["Title"] = "Courses";
            var hodDeptId = await GetHodDeptIdIfApplicable();
            await LoadFormDropdowns(hodDeptId);
            var model = new Course();
            if (hodDeptId.HasValue) model.DeptId = hodDeptId.Value;

            return View(model);
        }

        // POST: /Courses/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Course model)
        {
            var guard = Guard();
            if (guard != null)
            {
                return guard;
            }
            ViewData["Title"] = "Courses";
            ModelState.Remove("Dept");
            ModelState.Remove("CourseReassignmentLogs");
            ModelState.Remove("WorkloadAssignments");
            var hodDeptId = await GetHodDeptIdIfApplicable();
            // HOD can only create courses in their own department — enforce server-side
            if (hodDeptId.HasValue)
            {
                model.DeptId = hodDeptId.Value;
            }
            if (!await ValidateCourse(model, 0))
            {
                await LoadFormDropdowns(hodDeptId);
                return View(model);
            }
            model.IsActive = model.IsActive ?? true;
            model.IsDeleted = false;
            _db.Courses.Add(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Course '{model.CourseCode} - {model.Title}' added successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Courses/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var guard = Guard();
            if (guard != null)
            {
                return guard;
            }
            ViewData["Title"] = "Courses";
            var hodDeptId = await GetHodDeptIdIfApplicable();
            var course = await _db.Courses.FirstOrDefaultAsync(c => c.CourseId == id && !c.IsDeleted);
            if (course == null)
            {
                TempData["Error"] = "Course not found.";
                return RedirectToAction(nameof(Index));
            }
            // HOD cannot edit a course outside their own department
            if (hodDeptId.HasValue && course.DeptId != hodDeptId)
            {
                TempData["Error"] = "You can only edit courses in your own department.";
                return RedirectToAction(nameof(Index));
            }
            await LoadFormDropdowns(hodDeptId);
            return View(course);
        }

        // POST: /Courses/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Course model)
        {
            var guard = Guard();
            if (guard != null)
            {
                return guard;
            }
            ViewData["Title"] = "Courses";
            ModelState.Remove("Dept");
            ModelState.Remove("CourseReassignmentLogs");
            ModelState.Remove("WorkloadAssignments");
            var hodDeptId = await GetHodDeptIdIfApplicable();
            var course = await _db.Courses.FirstOrDefaultAsync(c => c.CourseId == id && !c.IsDeleted);
            if (course == null)
            {
                TempData["Error"] = "Course not found.";
                return RedirectToAction(nameof(Index));
            }
            if (hodDeptId.HasValue && course.DeptId != hodDeptId)
            {
                TempData["Error"] = "You can only edit courses in your own department.";
                return RedirectToAction(nameof(Index));
            }
            // HOD cannot move a course to a different department
            if (hodDeptId.HasValue)
                model.DeptId = hodDeptId.Value;

            if (!await ValidateCourse(model, id))
            {
                await LoadFormDropdowns(hodDeptId);
                return View(model);
            }
            course.CourseCode = model.CourseCode;
            course.Title = model.Title;
            course.CreditHours = model.CreditHours;
            course.CourseType = model.CourseType;
            course.DeptId = model.DeptId;
            course.IsActive = Request.Form["IsActive"].ToString().Contains("true");
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Course '{course.CourseCode}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Courses/Delete/5 (soft delete)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var guard = Guard();
            if (guard != null)
            {
                return guard;
            }
            var hodDeptId = await GetHodDeptIdIfApplicable();
            var course = await _db.Courses.FirstOrDefaultAsync(c => c.CourseId == id && !c.IsDeleted);
            if (course == null)
            {
                TempData["Error"] = "Course not found.";
                return RedirectToAction(nameof(Index));
            }
            if (hodDeptId.HasValue && course.DeptId != hodDeptId)
            {
                TempData["Error"] = "You can only delete courses in your own department.";
                return RedirectToAction(nameof(Index));
            }
            var hasWorkload = await _db.WorkloadAssignments.AnyAsync(w => w.CourseId == id);
            if (hasWorkload)
            {
                TempData["Error"] = $"Cannot delete '{course.CourseCode}' — it has workload assignments. Remove them first.";
                return RedirectToAction(nameof(Index));
            }
            course.IsDeleted = true;
            course.IsActive = false;
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Course '{course.CourseCode}' deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        // HELPERS 
        private async Task LoadDropdowns(int? hodDeptId)
        {
            if (hodDeptId.HasValue)
            {
                ViewBag.Departments = await _db.Departments
                    .Where(d => d.DeptId == hodDeptId && !d.IsDeleted)
                    .Select(d => new { d.DeptId, d.DeptName })
                    .ToListAsync();
            }
            else
            {
                ViewBag.Departments = await _db.Departments
                    .Where(d => !d.IsDeleted && d.IsActive == true)
                    .OrderBy(d => d.DeptName)
                    .Select(d => new { d.DeptId, d.DeptName })
                    .ToListAsync();
            }

            ViewBag.CourseTypes = new List<string> { "Theory", "Lab", "Theory+Lab" };
        }

        private async Task LoadFormDropdowns(int? hodDeptId)
        {
            await LoadDropdowns(hodDeptId);
        }

        private async Task<bool> ValidateCourse(Course model, int excludeId)
        {
            bool valid = true;
            if (string.IsNullOrWhiteSpace(model.CourseCode))
            {
                ModelState.AddModelError("CourseCode", "Course code is required.");
                valid = false;
            }
            else
            {
                var codeExists = await _db.Courses.AnyAsync(c => c.CourseCode == model.CourseCode && c.CourseId != excludeId && !c.IsDeleted);
                if (codeExists)
                {
                    ModelState.AddModelError("CourseCode", "A course with this code already exists.");
                    valid = false;
                }
            }
            if (string.IsNullOrWhiteSpace(model.Title))
            {
                ModelState.AddModelError("Title", "Course title is required.");
                valid = false;
            }
            if (model.CreditHours <= 0)
            {
                ModelState.AddModelError("CreditHours", "Credit hours must be greater than 0.");
                valid = false;
            }
            if (string.IsNullOrWhiteSpace(model.CourseType))
            {
                ModelState.AddModelError("CourseType", "Course type is required.");
                valid = false;
            }
            if (model.DeptId == 0)
            {
                ModelState.AddModelError("DeptId", "Department is required.");
                valid = false;
            }
            return valid;
        }
    }
}
