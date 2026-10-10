using FrierenHR.Application.Common.DTOs;
using FrierenHR.Application.Common.Interfaces;
using FrierenHR.Core.Entities;
using FrierenHR.Core.Enums;

// FrierenHR.Application.Features.Employee is a namespace, so the bare name Employee resolves
// to it rather than to the entity. The alias says which one is meant without repeating the
// full path at every use.
using EmployeeEntity = FrierenHR.Core.Entities.Employee;

namespace FrierenHR.Application.Features.Attendance.Corrections;

/// <summary>
/// A missed clock-in used to mean an administrator editing the attendance row by hand, which
/// left no record of who changed it or why. A correction is a request with a reason, decided
/// by the employee's manager, and the approval writes the row.
/// </summary>
public class AttendanceCorrectionService : IAttendanceCorrectionService
{
    private const string CorrectionSource = "Correction";

    private readonly IAttendanceCorrectionRepository _corrections;
    private readonly IAttendanceRepository _attendance;
    private readonly IEmployeeRepository _employees;

    public AttendanceCorrectionService(
        IAttendanceCorrectionRepository corrections,
        IAttendanceRepository attendance,
        IEmployeeRepository employees)
    {
        _corrections = corrections;
        _attendance = attendance;
        _employees = employees;
    }

    public async Task<AttendanceCorrectionDto> RequestAsync(
        CreateAttendanceCorrectionDto dto, CancellationToken ct = default)
    {
        var employee = await _employees.GetByIdAsync(dto.EmployeeId, ct)
            ?? throw new InvalidOperationException($"Employee '{dto.EmployeeId}' not found.");

        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new InvalidOperationException("Say why the correction is needed. An unexplained change cannot be reviewed.");

        if (dto.Date > DateOnly.FromDateTime(DateTime.UtcNow))
            throw new InvalidOperationException("A correction cannot be requested for a future date.");

        if (dto.RequestedBreakMinutes < 0 || dto.RequestedBreakMinutes > 12 * 60)
            throw new InvalidOperationException("Break minutes must be between 0 and 720.");

        var worked = WorkedMinutes(dto.RequestedTimeIn, dto.RequestedTimeOut) - dto.RequestedBreakMinutes;
        if (worked <= 0)
            throw new InvalidOperationException("The break cannot be as long as the shift being claimed.");

        // One open request per day. Without this, two requests for the same date would both
        // be approved and the second would silently overwrite the first.
        var existing = await _corrections.GetPendingForDateAsync(dto.EmployeeId, dto.Date, ct);
        if (existing is not null)
            throw new InvalidOperationException($"A correction for {dto.Date:d MMM yyyy} is already awaiting a decision.");

        var entity = new AttendanceCorrection
        {
            EmployeeId = dto.EmployeeId,
            Date = dto.Date,
            RequestedTimeIn = dto.RequestedTimeIn,
            RequestedTimeOut = dto.RequestedTimeOut,
            RequestedBreakMinutes = dto.RequestedBreakMinutes,
            Reason = dto.Reason.Trim(),
            Status = ApprovalStatus.Pending
        };

        await _corrections.AddAsync(entity, ct);
        await _corrections.SaveChangesAsync(ct);
        return ToDto(entity, employee, decidedBy: null);
    }

    public async Task<AttendanceCorrectionDto> DecideAsync(
        Guid correctionId, DecideAttendanceCorrectionDto dto, CancellationToken ct = default)
    {
        var correction = await _corrections.GetByIdAsync(correctionId, ct)
            ?? throw new InvalidOperationException($"Correction '{correctionId}' not found.");

        if (correction.Status != ApprovalStatus.Pending)
            throw new InvalidOperationException($"This correction is already '{correction.Status}'.");

        var approver = await _employees.GetByIdAsync(dto.DecidedByEmployeeId, ct)
            ?? throw new InvalidOperationException($"Employee '{dto.DecidedByEmployeeId}' not found.");

        if (correction.EmployeeId == dto.DecidedByEmployeeId)
            throw new InvalidOperationException("You cannot decide your own correction request.");

        correction.Status = dto.Approve ? ApprovalStatus.Approved : ApprovalStatus.Rejected;
        correction.DecidedByEmployeeId = dto.DecidedByEmployeeId;
        correction.DecidedAt = DateTime.UtcNow;
        correction.DecisionComment = dto.Comment?.Trim();
        correction.UpdatedAt = DateTime.UtcNow;

        if (dto.Approve) await ApplyToAttendanceAsync(correction, ct);

        _corrections.Update(correction);
        await _corrections.SaveChangesAsync(ct);

        var employee = await _employees.GetByIdAsync(correction.EmployeeId, ct);
        return ToDto(correction, employee, approver);
    }

