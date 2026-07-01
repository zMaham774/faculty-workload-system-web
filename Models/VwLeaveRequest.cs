using System;
using System.Collections.Generic;

namespace FacultyManagementSystem.Models;

public partial class VwLeaveRequest
{
    public int LrId { get; set; }

    public int EmpId { get; set; }

    public string FacultyName { get; set; } = null!;

    public string DeptName { get; set; } = null!;

    public string LeaveTypeName { get; set; } = null!;

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public long? TotalDays { get; set; }

    public string? Reason { get; set; }

    public string Status { get; set; } = null!;

    public string? ApprovalRemarks { get; set; }

    public DateTime AppliedOn { get; set; }

    public int? ApprovedBy { get; set; }

    public string? ApprovedByName { get; set; }
}
