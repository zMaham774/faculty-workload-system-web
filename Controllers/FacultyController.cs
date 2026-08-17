using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using FacultyManagementSystem.Models;
using MySqlConnector;

namespace FacultyManagementSystem.Controllers
{
    public class FacultyController : BaseController
    {
        public FacultyController(AppDbContext db) : base(db) { }

        private IActionResult? Guard(params string[] roles)
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(role))
            {
                return RedirectToAction("Login", "Account");
            }
            if (roles.Length > 0 && !roles.Contains(role))
            {
                return RedirectToAction("Index", "Home");
            }
            return null;
        }

        // INDEX 
        public async Task<IActionResult> Index(string? search, int? deptId, int? desigId)
        {
            var guard = Guard("Admin", "HOD");
            if (guard != null)
            {
                return guard;
            }
            ViewData["Title"] = "Faculty";
            // Use stored procedure for search 
            var faculty = await _db.VwFacultyDetails
                .Where(f => f.IsDeleted == false)
                .Where(f => string.IsNullOrEmpty(search) ||
                            f.Name.Contains(search) ||
                            f.DeptName.Contains(search) ||
                            f.DesignationName.Contains(search))
                .Where(f => deptId == null || f.DeptId == deptId)
                .Where(f => desigId == null || f.DesignationId == desigId)
                .OrderBy(f => f.Name)
                .ToListAsync();
            await LoadDropdowns(deptId, desigId);
            ViewData["Search"] = search;
            return View(faculty);
        }

        // CREATE GET 
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var guard = Guard("Admin", "HOD");
            if (guard != null)
            {
                return guard;
            }
            ViewData["Title"] = "Faculty";
            await LoadFormDropdowns();
            return View(new Faculty());
        }

        // CREATE POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Faculty model)
        {
            var guard = Guard("Admin", "HOD");
            if (guard != null)
            {
                return guard;
            }
            ViewData["Title"] = "Faculty";

            // Remove auto-validation on navigation properties not posted from the form
            ModelState.Remove("Dept");
            ModelState.Remove("Designation");
            ModelState.Remove("Emp");
            ModelState.Remove("Users");
            ModelState.Remove("WorkloadAssignments");
            ModelState.Remove("LeaveBalances");
            ModelState.Remove("LeaveRequests");
            ModelState.Remove("CourseReassignmentLogFromEmps");
            ModelState.Remove("CourseReassignmentLogToEmps");
            ModelState.Remove("FacultyChangeLogs");

            if (!await ValidateFaculty(model, 0))
            {
                await LoadFormDropdowns();
                return View(model);
            }
            model.IsActive = model.IsActive ?? true;
            model.IsDeleted = false;
            _db.Faculties.Add(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Faculty '{model.Name}' added successfully.";
            return RedirectToAction(nameof(Index));
        }

        // EDIT GET
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var guard = Guard("Admin", "HOD");
            if (guard != null)
            {
                return guard;
            }
            ViewData["Title"] = "Faculty";
            var faculty = await _db.Faculties.FirstOrDefaultAsync(f => f.EmpId == id && !f.IsDeleted);
            if (faculty == null)
            {
                TempData["Error"] = "Faculty member not found.";
                return RedirectToAction(nameof(Index));
            }
            await LoadFormDropdowns(faculty.DeptId, faculty.DesignationId);
            return View(faculty);
        }

        // EDIT POST 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Faculty model)
        {
            var guard = Guard("Admin", "HOD");
            if (guard != null)
            {
                return guard;
            }
            ViewData["Title"] = "Faculty";

            // Remove auto-validation on navigation properties not posted from the form
            ModelState.Remove("Dept");
            ModelState.Remove("Designation");
            ModelState.Remove("Emp");
            ModelState.Remove("Users");
            ModelState.Remove("WorkloadAssignments");
            ModelState.Remove("LeaveBalances");
            ModelState.Remove("LeaveRequests");
            ModelState.Remove("CourseReassignmentLogFromEmps");
            ModelState.Remove("CourseReassignmentLogToEmps");
            ModelState.Remove("FacultyChangeLogs");

            if (string.IsNullOrWhiteSpace(model.EmpType))
                model.EmpType = Request.Form["EmpType"].ToString();

            if (string.IsNullOrWhiteSpace(model.EmpType))
            {
                var existing = await _db.Faculties.AsNoTracking()
                    .Where(f => f.EmpId == id)
                    .Select(f => f.EmpType)
                    .FirstOrDefaultAsync();
                model.EmpType = existing ?? "";
            }

            if (!await ValidateFaculty(model, id))
            {
                await LoadFormDropdowns(model.DeptId, model.DesignationId);
                return View(model);
            }
            var faculty = await _db.Faculties.FirstOrDefaultAsync(f => f.EmpId == id && !f.IsDeleted);
            if (faculty == null)
            {
                TempData["Error"] = "Faculty member not found.";
                return RedirectToAction(nameof(Index));
            }
            string oldName = faculty.Name;
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            // Transaction
            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                // Set MySQL session variable for trigger
                // so trg_faculty_update_log knows who made the change
                await _db.Database.ExecuteSqlRawAsync($"SET @current_user_id = {userId}");
                // Update faculty
                faculty.Name = model.Name;
                faculty.DeptId = model.DeptId;
                faculty.DesignationId = model.DesignationId;
                faculty.EmpType = model.EmpType;
                faculty.Email = model.Email;
                faculty.Phone = model.Phone;
                faculty.Qualification = model.Qualification;
                faculty.IsActive = Request.Form["IsActive"].ToString().Contains("true");
                await _db.SaveChangesAsync();
                // Trigger trg_faculty_update_log fires automatically here

                // If name changed, sync hod_name in departments
                if (oldName != model.Name)
                {
                    await _db.Database.ExecuteSqlRawAsync(
                        "UPDATE departments SET hod_name = {0} WHERE hod_name = {1}",
                        model.Name, oldName);
                }
                await transaction.CommitAsync();
                TempData["Success"] = $"Faculty '{model.Name}' updated successfully.";
            }
            catch
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "An error occurred while updating. Please try again.";
            }
            return RedirectToAction(nameof(Index));
        }

        // DELETE (soft)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var guard = Guard("Admin", "HOD");
            if (guard != null)
            {
                return guard;
            }
            var faculty = await _db.Faculties.FirstOrDefaultAsync(f => f.EmpId == id && !f.IsDeleted);
            if (faculty == null)
            {
                TempData["Error"] = "Faculty member not found.";
                return RedirectToAction(nameof(Index));
            }
            // Block delete if faculty has workload assignments
            var hasAssignments = await _db.WorkloadAssignments.AnyAsync(w => w.EmpId == id);
            if (hasAssignments)
            {
                TempData["Error"] = $"Cannot delete '{faculty.Name}' — they have workload assignments. Remove assignments first.";
                return RedirectToAction(nameof(Index));
            }
            // Block delete if faculty is HOD of a department
            var isHod = await _db.Departments.AnyAsync(d => d.HodName == faculty.Name && !d.IsDeleted);
            if (isHod)
            {
                TempData["Error"] = $"Cannot delete '{faculty.Name}' — they are assigned as HOD of a department. Reassign the HOD first.";
                return RedirectToAction(nameof(Index));
            }
            faculty.IsDeleted = true;
            faculty.IsActive = false;
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Faculty '{faculty.Name}' deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        // ADD DESIGNATION
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDesignation(string designationName, int rankOrder)
        {
            var guard = Guard("Admin", "HOD");
            if (guard != null)
                return Json(new { success = false, message = "Unauthorized" });

            if (string.IsNullOrWhiteSpace(designationName))
                return Json(new { success = false, message = "Designation name is required." });

            var exists = await _db.Designations.AnyAsync(d => d.DesignationName == designationName);
            if (exists)
                return Json(new { success = false, message = "This designation already exists." });

            var desig = new Designation
            {
                DesignationName = designationName,
                RankOrder = rankOrder > 0 ? rankOrder : 1
            };
            _db.Designations.Add(desig);
            await _db.SaveChangesAsync();
            return Json(new
            {
                success = true,
                designationId = desig.DesignationId,
                designationName = desig.DesignationName
            });
        }

        // HELPERS
        private async Task LoadDropdowns(int? deptId = null, int? desigId = null)
        {
            ViewBag.Departments = await _db.Departments
                .Where(d => !d.IsDeleted && d.IsActive == true)
                .OrderBy(d => d.DeptName)
                .Select(d => new { d.DeptId, d.DeptName })
                .ToListAsync();

            ViewBag.Designations = await _db.Designations
                .OrderBy(d => d.RankOrder)
                .Select(d => new { d.DesignationId, d.DesignationName })
                .ToListAsync();

            ViewBag.SelectedDept = deptId;
            ViewBag.SelectedDesig = desigId;
        }

        private async Task LoadFormDropdowns(int? deptId = null, int? desigId = null)
        {
            ViewBag.Departments = await _db.Departments
                .Where(d => !d.IsDeleted && d.IsActive == true)
                .OrderBy(d => d.DeptName)
                .Select(d => new { d.DeptId, d.DeptName })
                .ToListAsync();

            ViewBag.Designations = await _db.Designations
                .OrderBy(d => d.RankOrder)
                .Select(d => new { d.DesignationId, d.DesignationName })
                .ToListAsync();

            ViewBag.EmpTypes = new List<string>
                { "Permanent", "Visiting", "Contract" };

            ViewBag.SelectedDept = deptId;
            ViewBag.SelectedDesig = desigId;
        }

        private async Task<bool> ValidateFaculty(Faculty model, int excludeId)
        {
            bool valid = true;
            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError("Name", "Name is required.");
                valid = false;
            }
            else
            {
                var nameExists = await _db.Faculties.AnyAsync(f => f.Name == model.Name && f.EmpId != excludeId && !f.IsDeleted);
                if (nameExists)
                {
                    ModelState.AddModelError("Name",
                        "A faculty member with this name already exists.");
                    valid = false;
                }
            }
            if (model.DeptId == 0)
            {
                ModelState.AddModelError("DeptId", "Department is required.");
                valid = false;
            }
            if (model.DesignationId == 0)
            {
                ModelState.AddModelError("DesignationId", "Designation is required.");
                valid = false;
            }
            if (string.IsNullOrWhiteSpace(model.EmpType))
            {
                ModelState.AddModelError("EmpType", "Employee type is required.");
                valid = false;
            }
            return valid;
        }
    }
}