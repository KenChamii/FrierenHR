using System.Text.Json;
using FrierenHR.Application.Common.DTOs;
using FrierenHR.Application.Common.Interfaces;
using FrierenHR.Core.Entities;
using FrierenHR.Core.Enums;
using FrierenHR.Core.RulesEngine;


namespace FrierenHR.Application.Features.Leave;

public class LeaveService : ILeaveService
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ILeaveRepository _leaveRepository;
    private readonly IRuleConfigRepository _ruleConfigRepository;
    private readonly IRuleEvaluator _ruleEvaluator;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public LeaveService(IEmployeeRepository employeeRepository, ILeaveRepository leaveRepository,
        IRuleConfigRepository ruleConfigRepository, IRuleEvaluator ruleEvaluator)
    {
        _employeeRepository = employeeRepository;
        _leaveRepository = leaveRepository;
        _ruleConfigRepository = ruleConfigRepository;
        _ruleEvaluator = ruleEvaluator;
    }

    public async Task<LeaveRequestDto> RequestLeaveAsync(CreateLeaveRequestDto dto, CancellationToken ct = default)
    {
        var employee = await _employeeRepository.GetByIdAsync(dto.EmployeeId, ct)
            ?? throw new InvalidOperationException($"Employee '{dto.EmployeeId}' not found.");

        var days = (decimal)(dto.EndDate.Date - dto.StartDate.Date).TotalDays + 1;

        // Ask the rules engine — NOT an if/else — whether this needs manual approval for THIS company.
        var rules = await LoadRules(employee.CompanyId, RuleType.LeaveApproval, ct);
        var context = new RuleContext()
            .Set("tenureMonths", employee.TenureMonths())
            .Set("leaveType", dto.LeaveType.ToString())
            .Set("days", days);

        var approvalResult = _ruleEvaluator.EvaluateFirstMatch(rules, RuleType.LeaveApproval.ToString(), context);
        // No configured rule => safe default is "requires approval".
        var requiresApproval = !approvalResult.Matched || approvalResult.ActionType != "AutoApprove";

        var entity = new LeaveRequest
        {
            EmployeeId = dto.EmployeeId,
            LeaveType = dto.LeaveType,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Days = days,
            Reason = dto.Reason,
            RequiresApproval = requiresApproval,
            Status = requiresApproval ? LeaveStatus.Pending : LeaveStatus.Approved
        };

        await _leaveRepository.AddAsync(entity, ct);
        if (!requiresApproval) await ApplyBalanceDeduction(employee.Id, dto.LeaveType, days, ct);
        await _leaveRepository.SaveChangesAsync(ct);
        return ToDto(entity, employee);
    }

    public async Task<LeaveBalanceDto> RunAccrualForEmployeeAsync(Guid employeeId, LeaveType leaveType, CancellationToken ct = default)
    {
        var employee = await _employeeRepository.GetByIdAsync(employeeId, ct)
            ?? throw new InvalidOperationException($"Employee '{employeeId}' not found.");
        var currentBalance = (await _leaveRepository.GetBalanceAsync(employeeId, leaveType, ct))?.Balance ?? 0;

        var rules = await LoadRules(employee.CompanyId, RuleType.LeaveAccrual, ct);
        var context = new RuleContext().Set("tenureMonths", employee.TenureMonths()).Set("currentBalance", currentBalance);
        var result = _ruleEvaluator.EvaluateFirstMatch(rules, RuleType.LeaveAccrual.ToString(), context);

        var newBalance = currentBalance + (result.Matched && result.Success ? result.ResultValue ?? 0 : 0);
        await _leaveRepository.UpsertBalanceAsync(new LeaveBalance { EmployeeId = employeeId, LeaveType = leaveType, Balance = newBalance, LastAccrualDate = DateTime.UtcNow }, ct);
        await _leaveRepository.SaveChangesAsync(ct);
        return new LeaveBalanceDto(leaveType, newBalance, DateTime.UtcNow);
    }

    public async Task<LeaveRequestDto> DecideAsync(Guid leaveRequestId, DecideLeaveRequestDto dto, CancellationToken ct = default)
    {
        var entity = await _leaveRepository.GetByIdAsync(leaveRequestId, ct)
            ?? throw new InvalidOperationException($"Leave request '{leaveRequestId}' not found.");
        if (entity.Status != LeaveStatus.Pending)
            throw new InvalidOperationException($"Leave request is already '{entity.Status}'.");

        entity.Status = dto.Approve ? LeaveStatus.Approved : LeaveStatus.Rejected;
        entity.DecidedAt = DateTime.UtcNow;
        entity.DecidedByEmployeeId = dto.DecidedByEmployeeId;

        if (dto.Approve) await ApplyBalanceDeduction(entity.EmployeeId, entity.LeaveType, entity.Days, ct);

        _leaveRepository.Update(entity);
        await _leaveRepository.SaveChangesAsync(ct);
        var employee = await _employeeRepository.GetByIdAsync(entity.EmployeeId, ct);
        return ToDto(entity, employee);
    }

    public async Task<List<LeaveRequestDto>> GetByEmployeeAsync(Guid employeeId, CancellationToken ct = default)
    {
        var requests = await _leaveRepository.GetByEmployeeAsync(employeeId, ct);
        var employee = await _employeeRepository.GetByIdAsync(employeeId, ct);
        return requests.Select(r => ToDto(r, employee)).ToList();
    }

    public async Task<List<LeaveRequestDto>> GetPendingForApproverAsync(Guid approverEmployeeId, CancellationToken ct = default)
    {
        var requests = await _leaveRepository.GetPendingForApproverAsync(approverEmployeeId, ct);
        return requests.Select(r => ToDto(r, r.Employee)).ToList();
    }

    public async Task<List<LeaveBalanceDto>> GetBalancesAsync(Guid employeeId, CancellationToken ct = default)
    {
        var balances = await _leaveRepository.GetBalancesAsync(employeeId, ct);
        return balances.Select(b => new LeaveBalanceDto(b.LeaveType, b.Balance, b.LastAccrualDate)).ToList();
    }

    private async Task ApplyBalanceDeduction(Guid employeeId, LeaveType leaveType, decimal days, CancellationToken ct)
    {
        var balance = await _leaveRepository.GetBalanceAsync(employeeId, leaveType, ct)
            ?? new LeaveBalance { EmployeeId = employeeId, LeaveType = leaveType, Balance = 0 };
        balance.Balance = Math.Max(0, balance.Balance - days);
        await _leaveRepository.UpsertBalanceAsync(balance, ct);
    }

    private async Task<List<RuleDefinition>> LoadRules(Guid companyId, RuleType ruleType, CancellationToken ct)
    {
        var configs = await _ruleConfigRepository.GetActiveRulesAsync(companyId, ruleType, ct);
        return configs.OrderBy(c => c.Priority)
            .Select(c => JsonSerializer.Deserialize<RuleDefinition>(c.RuleJson, JsonOptions))
            .Where(r => r is not null).Select(r => r!).ToList();
    }

    private static LeaveRequestDto ToDto(LeaveRequest r, FrierenHR.Core.Entities.Employee? employee) => new(r.Id, r.EmployeeId,
     employee is null ? "" : $"{employee.FirstName} {employee.LastName}", r.LeaveType, r.StartDate, r.EndDate,
     r.Days, r.Reason, r.Status, r.RequiresApproval, r.RequestedAt, r.DecidedAt);
}