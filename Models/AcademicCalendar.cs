using System;
using System.Collections.Generic;

namespace FacultyManagementSystem.Models;

/// <summary>
/// Holidays, exam weeks, study breaks per semester
/// </summary>
public partial class AcademicCalendar
{
    public int CalId { get; set; }

    public int SemId { get; set; }

    public DateOnly EventDate { get; set; }

    public string EventType { get; set; } = null!;

    public string EventName { get; set; } = null!;

    public bool IsTeaching { get; set; }

    public string? Description { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public virtual ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();

    public virtual User? CreatedByNavigation { get; set; }

    public virtual Semester Sem { get; set; } = null!;
}
