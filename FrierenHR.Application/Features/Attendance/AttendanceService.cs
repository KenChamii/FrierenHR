using FrierenHR.Application.Common.DTOs;
using FrierenHR.Application.Common.Interfaces;
using FrierenHR.Core.Entities;
using FrierenHR.Core.Enums;
using FrierenHR.Core.RulesEngine;
using System.Text.Json;

namespace FrierenHR.Application.Features.Attendance;

public class AttendanceService : IAttendanceService
{
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IRuleConfigRepository _ruleConfigRepository;
    private readonly IRuleEvaluator _ruleEvaluator;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public AttendanceService(IAttendanceRepository attendanceRepository, IEmployeeRepository employeeRepository,
        IRuleConfigRepository ruleConfigRepository, IRuleEvaluator ruleEvaluator)
    {
        _attendanceRepository = attendanceRepository;
        _employeeRepository = employeeRepository;
        _ruleConfigRepository = ruleConfigRepository;
        _ruleEvaluator = ruleEvaluator;
    }

    public async Task<AttendanceLogDto> ClockInAsync(ClockInDto dto, CancellationToken ct = default)
    {
        var existingOpen = await _attendanceRepository.GetOpenLogAsync(dto.EmployeeId, ct);
        if (existingOpen is not null)
            throw new InvalidOperationException("Employee already has an open clock-in. Clock out first.");

        var employee = await _employeeRepository.GetByIdAsync(dto.EmployeeId, ct)
            ?? throw new InvalidOperationException($"Employee '{dto.EmployeeId}' not found.");

        // Late/grace period check — driven by RuleType.LateGracePeriod, not hardcoded shift times.
        var graceRules = await LoadRules(employee.CompanyId, RuleType.LateGracePeriod, ct);
        var graceContext = new RuleContext().Set("clockInHour", DateTime.UtcNow.Hour);
        var graceResult = _ruleEvaluator.EvaluateFirstMatch(graceRules, RuleType.LateGracePeriod.ToString(), graceContext);
        // graceResult.ResultValue (minutes) is available here if you want to flag/store lateness explicitly.

        var log = new AttendanceLog { EmployeeId = dto.EmployeeId, TimeIn = DateTime.UtcNow, Source = dto.Source };
        await _attendanceRepository.AddAsync(log, ct);
        await _attendanceRepository.SaveChangesAsync(ct);
        return ToDto(log, employee);
    }

    public async Task<AttendanceLogDto> ClockOutAsync(ClockOutDto dto, CancellationToken ct = default)
    {
        var log = await _attendanceRepository.GetOpenLogAsync(dto.EmployeeId, ct)
            ?? throw new InvalidOperationException("No open clock-in found for this employee.");

        log.TimeOut = DateTime.UtcNow;
        _attendanceRepository.Update(log);
        await _attendanceRepository.SaveChangesAsync(ct);

        var employee = await _employeeRepository.GetByIdAsync(dto.EmployeeId, ct);
        return ToDto(log, employee);
    }

    public async Task<List<AttendanceLogDto>> GetByEmployeeAsync(Guid employeeId, DateTime? from = null, DateTime? to = null, CancellationToken ct = default)
    {
        var logs = await _attendanceRepository.GetByEmployeeAsync(employeeId, from, to, ct);
        var employee = await _employeeRepository.GetByIdAsync(employeeId, ct);
        return logs.Select(l => ToDto(l, employee)).ToList();
    }

    public async Task<OtComputationResultDto> ComputeOtAsync(Guid employeeId, DateTime date, CancellationToken ct = default)
    {
        var employee = await _employeeRepository.GetByIdAsync(employeeId, ct)
            ?? throw new InvalidOperationException($"Employee '{employeeId}' not found.");

        var logs = await _attendanceRepository.GetByEmployeeAsync(employeeId, date.Date, date.Date.AddDays(1), ct);
        var totalHours = logs.Where(l => l.TimeOut is not null)
            .Sum(l => (decimal)(l.TimeOut!.Value - l.TimeIn).TotalHours);

        const decimal standardHours = 8m; // MVP: fixed standard shift; make this an AppConfig/rule if you want it configurable too
        var otHours = Math.Max(0, totalHours - standardHours);

        var rules = await LoadRules(employee.CompanyId, RuleType.OTMultiplier, ct);
        var context = new RuleContext().Set("otHours", otHours);
        var result = _ruleEvaluator.EvaluateFirstMatch(rules, RuleType.OTMultiplier.ToString(), context);

        return new OtComputationResultDto(result.Matched, otHours, result.Matched ? result.ResultValue ?? 0 : 0, result.Message);
    }

    private async Task<List<RuleDefinition>> LoadRules(Guid companyId, RuleType ruleType, CancellationToken ct)
    {
        var configs = await _ruleConfigRepository.GetActiveRulesAsync(companyId, ruleType, ct);
        return configs.OrderBy(c => c.Priority)
            .Select(c => JsonSerializer.Deserialize<RuleDefinition>(c.RuleJson, JsonOptions))
            .Where(r => r is not null).Select(r => r!).ToList();
    }

    private static AttendanceLogDto ToDto(AttendanceLog l, FrierenHR.Core.Entities.Employee? employee)
    {
        decimal? hours = l.TimeOut is not null ? (decimal)(l.TimeOut.Value - l.TimeIn).TotalHours : null;
        return new AttendanceLogDto(l.Id, l.EmployeeId, employee is null ? "" : $"{employee.FirstName} {employee.LastName}", l.TimeIn, l.TimeOut, l.Source, hours);
    }
}