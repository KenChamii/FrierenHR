using FrierenHR.Core.Enums;

namespace FrierenHR.Application.Common.DTOs;

// --- Deduction setup ---

public record PayrollDeductionDto(
    Guid Id, Guid CompanyId, string Name, DeductionKind Kind, decimal Value, int SortOrder, bool IsActive);

public record SavePayrollDeductionDto(
    Guid? Id, Guid CompanyId, string Name, DeductionKind Kind, decimal Value, int SortOrder, bool IsActive);

// --- Payslips ---

public record PayslipLineDto(string Label, PayslipLineKind Kind, decimal Amount, string? Basis);

/// <summary>A payslip as it appears in a list: enough to recognise and open, nothing more.</summary>
public record PayslipSummaryDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    decimal GrossPay,
    decimal NetPay,
    decimal MonthlyRate,
    DateTime GeneratedAt);

public record PayslipDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    string? DepartmentName,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    decimal MonthlyRate,
    decimal HourlyRate,
    decimal RegularHours,
    decimal PayableOvertimeHours,
    decimal GrossPay,
    decimal TotalDeductions,
    decimal NetPay,
    DateTime GeneratedAt,
    List<PayslipLineDto> Lines);

/// <param name="Skipped">
/// Employees left out and why, usually because no monthly rate is recorded for them. Returned
/// rather than swallowed: a payroll run that quietly covers fewer people than expected is the
/// kind of thing nobody notices until payday.
/// </param>
public record PayslipRunDto(
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    int Generated,
    decimal TotalNetPay,
    List<PayslipSummaryDto> Payslips,
    List<string> Skipped);
