import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { LeaveService } from '../../../../core/services/leave.service';
import { ApprovalService } from '../../../../core/services/approval.service';
import { AuthService } from '../../../../core/services/auth.service';
import { LeaveRequestDto } from '../../../../core/models/leave.model';

// NOTE: LeaveRequestDto doesn't carry an ApprovalInstance id directly — in a real build, extend
// LeaveRequestDto (or add a dedicated endpoint) to include the current ApprovalInstanceDto so this
// screen can call ApprovalService.advance(instanceId, approve) instead of LeaveService.decide()
// for requests that are on a multi-step chain. Until that endpoint exists, this inbox reuses the
// same pending-for-approver + decide() path from Phase 3, matching the current API surface exactly.
@Component({
  selector: 'app-pending-approvals-inbox',
  standalone: true,
  imports: [CommonModule, TableModule, ButtonModule, TagModule],
  templateUrl: './pending-approvals-inbox.component.html',
})
export class PendingApprovalsInboxComponent implements OnInit {
  readonly requests = signal<LeaveRequestDto[]>([]);
  readonly loading = signal(true);
  readonly decidingId = signal<string | null>(null);

  constructor(
    private leaveService: LeaveService,
    private approvalService: ApprovalService,
    private authService: AuthService,
  ) {}

  ngOnInit(): void { this.load(); }

  load(): void {
    const approverId = this.authService.currentEmployeeId();
    if (!approverId) { this.loading.set(false); return; }
    this.loading.set(true);
    this.leaveService.getPendingForApprover(approverId).subscribe({
      next: (list) => { this.requests.set(list); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  decide(request: LeaveRequestDto, approve: boolean): void {
    const decidedByEmployeeId = this.authService.currentEmployeeId();
    if (!decidedByEmployeeId) return;
    this.decidingId.set(request.id);
    this.leaveService.decide(request.id, { decidedByEmployeeId, approve }).subscribe({
      next: () => { this.decidingId.set(null); this.load(); },
      error: () => this.decidingId.set(null),
    });
  }
}