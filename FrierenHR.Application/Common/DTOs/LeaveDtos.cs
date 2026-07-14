using FrierenHR.Core.Enums;

namespace FrierenHR.Application.Common.DTOs;

public record LeaveRequestDto(Guid Id, Guid EmployeeId, string EmployeeName, LeaveType LeaveType,
    DateTime StartDate, DateTime EndDate, decimal Days, string? Reason, LeaveStatus Status,
    bool RequiresApproval, DateTime RequestedAt, DateTime? DecidedAt);
public record CreateLeaveRequestDto(Guid EmployeeId, LeaveType LeaveType, DateTime StartDate, DateTime EndDate, string? Reason);
public record DecideLeaveRequestDto(Guid DecidedByEmployeeId, bool Approve, string? Comment);
public record LeaveBalanceDto(LeaveType LeaveType, decimal Balance, DateTime? LastAccrualDate);