using FrierenHR.Core.Common;
using FrierenHR.Core.Enums;

namespace FrierenHR.Core.Entities;

/// <summary>The hours an employee is expected to work.</summary>
public class Shift : BaseEntity, ICompanyScoped
{
    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>TimeOnly, not DateTime: a shift starts at a time of day, on no particular date.</summary>
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    /// <summary>Unpaid break, subtracted when computing hours worked.</summary>
    public int BreakMinutes { get; set; }

    /// <summary>Days this shift does not run. Used by the working-day calculator.</summary>
    public WeekDays RestDays { get; set; } = WeekDays.Weekend;

    /// <summary>Used when an employee and their department both have no shift of their own.</summary>
    public bool IsDefault { get; set; }

    /// <summary>True when the shift runs past midnight, so EndTime falls on the next day.</summary>
    public bool CrossesMidnight => EndTime <= StartTime;

    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
    public ICollection<Department> Departments { get; set; } = new List<Department>();
}
