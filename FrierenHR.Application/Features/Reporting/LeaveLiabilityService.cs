using FrierenHR.Application.Common;
using FrierenHR.Application.Common.DTOs;
using FrierenHR.Application.Common.Interfaces;
using FrierenHR.Core.Enums;

namespace FrierenHR.Application.Features.Reporting;

/// <summary>What unused leave would cost if it were all paid out: the accrued-liability figure finance asks for at period end.</summary>
public class LeaveLiabilityService : ILeaveLiabilityService
{
    private const string NoDepartment = "Unassigned";

    /// <summary>Unpaid leave has no payout value, so it is excluded rather than valued at zero.</summary>
    private static readonly LeaveType[] NonPayableTypes = [LeaveType.Unpaid];

    private readonly IEmployeeRepository _employees;
    private readonly IDepartmentRepository _departments;
    private readonly ILeaveRepository _leave;

    public LeaveLiabilityService(
        IEmployeeRepository employees, IDepartmentRepository departments, ILeaveRepository leave)
    {
        _employees = employees;
        _departments = departments;
        _leave = leave;
    }

    public async Task<LiabilityDto> GetAsync(
        Guid companyId, decimal dailyRate, CancellationToken ct = default)
    {
        if (dailyRate < 0) throw new InvalidOperationException("The daily rate cannot be negative.");

        var employees = (await _employees.GetByCompanyAsync(companyId, ct))
            .Where(e => e.IsActive)
            .ToDictionary(e => e.Id);

        var departmentNames = (await _departments.GetByCompanyAsync(companyId, ct))
            .ToDictionary(d => d.Id, d => d.Name);

        var rows = (await _leave.GetBalancesForCompanyAsync(companyId, ct))
            .Where(b => b.Balance > 0)
            .Where(b => !NonPayableTypes.Contains(b.LeaveType))
            // A balance may outlive its employee record being deactivated; those are no longer a liability on the active headcount.
            .Where(b => employees.ContainsKey(b.EmployeeId))
            .Select(b =>
            {
                var employee = employees[b.EmployeeId];
                return new LiabilityRowDto(
                    employee.Id,
                    $"{employee.FirstName} {employee.LastName}",
                    employee.DepartmentId is not null
                    && departmentNames.TryGetValue(employee.DepartmentId.Value, out var name)
                        ? name
                        : NoDepartment,
                    b.LeaveType,
                    b.Balance,
                    dailyRate,
                    Round(b.Balance * dailyRate));
            })
            .OrderByDescending(r => r.Value)
            .ThenBy(r => r.EmployeeName)
            .ToList();

        var byDepartment = rows
            .GroupBy(r => r.DepartmentName ?? NoDepartment)
            .Select(g => new LiabilityByDepartmentDto(g.Key, g.Sum(r => r.Days), Round(g.Sum(r => r.Value))))
            .OrderByDescending(d => d.Value)
            .ToList();

        return new LiabilityDto(
            dailyRate,
            rows.Sum(r => r.Days),
            Round(rows.Sum(r => r.Value)),
            rows,
            byDepartment);
    }

    public async Task<byte[]> GetCsvAsync(
        Guid companyId, decimal dailyRate, CancellationToken ct = default)
    {
        var report = await GetAsync(companyId, dailyRate, ct);

        var csv = new CsvBuilder(
            "Employee Id", "Employee", "Department", "Leave Type", "Days", "Daily Rate", "Value");

        foreach (var row in report.Rows)
        {
            csv.AppendRow(
                row.EmployeeId, row.EmployeeName, row.DepartmentName, row.LeaveType,
                row.Days, row.DailyRate, row.Value);
        }

        return csv.ToUtf8WithBom();
    }

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
