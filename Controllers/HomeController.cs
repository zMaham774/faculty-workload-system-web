using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using FacultyManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace FacultyManagementSystem.Controllers
{
    public class HomeController : BaseController
    {
        public HomeController(AppDbContext db) : base(db) { }

        public async Task<IActionResult> Index()
        {
            var role = HttpContext.Session.GetString("UserRole");
            var userName = HttpContext.Session.GetString("UserName");

            if (string.IsNullOrEmpty(role))
                return RedirectToAction("Login", "Account");

            ViewData["UserRole"] = role;
            ViewData["UserName"] = userName;
            ViewData["Title"] = "Dashboard";

            // Active semester
            var activeSemester = await _db.Semesters
                .Where(s => s.IsCurrent && !s.IsDeleted)
                .Select(s => s.SemName)
                .FirstOrDefaultAsync();

            ViewData["Semester"] = activeSemester ?? "Spring 2026";

            return role switch
            {
                "HOD" => await BuildHodDashboard(),
                "Faculty" => await BuildFacultyDashboard(),
                _ => await BuildAdminDashboard(),
            };
        }

        // ADMIN 
        private async Task<IActionResult> BuildAdminDashboard()
        {
            var vm = new AdminDashboardViewModel
            {
                TotalFaculty = await _db.Faculties.CountAsync(f => !f.IsDeleted),
                TotalDepartments = await _db.Departments.CountAsync(d => !d.IsDeleted),
                TotalCourses = await _db.Courses.CountAsync(c => !c.IsDeleted),
                PendingLeaves = await _db.LeaveRequests.CountAsync(l => l.ApprStatus == "Pending"),


                AttendancePresent = 87,
                AttendanceLeave = 8,
                AttendanceAbsent = 5,

                DepartmentWorkloads = await _db.Departments.Where(d => !d.IsDeleted).Select(d => new DeptWorkload
                {
                    DepartmentName = d.DeptName,
                    LoadPercent = d.Faculties
                    .Where(f => !f.IsDeleted)
                    .SelectMany(f => f.WorkloadAssignments.Where(w => !w.IsDeleted))
                    .Select(w => (double?)w.TotalHours)
                    .Average() ?? 0
                })
                .ToListAsync(),

                PendingLeaveRequests = await _db.LeaveRequests
                .Include(l => l.Emp)
                .ThenInclude(f => f.Dept)
                .Include(l => l.Lt)
                .Where(l => l.ApprStatus == "Pending")
                .OrderBy(l => l.SubmittedOn)
                .Take(5)
                .ToListAsync(),

                WorkloadChartLabels = await _db.Departments
                .Where(d => !d.IsDeleted)
                .Select(d => d.DeptName)
                .ToListAsync(),

                WorkloadChartData = await _db.Departments
                .Where(d => !d.IsDeleted)
                .Select(d => (double)(d.Faculties
                .Where(f => !f.IsDeleted)
                .SelectMany(f => f.WorkloadAssignments.Where(w => !w.IsDeleted))
                .Select(w => (double?)w.TotalHours)
                .Average() ?? 0))
                .ToListAsync(),
            };

            return View("AdminDashboard", vm);
        }

        // HOD 
        private async Task<IActionResult> BuildHodDashboard()
        {
            var empId = HttpContext.Session.GetInt32("FacultyId");
            var hod = empId.HasValue
                ? await _db.Faculties.Include(f => f.Dept).FirstOrDefaultAsync(f => f.EmpId == empId)
                : null;
            var deptId = hod?.DeptId ?? 0;

            var vm = new HodDashboardViewModel
            {
                DepartmentName = hod?.Dept?.DeptName ?? "Your Department",
                FacultyCount = await _db.Faculties.CountAsync(f => !f.IsDeleted && f.DeptId == deptId),
                CourseCount = await _db.Courses.CountAsync(c => !c.IsDeleted && c.DeptId == deptId),
                PendingLeaves = await _db.LeaveRequests.CountAsync(l => l.ApprStatus == "Pending" && l.Emp.DeptId == deptId),
                AvgAttendance = 91,

                FacultyStatus = await _db.Faculties
                .Where(f => !f.IsDeleted && f.DeptId == deptId)
                .Select(f => new FacultyStatusRow
                {
                    EmpId = f.EmpId,
                    FullName = f.Name,
                    CourseCount = f.WorkloadAssignments.Count(w => !w.IsDeleted),
                    CreditHours = (int)(f.WorkloadAssignments
                    .Where(w => !w.IsDeleted)
                    .Sum(w => (decimal?)w.TotalHours) ?? 0),
                    AttendancePct = 90,
                    IsPresent = true,
                })
                .ToListAsync(),

                WorkloadChartLabels = await _db.Faculties
                    .Where(f => !f.IsDeleted && f.DeptId == deptId)
                    .Select(f => f.Name)
                    .ToListAsync(),

                WorkloadChartData = await _db.Faculties
                    .Where(f => !f.IsDeleted && f.DeptId == deptId)
                    .Select(f => (double)(f.WorkloadAssignments
                        .Where(w => !w.IsDeleted)
                        .Sum(w => (int?)w.TotalHours) ?? 0))
                    .ToListAsync(),

                AttendanceTrendLabels = new List<string> { "Jan", "Feb", "Mar", "Apr", "May", "Jun" },
                AttendanceTrendData = new List<double> { 82, 85, 88, 87, 90, 91 },
            };

            return View("HodDashboard", vm);
        }

        // FACULTY
        private async Task<IActionResult> BuildFacultyDashboard()
        {
            var empId = HttpContext.Session.GetInt32("FacultyId");
            if (!empId.HasValue)
                return RedirectToAction("Login", "Account");

            var faculty = await _db.Faculties
                .Include(f => f.Dept)
                .Include(f => f.Designation)
                .FirstOrDefaultAsync(f => f.EmpId == empId);

            var vm = new FacultyDashboardViewModel
            {
                FullName = faculty?.Name ?? "Faculty",
                Designation = faculty?.Designation?.DesignationName ?? "",
                DepartmentName = faculty?.Dept?.DeptName ?? "",

                ActiveCourses = await _db.WorkloadAssignments
                    .CountAsync(w => w.EmpId == empId && !w.IsDeleted),

                CreditHours = (int)(await _db.WorkloadAssignments
                .Where(w => w.EmpId == empId && !w.IsDeleted)
                .SumAsync(w => (decimal?)w.TotalHours) ?? 0),

                AttendancePct = 94,

                LeavesTaken = await _db.LeaveRequests.CountAsync(l => l.EmpId == empId && l.ApprStatus == "Approved"),

                MyCourses = await _db.WorkloadAssignments
                .Include(w => w.Course)
                .Where(w => w.EmpId == empId && !w.IsDeleted)
                .Select(w => new MyCourseRow
                {
                    CourseCode = w.Course.CourseCode,
                    CourseName = w.Course.Title,        
                    CreditHours = (int)w.TotalHours,
                    AttendancePct = 92,
                })
                .ToListAsync(),



                AttendanceChartLabels = new List<string> { "Wk 1", "Wk 2", "Wk 3", "Wk 4", "Wk 5", "Wk 6", "Wk 7", "Wk 8" },
                AttendanceChartData = new List<double> { 100, 95, 90, 100, 95, 90, 95, 94 },

                WorkloadChartLabels = await _db.WorkloadAssignments
                .Include(w => w.Course)
                .Where(w => w.EmpId == empId && !w.IsDeleted)
                .Select(w => w.Course.Title)               
                .ToListAsync(),

                WorkloadChartData = await _db.WorkloadAssignments
                .Where(w => w.EmpId == empId && !w.IsDeleted)
                .Select(w => (double)w.TotalHours)
                .ToListAsync(),
            };

            return View("FacultyDashboard", vm);
        }
    }

    // VIEW MODELS 
    public class AdminDashboardViewModel
    {
        public int TotalFaculty { get; set; }
        public int TotalDepartments { get; set; }
        public int TotalCourses { get; set; }
        public int PendingLeaves { get; set; }
        public int AttendancePresent { get; set; }
        public int AttendanceLeave { get; set; }
        public int AttendanceAbsent { get; set; }
        public List<DeptWorkload> DepartmentWorkloads { get; set; } = new();
        public List<LeaveRequest> PendingLeaveRequests { get; set; } = new();
        public List<string> WorkloadChartLabels { get; set; } = new();
        public List<double> WorkloadChartData { get; set; } = new();
    }

    public class DeptWorkload
    {
        public string DepartmentName { get; set; } = "";
        public double LoadPercent { get; set; }
    }

    public class HodDashboardViewModel
    {
        public string DepartmentName { get; set; } = "";
        public int FacultyCount { get; set; }
        public int CourseCount { get; set; }
        public int PendingLeaves { get; set; }
        public int AvgAttendance { get; set; }
        public List<FacultyStatusRow> FacultyStatus { get; set; } = new();
        public List<string> WorkloadChartLabels { get; set; } = new();
        public List<double> WorkloadChartData { get; set; } = new();
        public List<string> AttendanceTrendLabels { get; set; } = new();
        public List<double> AttendanceTrendData { get; set; } = new();
    }

    public class FacultyStatusRow
    {
        public int EmpId { get; set; }
        public string FullName { get; set; } = "";
        public int CourseCount { get; set; }
        public int CreditHours { get; set; }
        public double AttendancePct { get; set; }
        public bool IsPresent { get; set; }
    }

    public class FacultyDashboardViewModel
    {
        public string FullName { get; set; } = "";
        public string Designation { get; set; } = "";
        public string DepartmentName { get; set; } = "";
        public int ActiveCourses { get; set; }
        public int CreditHours { get; set; }
        public double AttendancePct { get; set; }
        public int LeavesTaken { get; set; }
        public List<MyCourseRow> MyCourses { get; set; } = new();
        public List<string> AttendanceChartLabels { get; set; } = new();
        public List<double> AttendanceChartData { get; set; } = new();
        public List<string> WorkloadChartLabels { get; set; } = new();
        public List<double> WorkloadChartData { get; set; } = new();
    }

    public class MyCourseRow
    {
        public string CourseCode { get; set; } = "";
        public string CourseName { get; set; } = "";
        public int CreditHours { get; set; }
        public double AttendancePct { get; set; }
    }
}