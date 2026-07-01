using System;
using System.Collections.Generic;

namespace FacultyManagementSystem.Models;

public partial class VwCourseDetail
{
    public int CourseId { get; set; }

    public string CourseCode { get; set; } = null!;

    public string Title { get; set; } = null!;

    public int CreditHours { get; set; }

    public string CourseType { get; set; } = null!;

    public bool? IsActive { get; set; }

    public bool IsDeleted { get; set; }

    public int DeptId { get; set; }

    public string DeptName { get; set; } = null!;

    public string Status { get; set; } = null!;
}
