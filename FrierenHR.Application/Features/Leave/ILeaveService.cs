using FrierenHR.Core.Enums;
using FrierenHR.Application.Common.DTOs;

namespace FrierenHR.Application.Features.Leave;

public interface ILeaveService
{
    Task<LeaveRequestDto> RequestLeaveAsync(CreateLeaveRequestDto dto, CancellationToken ct = default);
    Task<LeaveRequestDto> DecideAsync(Guid leaveRequestId, DecideLeaveRequestDto dto, CancellationToken ct = default);
    Task<List<LeaveRequestDto>> GetByEmployeeAsync(Guid employeeId, CancellationToken ct = default);
    Task<List<LeaveRequestDto>> GetPendingForApproverAsync(Guid approverEmployeeId, CancellationToken ct = default);
    Task<List<LeaveBalanceDto>> GetBalancesAsync(Guid employeeId, CancellationToken ct = default);
    Task<LeaveBalanceDto> RunAccrualForEmployeeAsync(Guid employeeId, LeaveType leaveType, CancellationToken ct = default);
}