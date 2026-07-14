namespace FrierenHR.Application.Common.DTOs;

public record AttendanceLogDto(Guid Id, Guid EmployeeId, string EmployeeName, DateTime TimeIn, DateTime? TimeOut, string Source, decimal? HoursWorked);
public record ClockInDto(Guid EmployeeId, string Source = "Web");
public record ClockOutDto(Guid EmployeeId);
public record OtComputationResultDto(bool Matched, decimal OtHours, decimal PayableHours, string Message);