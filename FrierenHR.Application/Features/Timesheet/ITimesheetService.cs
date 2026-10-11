using FrierenHR.Application.Common.DTOs;

namespace FrierenHR.Application.Features.Timesheet;

public interface ITimesheetService
{
    Task<TimesheetDto> SubmitAsync(SubmitTimesheetDto dto, CancellationToken ct = default);
    Task<TimesheetDto> DecideAsync(Guid id, DecideTimesheetDto dto, CancellationToken ct = default);

    /// <summary>
    /// Returns a timesheet to draft so its week can be edited again, and the week unlocks
    /// with it. Resubmitting starts the approval over, so nothing slips past an approver by
    /// being changed after they saw it.
    ///
    /// An employee may recall their own while it is still awaiting a decision. Reopening one
    /// that has already been approved is HR's to do, since somebody has acted on it.
    /// </summary>
    Task<TimesheetDto> ReopenAsync(
        Guid id, Guid requestedByEmployeeId, bool requesterIsHrAdmin, CancellationToken ct = default);
    Task<TimesheetDto?> GetForWeekAsync(Guid employeeId, DateTime weekStartDate, CancellationToken ct = default);
    Task<List<TimesheetDto>> GetByEmployeeAsync(Guid employeeId, CancellationToken ct = default);
    Task<List<TimesheetDto>> GetPendingForApproverAsync(Guid approverId, CancellationToken ct = default);
}
