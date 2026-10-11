namespace FrierenHR.Application.Common.DTOs;

public record AttendanceLogDto(Guid Id, Guid EmployeeId, string EmployeeName, DateTime TimeIn, DateTime? TimeOut, int BreakMinutes, string Source, decimal? HoursWorked);
public record ClockInDto(Guid EmployeeId, string Source = "Web");
public record ClockOutDto(Guid EmployeeId);
public record LogShiftDto(Guid EmployeeId, DateTime Date, TimeSpan StartTime, TimeSpan EndTime, int BreakMinutes);

/// <summary>One day of a week being saved together.</summary>
public record LogWeekDayDto(DateTime Date, TimeSpan StartTime, TimeSpan EndTime, int BreakMinutes);

/// <summary>
/// A week of shifts saved in one request.
///
/// Saving seven days as seven calls can half-succeed, leaving a week that is neither the old
/// one nor the new one. These commit together or not at all.
/// </summary>
public record LogWeekDto(Guid EmployeeId, List<LogWeekDayDto> Days);
public record OtComputationResultDto(bool Matched, decimal OtHours, decimal PayableHours, string Message);