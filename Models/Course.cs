using System;
using System.Collections.Generic;

namespace FacultyManagementSystem.Models;

/// <summary>
/// Course catalogue
/// </summary>
public partial class Course
{
    public int CourseId { get; set; }

    public string CourseCode { get; set; } = null!;

    public string Title { get; set; } = null!;

    public int CreditHours { get; set; }

    public string CourseType { get; set; } = null!;

    public int DeptId { get; set; }

    public bool? IsActive { get; set; }

    public bool IsDeleted { get; set; }

    public virtual ICollection<CourseReassignmentLog> CourseReassignmentLogs { get; set; } = new List<CourseReassignmentLog>();

    public virtual Department Dept { get; set; } = null!;

    public virtual ICollection<WorkloadAssignment> WorkloadAssignments { get; set; } = new List<WorkloadAssignment>();
}
