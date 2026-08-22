using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using FacultyManagementSystem.Models;

namespace FacultyManagementSystem.Controllers
{
    public class WorkloadController : BaseController
    {
        public WorkloadController(AppDbContext db) : base(db) { }

        private IActionResult? Guard()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(role))
            {
                return RedirectToAction("Login", "Account");
            }
            if (role != "Admin" && role != "HOD" && role != "Faculty")
            {
                return RedirectToAction("Index", "Home");
            }
            return null;
        }

        private IActionResult? GuardManage()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(role))
            {
                return RedirectToAction("Login", "Account");
            }
            if (role != "Admin" && role != "HOD")
            {
                return RedirectToAction("Index", nameof(Index));
            }
            return null;
        }

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
            return await _db.Faculties.Where(f => f.EmpId == empId && !f.IsDeleted).Select(f => (int?)f.DeptId).FirstOrDefaultAsync();
        }

        // GET: /Workload/Index
        public async Task<IActionResult> Index(int? semId, int? deptId)
        {
            var guard = Guard();
            if (guard != null)
            {
                return guard;
            }
            ViewData["Title"] = "Workload";
            var role = HttpContext.Session.GetString("UserRole");
            // Faculty - read-only, own assignments only
            if (role == "Faculty")
            {
                var myEmpId = HttpContext.Session.GetInt32("FacultyId");
                if (!myEmpId.HasValue)
                {
                    return RedirectToAction("Login", "Account");
                }
                var activeSemIdForFaculty = semId ?? await _db.Semesters.Where(s => s.IsCurrent && !s.IsDeleted).Select(s => (int?)s.SemId).FirstOrDefaultAsync();
                var myQuery = _db.WorkloadAssignments
                    .Include(w => w.Emp).ThenInclude(f => f.Dept)
                    .Include(w => w.Course)
                    .Include(w => w.Sem)
                    .Where(w => !w.IsDeleted && w.EmpId == myEmpId &&
                                !w.Emp.IsDeleted && !w.Course.IsDeleted && !w.Sem.IsDeleted)
                    .AsQueryable();

                if (activeSemIdForFaculty.HasValue)
                {
                    myQuery = myQuery.Where(w => w.SemId == activeSemIdForFaculty);
                }
                var myAssignments = await myQuery.OrderBy(w => w.Course.Title).Select(w => new WorkloadRow{
                        WaId = w.WaId,
                        EmpId = w.EmpId,
                        FacultyName = w.Emp.Name,
                        CourseId = w.CourseId,
                        CourseTitle = w.Course.Title,
                        CourseCode = w.Course.CourseCode,
                        CreditHours = w.Course.CreditHours,
                        DeptName = w.Emp.Dept.DeptName,
                        DeptId = w.Emp.DeptId,
                        SemId = w.SemId,
                        SemName = w.Sem.SemName,
                        TotalHours = w.TotalHours,
                        Status = w.Status,
                        AssignedDate = w.AssignedDate, 
                }).ToListAsync();

                ViewBag.Semesters = await _db.Semesters.Where(s => !s.IsDeleted).OrderByDescending(s => s.StartDate).Select(s => new { s.SemId, s.SemName }).ToListAsync();
                ViewData["ReadOnly"] = true;
                ViewData["SelectedSem"] = activeSemIdForFaculty;
                return View(myAssignments);
            }
            // Admin / HOD
            var hodDeptId = await GetHodDeptIdIfApplicable();
            // Default to current semester if none specified
            var activeSemId = semId ?? await _db.Semesters.Where(s => s.IsCurrent && !s.IsDeleted).Select(s => (int?)s.SemId).FirstOrDefaultAsync();

            var query = _db.WorkloadAssignments
                .Include(w => w.Emp).ThenInclude(f => f.Dept)
                .Include(w => w.Course)
                .Include(w => w.Sem)
                .Where(w => !w.IsDeleted &&
                            !w.Emp.IsDeleted &&
                            !w.Course.IsDeleted &&
                            !w.Sem.IsDeleted)
                .AsQueryable();

            if (activeSemId.HasValue)
            {
                query = query.Where(w => w.SemId == activeSemId);
            }
            if (hodDeptId.HasValue)
            {
                query = query.Where(w => w.Emp.DeptId == hodDeptId);
            }
            else if (deptId.HasValue)
            {
                query = query.Where(w => w.Emp.DeptId == deptId);
            }
            var assignments = await query
                .OrderBy(w => w.Emp.Name)
                .ThenBy(w => w.Course.Title)
                .Select(w => new WorkloadRow
                {
                    WaId = w.WaId,
                    EmpId = w.EmpId,
                    FacultyName = w.Emp.Name,
                    CourseId = w.CourseId,
                    CourseTitle = w.Course.Title,
                    CourseCode = w.Course.CourseCode,
                    CreditHours = w.Course.CreditHours,
                    DeptName = w.Emp.Dept.DeptName,
                    DeptId = w.Emp.DeptId,
                    SemId = w.SemId,
                    SemName = w.Sem.SemName,
                    TotalHours = w.TotalHours,
                    Status = w.Status,
                    AssignedDate = w.AssignedDate,
                })
                .ToListAsync();

            await LoadFilterDropdowns(hodDeptId);

            // Reassign modal needs faculty grouped by department
            var facultyQuery = _db.Faculties.Where(f => !f.IsDeleted && f.IsActive == true).AsQueryable();
            if (hodDeptId.HasValue)
            {
                facultyQuery = facultyQuery.Where(f => f.DeptId == hodDeptId);
            }
            ViewBag.FacultyList = await facultyQuery.OrderBy(f => f.Name).Select(f => new { f.EmpId, f.Name, f.DeptId }).ToListAsync();
            ViewData["SelectedSem"] = activeSemId;
            ViewData["SelectedDept"] = deptId;
            ViewData["HodLocked"] = hodDeptId.HasValue;
            ViewData["ReadOnly"] = false;
            return View(assignments);
        }

        // GET: /Workload/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var guard = GuardManage();
            if (guard != null)
            {
                return guard;
            }
            ViewData["Title"] = "Workload";
            var hodDeptId = await GetHodDeptIdIfApplicable();
            await LoadFormDropdowns(hodDeptId);
            return View(new WorkloadAssignment { Status = "Active" });
        }

        // POST: /Workload/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(WorkloadAssignment model)
        {
            var guard = GuardManage();
            if (guard != null)
            {
                return guard;
            }
            ViewData["Title"] = "Workload";
            ModelState.Remove("Emp");
            ModelState.Remove("Course");
            ModelState.Remove("Sem");
            ModelState.Remove("AttendanceRecords");
            ModelState.Remove("Timetables");
            var hodDeptId = await GetHodDeptIdIfApplicable();
            // Enforce HOD can only assign within own department
            if (hodDeptId.HasValue)
            {
                var facultyDept = await _db.Faculties.Where(f => f.EmpId == model.EmpId).Select(f => (int?)f.DeptId).FirstOrDefaultAsync();
                if (facultyDept != hodDeptId)
                {
                    TempData["Error"] = "You can only assign workload to faculty in your own department.";
                    return RedirectToAction(nameof(Index));
                }
            }
            var (valid, warning) = await ValidateAssignment(model, 0);
            if (!valid)
            {
                await LoadFormDropdowns(hodDeptId);
                return View(model);
            }
            model.IsDeleted = false;
            model.AssignedDate = DateOnly.FromDateTime(DateTime.Now);
            _db.WorkloadAssignments.Add(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Workload assignment created successfully." +
                (warning != null ? " " + warning : "");
            return RedirectToAction(nameof(Index));
        }

        // GET: /Workload/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var guard = GuardManage();
            if (guard != null)
            {
                return guard;
            }
            ViewData["Title"] = "Workload";
            var hodDeptId = await GetHodDeptIdIfApplicable();
            var wa = await _db.WorkloadAssignments.Include(w => w.Emp).FirstOrDefaultAsync(w => w.WaId == id && !w.IsDeleted);
            if (wa == null)
            {
                TempData["Error"] = "Assignment not found.";
                return RedirectToAction(nameof(Index));
            }
            if (hodDeptId.HasValue && wa.Emp.DeptId != hodDeptId)
            {
                TempData["Error"] = "You can only edit assignments in your own department.";
                return RedirectToAction(nameof(Index));
            }
            await LoadFormDropdowns(hodDeptId);
            return View(wa);
        }

        // POST: /Workload/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, WorkloadAssignment model)
        {
            var guard = GuardManage();
            if (guard != null)
            {
                return guard;
            }
            ViewData["Title"] = "Workload";
            ModelState.Remove("Emp");
            ModelState.Remove("Course");
            ModelState.Remove("Sem");
            ModelState.Remove("AttendanceRecords");
            ModelState.Remove("Timetables");
            var hodDeptId = await GetHodDeptIdIfApplicable();
            var wa = await _db.WorkloadAssignments.Include(w => w.Emp).FirstOrDefaultAsync(w => w.WaId == id && !w.IsDeleted);
            if (wa == null)
            {
                TempData["Error"] = "Assignment not found.";
                return RedirectToAction(nameof(Index));
            }
            if (hodDeptId.HasValue && wa.Emp.DeptId != hodDeptId)
            {
                TempData["Error"] = "You can only edit assignments in your own department.";
                return RedirectToAction(nameof(Index));
            }
            // Emp/Course/Sem are not editable
            model.EmpId = wa.EmpId;
            model.CourseId = wa.CourseId;
            model.SemId = wa.SemId;
            var (valid, warning) = await ValidateAssignment(model, id);
            if (!valid)
            {
                await LoadFormDropdowns(hodDeptId);
                model.WaId = id;
                return View(model);
            }
            wa.TotalHours = model.TotalHours;
            wa.Status = model.Status;
            wa.AssignedDate = model.AssignedDate;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Workload assignment updated successfully." +
                (warning != null ? " " + warning : "");
            return RedirectToAction(nameof(Index));
        }

        // POST: /Workload/Delete/5 (soft delete)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var guard = GuardManage();
            if (guard != null)
            {
                return guard;
            }
            var hodDeptId = await GetHodDeptIdIfApplicable();
            var wa = await _db.WorkloadAssignments.Include(w => w.Emp).FirstOrDefaultAsync(w => w.WaId == id && !w.IsDeleted);
            if (wa == null)
            {
                TempData["Error"] = "Assignment not found.";
                return RedirectToAction(nameof(Index));
            }
            if (hodDeptId.HasValue && wa.Emp.DeptId != hodDeptId)
            {
                TempData["Error"] = "You can only delete assignments in your own department.";
                return RedirectToAction(nameof(Index));
            }
            // Block delete if timetable/attendance already reference this assignment
            var hasRelated = await _db.Timetables.AnyAsync(t => t.WaId == id) || await _db.AttendanceRecords.AnyAsync(a => a.WaId == id);
            if (hasRelated)
            {
                TempData["Error"] = "Cannot delete — this assignment has related timetable or attendance records.";
                return RedirectToAction(nameof(Index));
            }
            wa.IsDeleted = true;
            wa.Status = "Dropped";
            await _db.SaveChangesAsync();
            TempData["Success"] = "Workload assignment deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Workload/Reassign/5 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reassign(int waId, int toEmpId, string? reason)
        {
            var guard = GuardManage();
            if (guard != null)
            {
                return guard;
            }
            var hodDeptId = await GetHodDeptIdIfApplicable();
            var wa = await _db.WorkloadAssignments.Include(w => w.Emp).FirstOrDefaultAsync(w => w.WaId == waId && !w.IsDeleted);
            if (wa == null)
            {
                TempData["Error"] = "Assignment not found.";
                return RedirectToAction(nameof(Index));
            }
            if (hodDeptId.HasValue && wa.Emp.DeptId != hodDeptId)
            {
                TempData["Error"] = "You can only reassign courses within your own department.";
                return RedirectToAction(nameof(Index));
            }
            // New faculty must be in the same department scope for HOD
            if (hodDeptId.HasValue)
            {
                var toDept = await _db.Faculties.Where(f => f.EmpId == toEmpId).Select(f => (int?)f.DeptId).FirstOrDefaultAsync();
                if (toDept != hodDeptId)
                {
                    TempData["Error"] = "You can only reassign to faculty within your own department.";
                    return RedirectToAction(nameof(Index));
                }
            }
            // Prevent reassigning to the same faculty
            if (toEmpId == wa.EmpId)
            {
                TempData["Error"] = "This course is already assigned to that faculty member.";
                return RedirectToAction(nameof(Index));
            }
            // Check for duplicate assignment for target faculty
            var duplicate = await _db.WorkloadAssignments
                .AnyAsync(w => w.EmpId == toEmpId &&
                               w.CourseId == wa.CourseId &&
                               w.SemId == wa.SemId &&
                               !w.IsDeleted &&
                               w.WaId != waId);
            if (duplicate)
            {
                TempData["Error"] = "The target faculty member already has this course assigned this semester.";
                return RedirectToAction(nameof(Index));
            }
            int fromEmpId = wa.EmpId;
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                // update assignment to new faculty
                wa.EmpId = toEmpId;
                await _db.SaveChangesAsync();

                // log the reassignment
                var log = new CourseReassignmentLog
                {
                    CourseId = wa.CourseId,
                    SemId = wa.SemId,
                    FromEmpId = fromEmpId,
                    ToEmpId = toEmpId,
                    Reason = string.IsNullOrWhiteSpace(reason) ? null : reason,
                    ReassignedOn = DateTime.Now,
                    ReassignedBy = userId,
                };
                _db.CourseReassignmentLogs.Add(log);
                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
                TempData["Success"] = "Course reassigned successfully.";
            }
            catch
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "Failed to reassign course. Please try again.";
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: /Workload/Standards 
        [HttpGet]
        public async Task<IActionResult> Standards()
        {
            var guard = GuardManage();
            if (guard != null)
            {
                return guard;
            }
            var hodDeptId = await GetHodDeptIdIfApplicable();
            var query = _db.WorkloadStandards
                .Include(w => w.Dept)
                .Include(w => w.Sem)
                .Where(w => !w.Dept.IsDeleted)
                .AsQueryable();
            if (hodDeptId.HasValue)
            {
                query = query.Where(w => w.DeptId == hodDeptId);
            }
            var standards = await query
                .OrderBy(w => w.Dept.DeptName)
                .Select(w => new
                {
                    w.WsId,
                    w.DeptId,
                    DeptName = w.Dept.DeptName,
                    w.SemId,
                    SemName = w.Sem.SemName,
                    w.MinHours,
                    w.MaxHours,
                    w.StdHours
                })
                .ToListAsync();
            return Json(standards);
        }

        // POST: /Workload/SaveStandard
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveStandard(int wsId, int deptId, int semId, int minHours, int maxHours, int stdHours)
        {
            var guard = GuardManage();
            if (guard != null)
                return Json(new { success = false, message = "Unauthorized" });

            var hodDeptId = await GetHodDeptIdIfApplicable();
            if (hodDeptId.HasValue && deptId != hodDeptId)
                return Json(new { success = false, message = "You can only manage standards for your own department." });

            if (minHours <= 0 || maxHours <= 0 || stdHours <= 0)
                return Json(new { success = false, message = "All hour values must be greater than 0." });

            if (minHours > maxHours)
                return Json(new { success = false, message = "Minimum hours cannot exceed maximum hours." });

            if (stdHours < minHours || stdHours > maxHours)
                return Json(new { success = false, message = "Standard hours must be between min and max." });

            if (wsId == 0)
            {
                // Insert - check duplicate dept+sem combo
                var exists = await _db.WorkloadStandards
                    .AnyAsync(w => w.DeptId == deptId && w.SemId == semId);
                if (exists)
                    return Json(new { success = false, message = "A standard already exists for this department and semester." });

                var ws = new WorkloadStandard
                {
                    DeptId = deptId,
                    SemId = semId,
                    MinHours = minHours,
                    MaxHours = maxHours,
                    StdHours = stdHours,
                };
                _db.WorkloadStandards.Add(ws);
            }
            else
            {
                var ws = await _db.WorkloadStandards.FirstOrDefaultAsync(w => w.WsId == wsId);
                if (ws == null)
                    return Json(new { success = false, message = "Standard not found." });

                var exists = await _db.WorkloadStandards
                    .AnyAsync(w => w.DeptId == deptId && w.SemId == semId && w.WsId != wsId);
                if (exists)
                    return Json(new { success = false, message = "A standard already exists for this department and semester." });

                ws.DeptId = deptId;
                ws.SemId = semId;
                ws.MinHours = minHours;
                ws.MaxHours = maxHours;
                ws.StdHours = stdHours;
            }

            await _db.SaveChangesAsync();
            return Json(new { success = true });
        }

        // POST: /Workload/DeleteStandard
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteStandard(int wsId)
        {
            var guard = GuardManage();
            if (guard != null)
                return Json(new { success = false, message = "Unauthorized" });

            var ws = await _db.WorkloadStandards.FirstOrDefaultAsync(w => w.WsId == wsId);
            if (ws == null)
                return Json(new { success = false, message = "Standard not found." });

            var hodDeptId = await GetHodDeptIdIfApplicable();
            if (hodDeptId.HasValue && ws.DeptId != hodDeptId)
                return Json(new { success = false, message = "You can only manage standards for your own department." });

            _db.WorkloadStandards.Remove(ws);
            await _db.SaveChangesAsync();
            return Json(new { success = true });
        }

        // HELPERS

        private async Task LoadFilterDropdowns(int? hodDeptId)
        {
            ViewBag.Semesters = await _db.Semesters
                .Where(s => !s.IsDeleted)
                .OrderByDescending(s => s.StartDate)
                .Select(s => new { s.SemId, s.SemName })
                .ToListAsync();

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
                    .Where(d => !d.IsDeleted)
                    .OrderBy(d => d.DeptName)
                    .Select(d => new { d.DeptId, d.DeptName })
                    .ToListAsync();
            }
        }

        private async Task LoadFormDropdowns(int? hodDeptId)
        {
            var facultyQuery = _db.Faculties.Where(f => !f.IsDeleted && f.IsActive == true).AsQueryable();

            if (hodDeptId.HasValue)
            {
                facultyQuery = facultyQuery.Where(f => f.DeptId == hodDeptId);
            }
            ViewBag.FacultyList = await facultyQuery.OrderBy(f => f.Name).Select(f => new { f.EmpId, f.Name, f.DeptId }).ToListAsync();
            var courseQuery = _db.Courses.Where(c => !c.IsDeleted && c.IsActive == true).AsQueryable();
            if (hodDeptId.HasValue)
            {
                courseQuery = courseQuery.Where(c => c.DeptId == hodDeptId);
            }
            ViewBag.CourseList = await courseQuery
                .OrderBy(c => c.CourseCode)
                .Select(c => new { c.CourseId, c.CourseCode, c.Title, c.CreditHours, c.DeptId })
                .ToListAsync();

            ViewBag.Semesters = await _db.Semesters
                .Where(s => !s.IsDeleted)
                .OrderByDescending(s => s.StartDate)
                .Select(s => new { s.SemId, s.SemName, s.IsCurrent })
                .ToListAsync();

            ViewBag.StatusOptions = new List<string> { "Active", "Dropped", "Substituted" };
        }

        // Returns (isValid, warningMessage)
        // Blocks if exceeds MaxHours, warns (but allows) if exceeds StdHours
        private async Task<(bool valid, string? warning)> ValidateAssignment(WorkloadAssignment model, int excludeId)
        {
            if (model.EmpId == 0)
            {
                ModelState.AddModelError("EmpId", "Faculty member is required.");
                return (false, null);
            }
            if (model.CourseId == 0)
            {
                ModelState.AddModelError("CourseId", "Course is required.");
                return (false, null);
            }
            if (model.SemId == 0)
            {
                ModelState.AddModelError("SemId", "Semester is required.");
                return (false, null);
            }
            if (model.TotalHours <= 0)
            {
                ModelState.AddModelError("TotalHours", "Total hours must be greater than 0.");
                return (false, null);
            }

            // Duplicate assignment check
            var duplicate = await _db.WorkloadAssignments
                .AnyAsync(w => w.EmpId == model.EmpId &&
                               w.CourseId == model.CourseId &&
                               w.SemId == model.SemId &&
                               w.WaId != excludeId &&
                               !w.IsDeleted);
            if (duplicate)
            {
                ModelState.AddModelError("CourseId", "This faculty member is already assigned to this course this semester.");
                return (false, null);
            }

            // Get faculty's department for standards lookup
            var deptId = await _db.Faculties
                .Where(f => f.EmpId == model.EmpId)
                .Select(f => f.DeptId)
                .FirstOrDefaultAsync();

            var standard = await _db.WorkloadStandards
                .FirstOrDefaultAsync(w => w.DeptId == deptId && w.SemId == model.SemId);

            if (standard != null)
            {
                // Sum existing active hours (excluding this assignment if editing)
                var existingHours = await _db.WorkloadAssignments
                    .Where(w => w.EmpId == model.EmpId &&
                                w.SemId == model.SemId &&
                                w.Status == "Active" &&
                                !w.IsDeleted &&
                                w.WaId != excludeId)
                    .SumAsync(w => (decimal?)w.TotalHours) ?? 0;

                var projectedTotal = existingHours + model.TotalHours;

                // HARD BLOCK if exceeds max
                if (projectedTotal > standard.MaxHours)
                {
                    ModelState.AddModelError("TotalHours",
                        $"This assignment would bring the faculty's total to {projectedTotal} hours, exceeding the maximum of {standard.MaxHours} hours for this department. Reduce the hours or reassign.");
                    return (false, null);
                }

                // SOFT WARNING if exceeds standard (but still within max)
                if (projectedTotal > standard.StdHours)
                {
                    return (true, $"Note: this brings the faculty's total to {projectedTotal} hours, above the standard of {standard.StdHours} (max {standard.MaxHours}).");
                }
            }

            return (true, null);
        }
    }

    public class WorkloadRow
    {
        public int WaId { get; set; }
        public int EmpId { get; set; }
        public string FacultyName { get; set; } = "";
        public int CourseId { get; set; }
        public string CourseTitle { get; set; } = "";
        public string CourseCode { get; set; } = "";
        public int CreditHours { get; set; }
        public string DeptName { get; set; } = "";
        public int DeptId { get; set; }
        public int SemId { get; set; }
        public string SemName { get; set; } = "";
        public decimal TotalHours { get; set; }
        public string Status { get; set; } = "";
        public DateOnly AssignedDate { get; set; }
    }
}