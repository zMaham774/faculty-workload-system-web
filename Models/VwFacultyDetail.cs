using System;
using System.Collections.Generic;

namespace FacultyManagementSystem.Models;

public partial class VwFacultyDetail
{
    public int EmpId { get; set; }

    public string Name { get; set; } = null!;

    public string EmpType { get; set; } = null!;

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? Qualification { get; set; }

    public bool? IsActive { get; set; }

    public bool IsDeleted { get; set; }

    public int DeptId { get; set; }

    public string DeptName { get; set; } = null!;

    public int DesignationId { get; set; }

    public string DesignationName { get; set; } = null!;

    public string Status { get; set; } = null!;
}
