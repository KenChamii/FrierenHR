using FrierenHR.Application.Common.DTOs;
using FrierenHR.Application.Common.Interfaces;
using FrierenHR.Core.Enums;

namespace FrierenHR.Application.Features.Timesheet;

public class TimesheetService : ITimesheetService
{
    private readonly ITimesheetRepository _timesheetRepository;
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly IEmployeeRepository _employeeRepository;

    public TimesheetService(ITimesheetRepository timesheetRepository, IAttendanceRepository attendanceRepository, IEmployeeRepository employeeRepository)
    {
        _timesheetRepository = timesheetRepository;
        _attendanceRepository = attendanceRepository;
        _employeeRepository = employeeRepository;
    }

    /// <summary>Normalizes any date within a week to that week's Monday.</summary>
    public static DateTime GetWeekStart(DateTime date)
    {
        var d = date.Date;
        var diff = ((int)d.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return d.AddDays(-diff);
    }

    public async Task<TimesheetDto> SubmitAsync(SubmitTimesheetDto dto, CancellationToken ct = default)
    {
        var employee = await _employeeRepository.GetByIdAsync(dto.EmployeeId, ct)
            ?? throw new InvalidOperationException($"Employee '{dto.EmployeeId}' not found.");

        var weekStart = GetWeekStart(dto.WeekStartDate);
        var existing = await _timesheetRepository.GetForWeekAsync(dto.EmployeeId, weekStart, ct);

        if (existing is not null)
        {
            if (existing.Status == TimesheetStatus.Submitted)
                throw new InvalidOperationException("This week has already been submitted and is awaiting approval.");
            if (existing.Status == TimesheetStatus.Approved)
                throw new InvalidOperationException("This week has already been approved.");

            // Draft or Rejected -> resubmitting clears the previous decision.
            existing.Status = TimesheetStatus.Submitted;
            existing.SubmittedAt = DateTime.UtcNow;
            existing.DecidedAt = null;
            existing.DecidedByEmployeeId = null;
            existing.Comment = null;
            _timesheetRepository.Update(existing);
            await _timesheetRepository.SaveChangesAsync(ct);
            return await ToDtoAsync(existing, employee, ct);
        }

        var timesheet = new Core.Entities.Timesheet
        {
            EmployeeId = dto.EmployeeId,
            WeekStartDate = weekStart,
            Status = TimesheetStatus.Submitted,
            SubmittedAt = DateTime.UtcNow,
        };
        await _timesheetRepository.AddAsync(timesheet, ct);
        await _timesheetRepository.SaveChangesAsync(ct);
        return await ToDtoAsync(timesheet, employee, ct);
    }

    public async Task<TimesheetDto> DecideAsync(Guid id, DecideTimesheetDto dto, CancellationToken ct = default)
    {
        var timesheet = await _timesheetRepository.GetByIdAsync(id, ct)
            ?? throw new InvalidOperationException($"Timesheet '{id}' not found.");
        if (timesheet.Status != TimesheetStatus.Submitted)
            throw new InvalidOperationException("Only a submitted timesheet can be approved or rejected.");

        timesheet.Status = dto.Approve ? TimesheetStatus.Approved : TimesheetStatus.Rejected;
        timesheet.DecidedAt = DateTime.UtcNow;
        timesheet.DecidedByEmployeeId = dto.DecidedByEmployeeId;
        timesheet.Comment = dto.Comment;
        _timesheetRepository.Update(timesheet);
        await _timesheetRepository.SaveChangesAsync(ct);

        var employee = await _employeeRepository.GetByIdAsync(timesheet.EmployeeId, ct);
        return await ToDtoAsync(timesheet, employee, ct);
    }

    public async Task<TimesheetDto?> GetForWeekAsync(Guid employeeId, DateTime weekStartDate, CancellationToken ct = default)
    {
        var timesheet = await _timesheetRepository.GetForWeekAsync(employeeId, GetWeekStart(weekStartDate), ct);
        if (timesheet is null) return null;
        var employee = await _employeeRepository.GetByIdAsync(employeeId, ct);
        return await ToDtoAsync(timesheet, employee, ct);
    }

    public async Task<List<TimesheetDto>> GetByEmployeeAsync(Guid employeeId, CancellationToken ct = default)
    {
        var employee = await _employeeRepository.GetByIdAsync(employeeId, ct);
        var timesheets = await _timesheetRepository.GetByEmployeeAsync(employeeId, ct);
        var result = new List<TimesheetDto>();
        foreach (var t in timesheets) result.Add(await ToDtoAsync(t, employee, ct));
        return result;
    }

    public async Task<List<TimesheetDto>> GetPendingForApproverAsync(Guid approverId, CancellationToken ct = default)
    {
        var approver = await _employeeRepository.GetByIdAsync(approverId, ct)
            ?? throw new InvalidOperationException($"Employee '{approverId}' not found.");

        // HRAdmin sees every pending submission company-wide; a Manager only sees their direct reports'.
        var pending = approver.Role == EmployeeRole.HRAdmin
            ? await _timesheetRepository.GetAllPendingAsync(ct)
            : await _timesheetRepository.GetPendingForManagerAsync(approverId, ct);

        var result = new List<TimesheetDto>();
        foreach (var t in pending)
        {
            var employee = await _employeeRepository.GetByIdAsync(t.EmployeeId, ct);
            result.Add(await ToDtoAsync(t, employee, ct));
        }
        return result;
    }

    private async Task<TimesheetDto> ToDtoAsync(Core.Entities.Timesheet t, Core.Entities.Employee? employee, CancellationToken ct)
    {
        var weekEnd = t.WeekStartDate.AddDays(7);
        var logs = await _attendanceRepository.GetByEmployeeAsync(t.EmployeeId, t.WeekStartDate, weekEnd, ct);
        var totalHours = logs.Where(l => l.TimeOut is not null)
            .Sum(l => (decimal)(l.TimeOut!.Value - l.TimeIn).TotalHours - l.BreakMinutes / 60m);
        totalHours = Math.Max(0, totalHours);

        string? decidedByName = null;
        if (t.DecidedByEmployeeId is not null)
        {
            var decider = await _employeeRepository.GetByIdAsync(t.DecidedByEmployeeId.Value, ct);
            if (decider is not null) decidedByName = $"{decider.FirstName} {decider.LastName}";
        }

        return new TimesheetDto(
            t.Id, t.EmployeeId, employee is null ? "" : $"{employee.FirstName} {employee.LastName}",
            t.WeekStartDate, t.Status, totalHours, t.SubmittedAt, t.DecidedAt, decidedByName, t.Comment);
    }
}
