using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using FacultyManagementSystem.Models;

namespace FacultyManagementSystem.Controllers
{
    public class TimetableController : BaseController
    {
        public TimetableController(AppDbContext db) : base(db) { }

        private static readonly List<string> Days = new()
            { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };

        private IActionResult? Guard()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(role))
            {
                return RedirectToAction("Login", "Account");
            }
            return null; // all three roles allowed, just scoped differently
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

        // GET: /Timetable/Index
        public async Task<IActionResult> Index(int? semId, string? day)
        {
            var guard = Guard();
            if (guard != null)
            {
                return guard;
            }
            ViewData["Title"] = "Timetable";
            var role = HttpContext.Session.GetString("UserRole");
            // Faculty - read-only, own schedule only
            if (role == "Faculty")
            {
                var empId = HttpContext.Session.GetInt32("FacultyId");
                if (!empId.HasValue) return RedirectToAction("Login", "Account");

                var myTimetable = await _db.Timetables
                    .Include(t => t.Wa).ThenInclude(w => w.Emp)
                    .Include(t => t.Wa).ThenInclude(w => w.Course)
                    .Include(t => t.Wa).ThenInclude(w => w.Sem)
                    .Include(t => t.Slot)
                    .Where(t => !t.IsDeleted && !t.Wa.IsDeleted && t.Wa.EmpId == empId)
                    .OrderBy(t => t.Wa.Sem.SemName)
                    .ThenBy(t => t.DayOfWeek)
                    .ThenBy(t => t.Slot.StartTime)
                    .Select(t => new TimetableRow
                    {
                        TtId = t.TtId,
                        WaId = t.WaId,
                        FacultyName = t.Wa.Emp.Name,
                        EmpId = t.Wa.EmpId,
                        CourseTitle = t.Wa.Course.Title,
                        CourseCode = t.Wa.Course.CourseCode,
                        DeptName = t.Wa.Emp.Dept.DeptName,
                        SemId = t.Wa.SemId,
                        SemName = t.Wa.Sem.SemName,
                        DayOfWeek = t.DayOfWeek,
                        SlotId = t.SlotId,
                        SlotLabel = t.Slot.SlotLabel,
                        StartTime = t.Slot.StartTime,
                        Room = t.Room,
                        ConflictFlag = t.ConflictFlag,
                    })
                    .ToListAsync();

                ViewData["ReadOnly"] = true;
                await LoadFilterDropdowns();
                return View(myTimetable);
            }

            // Admin / HOD
            var hodDeptId = await GetHodDeptIdIfApplicable();
            var query = _db.Timetables
                .Include(t => t.Wa).ThenInclude(w => w.Emp).ThenInclude(f => f.Dept)
                .Include(t => t.Wa).ThenInclude(w => w.Course)
                .Include(t => t.Wa).ThenInclude(w => w.Sem)
                .Include(t => t.Slot)
                .Where(t => !t.IsDeleted && !t.Wa.IsDeleted)
                .AsQueryable();

            if (hodDeptId.HasValue)
            {
                query = query.Where(t => t.Wa.Emp.DeptId == hodDeptId);
            }
            if (semId.HasValue)
            {
                query = query.Where(t => t.Wa.SemId == semId);
            }
            if (!string.IsNullOrWhiteSpace(day))
            {
                query = query.Where(t => t.DayOfWeek == day);
            }
            var timetable = await query
                .OrderBy(t => t.Wa.Emp.Name)
                .ThenBy(t => t.DayOfWeek)
                .ThenBy(t => t.Slot.StartTime)
                .Select(t => new TimetableRow
                {
                    TtId = t.TtId,
                    WaId = t.WaId,
                    FacultyName = t.Wa.Emp.Name,
                    EmpId = t.Wa.EmpId,
                    CourseTitle = t.Wa.Course.Title,
                    CourseCode = t.Wa.Course.CourseCode,
                    DeptName = t.Wa.Emp.Dept.DeptName,
                    SemId = t.Wa.SemId,
                    SemName = t.Wa.Sem.SemName,
                    DayOfWeek = t.DayOfWeek,
                    SlotId = t.SlotId,
                    SlotLabel = t.Slot.SlotLabel,
                    StartTime = t.Slot.StartTime,
                    Room = t.Room,
                    ConflictFlag = t.ConflictFlag,
                })
                .ToListAsync();

            await LoadFilterDropdowns(hodDeptId);
            ViewData["ReadOnly"] = false;
            ViewData["HodLocked"] = hodDeptId.HasValue;
            ViewData["SelectedSem"] = semId;
            ViewData["SelectedDay"] = day;

            return View(timetable);
        }

