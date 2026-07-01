using System;
using System.Collections.Generic;

namespace FacultyManagementSystem.Models;

/// <summary>
/// Lookup: leave categories
/// </summary>
public partial class LeaveType
{
    public int LtId { get; set; }

    public string LtName { get; set; } = null!;

    public int DefaultEntitlement { get; set; }

    public bool? IsPaid { get; set; }

    public virtual ICollection<LeaveBalance> LeaveBalances { get; set; } = new List<LeaveBalance>();

    public virtual ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
}
