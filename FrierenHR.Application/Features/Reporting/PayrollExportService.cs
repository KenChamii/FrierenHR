using System.Text.Json;
using FrierenHR.Application.Common;
using FrierenHR.Application.Common.DTOs;
using FrierenHR.Application.Common.Interfaces;
using FrierenHR.Application.Features.Scheduling;
using FrierenHR.Core.Entities;
using FrierenHR.Core.Enums;
using FrierenHR.Core.RulesEngine;
using FrierenHR.Core.Time;
// FrierenHR.Application.Features.Employee is a namespace, so the entity is aliased.
using EmployeeEntity = FrierenHR.Core.Entities.Employee;

namespace FrierenHR.Application.Features.Reporting;

/// <summary>
/// One row per employee for a pay period: hours worked, overtime at the company's own
/// multiplier, minutes late after grace, and leave taken.
///
/// The numbers are derived here rather than stored, so a corrected attendance log changes the
/// export without anything needing to be recalculated or kept in step.
/// </summary>
public class PayrollExportService : IPayrollExportService
{
    /// <summary>Hours past which the rest of a day counts as overtime.</summary>
    private const decimal StandardDayHours = 8m;

    /// <summary>Used when no OT rule is configured: overtime is paid, but at plain rate.</summary>
    private const decimal DefaultOvertimeMultiplier = 1m;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IEmployeeRepository _employees;
    private readonly IDepartmentRepository _departments;
    private readonly IAttendanceRepository _attendance;
    private readonly ILeaveRepository _leave;
    private readonly IShiftService _shifts;
    private readonly IWorkingCalendarProvider _calendars;
    private readonly IRuleConfigRepository _rules;
    private readonly IRuleEvaluator _evaluator;

    public PayrollExportService(
        IEmployeeRepository employees,
        IDepartmentRepository departments,
        IAttendanceRepository attendance,
        ILeaveRepository leave,
        IShiftService shifts,
        IWorkingCalendarProvider calendars,
        IRuleConfigRepository rules,
        IRuleEvaluator evaluator)
    {
        _employees = employees;
        _departments = departments;
        _attendance = attendance;
        _leave = leave;
        _shifts = shifts;
        _calendars = calendars;
        _rules = rules;
        _evaluator = evaluator;
    }

