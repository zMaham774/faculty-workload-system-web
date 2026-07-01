using System;
using System.Collections.Generic;

namespace FacultyManagementSystem.Models;

public partial class VwFacultyWorkload
{
    public int WaId { get; set; }

    public int EmpId { get; set; }

    public string FacultyName { get; set; } = null!;

    public int CourseId { get; set; }

    public string CourseTitle { get; set; } = null!;

    public string CourseCode { get; set; } = null!;

    public int CreditHours { get; set; }

    public string DeptName { get; set; } = null!;

    public int SemId { get; set; }

    public string SemName { get; set; } = null!;

    public decimal TotalHours { get; set; }

    public string Status { get; set; } = null!;

    public DateOnly AssignedDate { get; set; }
}
