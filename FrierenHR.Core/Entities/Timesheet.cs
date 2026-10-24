using FrierenHR.Core.Common;
using FrierenHR.Core.Enums;

namespace FrierenHR.Core.Entities;

/// <summary>One employee's weekly timesheet — a submission wrapper around that week's AttendanceLogs.</summary>
public class Timesheet : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    // Always normalized to the Monday of the week (see TimesheetService.GetWeekStart).
    public DateTime WeekStartDate { get; set; }

    public TimesheetStatus Status { get; set; } = TimesheetStatus.Draft;
    public DateTime? SubmittedAt { get; set; }

    public Guid? DecidedByEmployeeId { get; set; }
    public Employee? DecidedByEmployee { get; set; }
    public DateTime? DecidedAt { get; set; }
    public string? Comment { get; set; }
}