    public async Task<PayrollPeriodDto> BuildAsync(
        Guid companyId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        if (to < from) throw new InvalidOperationException("The end date cannot be before the start date.");

        var periodStart = from.ToDateTime(TimeOnly.MinValue);
        var periodEnd = to.ToDateTime(TimeOnly.MaxValue);

        var employees = (await _employees.GetByCompanyAsync(companyId, ct))
            .OrderBy(e => e.LastName).ThenBy(e => e.FirstName)
            .ToList();

        var departmentNames = (await _departments.GetByCompanyAsync(companyId, ct))
            .ToDictionary(d => d.Id, d => d.Name);

        var logsByEmployee = (await _attendance.GetByCompanyAsync(companyId, periodStart, periodEnd, ct))
            .GroupBy(l => l.EmployeeId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var leaveByEmployee = (await _leave.GetApprovedInRangeAsync(companyId, periodStart, periodEnd, null, ct))
            .GroupBy(l => l.EmployeeId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var otRules = await LoadRules(companyId, RuleType.OTMultiplier, ct);
        var graceRules = await LoadRules(companyId, RuleType.LateGracePeriod, ct);

        var rows = new List<PayrollRowDto>(employees.Count);
        foreach (var employee in employees)
        {
            var logs = logsByEmployee.GetValueOrDefault(employee.Id) ?? [];
            var leave = leaveByEmployee.GetValueOrDefault(employee.Id) ?? [];

            // An employee with neither attendance nor leave has nothing to pay for this
            // period. Including them would pad the file with zero rows payroll has to skip.
            if (logs.Count == 0 && leave.Count == 0) continue;

            var calendar = await _calendars.ForEmployeeAsync(employee.Id, from, to, ct);
            var shift = await _shifts.ResolveForEmployeeAsync(employee.Id, ct);

            rows.Add(BuildRow(
                employee, departmentNames, logs, leave, calendar, shift, otRules, graceRules, from, to));
        }

        return new PayrollPeriodDto(from, to, rows);
    }

    public async Task<byte[]> BuildCsvAsync(
        Guid companyId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var period = await BuildAsync(companyId, from, to, ct);

        var csv = new CsvBuilder(
            "Employee Id", "Employee", "Department", "Regular Hours", "Overtime Hours",
            "OT Multiplier", "Payable OT Hours", "Late Minutes", "Leave Days", "Days Worked",
            "Holidays Worked");

        foreach (var row in period.Rows)
        {
            csv.AppendRow(
                row.EmployeeId, row.EmployeeName, row.DepartmentName, row.RegularHours,
                row.OvertimeHours, row.OvertimeMultiplier, row.PayableOvertimeHours,
                row.LateMinutes, row.LeaveDaysTaken, row.DaysWorked, row.HolidaysWorked);
        }

        return csv.ToUtf8WithBom();
    }

    private PayrollRowDto BuildRow(
        EmployeeEntity employee,
        IReadOnlyDictionary<Guid, string> departmentNames,
        List<AttendanceLog> logs,
        List<LeaveRequest> leave,
        WorkingCalendar calendar,
        Shift? shift,
        List<RuleDefinition> otRules,
        List<RuleDefinition> graceRules,
        DateOnly periodStart,
        DateOnly periodEnd)
    {
        var regularHours = 0m;
        var overtimeHours = 0m;
        var lateMinutes = 0;
        var daysWorked = 0;
        var holidaysWorked = 0;

        // Only closed logs are counted. An open one means the person is still clocked in, and
        // guessing a time out would put an invented number in a payroll file.
        foreach (var log in logs.Where(l => l.TimeOut is not null))
        {
            var worked = (decimal)(log.TimeOut!.Value - log.TimeIn).TotalHours
                         - log.BreakMinutes / 60m;
            if (worked <= 0) continue;

            daysWorked++;

            var date = DateOnly.FromDateTime(log.TimeIn);
            if (calendar.IsHoliday(date)) holidaysWorked++;

            regularHours += Math.Min(worked, StandardDayHours);
            overtimeHours += Math.Max(0, worked - StandardDayHours);

            lateMinutes += LateMinutesFor(log, shift, calendar, date, graceRules);
        }

        var multiplier = OvertimeMultiplierFor(employee, otRules);

        // Leave days are clipped to the period, so a request that straddles two pay periods
        // is not paid twice.
        var leaveDays = leave.Sum(l => ClippedLeaveDays(l, calendar, periodStart, periodEnd));

        return new PayrollRowDto(
            employee.Id,
            $"{employee.FirstName} {employee.LastName}",
            employee.DepartmentId is not null && departmentNames.TryGetValue(employee.DepartmentId.Value, out var name)
                ? name
                : null,
            Round(regularHours),
            Round(overtimeHours),
            multiplier,
            Round(overtimeHours * multiplier),
            lateMinutes,
            Round(leaveDays),
            daysWorked,
            holidaysWorked);
    }

    /// <summary>
    /// Minutes late beyond the configured grace period. Without a shift there is no expected
    /// start time, so nobody can be late.
    /// </summary>
    private int LateMinutesFor(
        AttendanceLog log,
        Shift? shift,
        WorkingCalendar calendar,
        DateOnly date,
        List<RuleDefinition> graceRules)
    {
        if (shift is null) return 0;

        var expected = date.ToDateTime(shift.StartTime);
        var raw = (int)Math.Floor((log.TimeIn - expected).TotalMinutes);
        if (raw <= 0) return 0;

        var context = new RuleContext()
            .Set("minutesLate", raw)
            .Set("isHoliday", calendar.IsHoliday(date))
            .Set("holidayType", calendar.HolidayTypeOn(date)?.ToString() ?? "None");

        var grace = FirstMatchingAmount(graceRules, RuleType.LateGracePeriod, context) ?? 0m;
        return Math.Max(0, raw - (int)grace);
    }

    private decimal OvertimeMultiplierFor(EmployeeEntity employee, List<RuleDefinition> otRules)
    {
        // Evaluated once per employee rather than per log. A rule keyed on the holiday of one
        // particular day would need a per-day multiplier, which the row format does not carry;
        // the period multiplier is the honest summary of it.
        var context = new RuleContext()
            .Set("tenureMonths", employee.TenureMonths())
            .Set("isHoliday", false)
            .Set("holidayType", "None");

        return FirstMatchingAmount(otRules, RuleType.OTMultiplier, context) ?? DefaultOvertimeMultiplier;
    }

    /// <summary>Working days of a leave request that fall inside the calendar's range.</summary>
    private static decimal ClippedLeaveDays(
        LeaveRequest request, WorkingCalendar calendar, DateOnly periodStart, DateOnly periodEnd)
    {
        // A request may start before the period or end after it, because the query matches on
        // overlap. Only the part inside the period belongs in this export.
        var start = Max(DateOnly.FromDateTime(request.StartDate), periodStart);
        var end = Min(DateOnly.FromDateTime(request.EndDate), periodEnd);
        return end < start ? 0 : calendar.CountWorkingDays(start, end);
    }

    private static DateOnly Max(DateOnly a, DateOnly b) => a > b ? a : b;

    private static DateOnly Min(DateOnly a, DateOnly b) => a < b ? a : b;

    /// <summary>
    /// The configured amount of the first rule whose conditions hold.
    ///
    /// The rule's own Action.Amount is read rather than the evaluator's ResultValue, because
    /// the OTMultiplier action returns hours already multiplied. This report needs the
    /// multiplier itself, to show alongside the hours it applies to.
    /// </summary>
    private decimal? FirstMatchingAmount(
        List<RuleDefinition> rules, RuleType ruleType, RuleContext context)
    {
        var candidates = rules.Where(r =>
            string.Equals(r.RuleType, ruleType.ToString(), StringComparison.OrdinalIgnoreCase));

        foreach (var rule in candidates)
        {
            var result = _evaluator.Evaluate(rule, context);
            if (result.Matched && result.Success) return rule.Action.Amount;
        }

        return null;
    }

    private async Task<List<RuleDefinition>> LoadRules(Guid companyId, RuleType ruleType, CancellationToken ct)
    {
        var configs = await _rules.GetActiveRulesAsync(companyId, ruleType, ct);
        return configs
            .OrderBy(c => c.Priority)
            .Select(c => JsonSerializer.Deserialize<RuleDefinition>(c.RuleJson, JsonOptions))
            .Where(r => r is not null)
            .Select(r => r!)
            .ToList();
    }

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
