using System;
using System.Collections.Generic;

namespace FacultyManagementSystem.Models;

/// <summary>
/// Per-session faculty attendance records
/// </summary>
public partial class AttendanceRecord
{
    public int ArId { get; set; }

    public int WaId { get; set; }

    public DateOnly AttDate { get; set; }

    public int? SlotId { get; set; }

    public int? CalId { get; set; }

    public string Status { get; set; } = null!;

    public string? Remarks { get; set; }

    public virtual AcademicCalendar? Cal { get; set; }

    public virtual TimeSlot? Slot { get; set; }

    public virtual WorkloadAssignment Wa { get; set; } = null!;
}