    /// <summary>
    /// Amends the day's existing row rather than adding a second one, because two rows for
    /// one day would double the hours on the timesheet.
    /// </summary>
    private async Task ApplyToAttendanceAsync(AttendanceCorrection correction, CancellationToken ct)
    {
        var dayStart = correction.Date.ToDateTime(TimeOnly.MinValue);
        var timeIn = correction.Date.ToDateTime(correction.RequestedTimeIn);
        var timeOut = correction.Date.ToDateTime(correction.RequestedTimeOut);
        if (timeOut <= timeIn) timeOut = timeOut.AddDays(1); // shift crossed midnight

        var existing = (await _attendance.GetByEmployeeAsync(correction.EmployeeId, dayStart, dayStart.AddDays(1), ct))
            .FirstOrDefault();

        if (existing is not null)
        {
            existing.TimeIn = timeIn;
            existing.TimeOut = timeOut;
            existing.BreakMinutes = correction.RequestedBreakMinutes;
            existing.Source = CorrectionSource;
            existing.UpdatedAt = DateTime.UtcNow;
            _attendance.Update(existing);
            correction.AttendanceLogId = existing.Id;
            return;
        }

        var log = new AttendanceLog
        {
            EmployeeId = correction.EmployeeId,
            TimeIn = timeIn,
            TimeOut = timeOut,
            BreakMinutes = correction.RequestedBreakMinutes,
            Source = CorrectionSource
        };
        await _attendance.AddAsync(log, ct);
        correction.AttendanceLogId = log.Id;
    }

    public async Task<List<AttendanceCorrectionDto>> GetByEmployeeAsync(
        Guid employeeId, CancellationToken ct = default)
    {
        var employee = await _employees.GetByIdAsync(employeeId, ct);
        var corrections = await _corrections.GetByEmployeeAsync(employeeId, ct);
        return corrections.Select(c => ToDto(c, employee, decidedBy: null)).ToList();
    }

    /// <summary>HR sees the whole company, a manager only their own reports.</summary>
    public async Task<List<AttendanceCorrectionDto>> GetPendingForApproverAsync(
        Guid approverEmployeeId, CancellationToken ct = default)
    {
        var approver = await _employees.GetByIdAsync(approverEmployeeId, ct)
            ?? throw new InvalidOperationException($"Employee '{approverEmployeeId}' not found.");

        var pending = await _corrections.GetPendingForApproverAsync(
            approverEmployeeId, approver.Role == EmployeeRole.HRAdmin, ct);

        return pending.Select(c => ToDto(c, c.Employee, decidedBy: null)).ToList();
    }

    private static int WorkedMinutes(TimeOnly from, TimeOnly to) =>
        to > from
            ? (int)(to - from).TotalMinutes
            : (int)(TimeSpan.FromHours(24) - (from - to)).TotalMinutes;

    private static AttendanceCorrectionDto ToDto(AttendanceCorrection c, EmployeeEntity? employee, EmployeeEntity? decidedBy) =>
        new(c.Id, c.EmployeeId, Name(employee), c.Date,
            c.RequestedTimeIn, c.RequestedTimeOut, c.RequestedBreakMinutes,
            c.Reason, c.Status, c.CreatedAt,
            decidedBy is null ? null : Name(decidedBy), c.DecidedAt, c.DecisionComment);

    private static string Name(EmployeeEntity? e) => e is null ? string.Empty : $"{e.FirstName} {e.LastName}";
}
