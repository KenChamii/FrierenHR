using FrierenHR.Core.Enums;

namespace FrierenHR.Application.Common.DTOs;

public record TimesheetDto(Guid Id, Guid EmployeeId, string EmployeeName, DateTime WeekStartDate,
    TimesheetStatus Status, decimal TotalHours, DateTime? SubmittedAt,
    DateTime? DecidedAt, string? DecidedByName, string? Comment);

public record SubmitTimesheetDto(Guid EmployeeId, DateTime WeekStartDate);
public record DecideTimesheetDto(Guid DecidedByEmployeeId, bool Approve, string? Comment);
