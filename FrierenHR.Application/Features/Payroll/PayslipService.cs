using FrierenHR.Application.Common.DTOs;
using FrierenHR.Application.Common.Interfaces;
using FrierenHR.Application.Features.Reporting;
using FrierenHR.Core.Entities;
using FrierenHR.Core.Payroll;
// FrierenHR.Application.Features.Employee is a namespace, so the entity is aliased.
using EmployeeEntity = FrierenHR.Core.Entities.Employee;

namespace FrierenHR.Application.Features.Payroll;

/// <summary>Issues payslips for a pay period.</summary>
public class PayslipService : IPayslipService
{
    private readonly IPayrollExportService _payroll;
    private readonly IEmployeeRepository _employees;
    private readonly IDepartmentRepository _departments;
    private readonly IPayrollDeductionRepository _deductions;
    private readonly IPayslipRepository _payslips;

    public PayslipService(
        IPayrollExportService payroll,
        IEmployeeRepository employees,
        IDepartmentRepository departments,
        IPayrollDeductionRepository deductions,
        IPayslipRepository payslips)
    {
        _payroll = payroll;
        _employees = employees;
        _departments = departments;
        _deductions = deductions;
        _payslips = payslips;
    }

    public async Task<PayslipRunDto> GenerateAsync(
        Guid companyId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        if (to < from) throw new InvalidOperationException("The end date cannot be before the start date.");

        var period = await _payroll.BuildAsync(companyId, from, to, ct);
        if (period.Rows.Count == 0)
            return new PayslipRunDto(from, to, 0, 0, [], ["Nobody has attendance or approved leave in this period."]);

        var employees = (await _employees.GetByCompanyAsync(companyId, ct)).ToDictionary(e => e.Id);

        var rules = (await _deductions.GetActiveAsync(companyId, ct))
            .Select(d => new DeductionRule(d.Name, d.Kind, d.Value, d.SortOrder))
            .ToList();

        var payslips = new List<Payslip>();
        var skipped = new List<string>();

        foreach (var row in period.Rows)
        {
            if (!employees.TryGetValue(row.EmployeeId, out var employee)) continue;

            // Pricing somebody at zero because nobody entered their salary would produce a payslip that looks issued and says they earned nothing.
            if (employee.MonthlyRate is not { } monthlyRate || monthlyRate <= 0)
            {
                skipped.Add($"{row.EmployeeName}: no monthly rate recorded.");
                continue;
            }

            var hourlyRate = PayslipCalculator.HourlyRateFrom(monthlyRate, employee.StandardMonthlyHours);
            if (hourlyRate <= 0)
            {
                skipped.Add($"{row.EmployeeName}: standard monthly hours is zero, so no hourly rate can be derived.");
                continue;
            }

            var calculation = PayslipCalculator.Calculate(
                hourlyRate, row.RegularHours, row.PayableOvertimeHours, rules);

            payslips.Add(new Payslip
            {
                CompanyId = companyId,
                EmployeeId = employee.Id,
                PeriodStart = from,
                PeriodEnd = to,
                MonthlyRate = monthlyRate,
                HourlyRate = hourlyRate,
                RegularHours = row.RegularHours,
                PayableOvertimeHours = row.PayableOvertimeHours,
                GrossPay = calculation.GrossPay,
                TotalDeductions = calculation.TotalDeductions,
                NetPay = calculation.NetPay,
                Lines = calculation.Lines
                    .Select(l => new PayslipLine
                    {
                        Label = l.Label,
                        Kind = l.Kind,
                        Amount = l.Amount,
                        Basis = l.Basis,
                        SortOrder = l.SortOrder,
                    })
                    .ToList(),
            });
        }

        await _payslips.ReplaceForPeriodAsync(companyId, from, to, payslips, ct);

        var summaries = payslips
            .Select(p => ToSummary(p, employees.GetValueOrDefault(p.EmployeeId)))
            .OrderBy(s => s.EmployeeName)
            .ToList();

        return new PayslipRunDto(
            from, to, summaries.Count, summaries.Sum(s => s.NetPay), summaries, skipped);
    }

    public async Task<List<PayslipSummaryDto>> GetForEmployeeAsync(
        Guid employeeId, CancellationToken ct = default)
    {
        var employee = await _employees.GetByIdAsync(employeeId, ct);
        var payslips = await _payslips.GetByEmployeeAsync(employeeId, ct);
        return payslips.Select(p => ToSummary(p, employee)).ToList();
    }

    public async Task<List<PayslipSummaryDto>> GetForPeriodAsync(
        Guid companyId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var payslips = await _payslips.GetForPeriodAsync(companyId, from, to, ct);
        return payslips.Select(p => ToSummary(p, p.Employee)).ToList();
    }

    public async Task<PayslipDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var payslip = await _payslips.GetByIdAsync(id, ct);
        if (payslip is null) return null;

        var departmentName = payslip.Employee?.DepartmentId is { } departmentId
            ? (await _departments.GetByCompanyAsync(payslip.CompanyId, ct))
                .FirstOrDefault(d => d.Id == departmentId)?.Name
            : null;

        return new PayslipDto(
            payslip.Id,
            payslip.EmployeeId,
            NameOf(payslip.Employee),
            departmentName,
            payslip.PeriodStart,
            payslip.PeriodEnd,
            payslip.MonthlyRate,
            payslip.HourlyRate,
            payslip.RegularHours,
            payslip.PayableOvertimeHours,
            payslip.GrossPay,
            payslip.TotalDeductions,
            payslip.NetPay,
            payslip.GeneratedAt,
            payslip.Lines
                .OrderBy(l => l.SortOrder)
                .Select(l => new PayslipLineDto(l.Label, l.Kind, l.Amount, l.Basis))
                .ToList());
    }

    private static PayslipSummaryDto ToSummary(Payslip payslip, EmployeeEntity? employee) =>
        new(payslip.Id, payslip.EmployeeId, NameOf(employee),
            payslip.PeriodStart, payslip.PeriodEnd,
            payslip.GrossPay, payslip.NetPay, payslip.MonthlyRate, payslip.GeneratedAt);

    private static string NameOf(EmployeeEntity? employee) =>
        employee is null ? "" : $"{employee.FirstName} {employee.LastName}";
}
