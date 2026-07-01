using System;
using System.Collections.Generic;

namespace FacultyManagementSystem.Models;

/// <summary>
/// Faculty-course-semester assignments (central hub)
/// </summary>
public partial class WorkloadAssignment
{
    public int WaId { get; set; }

    public int EmpId { get; set; }

    public int CourseId { get; set; }

    public int SemId { get; set; }

    public decimal TotalHours { get; set; }

    public string Status { get; set; } = null!;

    public DateOnly AssignedDate { get; set; }

    public bool IsDeleted { get; set; }

    public virtual ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();

    public virtual Course Course { get; set; } = null!;

    public virtual Faculty Emp { get; set; } = null!;

    public virtual Semester Sem { get; set; } = null!;

    public virtual ICollection<Timetable> Timetables { get; set; } = new List<Timetable>();
}
