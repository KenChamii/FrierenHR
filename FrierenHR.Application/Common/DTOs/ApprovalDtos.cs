using FrierenHR.Core.Enums;

namespace FrierenHR.Application.Common.DTOs;

public record ApprovalChainStepDto(int StepOrder, EmployeeRole ApproverRole, int EscalateAfterDays);
public record ApprovalChainDto(Guid Id, Guid CompanyId, string Name, List<ApprovalChainStepDto> Steps);
public record CreateApprovalChainDto(Guid CompanyId, string Name, List<ApprovalChainStepDto> Steps);
public record ApprovalInstanceDto(Guid Id, Guid LeaveRequestId, int CurrentStepOrder, ApprovalStatus Status, EmployeeRole CurrentApproverRole);