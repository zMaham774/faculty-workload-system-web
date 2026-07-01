using System;
using System.Collections.Generic;

namespace FacultyManagementSystem.Models;

/// <summary>
/// Leave entitlement balances per faculty per year
/// </summary>
public partial class LeaveBalance
{
    public int LbId { get; set; }

    public int EmpId { get; set; }

    public int LtId { get; set; }

    public string AcadYear { get; set; } = null!;

    public int TotalEntitled { get; set; }

    public int BalanceRemaining { get; set; }

    public virtual Faculty Emp { get; set; } = null!;

    public virtual LeaveType Lt { get; set; } = null!;
}
