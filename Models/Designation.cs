using System;
using System.Collections.Generic;

namespace FacultyManagementSystem.Models;

/// <summary>
/// Lookup: faculty designation types
/// </summary>
public partial class Designation
{
    public int DesignationId { get; set; }

    public string DesignationName { get; set; } = null!;

    public int RankOrder { get; set; }

    public virtual ICollection<Faculty> Faculties { get; set; } = new List<Faculty>();
}
