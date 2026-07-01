using System;
using System.Collections.Generic;

namespace FacultyManagementSystem.Models;

/// <summary>
/// System login accounts
/// </summary>
public partial class User
{
    public int UserId { get; set; }

    public int? EmpId { get; set; }

    public string Username { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string Role { get; set; } = null!;

    public bool? IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? LastLogin { get; set; }

    public virtual ICollection<AcademicCalendar> AcademicCalendars { get; set; } = new List<AcademicCalendar>();

    public virtual ICollection<CourseReassignmentLog> CourseReassignmentLogs { get; set; } = new List<CourseReassignmentLog>();

    public virtual Faculty? Emp { get; set; }

    public virtual ICollection<FacultyChangeLog> FacultyChangeLogs { get; set; } = new List<FacultyChangeLog>();

    public virtual ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
}
