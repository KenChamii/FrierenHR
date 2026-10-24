using FrierenHR.Core.Common;
using FrierenHR.Core.Enums;

namespace FrierenHR.Core.Entities;

/// <summary>An employee's request to fix a day they forgot to clock in or out.</summary>
public class AttendanceCorrection : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    /// <summary>The day being corrected.</summary>
    public DateOnly Date { get; set; }

    public TimeOnly RequestedTimeIn { get; set; }
    public TimeOnly RequestedTimeOut { get; set; }
    public int RequestedBreakMinutes { get; set; }

    /// <summary>Required. An unexplained correction is not reviewable.</summary>
    public string Reason { get; set; } = string.Empty;

    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;

    public Guid? DecidedByEmployeeId { get; set; }
    public Employee? DecidedByEmployee { get; set; }
    public DateTime? DecidedAt { get; set; }

    /// <summary>The approver's note, shown to the employee when a request is declined.</summary>
    public string? DecisionComment { get; set; }

    /// <summary>The attendance row this request created or amended once approved.</summary>
    public Guid? AttendanceLogId { get; set; }
    public AttendanceLog? AttendanceLog { get; set; }
}
