using System;
using System.Collections.Generic;

namespace FacultyManagementSystem.Models;

public partial class VwTodaysTimetable
{
    public int EmpId { get; set; }

    public string FacultyName { get; set; } = null!;

    public string CourseTitle { get; set; } = null!;

    public string CourseCode { get; set; } = null!;

    public string SlotLabel { get; set; } = null!;

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public string? Room { get; set; }

    public string DayOfWeek { get; set; } = null!;

    public bool ConflictFlag { get; set; }

    public string SemName { get; set; } = null!;
}
