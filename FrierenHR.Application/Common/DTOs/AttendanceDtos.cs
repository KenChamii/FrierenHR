namespace FrierenHR.Application.Common.DTOs;

public record AttendanceLogDto(Guid Id, Guid EmployeeId, string EmployeeName, DateTime TimeIn, DateTime? TimeOut, int BreakMinutes, string Source, decimal? HoursWorked);
public record ClockInDto(Guid EmployeeId, string Source = "Web");
public record ClockOutDto(Guid EmployeeId);
public record LogShiftDto(Guid EmployeeId, DateTime Date, TimeSpan StartTime, TimeSpan EndTime, int BreakMinutes);
public record OtComputationResultDto(bool Matched, decimal OtHours, decimal PayableHours, string Message);