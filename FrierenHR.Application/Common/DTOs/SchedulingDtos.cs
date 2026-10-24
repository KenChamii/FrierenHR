using FrierenHR.Core.Enums;

namespace FrierenHR.Application.Common.DTOs;

// --- Holidays ---

public record HolidayDto(
    Guid Id, Guid CompanyId, DateOnly Date, string Name, HolidayType Type,
    Guid? DepartmentId, string? DepartmentName);

/// <summary>Id is null on create and set on update, matching SaveRuleConfigDto.</summary>
public record SaveHolidayDto(
    Guid? Id, Guid CompanyId, DateOnly Date, string Name, HolidayType Type, Guid? DepartmentId);

/// <summary>Result of importing a year's holidays in one go.</summary>
public record HolidayImportResultDto(int Added, int Skipped, List<string> Messages);

/// <param name="AlreadyRecorded">
/// True when the company already has a holiday on that date, so the review screen can show
/// it as nothing to do rather than offering to add a duplicate.
/// </param>
/// <param name="SuggestedType">
/// Always Regular. The feed does not distinguish a Regular holiday from a Special
/// Non-Working one, and that distinction drives pay, so it is a starting point to correct
/// rather than an answer.
/// </param>
public record HolidaySuggestionDto(
    DateOnly Date, string Name, HolidayType SuggestedType, bool AlreadyRecorded);

/// <param name="Source">Named so the screen can say where the list came from.</param>
/// <param name="Caveat">Why the list still needs reading before it is saved.</param>
public record HolidaySuggestionsDto(
    int Year, string CountryCode, string Source, string Caveat, List<HolidaySuggestionDto> Holidays);

// --- Shifts ---

public record ShiftDto(
    Guid Id, Guid CompanyId, string Name, TimeOnly StartTime, TimeOnly EndTime,
    int BreakMinutes, WeekDays RestDays, bool IsDefault, bool CrossesMidnight,
    int AssignedEmployeeCount);

public record SaveShiftDto(
    Guid? Id, Guid CompanyId, string Name, TimeOnly StartTime, TimeOnly EndTime,
    int BreakMinutes, WeekDays RestDays, bool IsDefault);

/// <summary>Which shift applies to an employee, and which level of the chain supplied it.</summary>
public record ResolvedShiftDto(ShiftDto? Shift, string Source);

// --- Working days ---

/// <summary>A day count with its working days shown, so "you asked for 5 days and 3 were deducted" can be explained rather than asserted.</summary>
public record WorkingDaysDto(
    DateOnly From, DateOnly To, int CalendarDays, int WorkingDays,
    List<NonWorkingDayDto> NonWorkingDays);

public record NonWorkingDayDto(DateOnly Date, string Reason);

// --- Attendance corrections ---

public record AttendanceCorrectionDto(
    Guid Id, Guid EmployeeId, string EmployeeName, DateOnly Date,
    TimeOnly RequestedTimeIn, TimeOnly RequestedTimeOut, int RequestedBreakMinutes,
    string Reason, ApprovalStatus Status, DateTime RequestedAt,
    string? DecidedByName, DateTime? DecidedAt, string? DecisionComment);

public record CreateAttendanceCorrectionDto(
    Guid EmployeeId, DateOnly Date, TimeOnly RequestedTimeIn, TimeOnly RequestedTimeOut,
    int RequestedBreakMinutes, string Reason);

public record DecideAttendanceCorrectionDto(Guid DecidedByEmployeeId, bool Approve, string? Comment);

// --- Audit trail ---

public record AuditEntryDto(
    Guid Id, string EntityName, Guid EntityId, AuditAction Action,
    Guid? ChangedByEmployeeId, string? ChangedByName, DateTime ChangedAt, string Changes);

public record AuditPageDto(List<AuditEntryDto> Entries, int Total, int Skip, int Take);
