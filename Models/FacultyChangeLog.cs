using System;
using System.Collections.Generic;

namespace FacultyManagementSystem.Models;

/// <summary>
/// Audit: faculty profile change history
/// </summary>
public partial class FacultyChangeLog
{
    public int LogId { get; set; }

    public int EmpId { get; set; }

    public string ChangeType { get; set; } = null!;

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public DateTime ChangedOn { get; set; }

    public int? ChangedBy { get; set; }

    public virtual User? ChangedByNavigation { get; set; }

    public virtual Faculty Emp { get; set; } = null!;
}
