using FrierenHR.Core.Enums;

namespace FrierenHR.Application.Common.DTOs;

// --- Who is out ---

/// <summary>What an employee is doing on one day, as the availability grid needs it.</summary>
public enum DayAvailability
{
    Working = 0,
    OnLeave = 1,
    RestDay = 2,
    Holiday = 3
}

public record AvailabilityDayDto(DateOnly Date, DayAvailability Status, string? Detail);

public record AvailabilityRowDto(
    Guid EmployeeId, string EmployeeName, string? DepartmentName, List<AvailabilityDayDto> Days);

/// <summary>
/// How much of a department is actually in on a given day. The number people look at, since
/// it is what makes a thin day visible before it arrives.
/// </summary>
public record CoverageDto(DateOnly Date, string DepartmentName, int Working, int Total, int PercentWorking);

public record AvailabilityDto(
    DateOnly From, DateOnly To, List<AvailabilityRowDto> Rows, List<CoverageDto> ThinnestDays);

// --- Projected balance ---

/// <param name="Note">
/// Set when there is nothing to project, for example no accrual rule matches this employee.
/// Not an error: a company may simply not accrue that leave type.
/// </param>
public record LeaveProjectionDto(
    LeaveType LeaveType,
    decimal CurrentBalance,
    decimal MonthlyAccrual,
    decimal? Cap,
    int MonthsProjected,
    decimal ProjectedBalance,
    decimal Forfeited,
    DateOnly? ForfeitsFrom,
    string? Note);

public record LeaveForecastDto(Guid EmployeeId, DateOnly Horizon, List<LeaveProjectionDto> Projections);

// --- Payroll export ---

public record PayrollRowDto(
    Guid EmployeeId,
    string EmployeeName,
    string? DepartmentName,
    decimal RegularHours,
    decimal OvertimeHours,
    decimal OvertimeMultiplier,
    decimal PayableOvertimeHours,
    int LateMinutes,
    decimal LeaveDaysTaken,
    int DaysWorked,
    int HolidaysWorked);

public record PayrollPeriodDto(DateOnly From, DateOnly To, List<PayrollRowDto> Rows);

// --- Leave liability ---

public record LiabilityRowDto(
    Guid EmployeeId,
    string EmployeeName,
    string? DepartmentName,
    LeaveType LeaveType,
    decimal Days,
    decimal DailyRate,
    decimal Value);

public record LiabilityDto(
    decimal DailyRate,
    decimal TotalDays,
    decimal TotalValue,
    List<LiabilityRowDto> Rows,
    List<LiabilityByDepartmentDto> ByDepartment);

public record LiabilityByDepartmentDto(string DepartmentName, decimal Days, decimal Value);
