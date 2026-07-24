using FrierenHR.Application.Common.DTOs;

namespace FrierenHR.Application.Features.Timesheet;

public interface ITimesheetService
{
    Task<TimesheetDto> SubmitAsync(SubmitTimesheetDto dto, CancellationToken ct = default);
    Task<TimesheetDto> DecideAsync(Guid id, DecideTimesheetDto dto, CancellationToken ct = default);
    Task<TimesheetDto?> GetForWeekAsync(Guid employeeId, DateTime weekStartDate, CancellationToken ct = default);
    Task<List<TimesheetDto>> GetByEmployeeAsync(Guid employeeId, CancellationToken ct = default);
    Task<List<TimesheetDto>> GetPendingForApproverAsync(Guid approverId, CancellationToken ct = default);
}
