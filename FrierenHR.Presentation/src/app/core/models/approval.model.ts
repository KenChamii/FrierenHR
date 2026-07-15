import { EmployeeRole, ApprovalStatus } from './enums.model';

export interface ApprovalChainStepDto { stepOrder: number; approverRole: EmployeeRole; escalateAfterDays: number; }
export interface ApprovalChainDto { id: string; companyId: string; name: string; steps: ApprovalChainStepDto[]; }
export interface CreateApprovalChainDto { companyId: string; name: string; steps: ApprovalChainStepDto[]; }
export interface ApprovalInstanceDto {
  id: string; leaveRequestId: string; currentStepOrder: number; status: ApprovalStatus; currentApproverRole: EmployeeRole;
}