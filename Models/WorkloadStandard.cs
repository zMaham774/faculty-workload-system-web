using System;
using System.Collections.Generic;

namespace FacultyManagementSystem.Models;

/// <summary>
/// Credit hour thresholds per dept per semester
/// </summary>
public partial class WorkloadStandard
{
    public int WsId { get; set; }

    public int DeptId { get; set; }

    public int SemId { get; set; }

    public int MinHours { get; set; }

    public int MaxHours { get; set; }

    public int StdHours { get; set; }

    public virtual Department Dept { get; set; } = null!;

    public virtual Semester Sem { get; set; } = null!;
}
