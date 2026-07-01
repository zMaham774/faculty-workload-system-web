using System;
using System.Collections.Generic;

namespace FacultyManagementSystem.Models;

/// <summary>
/// Academic semester records
/// </summary>
public partial class Semester
{
    public int SemId { get; set; }

    public string SemName { get; set; } = null!;

    public string AcadYear { get; set; } = null!;

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public bool IsCurrent { get; set; }

    public bool IsDeleted { get; set; }

    public virtual ICollection<AcademicCalendar> AcademicCalendars { get; set; } = new List<AcademicCalendar>();

    public virtual ICollection<CourseReassignmentLog> CourseReassignmentLogs { get; set; } = new List<CourseReassignmentLog>();

    public virtual ICollection<WorkloadAssignment> WorkloadAssignments { get; set; } = new List<WorkloadAssignment>();

    public virtual ICollection<WorkloadStandard> WorkloadStandards { get; set; } = new List<WorkloadStandard>();
}
