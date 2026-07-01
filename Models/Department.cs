using System;
using System.Collections.Generic;

namespace FacultyManagementSystem.Models;

/// <summary>
/// University departments
/// </summary>
public partial class Department
{
    public int DeptId { get; set; }

    public string DeptName { get; set; } = null!;

    public string? HodName { get; set; }

    public string? Contact { get; set; }

    public string? Email { get; set; }

    public bool? IsActive { get; set; }

    public bool IsDeleted { get; set; }

    public virtual ICollection<Course> Courses { get; set; } = new List<Course>();

    public virtual ICollection<Faculty> Faculties { get; set; } = new List<Faculty>();

    public virtual ICollection<WorkloadStandard> WorkloadStandards { get; set; } = new List<WorkloadStandard>();
}
