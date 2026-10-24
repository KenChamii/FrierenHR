using FrierenHR.Application.Common.DTOs;
using FrierenHR.Application.Common.Interfaces;
using FrierenHR.Application.Features.Scheduling;
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
    private readonly ITimesheetRepository _timesheetRepository;
    private readonly IShiftService _shiftService;
    private readonly IWorkingCalendarProvider _workingCalendar;
    private readonly IUnitOfWork _unitOfWork;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>Days a single request may cover. One week, with a little room either side.</summary>
    private const int MaxDaysPerWeekRequest = 8;

    public AttendanceService(IAttendanceRepository attendanceRepository, IEmployeeRepository employeeRepository,
        IRuleConfigRepository ruleConfigRepository, IRuleEvaluator ruleEvaluator, ITimesheetRepository timesheetRepository,
        IShiftService shiftService, IWorkingCalendarProvider workingCalendar, IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
        _attendanceRepository = attendanceRepository;
        _employeeRepository = employeeRepository;
        _ruleConfigRepository = ruleConfigRepository;
        _ruleEvaluator = ruleEvaluator;
        _timesheetRepository = timesheetRepository;
        _shiftService = shiftService;
        _workingCalendar = workingCalendar;
    }

    /// <summary>Minutes past the shift's expected start.</summary>
    private static int MinutesLate(Shift? shift, DateTime clockIn)
    {
        if (shift is null) return 0;
        var actual = TimeOnly.FromDateTime(clockIn);
        var late = (actual - shift.StartTime).TotalMinutes;
        return late <= 0 ? 0 : (int)late;
    }

    /// <summary>Normalizes any date within a week to that week's Monday (mirrors TimesheetService.GetWeekStart).</summary>
    private static DateTime GetWeekStart(DateTime date)
    {
        var d = date.Date;
        var diff = ((int)d.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return d.AddDays(-diff);
    }

    /// <summary>Blocks attendance changes for a week whose timesheet has been submitted or approved.</summary>
    private async Task EnsureWeekUnlockedAsync(Guid employeeId, DateTime date, CancellationToken ct)
    {
        var weekStart = GetWeekStart(date);
        var timesheet = await _timesheetRepository.GetForWeekAsync(employeeId, weekStart, ct);
        if (timesheet is { Status: TimesheetStatus.Submitted or TimesheetStatus.Approved })
        {
            var verb = timesheet.Status == TimesheetStatus.Submitted ? "submitted" : "approved";
            throw new InvalidOperationException($"The week of {weekStart:MMM d} has already been {verb} — attendance for that week is locked. Ask your manager/HR to reject it first if a correction is needed.");
        }
    }

    public async Task<AttendanceLogDto> ClockInAsync(ClockInDto dto, CancellationToken ct = default)
    {
        var existingOpen = await _attendanceRepository.GetOpenLogAsync(dto.EmployeeId, ct);
        if (existingOpen is not null)
            throw new InvalidOperationException("Employee already has an open clock-in. Clock out first.");

        // Prevent the two time-tracking flows from colliding: if today already has a manually
        // logged shift, punching in on top of it would just create a second, conflicting entry
        // for the same day and make the attendance history unreadable. Delete the manual entry
        // first if you actually want to switch to punch-clock for today.
        var todayStart = DateTime.UtcNow.Date;
        var hasManualToday = (await _attendanceRepository.GetByEmployeeAsync(dto.EmployeeId, todayStart, todayStart.AddDays(1), ct))
            .Any(l => l.Source == "Manual");
        if (hasManualToday)
            throw new InvalidOperationException("You already logged a manual shift for today. Delete it first if you want to use the punch clock instead.");

        await EnsureWeekUnlockedAsync(dto.EmployeeId, todayStart, ct);

        var employee = await _employeeRepository.GetByIdAsync(dto.EmployeeId, ct)
            ?? throw new InvalidOperationException($"Employee '{dto.EmployeeId}' not found.");

        // Lateness is measured against the employee's shift rather than inferred from the hour alone.
        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);
        var shift = await _shiftService.ResolveForEmployeeAsync(employee.Id, ct);
        var calendar = await _workingCalendar.ForEmployeeAsync(employee.Id, today, today, ct);

        var graceRules = await LoadRules(employee.CompanyId, RuleType.LateGracePeriod, ct);
        var graceContext = new RuleContext()
            .Set("clockInHour", now.Hour)
            .Set("minutesLate", MinutesLate(shift, now))
            .Set("isHoliday", calendar.IsHoliday(today))
            .Set("holidayType", calendar.HolidayTypeOn(today)?.ToString() ?? "None");

        var graceResult = _ruleEvaluator.EvaluateFirstMatch(graceRules, RuleType.LateGracePeriod.ToString(), graceContext);
        // graceResult.ResultValue is the grace period in minutes.

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

    // Remote-friendly alternative to punch clocking: log a shift as a start time, end time.
    public async Task<AttendanceLogDto> LogShiftAsync(LogShiftDto dto, CancellationToken ct = default)
    {
        if (dto.BreakMinutes < 0 || dto.BreakMinutes > 12 * 60)
            throw new InvalidOperationException("Break minutes must be between 0 and 720.");

        var employee = await _employeeRepository.GetByIdAsync(dto.EmployeeId, ct)
            ?? throw new InvalidOperationException($"Employee '{dto.EmployeeId}' not found.");

        var dayStart = dto.Date.Date;
        var dayEnd = dayStart.AddDays(1);

        // Mirror image of the check in ClockInAsync.
        var punchLogsToday = (await _attendanceRepository.GetByEmployeeAsync(dto.EmployeeId, dayStart, dayEnd, ct))
            .Where(l => l.Source != "Manual").ToList();
        if (punchLogsToday.Count > 0)
            throw new InvalidOperationException("You already have a punch-clock entry for today. Clock out and delete it first if you want to log this shift manually instead.");

        await EnsureWeekUnlockedAsync(dto.EmployeeId, dayStart, ct);

        var timeIn = dayStart + dto.StartTime;
        var timeOut = dayStart + dto.EndTime;
        if (timeOut <= timeIn) timeOut = timeOut.AddDays(1); // overnight shift

        var workedMinutes = (timeOut - timeIn).TotalMinutes - dto.BreakMinutes;
        if (workedMinutes <= 0)
            throw new InvalidOperationException("Break time can't be longer than the shift itself.");

        var existing = (await _attendanceRepository.GetByEmployeeAsync(dto.EmployeeId, dayStart, dayEnd, ct))
            .FirstOrDefault(l => l.Source == "Manual");

        if (existing is not null)
        {
            existing.TimeIn = timeIn;
            existing.TimeOut = timeOut;
            existing.BreakMinutes = dto.BreakMinutes;
            _attendanceRepository.Update(existing);
            await _attendanceRepository.SaveChangesAsync(ct);
            return ToDto(existing, employee);
        }

        var log = new AttendanceLog { EmployeeId = dto.EmployeeId, TimeIn = timeIn, TimeOut = timeOut, BreakMinutes = dto.BreakMinutes, Source = "Manual" };
        await _attendanceRepository.AddAsync(log, ct);
        await _attendanceRepository.SaveChangesAsync(ct);
        return ToDto(log, employee);
    }

    public async Task<List<AttendanceLogDto>> LogWeekAsync(LogWeekDto dto, CancellationToken ct = default)
    {
        if (dto.Days is null || dto.Days.Count == 0)
            throw new InvalidOperationException("There is nothing to save.");

        if (dto.Days.Count > MaxDaysPerWeekRequest)
            throw new InvalidOperationException($"A single save covers at most {MaxDaysPerWeekRequest} days.");

        // Caught here rather than by the last write silently overwriting the first.
        var duplicate = dto.Days.GroupBy(d => d.Date.Date).FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
            throw new InvalidOperationException($"{duplicate.Key:MMM d} appears more than once.");

        // One transaction, so a week is never left half saved.
        return await _unitOfWork.ExecuteAsync(async token =>
        {
            var saved = new List<AttendanceLogDto>(dto.Days.Count);
            foreach (var day in dto.Days.OrderBy(d => d.Date))
            {
                saved.Add(await LogShiftAsync(
                    new LogShiftDto(dto.EmployeeId, day.Date, day.StartTime, day.EndTime, day.BreakMinutes),
                    token));
            }
            return saved;
        }, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var log = await _attendanceRepository.GetByIdAsync(id, ct)
            ?? throw new InvalidOperationException($"Attendance log '{id}' not found.");
        await EnsureWeekUnlockedAsync(log.EmployeeId, log.TimeIn, ct);
        _attendanceRepository.Remove(log);
        await _attendanceRepository.SaveChangesAsync(ct);
    }

    public async Task<AttendanceLogDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var log = await _attendanceRepository.GetByIdAsync(id, ct);
        if (log is null) return null;
        var employee = await _employeeRepository.GetByIdAsync(log.EmployeeId, ct);
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

        // Holiday facts so a company can configure a premium for overtime worked on one, without any change to the engine.
        var day = DateOnly.FromDateTime(date);
        var calendar = await _workingCalendar.ForEmployeeAsync(employeeId, day, day, ct);

        var rules = await LoadRules(employee.CompanyId, RuleType.OTMultiplier, ct);
        var context = new RuleContext()
            .Set("otHours", otHours)
            .Set("isHoliday", calendar.IsHoliday(day))
            .Set("holidayType", calendar.HolidayTypeOn(day)?.ToString() ?? "None");
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
        decimal? hours = l.TimeOut is not null
            ? (decimal)(l.TimeOut.Value - l.TimeIn).TotalHours - (l.BreakMinutes / 60m)
            : null;
        return new AttendanceLogDto(l.Id, l.EmployeeId, employee is null ? "" : $"{employee.FirstName} {employee.LastName}", l.TimeIn, l.TimeOut, l.BreakMinutes, l.Source, hours);
    }
}