        // GET: /Timetable/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var guard = Guard();
            if (guard != null)
            {
                return guard;
            }
            var role = HttpContext.Session.GetString("UserRole");
            if (role == "Faculty")
            {
                return RedirectToAction(nameof(Index));
            }
            ViewData["Title"] = "Timetable";
            var hodDeptId = await GetHodDeptIdIfApplicable();
            await LoadFormDropdowns(hodDeptId);
            return View(new Timetable());
        }

        // POST: /Timetable/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Timetable model)
        {
            var guard = Guard();
            if (guard != null)
            {
                return guard;
            }
            var role = HttpContext.Session.GetString("UserRole");
            if (role == "Faculty")
            {
                return RedirectToAction(nameof(Index));
            }
            ViewData["Title"] = "Timetable";
            ModelState.Remove("Wa");
            ModelState.Remove("Slot");
            var hodDeptId = await GetHodDeptIdIfApplicable();
            if (!await ValidateTimetable(model, 0, hodDeptId))
            {
                await LoadFormDropdowns(hodDeptId);
                return View(model);
            }
            // Detect conflict: same room + slot + day
            bool conflict = false;
            if (!string.IsNullOrWhiteSpace(model.Room))
            {
                conflict = await _db.Timetables.AnyAsync(t => t.SlotId == model.SlotId && t.DayOfWeek == model.DayOfWeek && t.Room == model.Room && !t.IsDeleted);
            }
            model.ConflictFlag = conflict;
            model.IsDeleted = false;
            _db.Timetables.Add(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Timetable entry added successfully." +
                (conflict ? " Note: this room/slot/day is already booked elsewhere — please review for conflicts." : "");
            return RedirectToAction(nameof(Index));
        }

        // GET: /Timetable/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var guard = Guard();
            if (guard != null)
            {
                return guard;
            }
            var role = HttpContext.Session.GetString("UserRole");
            if (role == "Faculty")
            {
                return RedirectToAction(nameof(Index));
            }
            ViewData["Title"] = "Timetable";
            var hodDeptId = await GetHodDeptIdIfApplicable();
            var tt = await _db.Timetables.Include(t => t.Wa).ThenInclude(w => w.Emp).FirstOrDefaultAsync(t => t.TtId == id && !t.IsDeleted);
            if (tt == null)
            {
                TempData["Error"] = "Timetable entry not found.";
                return RedirectToAction(nameof(Index));
            }
            if (hodDeptId.HasValue && tt.Wa.Emp.DeptId != hodDeptId)
            {
                TempData["Error"] = "You can only edit timetable entries in your own department.";
                return RedirectToAction(nameof(Index));
            }
            await LoadFormDropdowns(hodDeptId);
            return View(tt);
        }

        // POST: /Timetable/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Timetable model)
        {
            var guard = Guard();
            if (guard != null)
            {
                return guard;
            }
            var role = HttpContext.Session.GetString("UserRole");
            if (role == "Faculty")
            {
                return RedirectToAction(nameof(Index));
            }
            ViewData["Title"] = "Timetable";
            ModelState.Remove("Wa");
            ModelState.Remove("Slot");
            var hodDeptId = await GetHodDeptIdIfApplicable();
            var tt = await _db.Timetables.Include(t => t.Wa).ThenInclude(w => w.Emp).FirstOrDefaultAsync(t => t.TtId == id && !t.IsDeleted);
            if (tt == null)
            {
                TempData["Error"] = "Timetable entry not found.";
                return RedirectToAction(nameof(Index));
            }
            if (hodDeptId.HasValue && tt.Wa.Emp.DeptId != hodDeptId)
            {
                TempData["Error"] = "You can only edit timetable entries in your own department.";
                return RedirectToAction(nameof(Index));
            }
            model.WaId = tt.WaId;
            if (!await ValidateTimetable(model, id, hodDeptId))
            {
                await LoadFormDropdowns(hodDeptId);
                model.TtId = id;
                return View(model);
            }
            bool conflict = false;
            if (!string.IsNullOrWhiteSpace(model.Room))
            {
                conflict = await _db.Timetables.AnyAsync(t => t.SlotId == model.SlotId && t.DayOfWeek == model.DayOfWeek && t.Room == model.Room && t.TtId != id && !t.IsDeleted);
            }
            tt.DayOfWeek = model.DayOfWeek;
            tt.SlotId = model.SlotId;
            tt.Room = model.Room;
            tt.ConflictFlag = conflict;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Timetable entry updated successfully." +
                (conflict ? " Note: this room/slot/day is already booked elsewhere — please review for conflicts." : "");
            return RedirectToAction(nameof(Index));
        }

        // POST: /Timetable/Delete/5 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var guard = Guard();
            if (guard != null)
            {
                return guard;
            }
            var role = HttpContext.Session.GetString("UserRole");
            if (role == "Faculty")
            {
                return RedirectToAction(nameof(Index));
            }
            var hodDeptId = await GetHodDeptIdIfApplicable();
            var tt = await _db.Timetables.Include(t => t.Wa).ThenInclude(w => w.Emp).FirstOrDefaultAsync(t => t.TtId == id && !t.IsDeleted);
            if (tt == null)
            {
                TempData["Error"] = "Timetable entry not found.";
                return RedirectToAction(nameof(Index));
            }
            if (hodDeptId.HasValue && tt.Wa.Emp.DeptId != hodDeptId)
            {
                TempData["Error"] = "You can only delete timetable entries in your own department.";
                return RedirectToAction(nameof(Index));
            }
            tt.IsDeleted = true;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Timetable entry removed successfully.";
            return RedirectToAction(nameof(Index));
        }

        // HELPERS
        private async Task LoadFilterDropdowns(int? hodDeptId = null)
        {
            ViewBag.Semesters = await _db.Semesters
                .Where(s => !s.IsDeleted)
                .OrderByDescending(s => s.StartDate)
                .Select(s => new { s.SemId, s.SemName })
                .ToListAsync();

            ViewBag.Days = Days;
        }

        private async Task LoadFormDropdowns(int? hodDeptId)
        {
            // Assignments available to pick from
            var assignQuery = _db.WorkloadAssignments
                .Include(w => w.Emp)
                .Include(w => w.Course)
                .Include(w => w.Sem)
                .Where(w => w.Status == "Active" && !w.IsDeleted && !w.Sem.IsDeleted)
                .AsQueryable();

            if (hodDeptId.HasValue)
                assignQuery = assignQuery.Where(w => w.Emp.DeptId == hodDeptId);

            ViewBag.AssignmentList = await assignQuery
                .OrderBy(w => w.Sem.SemName)
                .ThenBy(w => w.Emp.Name)
                .Select(w => new
                {
                    w.WaId,
                    Display = w.Emp.Name + " \u2192 " + w.Course.CourseCode + " \u2192 " + w.Sem.SemName
                })
                .ToListAsync();

            ViewBag.TimeSlots = await _db.TimeSlots
                .OrderBy(s => s.StartTime)
                .Select(s => new { s.SlotId, s.SlotLabel })
                .ToListAsync();

            ViewBag.Days = Days;
        }

        private async Task<bool> ValidateTimetable(Timetable model, int excludeId, int? hodDeptId)
        {
            bool valid = true;
            if (model.WaId == 0)
            {
                ModelState.AddModelError("WaId", "Workload assignment is required.");
                valid = false;
            }
            else if (hodDeptId.HasValue)
            {
                var assignDept = await _db.WorkloadAssignments.Where(w => w.WaId == model.WaId).Select(w => (int?)w.Emp.DeptId).FirstOrDefaultAsync();
                if (assignDept != hodDeptId)
                {
                    ModelState.AddModelError("WaId", "You can only schedule assignments within your own department.");
                    valid = false;
                }
            }
            if (string.IsNullOrWhiteSpace(model.DayOfWeek) || !Days.Contains(model.DayOfWeek))
            {
                ModelState.AddModelError("DayOfWeek", "A valid day of week is required.");
                valid = false;
            }
            if (model.SlotId == 0)
            {
                ModelState.AddModelError("SlotId", "Time slot is required.");
                valid = false;
            }
            if (!valid) return false;

            // Duplicate slot check for this assignment — mirrors SlotExists
            var duplicate = await _db.Timetables.AnyAsync(t => t.WaId == model.WaId && t.DayOfWeek == model.DayOfWeek && t.SlotId == model.SlotId && t.TtId != excludeId && !t.IsDeleted);
            if (duplicate)
            {
                ModelState.AddModelError("SlotId", "This course already has a class scheduled in this slot on this day.");
                return false;
            }
            return true;
        }
    }

    public class TimetableRow
    {
        public int TtId { get; set; }
        public int WaId { get; set; }
        public string FacultyName { get; set; } = "";
        public int EmpId { get; set; }
        public string CourseTitle { get; set; } = "";
        public string CourseCode { get; set; } = "";
        public string DeptName { get; set; } = "";
        public int SemId { get; set; }
        public string SemName { get; set; } = "";
        public string DayOfWeek { get; set; } = "";
        public int SlotId { get; set; }
        public string SlotLabel { get; set; } = "";
        public TimeOnly StartTime { get; set; }
        public string? Room { get; set; }
        public bool ConflictFlag { get; set; }
    }
}