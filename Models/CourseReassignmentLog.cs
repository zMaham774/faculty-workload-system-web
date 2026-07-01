using System;
using System.Collections.Generic;

namespace FacultyManagementSystem.Models;

/// <summary>
/// Audit: course reassignment history
/// </summary>
public partial class CourseReassignmentLog
{
    public int RlId { get; set; }

    public int CourseId { get; set; }

    public int SemId { get; set; }

    public int FromEmpId { get; set; }

    public int ToEmpId { get; set; }

    public string? Reason { get; set; }

    public DateTime ReassignedOn { get; set; }

    public int? ReassignedBy { get; set; }

    public virtual Course Course { get; set; } = null!;

    public virtual Faculty FromEmp { get; set; } = null!;

    public virtual User? ReassignedByNavigation { get; set; }

    public virtual Semester Sem { get; set; } = null!;

    public virtual Faculty ToEmp { get; set; } = null!;
}
