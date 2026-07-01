using System;
using System.Collections.Generic;

namespace FacultyManagementSystem.Models;

/// <summary>
/// Weekly class schedule
/// </summary>
public partial class Timetable
{
    public int TtId { get; set; }

    public int WaId { get; set; }

    public string DayOfWeek { get; set; } = null!;

    public int SlotId { get; set; }

    public string? Room { get; set; }

    public bool ConflictFlag { get; set; }

    public bool IsDeleted { get; set; }

    public virtual TimeSlot Slot { get; set; } = null!;

    public virtual WorkloadAssignment Wa { get; set; } = null!;
}
