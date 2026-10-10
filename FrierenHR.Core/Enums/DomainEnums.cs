namespace FrierenHR.Core.Enums;

public enum EmployeeRole { Employee = 0, Manager = 1, HRAdmin = 2 }
public enum LeaveStatus { Pending = 0, Approved = 1, Rejected = 2, Cancelled = 3, Escalated = 4 }
public enum LeaveType { Vacation = 0, Sick = 1, Emergency = 2, Unpaid = 3, Maternity = 4, Paternity = 5 }
public enum RuleType { LeaveAccrual = 0, LeaveApproval = 1, OTMultiplier = 2, LateGracePeriod = 3, ApprovalEscalation = 4 }
public enum ConversationType { Direct = 0, Group = 1, Broadcast = 2 }
public enum ApprovalStatus { Pending = 0, Approved = 1, Rejected = 2, Escalated = 3 }
public enum TimesheetStatus { Draft = 0, Submitted = 1, Approved = 2, Rejected = 3 }

/// <summary>
/// Kinds of non-working day. Not a bool, because they are paid and multiplied differently in
/// most jurisdictions, and a rule needs to tell them apart.
/// </summary>
public enum HolidayType
{
    /// <summary>Paid whether worked or not, and worked hours carry a premium.</summary>
    Regular = 0,
    /// <summary>Unpaid unless worked, usually at a lower premium than a regular holiday.</summary>
    SpecialNonWorking = 1,
    /// <summary>Observed by one company or site rather than nationally.</summary>
    CompanySpecific = 2
}

/// <summary>Days of the week as a set, used for a shift's rest days.</summary>
[Flags]
public enum WeekDays
{
    None = 0,
    Monday = 1,
    Tuesday = 2,
    Wednesday = 4,
    Thursday = 8,
    Friday = 16,
    Saturday = 32,
    Sunday = 64,
    Weekend = Saturday | Sunday
}

/// <summary>What happened to a record, as written to the audit trail.</summary>
public enum AuditAction { Created = 0, Updated = 1, Deleted = 2 }
