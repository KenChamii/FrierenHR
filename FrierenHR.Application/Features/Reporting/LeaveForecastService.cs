using System.Text.Json;
using FrierenHR.Application.Common.DTOs;
using FrierenHR.Application.Common.Interfaces;
using FrierenHR.Core.Entities;
using FrierenHR.Core.Enums;
using FrierenHR.Core.Leave;
using FrierenHR.Core.RulesEngine;
// FrierenHR.Application.Features.Employee is a namespace, so the entity is aliased.
using EmployeeEntity = FrierenHR.Core.Entities.Employee;

namespace FrierenHR.Application.Features.Reporting;

/// <summary>Projects a leave balance forward and says whether any of it will be forfeited.</summary>
public class LeaveForecastService : ILeaveForecastService
{
    /// <summary>How far ahead to project when the caller does not say.</summary>
    private const int DefaultHorizonMonths = 12;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IEmployeeRepository _employees;
    private readonly ILeaveRepository _leave;
    private readonly IRuleConfigRepository _rules;
    private readonly IRuleEvaluator _evaluator;

    public LeaveForecastService(
        IEmployeeRepository employees,
        ILeaveRepository leave,
        IRuleConfigRepository rules,
        IRuleEvaluator evaluator)
    {
        _employees = employees;
        _leave = leave;
        _rules = rules;
        _evaluator = evaluator;
    }

    public async Task<LeaveForecastDto> ForEmployeeAsync(
        Guid employeeId, DateOnly? horizon = null, CancellationToken ct = default)
    {
        var employee = await _employees.GetByIdAsync(employeeId, ct)
            ?? throw new InvalidOperationException($"Employee '{employeeId}' not found.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var end = horizon ?? today.AddMonths(DefaultHorizonMonths);
        var months = LeaveProjector.MonthsBetween(today, end);

        var balances = await _leave.GetBalancesAsync(employeeId, ct);
        var accrualRules = await LoadAccrualRules(employee.CompanyId, ct);

        var projections = balances
            .OrderBy(b => b.LeaveType)
            .Select(balance => Project(employee, balance, accrualRules, today, months))
            .ToList();

        return new LeaveForecastDto(employeeId, end, projections);
    }

    private LeaveProjectionDto Project(
        EmployeeEntity employee,
        LeaveBalance balance,
        List<RuleDefinition> accrualRules,
        DateOnly today,
        int months)
    {
        var rule = FirstMatchingRule(employee, balance, accrualRules);

        // No rule for this leave type is a normal configuration, not a fault: the balance simply stands still.
        if (rule?.Action.Amount is not { } monthlyAccrual || monthlyAccrual <= 0)
        {
            return new LeaveProjectionDto(
                balance.LeaveType, balance.Balance, 0, rule?.Action.Cap, 0,
                balance.Balance, 0, null,
                "No accrual rule applies, so this balance is not projected to change.");
        }

        var projection = LeaveProjector.Project(balance.Balance, monthlyAccrual, rule.Action.Cap, months);

        return new LeaveProjectionDto(
            balance.LeaveType,
            balance.Balance,
            monthlyAccrual,
            projection.Cap,
            projection.MonthsProjected,
            projection.ProjectedBalance,
            projection.Forfeited,
            // The month the cap first bites, expressed as a date the screen can show.
            projection.FirstForfeitingMonth is { } month ? today.AddMonths(month) : null,
            null);
    }

    /// <summary>Rules are evaluated one at a time rather than through EvaluateFirstMatch.</summary>
    private RuleDefinition? FirstMatchingRule(
        EmployeeEntity employee, LeaveBalance balance, List<RuleDefinition> rules)
    {
        var context = new RuleContext()
            .Set("tenureMonths", employee.TenureMonths())
            .Set("currentBalance", balance.Balance)
            .Set("leaveType", balance.LeaveType.ToString());

        foreach (var rule in rules)
        {
            var result = _evaluator.Evaluate(rule, context);
            if (result.Matched && result.Success) return rule;
        }

        return null;
    }

    private async Task<List<RuleDefinition>> LoadAccrualRules(Guid companyId, CancellationToken ct)
    {
        var configs = await _rules.GetActiveRulesAsync(companyId, RuleType.LeaveAccrual, ct);
        return configs
            .OrderBy(c => c.Priority)
            .Select(c => JsonSerializer.Deserialize<RuleDefinition>(c.RuleJson, JsonOptions))
            .Where(r => r is not null)
            .Select(r => r!)
            .ToList();
    }
}
