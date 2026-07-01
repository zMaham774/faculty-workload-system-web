using System;
using System.Collections.Generic;

namespace FacultyManagementSystem.Models;

/// <summary>
/// Lookup: class time slots
/// </summary>
public partial class TimeSlot
{
    public int SlotId { get; set; }

    public string SlotLabel { get; set; } = null!;

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public virtual ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();

    public virtual ICollection<Timetable> Timetables { get; set; } = new List<Timetable>();
}
