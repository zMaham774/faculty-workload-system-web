using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using FacultyManagementSystem.Models;

namespace FacultyManagementSystem.Controllers
{
    public class BaseController : Controller
    {
        protected readonly AppDbContext _db;

        public BaseController(AppDbContext db)
        {
            _db = db;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);

            var role = HttpContext.Session.GetString("UserRole");
            var userName = HttpContext.Session.GetString("UserName");

            ViewData["UserRole"] = role;
            ViewData["UserName"] = userName;

            // Synchronous DB call to avoid concurrent context issue
            var semester = _db.Semesters
                .Where(s => s.IsCurrent && !s.IsDeleted)
                .Select(s => s.SemName)
                .FirstOrDefault();

            ViewData["Semester"] = semester ?? "No Active Semester";
        }
    }
}