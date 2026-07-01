using FacultyManagementSystem.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FacultyManagementSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _db;

        public AccountController(AppDbContext db)
        {
            _db = db;
        }

        // GET: /Account/Login
        [HttpGet]
        [HttpGet]
        public async Task<IActionResult> Login()
        {
            if (HttpContext.Session.GetString("UserRole") != null)
                return RedirectToAction("Index", "Home");

            ViewData["TotalFaculty"] = await _db.Faculties.CountAsync(f => !f.IsDeleted && f.IsActive == true);
            ViewData["TotalDepartments"] = await _db.Departments.CountAsync(d => !d.IsDeleted);
            ViewData["AvgAttendance"] = 87; // replace with real query once attendance module is built

            var rememberedUser = HttpContext.Request.Cookies["RememberUser"];
            if (rememberedUser != null)
                ViewData["Username"] = rememberedUser;

            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password, string role, bool rememberMe = false)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ViewData["Error"] = "Please enter both username and password.";
                ViewData["Username"] = username;
                return View();
            }

            // Hash the incoming password
            var hashedPassword = HashPassword(password);

            var user = await _db.Users
                .FirstOrDefaultAsync(u =>
                    u.Username == username &&
                    u.Password == hashedPassword &&
                    u.Role == role &&
                    u.IsActive == true);

            if (user == null)
            {
                ViewData["Error"] = "Invalid username, password, or role. Please try again.";
                ViewData["Username"] = username;
                return View();
            }

            // Store session data
            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("UserRole", user.Role);
            HttpContext.Session.SetString("UserName", user.Username);

            // Make session persist across browser restarts if remember me is checked
            if (rememberMe)
            {
                HttpContext.Response.Cookies.Append("RememberUser", user.Username, new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddDays(7),
                    HttpOnly = true,
                    IsEssential = true
                });
            }

            // If this user is linked to a faculty member store their EmpId
            if (user.EmpId.HasValue)
                HttpContext.Session.SetInt32("FacultyId", user.EmpId.Value);

            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            HttpContext.Response.Cookies.Delete("RememberUser");
            return RedirectToAction("Login", "Account");
        }

        private string HashPassword(string password)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToHexString(hash).ToLower();
        }
    }
}