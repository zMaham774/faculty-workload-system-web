using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using FacultyManagementSystem.Models;
using System.Security.Cryptography;
using System.Text;

namespace FacultyManagementSystem.Controllers
{
    public class UsersController : BaseController
    {
        public UsersController(AppDbContext db) : base(db) { }

        private IActionResult? Guard()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(role))
                return RedirectToAction("Login", "Account");
            if (role != "Admin")
                return RedirectToAction("Index", "Home");
            return null;
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToHexString(hash).ToLower();
        }

        // GET: /Users/Index
        public async Task<IActionResult> Index(string? search)
        {
            var guard = Guard();
            if (guard != null) return guard;
            ViewData["Title"] = "Users";

            var query = _db.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(u => u.Username.Contains(search) ||
                                          u.Role.Contains(search));

            var users = await query
                .OrderBy(u => u.Username)
                .Select(u => new UserListRow
                {
                    UserId = u.UserId,
                    Username = u.Username,
                    Role = u.Role,
                    IsActive = u.IsActive,
                    LastLogin = u.LastLogin,
                    CreatedAt = u.CreatedAt,
                    FacultyName = u.Emp != null ? u.Emp.Name : null,
                })
                .ToListAsync();

            // Filter by faculty name too if search matches (can't be done in the same query above cleanly)
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower();
                users = users.Where(u =>
                    u.Username.ToLower().Contains(searchLower) ||
                    u.Role.ToLower().Contains(searchLower) ||
                    (u.FacultyName != null && u.FacultyName.ToLower().Contains(searchLower))
                ).ToList();
            }

            ViewData["Search"] = search;
            return View(users);
        }

        // GET: /Users/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var guard = Guard();
            if (guard != null) return guard;
            ViewData["Title"] = "Users";
            await LoadFacultyDropdown();
            return View(new User());
        }

        // POST: /Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(User model, string password)
        {
            var guard = Guard();
            if (guard != null) return guard;
            ViewData["Title"] = "Users";

            ModelState.Remove("Emp");
            ModelState.Remove("AcademicCalendars");
            ModelState.Remove("CourseReassignmentLogs");
            ModelState.Remove("FacultyChangeLogs");
            ModelState.Remove("LeaveRequests");
            ModelState.Remove("Password");
            ModelState.Remove("CreatedAt");

            if (!await ValidateUser(model, password, 0, true))
            {
                await LoadFacultyDropdown();
                return View(model);
            }

            var user = new User
            {
                Username = model.Username,
                Password = HashPassword(password),
                Role = model.Role,
                EmpId = model.EmpId,
                IsActive = model.IsActive ?? true,
                CreatedAt = DateTime.Now,
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"User '{user.Username}' created successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Users/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var guard = Guard();
            if (guard != null) return guard;
            ViewData["Title"] = "Users";

            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == id);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction(nameof(Index));
            }

            await LoadFacultyDropdown();
            return View(user);
        }

        // POST: /Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, User model, string? newPassword)
        {
            var guard = Guard();
            if (guard != null) return guard;
            ViewData["Title"] = "Users";

            ModelState.Remove("Emp");
            ModelState.Remove("AcademicCalendars");
            ModelState.Remove("CourseReassignmentLogs");
            ModelState.Remove("FacultyChangeLogs");
            ModelState.Remove("LeaveRequests");
            ModelState.Remove("Password");
            ModelState.Remove("CreatedAt");

            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == id);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction(nameof(Index));
            }

            // Only validate password if one was provided (optional on edit)
            bool changingPassword = !string.IsNullOrWhiteSpace(newPassword);

            if (!await ValidateUser(model, newPassword, id, changingPassword))
            {
                await LoadFacultyDropdown();
                model.UserId = id;
                return View(model);
            }

            user.Username = model.Username;
            user.Role = model.Role;
            user.EmpId = model.EmpId;
            user.IsActive = Request.Form["IsActive"].ToString().Contains("true");

            if (changingPassword)
                user.Password = HashPassword(newPassword!);

            await _db.SaveChangesAsync();

            TempData["Success"] = $"User '{user.Username}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Users/Delete/5 — HARD delete, mirrors WinForms (no soft-delete column on users)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var guard = Guard();
            if (guard != null) return guard;

            var currentUserId = HttpContext.Session.GetInt32("UserId");

            // Cannot delete your own account — mirrors WinForms check
            if (id == currentUserId)
            {
                TempData["Error"] = "You cannot delete your own account while logged in.";
                return RedirectToAction(nameof(Index));
            }

            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == id);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction(nameof(Index));
            }

            _db.Users.Remove(user);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"User '{user.Username}' deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        // ── HELPERS ────────────────────────────────────────────
        private async Task LoadFacultyDropdown()
        {
            ViewBag.FacultyList = await _db.Faculties
                .Where(f => !f.IsDeleted && f.IsActive == true)
                .OrderBy(f => f.Name)
                .Select(f => new { f.EmpId, f.Name })
                .ToListAsync();

            // Faculty members who are designated as hod_name on some department
            var hodNames = await _db.Departments
                .Where(d => !d.IsDeleted && d.HodName != null)
                .Select(d => d.HodName)
                .ToListAsync();

            ViewBag.HodFacultyList = await _db.Faculties
                .Where(f => !f.IsDeleted && f.IsActive == true && hodNames.Contains(f.Name))
                .OrderBy(f => f.Name)
                .Select(f => new { f.EmpId, f.Name })
                .ToListAsync();

            ViewBag.Roles = new List<string> { "Admin", "HOD", "Faculty" };
        }

        private async Task<bool> ValidateUser(User model, string? password, int excludeId, bool passwordRequired)
        {
            bool valid = true;

            if (string.IsNullOrWhiteSpace(model.Username))
            {
                ModelState.AddModelError("Username", "Username is required.");
                valid = false;
            }
            else
            {
                var usernameExists = await _db.Users
                    .AnyAsync(u => u.Username == model.Username && u.UserId != excludeId);
                if (usernameExists)
                {
                    ModelState.AddModelError("Username", "This username is already taken.");
                    valid = false;
                }
            }

            if (passwordRequired && string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("Password", "Password is required.");
                valid = false;
            }
            else if (!string.IsNullOrWhiteSpace(password) && password.Length < 6)
            {
                ModelState.AddModelError("Password", "Password must be at least 6 characters.");
                valid = false;
            }

            if (string.IsNullOrWhiteSpace(model.Role))
            {
                ModelState.AddModelError("Role", "Role is required.");
                valid = false;
            }

            // HOD and Faculty roles require a linked faculty account
            if ((model.Role == "HOD" || model.Role == "Faculty") && model.EmpId == null)
            {
                ModelState.AddModelError("EmpId", $"A faculty member must be linked for the {model.Role} role.");
                valid = false;
            }

            if (model.EmpId.HasValue)
            {
                // Faculty already has a user account
                var facultyHasUser = await _db.Users
                    .AnyAsync(u => u.EmpId == model.EmpId && u.UserId != excludeId);
                if (facultyHasUser)
                {
                    ModelState.AddModelError("EmpId", "This faculty member already has a user account.");
                    valid = false;
                }

                // Faculty is already HOD elsewhere
                if (model.Role == "HOD")
                {
                    var alreadyHod = await _db.Users
                        .AnyAsync(u => u.EmpId == model.EmpId &&
                                       u.Role == "HOD" &&
                                       u.UserId != excludeId);
                    if (alreadyHod)
                    {
                        ModelState.AddModelError("EmpId", "This faculty member is already assigned as HOD elsewhere.");
                        valid = false;
                    }

                    // Faculty must actually be marked as hod_name on a department
                    var facultyName = await _db.Faculties
                        .Where(f => f.EmpId == model.EmpId)
                        .Select(f => f.Name)
                        .FirstOrDefaultAsync();

                    var isDesignatedHod = facultyName != null && await _db.Departments
                        .AnyAsync(d => d.HodName == facultyName && !d.IsDeleted);

                    if (!isDesignatedHod)
                    {
                        ModelState.AddModelError("EmpId", "This faculty member is not currently designated as an HOD on any department.");
                        valid = false;
                    }
                }
            }

            return valid;
        }
    }

    public class UserListRow
    {
        public int UserId { get; set; }
        public string Username { get; set; } = "";
        public string Role { get; set; } = "";
        public bool? IsActive { get; set; }
        public DateTime? LastLogin { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? FacultyName { get; set; }
    }
}