using System;
using System.Collections.Generic;

namespace FacultyManagementSystem.Models;

/// <summary>
/// Leave applications submitted by faculty
/// </summary>
public partial class LeaveRequest
{
    public int LrId { get; set; }

    public int EmpId { get; set; }

    public int LtId { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public string? Reason { get; set; }

    public string ApprStatus { get; set; } = null!;

    public string? ApprRemarks { get; set; }

    public int? ApprovedBy { get; set; }

    public DateTime SubmittedOn { get; set; }

    public virtual User? ApprovedByNavigation { get; set; }

    public virtual Faculty Emp { get; set; } = null!;

    public virtual LeaveType Lt { get; set; } = null!;
}
