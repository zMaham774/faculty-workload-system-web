using System;
using System.Collections.Generic;

namespace FacultyManagementSystem.Models;

/// <summary>
/// Faculty member profiles
/// </summary>
public partial class Faculty
{
    public int EmpId { get; set; }

    public string Name { get; set; } = null!;

    public int DesignationId { get; set; }

    public int DeptId { get; set; }

    public string EmpType { get; set; } = null!;

    public string? Qualification { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public bool? IsActive { get; set; }

    public bool IsDeleted { get; set; }

    public virtual ICollection<CourseReassignmentLog> CourseReassignmentLogFromEmps { get; set; } = new List<CourseReassignmentLog>();

    public virtual ICollection<CourseReassignmentLog> CourseReassignmentLogToEmps { get; set; } = new List<CourseReassignmentLog>();

    public virtual Department Dept { get; set; } = null!;

    public virtual Designation Designation { get; set; } = null!;

    public virtual ICollection<FacultyChangeLog> FacultyChangeLogs { get; set; } = new List<FacultyChangeLog>();

    public virtual ICollection<LeaveBalance> LeaveBalances { get; set; } = new List<LeaveBalance>();

    public virtual ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();

    public virtual ICollection<User> Users { get; set; } = new List<User>();

    public virtual ICollection<WorkloadAssignment> WorkloadAssignments { get; set; } = new List<WorkloadAssignment>();
}
